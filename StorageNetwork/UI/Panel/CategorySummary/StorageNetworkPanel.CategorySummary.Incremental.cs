using System.Collections.Generic;
using StorageNetwork.Services;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private readonly StorageNetworkIncrementalTotals<Tag, StorageNetworkIndexedItemTotal> categorySummaryAggregation =
            new StorageNetworkIncrementalTotals<Tag, StorageNetworkIndexedItemTotal>();
        private readonly List<Storage> categorySummaryAggregationStorages = new List<Storage>();
        private readonly List<int> categorySummarySourceVersions = new List<int>();
        private readonly List<float> categorySummarySourceMasses = new List<float>();
        private readonly Dictionary<Tag, StorageNetworkIndexedItemTotal> categorySummarySourceItems =
            new Dictionary<Tag, StorageNetworkIndexedItemTotal>();
        private readonly StorageNetworkStableDisplayOrder<StorageNetworkCategorySummaryItemTotal> categorySummaryDisplayOrder =
            new StorageNetworkStableDisplayOrder<StorageNetworkCategorySummaryItemTotal>();
        private bool categorySummaryAggregationValid;

        private void ResetCategorySummaryAggregation()
        {
            categorySummaryAggregationValid = false;
            categorySummaryAggregation.Clear();
            categorySummaryAggregationStorages.Clear();
            categorySummarySourceVersions.Clear();
            categorySummarySourceMasses.Clear();
            categorySummarySourceItems.Clear();
            categorySummaryIndexedTotals.Clear();
            categorySummaryDisplayOrder.Clear();
        }

        private bool TryUpdateCategorySummaryIndexedTotals(IReadOnlyList<Storage> storages, out float storedKg)
        {
            storedKg = 0f;
            bool sameSources = categorySummaryAggregationValid && storages.Count == categorySummaryAggregationStorages.Count;
            if (sameSources)
                for (int i = 0; i < storages.Count; i++)
                    if (storages[i] != categorySummaryAggregationStorages[i]) { sameSources = false; break; }
            if (!sameSources)
            {
                ResetCategorySummaryAggregation();
                for (int i = 0; i < storages.Count; i++)
                {
                    categorySummaryAggregationStorages.Add(storages[i]);
                    categorySummarySourceVersions.Add(0);
                    categorySummarySourceMasses.Add(0f);
                }
            }

            for (int i = 0; i < storages.Count; i++)
            {
                if (!sameSources ||
                    !StorageNetworkContentIndexService.TryGetSingleStorageDisplayVersion(storages[i], out int observed) ||
                    observed != categorySummarySourceVersions[i])
                {
                    if (!StorageNetworkContentIndexService.TryReadSingleStorageItemTotals(storages[i],
                            out IReadOnlyList<StorageNetworkIndexedItemTotal> items, out float mass, out int version))
                    {
                        ResetCategorySummaryAggregation();
                        return false;
                    }
                    categorySummarySourceItems.Clear();
                    for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                        categorySummarySourceItems.Add(items[itemIndex].KeyTag, items[itemIndex]);
                    categorySummaryAggregation.SetSource(i, categorySummarySourceItems);
                    categorySummarySourceVersions[i] = version;
                    categorySummarySourceMasses[i] = mass;
                }
                storedKg += categorySummarySourceMasses[i];
            }
            categorySummaryAggregation.ApplyChanges(categorySummaryIndexedTotals, MergeCategorySummaryTotal);
            categorySummaryAggregationValid = true;
            return true;
        }

        private static StorageNetworkIndexedItemTotal MergeCategorySummaryTotal(
            StorageNetworkIndexedItemTotal first, StorageNetworkIndexedItemTotal second)
        {
            first.Merge(second);
            return first;
        }
    }
}
