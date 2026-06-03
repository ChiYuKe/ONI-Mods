using System.Collections.Generic;
using StorageNetwork.Components;
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
        private static int version;
        private static bool sceneSeeded;

        public static int Version => version;

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

            if (changed)
            {
                Invalidate();
            }
        }

        public static IReadOnlyCollection<Storage> GetStorages()
        {
            PruneDeadEntries();
            return Storages;
        }

        public static IReadOnlyCollection<Geyser> GetGeysers()
        {
            PruneDeadEntries();
            return Geysers;
        }

        public static IReadOnlyCollection<StorageNetworkEnrollment> GetEnrollments()
        {
            PruneDeadEntries();
            return Enrollments;
        }

        public static bool HasOnlineCoreInActiveWorld()
        {
            return HasOnlineCoreInActiveWorld(out _);
        }

        public static bool HasOnlineCoreInActiveWorld(out bool crossPlanetRelayOnline)
        {
            PruneDeadEntries();
            crossPlanetRelayOnline = HasRelayInSpace();
            int activeWorldId = ClusterManager.Instance != null ? ClusterManager.Instance.activeWorldId : -1;
            return HasOnlineCoreInWorld(activeWorldId);
        }

        public static bool HasOnlineCoreInWorld(int worldId)
        {
            PruneDeadEntries();
            foreach (StorageNetworkCore core in Cores)
            {
                if (core == null || core.gameObject == null)
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
                    return true;
                }
            }

            return false;
        }

        public static bool IsCrossPlanetRelayOnline()
        {
            PruneDeadEntries();
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

            if (changed)
            {
                Invalidate();
            }
        }

        public static void Invalidate()
        {
            version++;
            StorageSceneCollector.InvalidateCache();
        }

        private static void PruneDeadEntries()
        {
            bool changed = Storages.RemoveWhere(storage => storage == null || storage.gameObject == null) > 0;
            changed |= Geysers.RemoveWhere(geyser => geyser == null || geyser.gameObject == null) > 0;
            changed |= Enrollments.RemoveWhere(enrollment => enrollment == null || enrollment.gameObject == null) > 0;
            changed |= Cores.RemoveWhere(core => core == null || core.gameObject == null) > 0;
            changed |= Relays.RemoveWhere(relay => relay == null || relay.gameObject == null) > 0;
            if (changed)
            {
                Invalidate();
            }
        }

        private static bool HasRelayInSpace()
        {
            foreach (StorageNetworkRelayModule relay in Relays)
            {
                if (relay != null && relay.gameObject != null && relay.IsInSpace())
                {
                    return true;
                }
            }

            return false;
        }
    }
}
