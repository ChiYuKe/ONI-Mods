using System.Collections.Generic;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkCategorySummarySignature
    {
        public static string BuildStructure(
            string categoryKey,
            List<StorageNetworkCategorySummaryItemTotal> totals)
        {
            List<string> keys = new List<string>(totals?.Count ?? 0);
            if (totals != null)
            {
                foreach (StorageNetworkCategorySummaryItemTotal total in totals)
                {
                    keys.Add(total.Key ?? string.Empty);
                }
            }

            keys.Sort(System.StringComparer.Ordinal);
            return (categoryKey ?? string.Empty) + "|" + string.Join(",", keys);
        }
    }
}
