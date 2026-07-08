using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageSceneRegistry
    {
        private static readonly HashSet<Storage> Storages = new HashSet<Storage>();
        private static readonly HashSet<Geyser> Geysers = new HashSet<Geyser>();
        private static readonly HashSet<StorageNetworkEnrollment> Enrollments = new HashSet<StorageNetworkEnrollment>();
        private static readonly HashSet<StorageNetworkCore> Cores = new HashSet<StorageNetworkCore>();
        private static readonly HashSet<StorageNetworkRelayModule> Relays = new HashSet<StorageNetworkRelayModule>();
        private static readonly HashSet<StorageNetworkPowerStorage> PowerStorages = new HashSet<StorageNetworkPowerStorage>();
        private static readonly Dictionary<int, CoreOnlineCacheEntry> OnlineCoreCache = new Dictionary<int, CoreOnlineCacheEntry>();
        private static int version;
        private static bool sceneSeeded;
        private static int lastPruneFrame = -1;
        private static float lastPruneAt = -1f;
        private const float PruneIntervalSeconds = 1f;

        public static int Version => version;

        public static void ResetRuntimeState()
        {
            StorageNetworkParticleStorageService.Reset();
            Storages.Clear();
            Geysers.Clear();
            Enrollments.Clear();
            Cores.Clear();
            Relays.Clear();
            PowerStorages.Clear();
            OnlineCoreCache.Clear();
            sceneSeeded = false;
            lastPruneFrame = -1;
            lastPruneAt = -1f;
            Invalidate();
        }

        public static void Register(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            bool changed = false;
            Storage storage = gameObject.GetComponent<Storage>();
            if (storage != null)
            {
                changed |= Storages.Add(storage);
            }

            Geyser geyser = gameObject.GetComponent<Geyser>();
            if (geyser != null)
            {
                changed |= Geysers.Add(geyser);
            }

            StorageNetworkEnrollment enrollment = gameObject.GetComponent<StorageNetworkEnrollment>();
            if (enrollment != null)
            {
                changed |= Enrollments.Add(enrollment);
            }

            StorageNetworkCore core = gameObject.GetComponent<StorageNetworkCore>();
            if (core != null)
            {
                changed |= Cores.Add(core);
            }

            StorageNetworkRelayModule relay = gameObject.GetComponent<StorageNetworkRelayModule>();
            if (relay != null)
            {
                changed |= Relays.Add(relay);
            }

            StorageNetworkPowerStorage powerStorage = gameObject.GetComponent<StorageNetworkPowerStorage>();
            if (powerStorage != null)
            {
                changed |= PowerStorages.Add(powerStorage);
            }

            if (changed)
            {
                Invalidate();
            }
        }

        public static void Unregister(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            bool changed = false;
            Storage storage = gameObject.GetComponent<Storage>();
            if (storage != null)
            {
                changed |= Storages.Remove(storage);
            }

            Geyser geyser = gameObject.GetComponent<Geyser>();
            if (geyser != null)
            {
                changed |= Geysers.Remove(geyser);
            }

            StorageNetworkEnrollment enrollment = gameObject.GetComponent<StorageNetworkEnrollment>();
            if (enrollment != null)
            {
                changed |= Enrollments.Remove(enrollment);
            }

            StorageNetworkCore core = gameObject.GetComponent<StorageNetworkCore>();
            if (core != null)
            {
                changed |= Cores.Remove(core);
            }

            StorageNetworkRelayModule relay = gameObject.GetComponent<StorageNetworkRelayModule>();
            if (relay != null)
            {
                changed |= Relays.Remove(relay);
            }

            StorageNetworkPowerStorage powerStorage = gameObject.GetComponent<StorageNetworkPowerStorage>();
            if (powerStorage != null)
            {
                changed |= PowerStorages.Remove(powerStorage);
            }

            if (changed)
            {
                Invalidate();
            }
        }

        public static IReadOnlyCollection<Storage> GetStorages()
        {
            PruneDeadEntriesThrottled();
            return Storages;
        }

        public static IReadOnlyCollection<Geyser> GetGeysers()
        {
            PruneDeadEntriesThrottled();
            return Geysers;
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
            int frame = Time.frameCount;
            if (OnlineCoreCache.TryGetValue(worldId, out CoreOnlineCacheEntry cached) &&
                cached.Frame == frame &&
                cached.RegistryVersion == version)
            {
                return cached.Online;
            }

            foreach (StorageNetworkCore core in Cores)
            {
                if (!IsLive(core))
                {
                    continue;
                }

                if (worldId >= 0 && core.gameObject.GetMyWorldId() != worldId)
                {
                    continue;
                }

                Operational operational = core.GetComponent<Operational>();
                if (operational != null && operational.IsOperational)
                {
                    OnlineCoreCache[worldId] = new CoreOnlineCacheEntry(true, frame, version);
                    return true;
                }
            }

            OnlineCoreCache[worldId] = new CoreOnlineCacheEntry(false, frame, version);
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

            sceneSeeded = true;
            bool changed = false;

            foreach (Storage storage in Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (storage != null)
                {
                    changed |= Storages.Add(storage);
                    StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        changed |= Enrollments.Add(enrollment);
                    }
                }
            }

            foreach (Geyser geyser in Object.FindObjectsByType<Geyser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (geyser != null)
                {
                    changed |= Geysers.Add(geyser);
                    StorageNetworkEnrollment enrollment = geyser.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        changed |= Enrollments.Add(enrollment);
                    }
                }
            }

            foreach (StorageNetworkCore core in Object.FindObjectsByType<StorageNetworkCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (core != null)
                {
                    changed |= Cores.Add(core);
                }
            }

            foreach (StorageNetworkRelayModule relay in Object.FindObjectsByType<StorageNetworkRelayModule>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (relay != null)
                {
                    changed |= Relays.Add(relay);
                }
            }

            foreach (StorageNetworkPowerStorage powerStorage in Object.FindObjectsByType<StorageNetworkPowerStorage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (powerStorage != null)
                {
                    changed |= PowerStorages.Add(powerStorage);
                }
            }

            if (changed)
            {
                Invalidate();
            }
        }

        public static void Invalidate()
        {
            version++;
            OnlineCoreCache.Clear();
            StorageSceneCollector.InvalidateCache();
        }

        private static void PruneDeadEntriesThrottled()
        {
            if (lastPruneFrame == Time.frameCount)
            {
                return;
            }

            if (lastPruneAt >= 0f && Time.unscaledTime - lastPruneAt < PruneIntervalSeconds)
            {
                return;
            }

            lastPruneFrame = Time.frameCount;
            lastPruneAt = Time.unscaledTime;
            PruneDeadEntries();
        }

        private static void PruneDeadEntries()
        {
            bool changed = Storages.RemoveWhere(storage => !IsLive(storage)) > 0;
            changed |= Geysers.RemoveWhere(geyser => !IsLive(geyser)) > 0;
            changed |= Enrollments.RemoveWhere(enrollment => !IsLive(enrollment)) > 0;
            changed |= Cores.RemoveWhere(core => !IsLive(core)) > 0;
            changed |= Relays.RemoveWhere(relay => !IsLive(relay)) > 0;
            changed |= PowerStorages.RemoveWhere(powerStorage => !IsLive(powerStorage)) > 0;
            if (changed)
            {
                Invalidate();
            }
        }

        private static bool HasRelayInSpace()
        {
            foreach (StorageNetworkRelayModule relay in Relays)
            {
                if (IsLive(relay) && relay.IsInSpace())
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct CoreOnlineCacheEntry
        {
            public CoreOnlineCacheEntry(bool online, int frame, int registryVersion)
            {
                Online = online;
                Frame = frame;
                RegistryVersion = registryVersion;
            }

            public bool Online { get; }

            public int Frame { get; }

            public int RegistryVersion { get; }
        }
    }
}
