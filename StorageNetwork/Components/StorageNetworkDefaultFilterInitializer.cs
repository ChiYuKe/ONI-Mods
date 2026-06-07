using System.Collections.Generic;
using System.Linq;
using KSerialization;

namespace StorageNetwork.Components
{
    /// <summary>
    /// Initializes server filters once so the vanilla filter side screen starts fully selected.
    /// </summary>
    public sealed class StorageNetworkDefaultFilterInitializer : KMonoBehaviour
    {
        [Serialize]
        private bool initialized;

        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private TreeFilterable filterable = null;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (initialized || storage == null || filterable == null)
            {
                return;
            }

            HashSet<Tag> defaultTags = BuildDefaultTags(storage.storageFilters);
            if (defaultTags.Count > 0)
            {
                filterable.UpdateFilters(defaultTags);
            }

            initialized = true;
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

                tags.Add(filter);
                if (DiscoveredResources.Instance == null)
                {
                    continue;
                }

                foreach (Tag discoveredTag in DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(filter).Where(tag => tag != Tag.Invalid))
                {
                    tags.Add(discoveredTag);
                }
            }

            return tags;
        }
    }
}
