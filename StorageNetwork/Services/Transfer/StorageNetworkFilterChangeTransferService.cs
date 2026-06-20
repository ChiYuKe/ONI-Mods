using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFilterChangeTransferService
    {
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

            List<GameObject> rejectedItems = new List<GameObject>();
            foreach (GameObject item in source.items)
            {
                if (item != null && !IsItemAcceptedByFilter(item, filterable))
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

                HashSet<Tag> matchTags = StorageItemUtility.GetStorageMatchTags(item);
                Storage target = FindAcceptingServer(source, item, matchTags, sourceWorldId);
                if (target == null)
                {
                    continue;
                }

                source.Transfer(item, target, block_events: false, hide_popups: true);
            }
        }

        private static Storage FindAcceptingServer(Storage source, GameObject item, HashSet<Tag> matchTags, int sourceWorldId)
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

        private static bool IsAcceptingServer(Storage source, Storage target, GameObject item, HashSet<Tag> matchTags, int sourceWorldId)
        {
            return StorageSceneRegistry.IsLive(target) &&
                   target != source &&
                   StorageNetworkStorageRules.IsNetworkStorageTarget(target, source) &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   target.RemainingCapacity() >= StorageItemUtility.GetMass(item) &&
                   IsAcceptedByStorageFilters(target.storageFilters, matchTags) &&
                   IsAcceptedByTreeFilter(target.GetComponent<TreeFilterable>(), item);
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

        private static bool IsItemAcceptedByFilter(GameObject item, TreeFilterable filterable)
        {
            return IsAcceptedByTreeFilter(filterable, item);
        }

        private static bool IsAcceptedByTreeFilter(TreeFilterable filterable, GameObject item)
        {
            HashSet<Tag> acceptedTags = GetAcceptedTags(filterable);
            if (acceptedTags.Count == 0)
            {
                return false;
            }

            foreach (Tag tag in StorageItemUtility.GetStorageMatchTags(item))
            {
                if (IsAcceptedTagOrCategory(acceptedTags, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAcceptedByStorageFilters(IEnumerable<Tag> storageFilters, IEnumerable<Tag> matchTags)
        {
            if (storageFilters == null || !storageFilters.Any())
            {
                return true;
            }

            foreach (Tag tag in matchTags)
            {
                if (IsAcceptedTagOrCategory(storageFilters, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAcceptedTagOrCategory(IEnumerable<Tag> acceptedTags, Tag itemTag)
        {
            if (itemTag == Tag.Invalid)
            {
                return false;
            }

            foreach (Tag acceptedTag in acceptedTags)
            {
                if (acceptedTag == itemTag ||
                    DiscoveredResources.Instance != null &&
                    DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(acceptedTag).Contains(itemTag))
                {
                    return true;
                }
            }

            return false;
        }

        private static HashSet<Tag> GetAcceptedTags(TreeFilterable filterable)
        {
            HashSet<Tag> tags = new HashSet<Tag>();
            if (filterable?.AcceptedTags == null)
            {
                return tags;
            }

            foreach (Tag tag in filterable.AcceptedTags)
            {
                if (tag != Tag.Invalid)
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }
    }
}
