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
        private static int version;

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

        public static void EnsureSceneSeeded()
        {
            if (Storages.Count > 0 || Geysers.Count > 0 || Enrollments.Count > 0)
            {
                return;
            }

            foreach (Storage storage in Object.FindObjectsByType<Storage>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (storage != null)
                {
                    Storages.Add(storage);
                    StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        Enrollments.Add(enrollment);
                    }
                }
            }

            foreach (Geyser geyser in Object.FindObjectsByType<Geyser>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (geyser != null)
                {
                    Geysers.Add(geyser);
                    StorageNetworkEnrollment enrollment = geyser.GetComponent<StorageNetworkEnrollment>();
                    if (enrollment != null)
                    {
                        Enrollments.Add(enrollment);
                    }
                }
            }

            Invalidate();
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
            if (changed)
            {
                Invalidate();
            }
        }
    }
}
