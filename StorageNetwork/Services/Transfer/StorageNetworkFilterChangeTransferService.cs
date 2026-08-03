using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFilterChangeTransferService
    {
        [System.ThreadStatic]
        private static FilterChangeWorkspace threadWorkspace;

        public static void MoveRejectedItemsToNetwork(TreeFilterable filterable)
        {
            Storage source = filterable != null ? filterable.GetFilterStorage() : null;
            if (source == null ||
                source.items == null ||
                !StorageNetworkMembership.IsCollectableStorage(source) ||
                !StorageNetworkStorageRules.IsServerStorage(source) ||
                StorageNetworkStorageRules.IsNetworkPortStorage(source) ||
                StorageNetworkStorageRules.IsProductionStorage(source))
            {
                return;
            }

            FilterChangeWorkspace workspace = threadWorkspace ??=
                new FilterChangeWorkspace();
            List<GameObject> rejectedItems = workspace.RejectedItems;
            rejectedItems.Clear();
            HashSet<Tag> acceptedTags = workspace.AcceptedTags;
            FillAcceptedTags(filterable, acceptedTags);
            foreach (GameObject item in source.items)
            {
                if (item != null && !IsItemAcceptedByFilter(item, acceptedTags))
                {
                    rejectedItems.Add(item);
                }
            }

            if (rejectedItems.Count == 0)
            {
                return;
            }

            int sourceWorldId = StorageTargetSelector.GetObjectWorldId(source.gameObject);
            foreach (GameObject item in rejectedItems)
            {
                if (item == null || !source.items.Contains(item))
                {
                    continue;
                }

                StorageItemUtility.StorageMatchTags matchTags =
                    StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
                Storage target = FindAcceptingServer(source, item, matchTags, sourceWorldId);
                if (target == null)
                {
                    continue;
                }

                source.Transfer(item, target, block_events: false, hide_popups: true);
            }

            rejectedItems.Clear();
            acceptedTags.Clear();
        }

        private static Storage FindAcceptingServer(
            Storage source,
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            int sourceWorldId)
        {
            StorageSceneLightweightSnapshot snapshot = StorageSceneCollector.CollectLightweightForWorld(sourceWorldId);
            if (snapshot?.Storages == null)
            {
                return null;
            }

            Storage best = null;
            float bestRemaining = 0f;
            foreach (Storage target in snapshot.Storages)
            {
                if (!IsAcceptingServer(source, target, item, matchTags, sourceWorldId))
                {
                    continue;
                }

                float remaining = target.RemainingCapacity();
                if (best == null || remaining > bestRemaining)
                {
                    best = target;
                    bestRemaining = remaining;
                }
            }

            return best;
        }

        private static bool IsAcceptingServer(
            Storage source,
            Storage target,
            GameObject item,
            StorageItemUtility.StorageMatchTags matchTags,
            int sourceWorldId)
        {
            TreeFilterable targetFilter =
                StorageNetworkRuntimeCatalog.TryGet(target, out StorageRuntimeDescriptor descriptor)
                    ? descriptor.TreeFilterable
                    : null;
            return StorageSceneRegistry.IsLive(target) &&
                   target != source &&
                   StorageNetworkStorageRules.IsNetworkStorageTarget(target, source) &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   target.RemainingCapacity() >= StorageItemUtility.GetMass(item) &&
                   IsAcceptedByStorageFilters(target.storageFilters, matchTags) &&
                   IsAcceptedByTreeFilter(targetFilter, item);
        }

        private static bool IsStorageReachableFromWorld(Storage storage, int worldId)
        {
            if (!StorageSceneRegistry.IsLive(storage))
            {
                return false;
            }

            if (worldId < 0 || StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return true;
            }

            return StorageTargetSelector.GetObjectWorldId(storage.gameObject) == worldId;
        }

        private static bool IsItemAcceptedByFilter(
            GameObject item,
            HashSet<Tag> acceptedTags)
        {
            return acceptedTags.Count > 0 &&
                   IsAnyMatchTagAccepted(
                       acceptedTags,
                       StorageItemUtility.GetStorageMatchTagsNonAlloc(item));
        }

        private static bool IsAcceptedByTreeFilter(TreeFilterable filterable, GameObject item)
        {
            HashSet<Tag> acceptedTags = filterable?.AcceptedTags;
            if (acceptedTags == null || acceptedTags.Count == 0)
            {
                return false;
            }

            return IsAnyMatchTagAccepted(
                acceptedTags,
                StorageItemUtility.GetStorageMatchTagsNonAlloc(item));
        }

        private static bool IsAcceptedByStorageFilters(
            IEnumerable<Tag> storageFilters,
            StorageItemUtility.StorageMatchTags matchTags)
        {
            if (storageFilters == null)
            {
                return true;
            }

            bool hasFilter = false;
            foreach (Tag acceptedTag in storageFilters)
            {
                hasFilter = true;
                if (IsAcceptedTagOrCategory(acceptedTag, matchTags.PrefabIdTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.PrefabTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.ElementTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.TransferTag))
                {
                    return true;
                }
            }

            return !hasFilter;
        }

        private static bool IsAnyMatchTagAccepted(
            IEnumerable<Tag> acceptedTags,
            StorageItemUtility.StorageMatchTags matchTags)
        {
            foreach (Tag acceptedTag in acceptedTags)
            {
                if (IsAcceptedTagOrCategory(acceptedTag, matchTags.PrefabIdTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.PrefabTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.ElementTag) ||
                    IsAcceptedTagOrCategory(acceptedTag, matchTags.TransferTag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAcceptedTagOrCategory(Tag acceptedTag, Tag itemTag)
        {
            if (acceptedTag == Tag.Invalid || itemTag == Tag.Invalid)
            {
                return false;
            }

            return acceptedTag == itemTag ||
                   DiscoveredResources.Instance != null &&
                   DiscoveredResources.Instance
                       .GetDiscoveredResourcesFromTag(acceptedTag)
                       .Contains(itemTag);
        }

        private static void FillAcceptedTags(
            TreeFilterable filterable,
            HashSet<Tag> tags)
        {
            tags.Clear();
            if (filterable?.AcceptedTags == null)
            {
                return;
            }

            foreach (Tag tag in filterable.AcceptedTags)
            {
                if (tag != Tag.Invalid)
                {
                    tags.Add(tag);
                }
            }
        }

        private sealed class FilterChangeWorkspace
        {
            public readonly List<GameObject> RejectedItems = new List<GameObject>();
            public readonly HashSet<Tag> AcceptedTags = new HashSet<Tag>();
        }
    }
}
