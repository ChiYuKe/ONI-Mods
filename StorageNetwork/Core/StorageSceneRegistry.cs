using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageSceneRegistry
    {
        private static readonly HashSet<Storage> Storages = new HashSet<Storage>();
        private static readonly HashSet<Storage> ExplicitlyRegisteredStorages = new HashSet<Storage>();
        private static readonly HashSet<Storage> CollectableStorages = new HashSet<Storage>();
        private static readonly Dictionary<int, Storage> StoragesByPrefabInstanceId = new Dictionary<int, Storage>();
        private static readonly Dictionary<int, HashSet<Storage>> StoragesByWorld = new Dictionary<int, HashSet<Storage>>();
        private static readonly Dictionary<int, HashSet<Storage>> CollectableStoragesByWorld = new Dictionary<int, HashSet<Storage>>();
        private static readonly HashSet<Geyser> Geysers = new HashSet<Geyser>();
        private static readonly Dictionary<int, HashSet<Geyser>> GeysersByWorld = new Dictionary<int, HashSet<Geyser>>();
        private static readonly HashSet<StorageNetworkEnrollment> Enrollments = new HashSet<StorageNetworkEnrollment>();
        private static readonly HashSet<StorageNetworkCore> Cores = new HashSet<StorageNetworkCore>();
        private static readonly Dictionary<int, HashSet<StorageNetworkCore>> CoresByWorld = new Dictionary<int, HashSet<StorageNetworkCore>>();
        private static readonly HashSet<StorageNetworkRelayModule> Relays = new HashSet<StorageNetworkRelayModule>();
        private static readonly HashSet<StorageNetworkPowerStorage> PowerStorages = new HashSet<StorageNetworkPowerStorage>();
        private static readonly HashSet<Component> AuditedComponents = new HashSet<Component>();
        private static readonly List<Component> DeadAuditedComponents = new List<Component>();
        private static readonly System.Diagnostics.Stopwatch PruneStopwatch =
            new System.Diagnostics.Stopwatch();
        private static readonly Dictionary<int, CoreOnlineCacheEntry> OnlineCoreCache = new Dictionary<int, CoreOnlineCacheEntry>();
        private static readonly HashSet<Storage> EmptyStorages = new HashSet<Storage>();
        private static readonly HashSet<Geyser> EmptyGeysers = new HashSet<Geyser>();
        private static readonly HashSet<StorageNetworkCore> EmptyCores = new HashSet<StorageNetworkCore>();
        private static int version;
        private static int topologyVersion;
        private static int membershipVersion;
        private static int capabilityVersion;
        private static int connectivityVersion;
        private static bool sceneSeeded;
        private static bool worldDirectoriesDirty;
        private static bool collectableCatalogDirty;
        private static int lastPruneFrame = -1;
        private static float lastPruneAt = -1f;
        private static bool pruneAuditInProgress;
        private static int pruneAuditTopologyVersion = -1;
        private static HashSet<Component>.Enumerator pruneAuditEnumerator;
        private const float PruneIntervalSeconds = 30f;
        private const double PruneBudgetMilliseconds = 0.25d;

        public static int Version => version;

        internal static int TopologyVersion => topologyVersion;

        internal static int MembershipVersion => membershipVersion;

        internal static int CapabilityVersion => capabilityVersion;

        internal static int ConnectivityVersion => connectivityVersion;

        public static void ResetRuntimeState()
        {
            StorageNetworkParticleStorageService.Reset();
            StorageNetworkContentIndexService.ResetRuntimeState();
            StorageNetworkRuntimeCatalog.ResetRuntimeState();
            Storages.Clear();
            ExplicitlyRegisteredStorages.Clear();
            CollectableStorages.Clear();
            StoragesByPrefabInstanceId.Clear();
            StoragesByWorld.Clear();
            CollectableStoragesByWorld.Clear();
            Geysers.Clear();
            GeysersByWorld.Clear();
            Enrollments.Clear();
            Cores.Clear();
            CoresByWorld.Clear();
            Relays.Clear();
            PowerStorages.Clear();
            AuditedComponents.Clear();
            DeadAuditedComponents.Clear();
            PruneStopwatch.Reset();
            OnlineCoreCache.Clear();
            sceneSeeded = false;
            worldDirectoriesDirty = false;
            collectableCatalogDirty = false;
            lastPruneFrame = -1;
            lastPruneAt = -1f;
            pruneAuditInProgress = false;
            pruneAuditTopologyVersion = -1;
            pruneAuditEnumerator = default;
            InvalidateTopology(
                membershipChanged: true,
                capabilityChanged: true,
                connectivityChanged: true);
        }

        public static void Register(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            StorageNetworkInterfaceResolver.Invalidate(gameObject);
            bool changed = false;
            bool membershipChanged = false;
            bool capabilityChanged = false;
            bool connectivityChanged = false;
            Storage storage = gameObject.GetComponent<Storage>();
            if (storage != null)
            {
                bool storageAdded = Storages.Add(storage);
                changed |= storageAdded;
                bool explicitStorageAdded = ExplicitlyRegisteredStorages.Add(storage);
                changed |= explicitStorageAdded;
                membershipChanged |= storageAdded || explicitStorageAdded;
                capabilityChanged |= storageAdded || explicitStorageAdded;
                AuditedComponents.Add(storage);
                AddStorageLookup(storage);
                if (storageAdded)
                {
                    AddByWorld(StoragesByWorld, GetWorldId(storage), storage);
                }

                StorageNetworkRuntimeCatalog.Register(storage);
                StorageNetworkContentIndexService.Register(storage);
                RefreshCollectableStorage(storage);
            }

            Geyser geyser = gameObject.GetComponent<Geyser>();
            if (geyser != null)
            {
                bool geyserAdded = Geysers.Add(geyser);
                changed |= geyserAdded;
                membershipChanged |= geyserAdded;
                AuditedComponents.Add(geyser);
                if (geyserAdded)
                {
                    AddByWorld(GeysersByWorld, GetWorldId(geyser), geyser);
                }
            }

            StorageNetworkEnrollment enrollment = gameObject.GetComponent<StorageNetworkEnrollment>();
            if (enrollment != null)
            {
                bool enrollmentAdded = Enrollments.Add(enrollment);
                changed |= enrollmentAdded;
                capabilityChanged |= enrollmentAdded;
                AuditedComponents.Add(enrollment);
            }

            StorageNetworkCore core = gameObject.GetComponent<StorageNetworkCore>();
            if (core != null)
            {
                bool coreAdded = Cores.Add(core);
                changed |= coreAdded;
                connectivityChanged |= coreAdded;
                AuditedComponents.Add(core);
                if (coreAdded)
                {
                    AddByWorld(CoresByWorld, GetWorldId(core), core);
                }
            }

            StorageNetworkRelayModule relay = gameObject.GetComponent<StorageNetworkRelayModule>();
            if (relay != null)
            {
                bool relayAdded = Relays.Add(relay);
                changed |= relayAdded;
                connectivityChanged |= relayAdded;
                AuditedComponents.Add(relay);
            }

            StorageNetworkPowerStorage powerStorage = gameObject.GetComponent<StorageNetworkPowerStorage>();
            if (powerStorage != null)
            {
                bool powerStorageAdded = PowerStorages.Add(powerStorage);
                changed |= powerStorageAdded;
                capabilityChanged |= powerStorageAdded;
                AuditedComponents.Add(powerStorage);
            }

            if (changed)
            {
                InvalidateTopology(
                    membershipChanged,
                    capabilityChanged,
                    connectivityChanged);
            }
        }

        public static void Unregister(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            StorageNetworkInterfaceResolver.Invalidate(gameObject);
            bool changed = false;
            bool membershipChanged = false;
            bool capabilityChanged = false;
            bool connectivityChanged = false;
            Storage storage = gameObject.GetComponent<Storage>();
            if (storage != null)
            {
                StorageNetworkContentIndexService.Unregister(storage);
                StorageNetworkRuntimeCatalog.Unregister(storage);
                AuditedComponents.Remove(storage);
                bool storageRemoved = Storages.Remove(storage);
                bool explicitStorageRemoved = ExplicitlyRegisteredStorages.Remove(storage);
                changed |= storageRemoved || explicitStorageRemoved;
                membershipChanged |= storageRemoved || explicitStorageRemoved;
                capabilityChanged |= storageRemoved || explicitStorageRemoved;
                CollectableStorages.Remove(storage);
                RemoveByWorld(StoragesByWorld, GetWorldId(storage), storage);
                RemoveByWorld(CollectableStoragesByWorld, GetWorldId(storage), storage);
                RemoveStorageLookup(storage);
            }

            Geyser geyser = gameObject.GetComponent<Geyser>();
            if (geyser != null)
            {
                AuditedComponents.Remove(geyser);
                bool geyserRemoved = Geysers.Remove(geyser);
                changed |= geyserRemoved;
                membershipChanged |= geyserRemoved;
                RemoveByWorld(GeysersByWorld, GetWorldId(geyser), geyser);
            }

            StorageNetworkEnrollment enrollment = gameObject.GetComponent<StorageNetworkEnrollment>();
            if (enrollment != null)
            {
                AuditedComponents.Remove(enrollment);
                bool enrollmentRemoved = Enrollments.Remove(enrollment);
                changed |= enrollmentRemoved;
                capabilityChanged |= enrollmentRemoved;
            }

            StorageNetworkCore core = gameObject.GetComponent<StorageNetworkCore>();
            if (core != null)
            {
                AuditedComponents.Remove(core);
                bool coreRemoved = Cores.Remove(core);
                changed |= coreRemoved;
                connectivityChanged |= coreRemoved;
                RemoveByWorld(CoresByWorld, GetWorldId(core), core);
            }

            StorageNetworkRelayModule relay = gameObject.GetComponent<StorageNetworkRelayModule>();
            if (relay != null)
            {
                AuditedComponents.Remove(relay);
                bool relayRemoved = Relays.Remove(relay);
                changed |= relayRemoved;
                connectivityChanged |= relayRemoved;
            }

            StorageNetworkPowerStorage powerStorage = gameObject.GetComponent<StorageNetworkPowerStorage>();
            if (powerStorage != null)
            {
                AuditedComponents.Remove(powerStorage);
                bool powerStorageRemoved = PowerStorages.Remove(powerStorage);
                changed |= powerStorageRemoved;
                capabilityChanged |= powerStorageRemoved;
            }

            if (changed)
            {
                InvalidateTopology(
                    membershipChanged,
                    capabilityChanged,
                    connectivityChanged);
            }
        }

        public static IReadOnlyCollection<Storage> GetStorages()
        {
            PruneDeadEntriesThrottled();
            return Storages;
        }

        internal static bool IsExplicitlyRegisteredStorage(Storage storage)
        {
            return storage != null && ExplicitlyRegisteredStorages.Contains(storage);
        }

        internal static IReadOnlyCollection<Storage> GetCollectableStoragesForWorld(
            int worldId,
            bool includeReachableWorlds)
        {
            PruneDeadEntriesThrottled();
            EnsureCollectableCatalogCurrent();
            if (worldId < 0 || includeReachableWorlds)
            {
                return CollectableStorages;
            }

            return CollectableStoragesByWorld.TryGetValue(worldId, out HashSet<Storage> storages)
                ? storages
                : EmptyStorages;
        }

        internal static bool TryGetStorage(int prefabInstanceId, out Storage storage)
        {
            storage = null;
            if (prefabInstanceId == KPrefabID.InvalidInstanceID)
            {
                return false;
            }

            PruneDeadEntriesThrottled();
            if (StoragesByPrefabInstanceId.TryGetValue(prefabInstanceId, out storage) && IsLive(storage))
            {
                return true;
            }

            if (!ReferenceEquals(storage, null))
            {
                StoragesByPrefabInstanceId.Remove(prefabInstanceId);
            }

            storage = null;
            return false;
        }

        internal static bool TryGetReachableStorage(int prefabInstanceId, int worldId, out Storage storage)
        {
            if (!TryGetStorage(prefabInstanceId, out storage))
            {
                return false;
            }

            if (worldId < 0 || storage.gameObject.GetMyWorldId() == worldId || IsCrossPlanetRelayOnline())
            {
                return true;
            }

            storage = null;
            return false;
        }

        public static IReadOnlyCollection<Geyser> GetGeysers()
        {
            PruneDeadEntriesThrottled();
            return Geysers;
        }

        internal static IReadOnlyCollection<Geyser> GetGeysersForWorld(int worldId, bool includeReachableWorlds)
        {
            PruneDeadEntriesThrottled();
            EnsureWorldDirectoriesCurrent();
            if (worldId < 0 || includeReachableWorlds)
            {
                return Geysers;
            }

            return GeysersByWorld.TryGetValue(worldId, out HashSet<Geyser> geysers)
                ? geysers
                : EmptyGeysers;
        }

        public static IReadOnlyCollection<StorageNetworkEnrollment> GetEnrollments()
        {
            PruneDeadEntriesThrottled();
            return Enrollments;
        }

        public static IReadOnlyCollection<StorageNetworkPowerStorage> GetPowerStorages()
        {
            PruneDeadEntriesThrottled();
            return PowerStorages;
        }

        public static IReadOnlyCollection<StorageNetworkCore> GetCores()
        {
            PruneDeadEntriesThrottled();
            return Cores;
        }

        public static bool IsLive(Component component)
        {
            if (component == null)
            {
                return false;
            }

            try
            {
                return component.gameObject != null;
            }
            catch (System.NullReferenceException)
            {
                return false;
            }
        }

        public static bool HasOnlineCoreInActiveWorld()
        {
            return HasOnlineCoreInActiveWorld(out _);
        }

        public static bool HasOnlineCoreInActiveWorld(out bool crossPlanetRelayOnline)
        {
            crossPlanetRelayOnline = HasRelayInSpace();
            int activeWorldId = ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : -1;
            return HasOnlineCoreInWorld(activeWorldId);
        }

        public static bool HasOnlineCoreInWorld(int worldId)
        {
            EnsureWorldDirectoriesCurrent();
            int frame = Time.frameCount;
            if (OnlineCoreCache.TryGetValue(worldId, out CoreOnlineCacheEntry cached) &&
                cached.Frame == frame &&
                cached.ConnectivityVersion == connectivityVersion)
            {
                return cached.Online;
            }

            IReadOnlyCollection<StorageNetworkCore> cores = worldId < 0
                ? Cores
                : CoresByWorld.TryGetValue(worldId, out HashSet<StorageNetworkCore> worldCores)
                    ? worldCores
                    : EmptyCores;
            foreach (StorageNetworkCore core in cores)
            {
                if (!IsLive(core))
                {
                    continue;
                }

                if (core.IsNetworkOnline)
                {
                    OnlineCoreCache[worldId] = new CoreOnlineCacheEntry(true, frame, connectivityVersion);
                    return true;
                }
            }

            OnlineCoreCache[worldId] = new CoreOnlineCacheEntry(false, frame, connectivityVersion);
            return false;
        }

        public static bool IsCrossPlanetRelayOnline()
        {
            return HasRelayInSpace();
        }

        public static void EnsureSceneSeeded()
        {
            if (sceneSeeded)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            sceneSeeded = true;
            bool changed = false;

            foreach (Storage storage in Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (storage != null)
                {
                    bool storageAdded = Storages.Add(storage);
                    changed |= storageAdded;
                    AuditedComponents.Add(storage);
                    AddStorageLookup(storage);
                    if (storageAdded)
                    {
                        AddByWorld(StoragesByWorld, GetWorldId(storage), storage);
                    }

                    StorageNetworkRuntimeCatalog.Register(storage);
                    StorageNetworkContentIndexService.Register(storage);
                    RefreshCollectableStorage(storage);
                    StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        changed |= Enrollments.Add(enrollment);
                        AuditedComponents.Add(enrollment);
                    }
                }
            }

            foreach (Geyser geyser in Object.FindObjectsByType<Geyser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (geyser != null)
                {
                    bool geyserAdded = Geysers.Add(geyser);
                    changed |= geyserAdded;
                    AuditedComponents.Add(geyser);
                    if (geyserAdded)
                    {
                        AddByWorld(GeysersByWorld, GetWorldId(geyser), geyser);
                    }
                    StorageNetworkEnrollment enrollment = geyser.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        changed |= Enrollments.Add(enrollment);
                        AuditedComponents.Add(enrollment);
                    }
                }
            }

            foreach (StorageNetworkCore core in Object.FindObjectsByType<StorageNetworkCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (core != null)
                {
                    bool coreAdded = Cores.Add(core);
                    changed |= coreAdded;
                    AuditedComponents.Add(core);
                    if (coreAdded)
                    {
                        AddByWorld(CoresByWorld, GetWorldId(core), core);
                    }
                }
            }

            foreach (StorageNetworkRelayModule relay in Object.FindObjectsByType<StorageNetworkRelayModule>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (relay != null)
                {
                    changed |= Relays.Add(relay);
                    AuditedComponents.Add(relay);
                }
            }

            foreach (StorageNetworkPowerStorage powerStorage in Object.FindObjectsByType<StorageNetworkPowerStorage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (powerStorage != null)
                {
                    changed |= PowerStorages.Add(powerStorage);
                    AuditedComponents.Add(powerStorage);
                }
            }

            if (changed)
            {
                InvalidateTopology(
                    membershipChanged: true,
                    capabilityChanged: true,
                    connectivityChanged: true);
            }

            worldDirectoriesDirty = false;
            collectableCatalogDirty = false;
        }

        public static void Invalidate()
        {
            version++;
            topologyVersion++;
            membershipVersion++;
            capabilityVersion++;
            connectivityVersion++;
            worldDirectoriesDirty = true;
            collectableCatalogDirty = true;
            StorageNetworkRuntimeCatalog.InvalidateAllCapabilities();
            InvalidateDerivedCaches(invalidateContentIndexes: true, invalidateStorageFlags: true);
        }

        internal static void InvalidateConnectivity()
        {
            version++;
            connectivityVersion++;
            InvalidateDerivedCaches(invalidateContentIndexes: false, invalidateStorageFlags: false);
        }

        internal static void InvalidateCapabilities()
        {
            version++;
            capabilityVersion++;
            collectableCatalogDirty = true;
            StorageNetworkRuntimeCatalog.InvalidateAllCapabilities();
            StorageNetworkInterfaceResolver.InvalidateDynamicStorageFlags();
            StorageSceneCollector.InvalidateSnapshotCache();
        }

        internal static void InvalidateCapabilities(Storage storage)
        {
            version++;
            capabilityVersion++;
            if (storage != null)
            {
                StorageNetworkRuntimeCatalog.Register(storage);
                RefreshCollectableStorage(storage);
            }

            StorageSceneCollector.InvalidateSnapshotCache();
            StorageNetworkContentIndexService.AcceptRegistryVersions(
                membershipVersion,
                capabilityVersion);
        }

        internal static void InvalidateMembership(Storage storage)
        {
            version++;
            membershipVersion++;
            capabilityVersion++;
            if (storage != null)
            {
                StorageNetworkRuntimeCatalog.Register(storage);
                RefreshCollectableStorage(storage);
                if (StorageNetworkMembership.IsCollectableStorage(storage) &&
                    StorageNetworkStorageRules.IsServerStorage(storage))
                {
                    StorageNetworkContentIndexService.Register(storage);
                    StorageNetworkContentIndexService.Invalidate(storage);
                }
                else
                {
                    StorageNetworkContentIndexService.Unregister(storage);
                }
            }

            StorageSceneCollector.InvalidateCache();
            StorageNetworkContentIndexService.AcceptRegistryVersions(
                membershipVersion,
                capabilityVersion);
        }

        private static void InvalidateTopology(
            bool membershipChanged,
            bool capabilityChanged,
            bool connectivityChanged)
        {
            version++;
            topologyVersion++;
            if (membershipChanged)
            {
                membershipVersion++;
            }

            if (capabilityChanged)
            {
                capabilityVersion++;
            }

            if (connectivityChanged)
            {
                connectivityVersion++;
            }

            // Authoritative lifecycle callbacks refresh only the changed descriptor.
            // A global third-party invalidation uses Invalidate()/InvalidateCapabilities()
            // above and advances the catalog-wide capability generation explicitly.
            InvalidateDerivedCaches(invalidateContentIndexes: false, invalidateStorageFlags: false);
            StorageNetworkContentIndexService.AcceptRegistryVersions(
                membershipVersion,
                capabilityVersion);
        }

        private static void InvalidateDerivedCaches(
            bool invalidateContentIndexes,
            bool invalidateStorageFlags)
        {
            OnlineCoreCache.Clear();
            if (invalidateStorageFlags)
            {
                StorageNetworkInterfaceResolver.InvalidateDynamicStorageFlags();
            }

            if (invalidateContentIndexes)
            {
                StorageSceneCollector.InvalidateCache();
            }
            else
            {
                StorageSceneCollector.InvalidateSnapshotCache();
            }
        }

        private static void PruneDeadEntriesThrottled()
        {
            if (lastPruneFrame == Time.frameCount)
            {
                return;
            }

            if (!pruneAuditInProgress &&
                lastPruneAt >= 0f &&
                Time.unscaledTime - lastPruneAt < PruneIntervalSeconds)
            {
                return;
            }

            lastPruneFrame = Time.frameCount;
            if (!pruneAuditInProgress)
            {
                StartPruneAudit();
            }

            ContinuePruneAudit();
        }

        private static void StartPruneAudit()
        {
            lastPruneAt = Time.unscaledTime;
            pruneAuditTopologyVersion = topologyVersion;
            DeadAuditedComponents.Clear();
            pruneAuditEnumerator = AuditedComponents.GetEnumerator();
            pruneAuditInProgress = true;
        }

        private static void ContinuePruneAudit()
        {
            if (!pruneAuditInProgress)
            {
                return;
            }

            // Registration or cleanup invalidates HashSet enumerators. Normal lifecycle
            // registration is authoritative, so abort this safety audit and retry later.
            if (pruneAuditTopologyVersion != topologyVersion)
            {
                CancelPruneAudit();
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            PruneStopwatch.Restart();
            while (PruneStopwatch.Elapsed.TotalMilliseconds < PruneBudgetMilliseconds)
            {
                if (!pruneAuditEnumerator.MoveNext())
                {
                    PruneStopwatch.Stop();
                    CompletePruneAudit();
                    return;
                }

                Component component = pruneAuditEnumerator.Current;
                if (!IsLive(component))
                {
                    DeadAuditedComponents.Add(component);
                }
            }

            PruneStopwatch.Stop();
        }

        private static void CompletePruneAudit()
        {
            pruneAuditEnumerator.Dispose();
            pruneAuditEnumerator = default;
            pruneAuditInProgress = false;
            pruneAuditTopologyVersion = -1;
            bool changed = false;
            bool membershipChanged = false;
            bool capabilityChanged = false;
            bool connectivityChanged = false;
            foreach (Component component in DeadAuditedComponents)
            {
                AuditedComponents.Remove(component);
                if (component is Storage storage)
                {
                    StorageNetworkContentIndexService.Unregister(storage);
                    StorageNetworkRuntimeCatalog.Unregister(storage);
                    bool storageRemoved = Storages.Remove(storage);
                    bool explicitStorageRemoved = ExplicitlyRegisteredStorages.Remove(storage);
                    changed |= storageRemoved || explicitStorageRemoved;
                    membershipChanged |= storageRemoved || explicitStorageRemoved;
                    capabilityChanged |= storageRemoved || explicitStorageRemoved;
                    CollectableStorages.Remove(storage);
                    RemoveFromAllWorlds(StoragesByWorld, storage);
                    RemoveFromAllWorlds(CollectableStoragesByWorld, storage);
                }
                else if (component is Geyser geyser)
                {
                    bool geyserRemoved = Geysers.Remove(geyser);
                    changed |= geyserRemoved;
                    membershipChanged |= geyserRemoved;
                    RemoveFromAllWorlds(GeysersByWorld, geyser);
                }
                else if (component is StorageNetworkEnrollment enrollment)
                {
                    bool enrollmentRemoved = Enrollments.Remove(enrollment);
                    changed |= enrollmentRemoved;
                    capabilityChanged |= enrollmentRemoved;
                }
                else if (component is StorageNetworkCore core)
                {
                    bool coreRemoved = Cores.Remove(core);
                    changed |= coreRemoved;
                    connectivityChanged |= coreRemoved;
                    RemoveFromAllWorlds(CoresByWorld, core);
                }
                else if (component is StorageNetworkRelayModule relay)
                {
                    StorageNetworkRocketRelayService.Unregister(relay);
                    bool relayRemoved = Relays.Remove(relay);
                    changed |= relayRemoved;
                    connectivityChanged |= relayRemoved;
                }
                else if (component is StorageNetworkPowerStorage powerStorage)
                {
                    bool powerStorageRemoved = PowerStorages.Remove(powerStorage);
                    changed |= powerStorageRemoved;
                    capabilityChanged |= powerStorageRemoved;
                }
            }

            DeadAuditedComponents.Clear();
            if (changed)
            {
                RebuildStorageLookup();
                InvalidateTopology(
                    membershipChanged,
                    capabilityChanged,
                    connectivityChanged);
            }
        }

        private static void CancelPruneAudit()
        {
            pruneAuditEnumerator.Dispose();
            pruneAuditEnumerator = default;
            pruneAuditInProgress = false;
            pruneAuditTopologyVersion = -1;
            DeadAuditedComponents.Clear();
            PruneStopwatch.Reset();
        }

        private static void EnsureWorldDirectoriesCurrent()
        {
            if (!worldDirectoriesDirty)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            StoragesByWorld.Clear();
            GeysersByWorld.Clear();
            CoresByWorld.Clear();
            foreach (Storage storage in Storages)
            {
                if (IsLive(storage))
                {
                    AddByWorld(StoragesByWorld, storage.gameObject.GetMyWorldId(), storage);
                }
            }

            foreach (Geyser geyser in Geysers)
            {
                if (IsLive(geyser))
                {
                    AddByWorld(GeysersByWorld, geyser.gameObject.GetMyWorldId(), geyser);
                }
            }

            foreach (StorageNetworkCore core in Cores)
            {
                if (IsLive(core))
                {
                    AddByWorld(CoresByWorld, core.gameObject.GetMyWorldId(), core);
                }
            }

            worldDirectoriesDirty = false;
        }

        private static void EnsureCollectableCatalogCurrent()
        {
            EnsureWorldDirectoriesCurrent();
            if (!collectableCatalogDirty)
            {
                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Registry);
            CollectableStorages.Clear();
            CollectableStoragesByWorld.Clear();
            foreach (KeyValuePair<int, HashSet<Storage>> world in StoragesByWorld)
            {
                foreach (Storage storage in world.Value)
                {
                    if (!StorageNetworkMembership.IsCollectableStorage(storage))
                    {
                        continue;
                    }

                    CollectableStorages.Add(storage);
                    AddByWorld(CollectableStoragesByWorld, world.Key, storage);
                }
            }

            collectableCatalogDirty = false;
        }

        private static void RefreshCollectableStorage(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            int worldId = GetWorldId(storage);
            if (StorageNetworkMembership.IsCollectableStorage(storage))
            {
                CollectableStorages.Add(storage);
                AddByWorld(CollectableStoragesByWorld, worldId, storage);
            }
            else
            {
                CollectableStorages.Remove(storage);
                RemoveByWorld(CollectableStoragesByWorld, worldId, storage);
            }
        }

        private static void AddByWorld<T>(Dictionary<int, HashSet<T>> directory, int worldId, T value)
        {
            if (!directory.TryGetValue(worldId, out HashSet<T> values))
            {
                values = new HashSet<T>();
                directory.Add(worldId, values);
            }

            values.Add(value);
        }

        private static void RemoveByWorld<T>(
            Dictionary<int, HashSet<T>> directory,
            int worldId,
            T value)
        {
            if (!directory.TryGetValue(worldId, out HashSet<T> values))
            {
                return;
            }

            values.Remove(value);
            if (values.Count == 0)
            {
                directory.Remove(worldId);
            }
        }

        private static void RemoveFromAllWorlds<T>(
            Dictionary<int, HashSet<T>> directory,
            T value)
        {
            List<int> emptyWorlds = null;
            foreach (KeyValuePair<int, HashSet<T>> world in directory)
            {
                if (world.Value.Remove(value) && world.Value.Count == 0)
                {
                    emptyWorlds ??= new List<int>();
                    emptyWorlds.Add(world.Key);
                }
            }

            if (emptyWorlds == null)
            {
                return;
            }

            foreach (int worldId in emptyWorlds)
            {
                directory.Remove(worldId);
            }
        }

        private static int GetWorldId(Component component)
        {
            return component != null && component.gameObject != null
                ? component.gameObject.GetMyWorldId()
                : -1;
        }

        private static void AddStorageLookup(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            if (prefabId != null && prefabId.InstanceID != KPrefabID.InvalidInstanceID)
            {
                StoragesByPrefabInstanceId[prefabId.InstanceID] = storage;
            }
        }

        private static void RemoveStorageLookup(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            if (prefabId != null &&
                StoragesByPrefabInstanceId.TryGetValue(prefabId.InstanceID, out Storage registered) &&
                registered == storage)
            {
                StoragesByPrefabInstanceId.Remove(prefabId.InstanceID);
            }
        }

        private static void RebuildStorageLookup()
        {
            StoragesByPrefabInstanceId.Clear();
            foreach (Storage storage in Storages)
            {
                AddStorageLookup(storage);
            }
        }

        private static bool HasRelayInSpace()
        {
            return StorageNetworkRocketRelayService.HasRelayInSpace();
        }

        private readonly struct CoreOnlineCacheEntry
        {
            public CoreOnlineCacheEntry(bool online, int frame, int connectivityVersion)
            {
                Online = online;
                Frame = frame;
                ConnectivityVersion = connectivityVersion;
            }

            public bool Online { get; }

            public int Frame { get; }

            public int ConnectivityVersion { get; }
        }
    }
}
