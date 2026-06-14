using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkFilterSelectionNormalizer
    {
        [System.ThreadStatic]
        private static int existingSelectionNormalizationDepth;

        public static bool ShouldNormalize(TreeFilterable filterable)
        {
            Storage storage = filterable != null ? filterable.GetFilterStorage() : null;
            return StorageNetworkMembership.IsCollectableStorage(storage) ||
                   StorageNetworkStorageRules.IsNetworkPortStorage(storage);
        }

        public static void NormalizeExistingSelection(TreeFilterable filterable)
        {
            if (existingSelectionNormalizationDepth > 0)
            {
                return;
            }

            if (filterable?.AcceptedTags == null ||
                filterable.AcceptedTags.Count == 0 ||
                !TryNormalize(filterable, new HashSet<Tag>(filterable.AcceptedTags), out HashSet<Tag> normalized))
            {
                return;
            }

            existingSelectionNormalizationDepth++;
            try
            {
                using (StorageNetwork.Components.StorageNetworkFilterState.SuppressUserConfigurationTracking())
                {
                    filterable.UpdateFilters(normalized);
                }
            }
            finally
            {
                existingSelectionNormalizationDepth = System.Math.Max(0, existingSelectionNormalizationDepth - 1);
            }
        }

        public static bool TryNormalize(TreeFilterable filterable, HashSet<Tag> filters, out HashSet<Tag> normalized)
        {
            normalized = filters;
            if (filterable == null || filters == null || filters.Count == 0 || !ShouldNormalize(filterable))
            {
                return false;
            }

            HashSet<Tag> result = new HashSet<Tag>();
            bool hasConcreteSelection = filters.Any(tag => tag != Tag.Invalid && !IsExpandableCategory(tag));
            foreach (Tag tag in filters.Where(tag => tag != Tag.Invalid))
            {
                if (hasConcreteSelection)
                {
                    if (!IsExpandableCategory(tag))
                    {
                        result.Add(tag);
                    }

                    continue;
                }

                AddConcreteTags(result, tag);
            }

            normalized = result;
            return result.Count != filters.Count || !result.SetEquals(filters);
        }

        private static void AddConcreteTags(HashSet<Tag> result, Tag tag)
        {
            if (DiscoveredResources.Instance == null)
            {
                result.Add(tag);
                return;
            }

            List<Tag> discoveredTags = DiscoveredResources.Instance
                .GetDiscoveredResourcesFromTag(tag)
                .Where(discoveredTag => discoveredTag != Tag.Invalid)
                .ToList();

            if (discoveredTags.Count == 0 || discoveredTags.Contains(tag))
            {
                result.Add(tag);
                return;
            }

            foreach (Tag discoveredTag in discoveredTags)
            {
                result.Add(discoveredTag);
            }
        }

        private static bool IsExpandableCategory(Tag tag)
        {
            if (tag == Tag.Invalid || DiscoveredResources.Instance == null)
            {
                return false;
            }

            List<Tag> discoveredTags = DiscoveredResources.Instance
                .GetDiscoveredResourcesFromTag(tag)
                .Where(discoveredTag => discoveredTag != Tag.Invalid)
                .ToList();
            return discoveredTags.Count > 0 && !discoveredTags.Contains(tag);
        }
    }
}
