// Engine boundaries only. StorageNetworkContentIndexService itself is linked
// unchanged from production, including its transactions, buckets and dirty sets.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal readonly struct Tag : IEquatable<Tag>
{
    private readonly string value;
    public Tag(string value) { this.value = value; }
    public static Tag Invalid => default;
    public bool IsValid => value != null;
    public bool Equals(Tag other) => value == other.value;
    public override bool Equals(object other) => other is Tag tag && Equals(tag);
    public override int GetHashCode() => value?.GetHashCode() ?? 0;
    public int GetHash() => GetHashCode();
    public override string ToString() => value ?? "";
    public static bool operator ==(Tag a, Tag b) => a.Equals(b);
    public static bool operator !=(Tag a, Tag b) => !a.Equals(b);
}
internal enum SimHashes { Void, Water, Dirt, Oxygen }
internal static class PICKUPABLETUNING { public const float MINIMUM_PICKABLE_AMOUNT = 0.001f; }
internal static class GameTags
{
    public static readonly Tag Solid = new Tag("Solid"), Liquid = new Tag("Liquid"), Gas = new Tag("Gas");
}
internal sealed class Element
{
    public bool IsLiquid, IsGas;
}
internal static class ElementLoader
{
    public static Element FindElementByHash(SimHashes hash) => hash == SimHashes.Void ? null :
        new Element { IsLiquid = hash == SimHashes.Water, IsGas = hash == SimHashes.Oxygen };
}
internal sealed class PrimaryElement { public float Mass, Temperature = 300f; public SimHashes ElementID; }
internal sealed class Pickupable { public float TotalAmount; }
internal sealed class Edible { public string FoodID; public float Calories, Units; }
internal sealed class PlantableSeed { }
internal sealed class KPrefabID
{
    public const int InvalidInstanceID = -1;
    public int InstanceID;
    public GameObject gameObject;
    public readonly HashSet<Tag> Tags = new HashSet<Tag>();
    public bool HasTag(Tag tag) => Tags.Contains(tag);
    public bool HasAnyTags(Tag[] tags) => tags.Any(HasTag);
    public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
}
internal sealed partial class Storage
{
    public GameObject gameObject { get; }
    public string name => InstanceId.ToString();
    public T GetComponent<T>() where T : class => gameObject.GetComponent<T>();
    public readonly List<GameObject> items = new List<GameObject>();
    public Action<GameObject> OnStorageChange;
    public bool Live = true, Connected = true, Collectable = true, Server = true, CountsCapacity = true;
    public bool Minion, Production;
    public float CapacityKg = 10000f;
    public float Capacity() => CapacityKg;
    public float RemainingCapacity() => CapacityKg - MassStored();
}
namespace UnityEngine
{
    internal sealed partial class GameObject
    {
        public int WorldId;
        public StorageNetwork.Services.StorageItemUtility.StorageMatchTags MatchTags;
        private readonly Dictionary<Type, object> components = new Dictionary<Type, object>();
        public T GetComponent<T>() where T : class => components.TryGetValue(typeof(T), out var item) ? item as T : null;
        public void Set<T>(T component) where T : class => components[typeof(T)] = component;
    }
}
namespace StorageNetwork.Core
{
    internal static class StorageSceneRegistry
    {
        public static int MembershipVersion, CapabilityVersion;
        public static bool Relay;
        public static readonly HashSet<int> OfflineWorlds = new HashSet<int>();
        public static readonly List<Storage> Storages = new List<Storage>();
        public static void EnsureSceneSeeded() { }
        public static IEnumerable<Storage> GetStorages() => Storages;
        public static bool HasOnlineCoreInWorld(int id) => !OfflineWorlds.Contains(id);
        public static bool IsCrossPlanetRelayOnline() => Relay;
        public static bool IsLive(Storage storage) => storage != null && storage.Live;
    }
    internal static class StorageNetworkMembership
    {
        public static bool IsCollectableStorage(Storage s) => s.Collectable;
    }
    internal static class StorageNetworkStorageRules
    {
        public static bool IsServerStorage(Storage s) => s.Server;
        public static bool IsConnectedNetworkStorage(Storage s) => s.Connected;
        public static bool CountsTowardNetworkCapacity(Storage s) => s.CountsCapacity;
        public static bool IsMinionStorage(Storage s) => s.Minion;
        public static bool IsProductionStorage(Storage s) => s.Production;
    }
    internal static class StorageNetworkWorldUtility
    {
        public static readonly Dictionary<int, int> Parents = new Dictionary<int, int>();
        public static int GetParentWorldId(int id) => Parents.TryGetValue(id, out int parent) ? parent : id;
        public static IReadOnlyList<int> GetLandedRocketWorldIds(int id) => Parents.Where(p => p.Value == id).Select(p => p.Key).ToArray();
        public static bool HasLandedRocketsOrIsLandedRocket(int id) => Parents.ContainsKey(id) || Parents.ContainsValue(id);
        public static bool AreWorldsSameOrLanded(int a, int b) => a == b || GetParentWorldId(a) == GetParentWorldId(b);
    }
    internal enum StorageNetworkPerformanceArea { ContentIndex }
    internal static class StorageNetworkFrameProfileTool
    {
        public static Scope BeginWork(StorageNetworkPerformanceArea area) => default;
        internal readonly struct Scope : IDisposable { public void Dispose() { } }
    }
}
namespace StorageNetwork.Services
{
    internal static class StorageTargetSelector
    {
        public static int GetObjectWorldId(GameObject obj) => obj.WorldId;
        public static void InvalidateOutputTargetCache() { }
        public static void NotifyTransferMutation(Storage s, StorageItemUtility.StorageMatchTags tags, float amount, float mass) { }
    }
    internal static class StorageItemUtility
    {
        public static int GetStorageInstanceId(Storage storage) => storage.InstanceId;
        public static StorageMatchTags GetStorageMatchTagsNonAlloc(GameObject item) => item.MatchTags;
        internal struct StorageMatchTags
        {
            public Tag PrefabIdTag, PrefabTag, ElementTag, TransferTag;
            public bool Contains(Tag tag) => Matches(tag);
            public bool Matches(Tag tag) => tag == PrefabIdTag || tag == PrefabTag || tag == ElementTag || tag == TransferTag;
        }
    }
    internal readonly struct StorageNetworkInventoryMetrics
    {
        public StorageNetworkInventoryMetrics(bool online, float mass, float capacity)
        { NetworkOnline = online; TotalStoredKg = mass; TotalCapacityKg = capacity; }
        public bool NetworkOnline { get; }
        public float TotalStoredKg { get; }
        public float TotalCapacityKg { get; }
    }
    internal static class StorageNetworkContentShadowReader
    {
        public static float GetStorageAmount(Storage storage, Tag tag) => storage.items
            .Where(i => i.MatchTags.Matches(tag)).Sum(i => i.GetComponent<Pickupable>()?.TotalAmount ?? i.GetComponent<PrimaryElement>()?.Mass ?? 0f);
        public static float GetRemainingCapacity(Storage s) => s.RemainingCapacity();
        // Shadow mode isn't used here: these tests compare against their explicit
        // native fixtures. Fail loudly if a test accidentally enables it.
        public static float GetAmount(int world, bool related, Tag tag, Tag[] forbidden) => throw new NotSupportedException();
        public static StorageNetworkInventoryMetrics GetMetrics(int world, bool related) => throw new NotSupportedException();
        public static void FillSourceStorages(int world, bool related, IEnumerable<Tag> tags, HashSet<Storage> excluded,
            Storage source, List<Storage> output) => throw new NotSupportedException();
    }
}
