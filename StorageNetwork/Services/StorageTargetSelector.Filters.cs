using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using TUNING;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static partial class StorageTargetSelector
    {
        private static int CompareElementTargets(Storage left, Storage right, Tag tag)
        {
            if (!StorageSceneRegistry.IsLive(left) || !StorageSceneRegistry.IsLive(right))
            {
                return StorageSceneRegistry.IsLive(left).CompareTo(StorageSceneRegistry.IsLive(right));
            }

            float leftAvailable = left.GetAmountAvailable(tag);
            float rightAvailable = right.GetAmountAvailable(tag);
            int compare = leftAvailable.CompareTo(rightAvailable);
            if (compare != 0)
            {
                return compare;
            }

            compare = IsFilterAccepting(left, tag).CompareTo(IsFilterAccepting(right, tag));
            if (compare != 0)
            {
                return compare;
            }

            return left.RemainingCapacity().CompareTo(right.RemainingCapacity());
        }

        private static bool IsUsableOutputTarget(Storage target, GameObject item, HashSet<Tag> matchTags, HashSet<Storage> excludedStorages, int sourceWorldId = -1)
        {
            return StorageSceneRegistry.IsLive(target) &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   !excludedStorages.Contains(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   !StorageNetworkStorageRules.IsProductionStorage(target) &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, matchTags) &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsUsableOutputTarget(Storage target, GameObject item, StorageItemUtility.StorageMatchTags matchTags, HashSet<Storage> excludedStorages, int sourceWorldId = -1)
        {
            return StorageSceneRegistry.IsLive(target) &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   !excludedStorages.Contains(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   !StorageNetworkStorageRules.IsProductionStorage(target) &&
                   target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                   IsStorageAccepting(target, matchTags) &&
                   (target.items == null || !target.items.Contains(item));
        }

        private static bool IsUsableElementOutputTarget(Storage target, Element element, Tag tag, HashSet<Storage> excludedStorages, int sourceWorldId = -1)
        {
            return IsElementOutputTargetCandidate(target, element, tag, excludedStorages, sourceWorldId, true);
        }

        private static bool IsElementOutputTargetCandidate(Storage target, Element element, Tag tag, HashSet<Storage> excludedStorages, int sourceWorldId, bool requireCapacity)
        {
            return StorageSceneRegistry.IsLive(target) &&
                   element != null &&
                   tag != Tag.Invalid &&
                   IsStorageReachableFromWorld(target, sourceWorldId) &&
                   !excludedStorages.Contains(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   StorageNetworkStorageRules.MatchesElementState(target, element) &&
                   !StorageNetworkStorageRules.IsNetworkPortStorage(target) &&
                   !StorageNetworkStorageRules.IsMinionStorage(target) &&
                   !StorageNetworkStorageRules.IsProductionStorage(target) &&
                   (!requireCapacity || target.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) &&
                   IsElementOutputStorageAccepting(target, element, tag);
        }

        private static bool IsUsableNetworkSource(Storage source, IEnumerable<Tag> tags, HashSet<Storage> excludedStorages, int destinationWorldId)
        {
            return StorageSceneRegistry.IsLive(source) &&
                   IsStorageReachableFromWorld(source, destinationWorldId) &&
                   !excludedStorages.Contains(source) &&
                   StorageNetworkStorageRules.IsServerStorage(source) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(source) &&
                   !StorageNetworkStorageRules.IsMinionStorage(source) &&
                   !StorageNetworkStorageRules.IsProductionStorage(source) &&
                   GetAmountAvailableByAnyMatchTag(source, tags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
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

            return GetObjectWorldId(storage.gameObject) == worldId;
        }

        private static bool IsAutoOutputMatch(Storage target, HashSet<Tag> matchTags)
        {
            return GetAmountAvailableByAnyMatchTag(target, matchTags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   AcceptsByElementStateWithoutFilterUi(target, matchTags) ||
                   IsFilterAccepting(target, matchTags) ||
                   HasNoExplicitStorageFilter(target);
        }

        private static bool IsAutoOutputMatch(Storage target, StorageItemUtility.StorageMatchTags matchTags)
        {
            return GetAmountAvailableByAnyMatchTag(target, matchTags) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   AcceptsByElementStateWithoutFilterUi(target, matchTags) ||
                   IsFilterAccepting(target, matchTags) ||
                   HasNoExplicitStorageFilter(target);
        }

        private static bool IsFilterAccepting(Storage target, HashSet<Tag> matchTags)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            if (IsEmptyFilteredPort(target, filterable))
            {
                return false;
            }

            return filterable != null && AnyTagAcceptedByTreeFilter(filterable, target.storageFilters, matchTags);
        }

        private static bool IsFilterAccepting(Storage target, Tag tag)
        {
            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return IsStorageFilterAcceptingTag(target.storageFilters, tag);
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            if (IsEmptyFilteredPort(target, filterable))
            {
                return false;
            }

            return filterable != null && IsFilterAcceptingTag(filterable, tag, target.storageFilters);
        }

        private static bool IsFilterAccepting(Storage target, StorageItemUtility.StorageMatchTags matchTags)
        {
            return IsFilterAccepting(target, matchTags.PrefabIdTag) ||
                   IsFilterAccepting(target, matchTags.PrefabTag) ||
                   IsFilterAccepting(target, matchTags.ElementTag) ||
                   IsFilterAccepting(target, matchTags.TransferTag);
        }

        private static bool IsStorageAccepting(Storage target, HashSet<Tag> matchTags)
        {
            if (AcceptsByElementStateWithoutFilterUi(target, matchTags))
            {
                return true;
            }

            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
            }

            if (IsFilteredPort(target))
            {
                return IsFilterAccepting(target, matchTags);
            }

            return target != null &&
                   (IsFilterAccepting(target, matchTags) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                   AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags));
        }

        private static bool IsStorageAccepting(Storage target, StorageItemUtility.StorageMatchTags matchTags)
        {
            if (AcceptsByElementStateWithoutFilterUi(target, matchTags))
            {
                return true;
            }

            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags);
            }

            if (IsFilteredPort(target))
            {
                return IsFilterAccepting(target, matchTags);
            }

            return target != null &&
                   (IsFilterAccepting(target, matchTags) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                   AnyTagAcceptedByStorageFilter(target.storageFilters, matchTags));
        }

        private static bool IsElementOutputStorageAccepting(Storage target, Element element, Tag tag)
        {
            if (target == null || element == null || tag == Tag.Invalid)
            {
                return false;
            }

            if (element.IsLiquid)
            {
                return IsStorageAccepting(target, tag) &&
                       (target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                        IsFilterAccepting(target, tag) ||
                        HasNoExplicitStorageFilter(target));
            }

            if (element.IsGas)
            {
                return IsStorageAccepting(target, tag) &&
                       (target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                        IsFilterAccepting(target, tag) ||
                        HasNoExplicitStorageFilter(target));
            }

            return IsStorageAccepting(target, tag) &&
                   (target.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    IsFilterAccepting(target, tag) ||
                    HasNoExplicitStorageFilter(target));
        }

        private static bool IsStorageAccepting(Storage target, Tag tag)
        {
            if (AcceptsByElementStateWithoutFilterUi(target, tag))
            {
                return true;
            }

            if (StorageNetworkFilterBypass.ShouldBypassUserFilter(target))
            {
                return IsStorageFilterAcceptingTag(target.storageFilters, tag);
            }

            if (IsFilteredPort(target))
            {
                return IsFilterAccepting(target, tag);
            }

            return target != null &&
                   (IsFilterAccepting(target, tag) ||
                    target.storageFilters == null ||
                    target.storageFilters.Count == 0 ||
                    IsStorageFilterAcceptingTag(target.storageFilters, tag));
        }

        private static bool IsFilterAcceptingTag(TreeFilterable filterable, Tag tag, List<Tag> storageFilters)
        {
            if (IsEmptyFilter(filterable))
            {
                return false;
            }

            if (filterable == null || filterable.ContainsTag(tag))
            {
                return filterable != null;
            }

            foreach (Tag accepted in filterable.AcceptedTags)
            {
                if (IsDiscoveredCategoryAccepting(accepted, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStorageFilterAcceptingTag(List<Tag> storageFilters, Tag tag)
        {
            if (storageFilters == null || storageFilters.Count == 0 || storageFilters.Contains(tag))
            {
                return true;
            }

            foreach (Tag filter in storageFilters)
            {
                if (IsDiscoveredCategoryAccepting(filter, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AcceptsByElementStateWithoutFilterUi(Storage target, IEnumerable<Tag> tags)
        {
            if (target == null || tags == null || target.GetComponent<TreeFilterable>() != null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (AcceptsByElementStateWithoutFilterUi(target, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AcceptsByElementStateWithoutFilterUi(Storage target, StorageItemUtility.StorageMatchTags matchTags)
        {
            return AcceptsByElementStateWithoutFilterUi(target, matchTags.PrefabIdTag) ||
                   AcceptsByElementStateWithoutFilterUi(target, matchTags.PrefabTag) ||
                   AcceptsByElementStateWithoutFilterUi(target, matchTags.ElementTag) ||
                   AcceptsByElementStateWithoutFilterUi(target, matchTags.TransferTag);
        }

        private static bool AcceptsByElementStateWithoutFilterUi(Storage target, Tag tag)
        {
            if (target == null || tag == Tag.Invalid || target.GetComponent<TreeFilterable>() != null)
            {
                return false;
            }

            Element element = ElementLoader.FindElementByHash((SimHashes)tag.GetHash());
            if (element == null)
            {
                return false;
            }

            List<Tag> storageFilters = target.storageFilters;
            if (storageFilters == null || storageFilters.Count == 0)
            {
                return false;
            }

            foreach (Tag filter in storageFilters)
            {
                if (STORAGEFILTERS.LIQUIDS.Contains(filter))
                {
                    return element.IsLiquid;
                }
            }

            foreach (Tag filter in storageFilters)
            {
                if (STORAGEFILTERS.GASES.Contains(filter))
                {
                    return element.IsGas;
                }
            }

            return false;
        }

        private static bool IsDiscoveredCategoryAccepting(Tag categoryTag, Tag itemTag)
        {
            return categoryTag == itemTag ||
                   DiscoveredResources.Instance != null &&
                   DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(categoryTag).Contains(itemTag);
        }

        private static bool AnyTagAcceptedByTreeFilter(TreeFilterable filterable, List<Tag> storageFilters, IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (IsFilterAcceptingTag(filterable, tag, storageFilters))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyTagAcceptedByStorageFilter(List<Tag> storageFilters, IEnumerable<Tag> tags)
        {
            if (tags == null)
            {
                return false;
            }

            foreach (Tag tag in tags)
            {
                if (IsStorageFilterAcceptingTag(storageFilters, tag))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AnyTagAcceptedByStorageFilter(List<Tag> storageFilters, StorageItemUtility.StorageMatchTags matchTags)
        {
            return IsStorageFilterAcceptingTag(storageFilters, matchTags.PrefabIdTag) ||
                   IsStorageFilterAcceptingTag(storageFilters, matchTags.PrefabTag) ||
                   IsStorageFilterAcceptingTag(storageFilters, matchTags.ElementTag) ||
                   IsStorageFilterAcceptingTag(storageFilters, matchTags.TransferTag);
        }

        private static float GetAmountAvailableByAnyMatchTag(Storage target, IEnumerable<Tag> matchTags)
        {
            if (!StorageSceneRegistry.IsLive(target) || matchTags == null)
            {
                return 0f;
            }

            float available = 0f;
            foreach (Tag tag in matchTags)
            {
                available = Mathf.Max(available, target.GetAmountAvailable(tag));
            }

            return available;
        }

        private static float GetAmountAvailableByAnyMatchTag(Storage target, StorageItemUtility.StorageMatchTags matchTags)
        {
            if (!StorageSceneRegistry.IsLive(target))
            {
                return 0f;
            }

            float available = 0f;
            available = Mathf.Max(available, target.GetAmountAvailable(matchTags.PrefabIdTag));
            available = Mathf.Max(available, target.GetAmountAvailable(matchTags.PrefabTag));
            available = Mathf.Max(available, target.GetAmountAvailable(matchTags.ElementTag));
            available = Mathf.Max(available, target.GetAmountAvailable(matchTags.TransferTag));
            return available;
        }

        private static bool HasNoExplicitStorageFilter(Storage target)
        {
            if (IsFilteredPort(target))
            {
                return false;
            }

            TreeFilterable filterable = target != null ? target.GetComponent<TreeFilterable>() : null;
            return (filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0) &&
                   (target.storageFilters == null || target.storageFilters.Count == 0);
        }

        private static bool IsFilteredPort(Storage target)
        {
            return false;
        }

        private static bool IsEmptyFilteredPort(Storage target, TreeFilterable filterable)
        {
            return IsFilteredPort(target) &&
                   IsEmptyFilter(filterable);
        }

        private static bool IsEmptyFilter(TreeFilterable filterable)
        {
            return filterable == null ||
                   filterable.AcceptedTags == null ||
                   filterable.AcceptedTags.Count == 0;
        }

        private static string DescribeElementTargetCandidate(Storage target, Tag tag, HashSet<Storage> excludedStorages, int sourceWorldId)
        {
            string name = target != null ? target.GetProperName() : "null";
            if (!StorageSceneRegistry.IsLive(target))
            {
                return "null target";
            }

            if (tag == Tag.Invalid)
            {
                return name + "=invalid tag";
            }

            if (!IsStorageReachableFromWorld(target, sourceWorldId))
            {
                return string.Format("{0}=world mismatch(targetWorld={1}, sourceWorld={2})", name, GetObjectWorldId(target.gameObject), sourceWorldId);
            }

            if (excludedStorages != null && excludedStorages.Contains(target))
            {
                return name + "=excluded";
            }

            if (!StorageNetworkStorageRules.IsServerStorage(target))
            {
                return name + "=not server storage";
            }

            Element element = ElementLoader.FindElementByHash((SimHashes)tag.GetHash());
            if (!StorageNetworkStorageRules.MatchesElementState(target, element))
            {
                return string.Format("{0}=wrong state for {1}", name, tag);
            }

            if (StorageNetworkStorageRules.IsNetworkPortStorage(target))
            {
                return name + "=network port";
            }

            if (!StorageNetworkStorageRules.IsConnectedNetworkStorage(target))
            {
                return name + "=server offline";
            }

            if (StorageNetworkStorageRules.IsMinionStorage(target))
            {
                return name + "=minion storage";
            }

            if (StorageNetworkStorageRules.IsProductionStorage(target))
            {
                return name + "=production storage";
            }

            if (target.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return string.Format("{0}=full(remaining={1:0.###}kg)", name, target.RemainingCapacity());
            }

            if (!IsStorageAccepting(target, tag))
            {
                return string.Format("{0}=rejects {1}", name, tag);
            }

            if (target.GetAmountAvailable(tag) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT &&
                !AcceptsByElementStateWithoutFilterUi(target, tag) &&
                !IsFilterAccepting(target, tag) &&
                !HasNoExplicitStorageFilter(target))
            {
                return string.Format("{0}=no auto-match for {1}", name, tag);
            }

            return string.Format("{0}=eligible(remaining={1:0.###}kg)", name, target.RemainingCapacity());
        }
    }
}
