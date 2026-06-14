using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    /// <summary>
    /// Initializes server filters once so the vanilla filter side screen starts fully selected.
    /// </summary>
    public sealed class StorageNetworkDefaultFilterInitializer : KMonoBehaviour
    {
        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private TreeFilterable filterable = null;

        [MyCmpGet]
        private StorageNetworkFilterState filterState = null;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            filterState ??= StorageNetworkFilterState.Ensure(filterable) ?? gameObject.AddOrGet<StorageNetworkFilterState>();
            if (storage == null || filterable == null || filterState == null)
            {
                return;
            }

            StorageNetworkFilterConfigurator.Configure(filterable);
            if (filterState.DefaultInitialized || filterState.UserConfigured)
            {
                return;
            }

            if (HasExistingFilterSelection(filterable))
            {
                filterState.MarkDefaultInitialized();
                return;
            }

            HashSet<Tag> defaultTags = BuildDefaultTags(storage.storageFilters);
            if (defaultTags.Count > 0)
            {
                using (StorageNetworkFilterState.SuppressUserConfigurationTracking())
                {
                    filterable.UpdateFilters(defaultTags);
                }
            }

            filterState.MarkDefaultInitialized();
        }

        private static bool HasExistingFilterSelection(TreeFilterable filterable)
        {
            return filterable != null &&
                   filterable.AcceptedTags != null &&
                   filterable.AcceptedTags.Any(tag => tag != Tag.Invalid);
        }

        private static HashSet<Tag> BuildDefaultTags(IEnumerable<Tag> storageFilters)
        {
            HashSet<Tag> tags = new HashSet<Tag>();
            if (storageFilters == null)
            {
                return tags;
            }

            foreach (Tag filter in storageFilters)
            {
                if (filter == Tag.Invalid)
                {
                    continue;
                }

                if (DiscoveredResources.Instance == null)
                {
                    tags.Add(filter);
                    continue;
                }

                List<Tag> discoveredTags = DiscoveredResources.Instance
                    .GetDiscoveredResourcesFromTag(filter)
                    .Where(tag => tag != Tag.Invalid)
                    .ToList();
                if (discoveredTags.Count == 0)
                {
                    tags.Add(filter);
                    continue;
                }

                foreach (Tag discoveredTag in discoveredTags)
                {
                    tags.Add(discoveredTag);
                }
            }

            return tags;
        }
    }
}
