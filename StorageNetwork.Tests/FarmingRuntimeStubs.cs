using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal sealed class FetchChore { public Tag tagsFirst; public float originalAmount; }
internal sealed class PlantablePlot
{
    public GameObject gameObject = new GameObject();
    public bool isActiveAndEnabled = true;
    public GameObject Occupant { get; set; }
    public FetchChore GetActiveRequest;
}
internal static class Components
{
    public static class PlantablePlots
    {
        public static readonly List<PlantablePlot> Items = new List<PlantablePlot>();
        public static IEnumerable<PlantablePlot> GetItems(int world) => Items.Where(p => p.gameObject.WorldId == world);
    }
}
internal sealed partial class KPrefabID
{
    public void AddTag(Tag tag, bool persistent) => Tags.Add(tag);
    public void RemoveTag(Tag tag) => Tags.Remove(tag);
}
internal sealed partial class Pickupable
{
    public Pickupable Take(float amount)
    {
        var element = gameObject.GetComponent<PrimaryElement>();
        if (amount <= 0 || amount > TotalAmount) return null;
        TotalAmount -= amount;
        element.Mass -= amount;
        var item = new GameObject { MatchTags = gameObject.MatchTags };
        item.Set(new PrimaryElement { Mass = amount, ElementID = element.ElementID, Temperature = element.Temperature });
        var result = new Pickupable { TotalAmount = amount, gameObject = item };
        item.Set(result);
        var prefab = new KPrefabID { gameObject = item };
        prefab.Tags.UnionWith(gameObject.GetComponent<KPrefabID>().Tags);
        item.Set(prefab);
        return result;
    }
}
internal sealed partial class Storage
{
    public bool Transfer(GameObject item, Storage destination, bool block_events, bool hide_popups)
    {
        if (!items.Remove(item)) return false;
        destination.items.Add(item);
        OnStorageChange?.Invoke(item);
        destination.OnStorageChange?.Invoke(item);
        return true;
    }
    public void Store(GameObject item, bool hide_popups, bool block_events, bool do_disease_transfer, bool is_deserializing)
    {
        items.Add(item);
        OnStorageChange?.Invoke(item);
    }
    public void Trigger(int hash, GameObject item) { }
}
namespace StorageNetwork.API
{
    internal static class StorageNetworkTags
    {
        public static readonly Tag ReservedForFarming = new Tag("ReservedForFarming");
    }
}
namespace StorageNetwork.Components
{
    internal sealed class StorageNetworkSolidOutputPortEgress { }
}
namespace StorageNetwork.Core
{
    internal static class StorageSceneCollector
    {
        internal sealed class Snapshot { public List<Storage> Storages; }
        public static Snapshot CollectLightweightForWorld(int world) => new Snapshot
        { Storages = StorageSceneRegistry.Storages.Where(s => s.Live && s.gameObject.WorldId == world).ToList() };
    }
}
namespace StorageNetwork.Services
{
    internal static partial class StorageItemUtility
    {
        public static float GetMass(GameObject item) => item?.GetComponent<PrimaryElement>()?.Mass ?? 0f;
        public static bool MatchesStorageTag(GameObject item, Tag tag) => item.MatchTags.Contains(tag);
        public static Tag GetStorageTransferTag(GameObject item) => item.MatchTags.TransferTag;
    }
    internal static partial class StorageTargetSelector
    {
        public static HashSet<Storage> BuildExclusionSet(IEnumerable<Storage> storages) =>
            storages == null ? new HashSet<Storage>() : new HashSet<Storage>(storages);
        public static List<Storage> FindNetworkSources(IEnumerable<Tag> tags, HashSet<Storage> excluded, Storage source, int world)
        {
            var result = new List<Storage>();
            StorageNetworkContentIndexService.FillSourceStorages(world, false, tags, excluded, source, result, false);
            return result;
        }
    }
    internal sealed class StorageTransferResult
    {
        public static readonly StorageTransferResult Idle = new StorageTransferResult(0f, null);
        public static readonly StorageTransferResult Offline = new StorageTransferResult(0f, null);
        public StorageTransferResult(float moved, string blocked) { MovedKg = moved; }
        public float MovedKg { get; }
    }
    internal static class StorageNetworkConstructionSupplyService
    {
        public static void ClearSolidOutputBufferMarker(GameObject item) { }
    }
    internal static class NetworkStorageTransferService
    {
        public static Action<Storage, GameObject> ReturnCallback;
        public static void TransferStoredItemToNetwork(Storage port, GameObject item, Storage[] excluded)
        {
            port.items.Remove(item);
            port.OnStorageChange?.Invoke(item);
            ReturnCallback?.Invoke(port, item);
        }
    }
}
