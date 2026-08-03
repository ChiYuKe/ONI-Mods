using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    /// <summary>
    /// Native Storage reader used only by developer shadow validation. It
    /// deliberately shares no derived state with StorageNetworkContentIndexService.
    /// </summary>
    internal static class StorageNetworkContentShadowReader
    {
        private static readonly Dictionary<Storage, float> SourceAmounts =
            new Dictionary<Storage, float>();
        private static readonly List<KeyValuePair<Storage, float>> OrderedSources =
            new List<KeyValuePair<Storage, float>>();

        public static float GetAmount(
            int worldId,
            bool includeRelatedWorlds,
            Tag tag,
            Tag[] forbiddenTags)
        {
            float result = 0f;
            foreach (Storage storage in GetStorages(worldId, includeRelatedWorlds))
            {
                if (!IsInventoryStorage(storage) || storage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    StorageItemUtility.StorageMatchTags matchTags =
                        StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
                    if (!matchTags.Contains(tag))
                    {
                        continue;
                    }

                    KPrefabID prefabId = item.GetComponent<KPrefabID>();
                    if (prefabId != null &&
                        forbiddenTags != null &&
                        forbiddenTags.Length > 0 &&
                        prefabId.HasAnyTags(forbiddenTags))
                    {
                        continue;
                    }

                    result += GetItemAmount(item);
                }
            }

            return result;
        }

        public static StorageNetworkInventoryMetrics GetMetrics(
            int worldId,
            bool includeRelatedWorlds)
        {
            float storedKg = 0f;
            float capacityKg = 0f;
            foreach (Storage storage in GetStorages(worldId, includeRelatedWorlds))
            {
                if (!IsInventoryStorage(storage) ||
                    !StorageNetworkStorageRules.CountsTowardNetworkCapacity(storage))
                {
                    continue;
                }

                storedKg += storage.MassStored();
                capacityKg += storage.Capacity();
            }

            return new StorageNetworkInventoryMetrics(true, storedKg, capacityKg);
        }

        public static float GetStorageAmount(Storage storage, Tag tag)
        {
            if (storage == null || storage.items == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            float result = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                if (IsElementStateTag(tag))
                {
                    if (MatchesElementState(item, tag))
                    {
                        result += GetItemAmount(item);
                    }
                }
                else if (StorageItemUtility.GetStorageMatchTagsNonAlloc(item).Contains(tag))
                {
                    result += GetItemAmount(item);
                }
            }

            return result;
        }

        public static float GetRemainingCapacity(Storage storage)
        {
            return storage != null
                ? Mathf.Max(0f, storage.RemainingCapacity())
                : 0f;
        }

        public static float GetMaximumStorageAmount(
            Storage storage,
            StorageItemUtility.StorageMatchTags wantedTags)
        {
            if (storage == null || storage.items == null)
            {
                return 0f;
            }

            float prefabIdAmount = 0f;
            float prefabAmount = 0f;
            float elementAmount = 0f;
            float transferAmount = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                StorageItemUtility.StorageMatchTags itemTags =
                    StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
                float amount = GetItemAmount(item);
                if (wantedTags.PrefabIdTag != Tag.Invalid &&
                    itemTags.Contains(wantedTags.PrefabIdTag))
                {
                    prefabIdAmount += amount;
                }

                if (wantedTags.PrefabTag != Tag.Invalid &&
                    itemTags.Contains(wantedTags.PrefabTag))
                {
                    prefabAmount += amount;
                }

                if (wantedTags.ElementTag != Tag.Invalid &&
                    itemTags.Contains(wantedTags.ElementTag))
                {
                    elementAmount += amount;
                }

                if (wantedTags.TransferTag != Tag.Invalid &&
                    itemTags.Contains(wantedTags.TransferTag))
                {
                    transferAmount += amount;
                }
            }

            return Mathf.Max(
                Mathf.Max(prefabIdAmount, prefabAmount),
                Mathf.Max(elementAmount, transferAmount));
        }

        public static void FillSourceStorages(
            int worldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            Storage specificSource,
            List<Storage> destination)
        {
            destination.Clear();
            if (specificSource != null)
            {
                if (IsUsableSource(
                    specificSource,
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    out _))
                {
                    destination.Add(specificSource);
                }

                return;
            }

            SourceAmounts.Clear();
            foreach (Storage storage in GetStorages(worldId, includeRelatedWorlds))
            {
                if (IsUsableSource(
                    storage,
                    worldId,
                    includeRelatedWorlds,
                    wantedTags,
                    excludedStorages,
                    out float amount))
                {
                    SourceAmounts[storage] = amount;
                }
            }

            OrderedSources.Clear();
            foreach (KeyValuePair<Storage, float> pair in SourceAmounts)
            {
                OrderedSources.Add(pair);
            }

            OrderedSources.Sort(CompareSources);
            foreach (KeyValuePair<Storage, float> pair in OrderedSources)
            {
                destination.Add(pair.Key);
            }

            OrderedSources.Clear();
            SourceAmounts.Clear();
        }

        private static IReadOnlyCollection<Storage> GetStorages(
            int worldId,
            bool includeRelatedWorlds)
        {
            bool includeAllWorlds =
                includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
            return StorageSceneRegistry.GetCollectableStoragesForWorld(
                worldId,
                includeAllWorlds);
        }

        private static bool IsInventoryStorage(Storage storage)
        {
            return StorageSceneRegistry.IsLive(storage) &&
                   StorageNetworkStorageRules.IsServerStorage(storage) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
        }

        private static bool IsUsableSource(
            Storage storage,
            int destinationWorldId,
            bool includeRelatedWorlds,
            IEnumerable<Tag> wantedTags,
            HashSet<Storage> excludedStorages,
            out float amount)
        {
            amount = 0f;
            if (!IsInventoryStorage(storage) ||
                StorageNetworkStorageRules.IsMinionStorage(storage) ||
                StorageNetworkStorageRules.IsProductionStorage(storage) ||
                excludedStorages != null && excludedStorages.Contains(storage))
            {
                return false;
            }

            bool includeAllWorlds =
                includeRelatedWorlds && StorageSceneRegistry.IsCrossPlanetRelayOnline();
            if (!includeAllWorlds &&
                StorageTargetSelector.GetObjectWorldId(storage.gameObject) != destinationWorldId)
            {
                return false;
            }

            if (wantedTags != null)
            {
                foreach (Tag tag in wantedTags)
                {
                    if (tag != Tag.Invalid)
                    {
                        amount = Mathf.Max(amount, GetStorageAmount(storage, tag));
                    }
                }
            }

            return amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static float GetItemAmount(GameObject item)
        {
            Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
            return pickupable != null
                ? pickupable.TotalAmount
                : StorageItemUtility.GetMass(item);
        }

        private static bool IsElementStateTag(Tag tag)
        {
            return tag == GameTags.Solid || tag == GameTags.Liquid || tag == GameTags.Gas;
        }

        private static bool MatchesElementState(GameObject item, Tag tag)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            Element element = primaryElement != null
                ? ElementLoader.FindElementByHash(primaryElement.ElementID)
                : null;
            return element != null &&
                   (tag == GameTags.Liquid && element.IsLiquid ||
                    tag == GameTags.Gas && element.IsGas ||
                    tag == GameTags.Solid && !element.IsLiquid && !element.IsGas);
        }

        private static int CompareSources(
            KeyValuePair<Storage, float> left,
            KeyValuePair<Storage, float> right)
        {
            int amountComparison = right.Value.CompareTo(left.Value);
            return amountComparison != 0
                ? amountComparison
                : StorageItemUtility.GetStorageInstanceId(left.Key)
                    .CompareTo(StorageItemUtility.GetStorageInstanceId(right.Key));
        }
    }
}
