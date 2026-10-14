using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkCategoryMatcher
    {
        internal static bool Accepts(Tag category, Tag item)
        {
            if (category == Tag.Invalid || item == Tag.Invalid) return false;
            if (category == item) return true;
            // A first trader delivery can be a prefab the colony has never seen.
            // Its declared tags define membership independently of discovery.
            GameObject prefab = Assets.TryGetPrefab(item);
            KPrefabID prefabId = prefab != null ? prefab.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.HasTag(category)) return true;
            Element element = ElementLoader.FindElementByHash((SimHashes)item.GetHash());
            if (element != null && element.HasTag(category)) return true;
            return DiscoveredResources.Instance != null &&
                DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(category).Contains(item);
        }
    }
}
