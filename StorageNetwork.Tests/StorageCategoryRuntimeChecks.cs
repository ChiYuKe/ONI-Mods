using System;
using System.Collections.Generic;
using StorageNetwork.Services;
using UnityEngine;

// Engine boundaries for the production category matcher. The classification
// itself is linked unchanged; discovery and registered prefab tags vary here.
internal static class Assets
{
    internal static readonly Dictionary<Tag, GameObject> Prefabs = new Dictionary<Tag, GameObject>();
    public static GameObject TryGetPrefab(Tag tag) => Prefabs.TryGetValue(tag, out var prefab) ? prefab : null;
}

internal sealed class DiscoveredResources
{
    public static DiscoveredResources Instance;
    internal readonly Dictionary<Tag, List<Tag>> Resources = new Dictionary<Tag, List<Tag>>();
    public List<Tag> GetDiscoveredResourcesFromTag(Tag tag) => Resources.TryGetValue(tag, out var list) ? list : new List<Tag>();
}

internal static class StorageCategoryRuntimeChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var food = new Tag("Edible");
        var seed = new Tag("Seed");
        var suit = new Tag("Suit");
        var fish = new Tag("CookedFish");
        var crop = new Tag("CropSeed");
        var equipment = new Tag("AtmoSuit");
        foreach (var entry in new[] { (fish, food), (crop, seed), (equipment, suit) })
        {
            var prefab = new GameObject();
            var id = new KPrefabID();
            id.Tags.Add(entry.Item2);
            prefab.Set(id);
            Assets.Prefabs.Add(entry.Item1, prefab);
        }
        DiscoveredResources.Instance = null;
        check(StorageNetworkCategoryMatcher.Accepts(food, fish), "first undiscovered cooked fish is accepted by the food category");
        check(StorageNetworkCategoryMatcher.Accepts(seed, crop), "undiscovered seeds match their prefab category");
        check(StorageNetworkCategoryMatcher.Accepts(suit, equipment), "undiscovered equipment matches its prefab category");
        check(!StorageNetworkCategoryMatcher.Accepts(seed, fish) && !StorageNetworkCategoryMatcher.Accepts(food, crop),
            "prefab matching does not bypass unrelated storage categories");
        check(StorageNetworkCategoryMatcher.Accepts(fish, fish), "an explicit item selection is accepted without discovery");
        DiscoveredResources.Instance = new DiscoveredResources();
        check(StorageNetworkCategoryMatcher.Accepts(food, fish), "empty discovery lists cannot veto actual food prefab tags");
        var legacy = new Tag("LegacyCategory");
        DiscoveredResources.Instance.Resources[legacy] = new List<Tag> { fish };
        check(StorageNetworkCategoryMatcher.Accepts(legacy, fish), "discovered category aliases remain compatible");
        check(!StorageNetworkCategoryMatcher.Accepts(Tag.Invalid, fish), "invalid filter categories are rejected");
        Assets.Prefabs.Clear();
        DiscoveredResources.Instance = null;
    }
}
