using System.Collections.Generic;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkParticleStorageService
    {
        private const float SnapshotLifetimeSeconds = 0.2f;

        private static readonly Dictionary<HighEnergyParticleStorage, ParticleStorageEntry> EntriesByParticleStorage =
            new Dictionary<HighEnergyParticleStorage, ParticleStorageEntry>();
        private static readonly Dictionary<Storage, ParticleStorageEntry> EntriesByStorage =
            new Dictionary<Storage, ParticleStorageEntry>();
        private static readonly Dictionary<int, ParticleStorageEntry> EntriesByInstanceId =
            new Dictionary<int, ParticleStorageEntry>();
        private static readonly Dictionary<int, WorldStorageBucket> BucketsByWorld =
            new Dictionary<int, WorldStorageBucket>();

        public static void Reset()
        {
            EntriesByParticleStorage.Clear();
            EntriesByStorage.Clear();
            EntriesByInstanceId.Clear();
            BucketsByWorld.Clear();
        }

        public static void Register(HighEnergyParticleStorage storage)
        {
            if (storage == null || storage.gameObject == null)
            {
                return;
            }

            if (EntriesByParticleStorage.TryGetValue(storage, out ParticleStorageEntry existing))
            {
                UnregisterEntry(existing);
            }

            Storage backingStorage = storage.GetComponent<Storage>();
            Operational operational = storage.GetComponent<Operational>();
            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            int instanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
            int worldId = storage.gameObject.GetMyWorldId();
            ParticleStorageEntry entry = new ParticleStorageEntry(
                storage,
                backingStorage,
                operational,
                instanceId,
                worldId);

            EntriesByParticleStorage[storage] = entry;
            if (backingStorage != null)
            {
                EntriesByStorage[backingStorage] = entry;
            }

            if (instanceId != KPrefabID.InvalidInstanceID)
            {
                EntriesByInstanceId[instanceId] = entry;
            }

            GetOrCreateBucket(worldId).Entries.Add(entry);
        }

        public static void Unregister(HighEnergyParticleStorage storage)
        {
            if (storage != null &&
                EntriesByParticleStorage.TryGetValue(storage, out ParticleStorageEntry entry))
            {
                UnregisterEntry(entry);
            }
        }

        public static Storage FindStorageByInstanceId(int worldId, int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID ||
                !EntriesByInstanceId.TryGetValue(instanceId, out ParticleStorageEntry entry) ||
                !entry.IsLive ||
                entry.WorldId != worldId)
            {
                return null;
            }

            return entry.BackingStorage;
        }

        public static void Invalidate(Storage storage)
        {
            if (storage != null &&
                EntriesByStorage.TryGetValue(storage, out ParticleStorageEntry entry) &&
                BucketsByWorld.TryGetValue(entry.WorldId, out WorldStorageBucket bucket))
            {
                bucket.SnapshotValid = false;
            }
        }

        public static float Store(GameObject source, float amount)
        {
            if (source == null || amount <= 0f)
            {
                return 0f;
            }

            int worldId = source.GetMyWorldId();
            if (!StorageNetworkPowerService.IsNetworkOnlineForWorld(worldId) ||
                !BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                return 0f;
            }

            PruneDeadEntries(bucket);
            float moved = 0f;
            for (int i = 0; i < bucket.Entries.Count && amount - moved > 0f; i++)
            {
                ParticleStorageEntry entry = bucket.Entries[i];
                if (entry.GameObject == source || !entry.IsOnline)
                {
                    continue;
                }

                float stored = entry.ParticleStorage.Store(amount - moved);
                moved += stored;
                bucket.RecordParticleDelta(entry, stored);
            }

            return moved;
        }

        public static float Consume(GameObject requester, float amount)
        {
            return Consume(requester, amount, null);
        }

        public static float Consume(GameObject requester, float amount, Storage specificSource)
        {
            if (requester == null || amount <= 0f)
            {
                return 0f;
            }

            int worldId = requester.GetMyWorldId();
            if (!StorageNetworkPowerService.IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            if (specificSource != null)
            {
                return TryGetEntry(specificSource, out ParticleStorageEntry specificEntry) &&
                       IsUsableSource(specificEntry, requester, worldId)
                    ? ConsumeFromEntry(specificEntry, amount)
                    : 0f;
            }

            if (!BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                return 0f;
            }

            return ConsumeFromBucket(bucket, requester, amount);
        }

        public static float ConsumeIfAvailable(GameObject requester, float amount, Storage specificSource)
        {
            if (requester == null || amount <= 0f)
            {
                return 0f;
            }

            int worldId = requester.GetMyWorldId();
            if (!StorageNetworkPowerService.IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            if (specificSource != null)
            {
                if (!TryGetEntry(specificSource, out ParticleStorageEntry specificEntry) ||
                    !IsUsableSource(specificEntry, requester, worldId) ||
                    specificEntry.ParticleStorage.Particles < amount)
                {
                    return 0f;
                }

                return ConsumeFromEntry(specificEntry, amount);
            }

            if (!BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                return 0f;
            }

            RefreshSnapshotIfNeeded(bucket);
            float available = bucket.AvailableParticles;
            if (TryGetEntry(requester, out ParticleStorageEntry requesterEntry) &&
                requesterEntry.WorldId == worldId &&
                requesterEntry.SnapshotOnline)
            {
                available -= requesterEntry.ParticleStorage.Particles;
            }

            return available < amount
                ? 0f
                : ConsumeFromBucket(bucket, requester, amount);
        }

        public static float GetAvailable(GameObject requester)
        {
            return GetAvailable(requester, null);
        }

        public static float GetAvailable(GameObject requester, Storage specificSource)
        {
            if (requester == null)
            {
                return 0f;
            }

            int worldId = requester.GetMyWorldId();
            if (!StorageNetworkPowerService.IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            if (specificSource != null)
            {
                return TryGetEntry(specificSource, out ParticleStorageEntry specificEntry) &&
                       IsUsableSource(specificEntry, requester, worldId)
                    ? specificEntry.ParticleStorage.Particles
                    : 0f;
            }

            if (!BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                return 0f;
            }

            RefreshSnapshotIfNeeded(bucket);
            float available = bucket.AvailableParticles;
            if (TryGetEntry(requester, out ParticleStorageEntry requesterEntry) &&
                requesterEntry.WorldId == worldId &&
                requesterEntry.SnapshotOnline)
            {
                available -= requesterEntry.ParticleStorage.Particles;
            }

            return Mathf.Max(0f, available);
        }

        public static float GetCapacity(GameObject requester)
        {
            return GetCapacity(requester, null);
        }

        public static float GetCapacity(GameObject requester, Storage specificSource)
        {
            if (requester == null)
            {
                return 0f;
            }

            int worldId = requester.GetMyWorldId();
            if (specificSource != null)
            {
                return TryGetEntry(specificSource, out ParticleStorageEntry specificEntry) &&
                       IsUsableSource(specificEntry, requester, worldId)
                    ? specificEntry.ParticleStorage.Capacity()
                    : 0f;
            }

            if (!BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                return 0f;
            }

            RefreshSnapshotIfNeeded(bucket);
            float capacity = bucket.Capacity;
            if (TryGetEntry(requester, out ParticleStorageEntry requesterEntry) &&
                requesterEntry.WorldId == worldId &&
                requesterEntry.SnapshotOnline)
            {
                capacity -= requesterEntry.ParticleStorage.Capacity();
            }

            return Mathf.Max(0f, capacity);
        }

        private static float ConsumeFromEntry(ParticleStorageEntry entry, float amount)
        {
            float consumed = entry.ParticleStorage.ConsumeAndGet(amount);
            if (BucketsByWorld.TryGetValue(entry.WorldId, out WorldStorageBucket bucket))
            {
                bucket.RecordParticleDelta(entry, -consumed);
            }

            return consumed;
        }

        private static float ConsumeFromBucket(
            WorldStorageBucket bucket,
            GameObject requester,
            float amount)
        {
            PruneDeadEntries(bucket);
            float moved = 0f;
            for (int i = 0; i < bucket.Entries.Count && amount - moved > 0f; i++)
            {
                ParticleStorageEntry entry = bucket.Entries[i];
                if (entry.GameObject == requester || !entry.IsOnline)
                {
                    continue;
                }

                float consumed = entry.ParticleStorage.ConsumeAndGet(amount - moved);
                moved += consumed;
                bucket.RecordParticleDelta(entry, -consumed);
            }

            return moved;
        }

        private static bool TryGetEntry(Storage storage, out ParticleStorageEntry entry)
        {
            if (storage != null &&
                EntriesByStorage.TryGetValue(storage, out entry) &&
                entry.IsLive)
            {
                return true;
            }

            entry = null;
            return false;
        }

        private static bool TryGetEntry(GameObject gameObject, out ParticleStorageEntry entry)
        {
            entry = null;
            HighEnergyParticleStorage storage = gameObject != null
                ? gameObject.GetComponent<HighEnergyParticleStorage>()
                : null;
            return storage != null &&
                   EntriesByParticleStorage.TryGetValue(storage, out entry) &&
                   entry.IsLive;
        }

        private static bool IsUsableSource(
            ParticleStorageEntry entry,
            GameObject requester,
            int worldId)
        {
            return entry != null &&
                   entry.IsLive &&
                   entry.GameObject != requester &&
                   entry.WorldId == worldId &&
                   entry.IsOnline;
        }

        private static void RefreshSnapshotIfNeeded(WorldStorageBucket bucket)
        {
            float now = Time.unscaledTime;
            if (bucket.SnapshotValid &&
                now >= bucket.SnapshotAt &&
                now - bucket.SnapshotAt < SnapshotLifetimeSeconds)
            {
                return;
            }

            PruneDeadEntries(bucket);
            float available = 0f;
            float capacity = 0f;
            for (int i = 0; i < bucket.Entries.Count; i++)
            {
                ParticleStorageEntry entry = bucket.Entries[i];
                entry.SnapshotOnline = entry.IsOnline;
                if (!entry.SnapshotOnline)
                {
                    continue;
                }

                available += entry.ParticleStorage.Particles;
                capacity += entry.ParticleStorage.Capacity();
            }

            bucket.AvailableParticles = available;
            bucket.Capacity = capacity;
            bucket.SnapshotAt = now;
            bucket.SnapshotValid = true;
        }

        private static void PruneDeadEntries(WorldStorageBucket bucket)
        {
            for (int i = bucket.Entries.Count - 1; i >= 0; i--)
            {
                ParticleStorageEntry entry = bucket.Entries[i];
                if (entry.IsLive)
                {
                    continue;
                }

                bucket.Entries.RemoveAt(i);
                RemoveEntryIndexes(entry);
                bucket.SnapshotValid = false;
            }
        }

        private static WorldStorageBucket GetOrCreateBucket(int worldId)
        {
            if (!BucketsByWorld.TryGetValue(worldId, out WorldStorageBucket bucket))
            {
                bucket = new WorldStorageBucket();
                BucketsByWorld.Add(worldId, bucket);
            }

            bucket.SnapshotValid = false;
            return bucket;
        }

        private static void UnregisterEntry(ParticleStorageEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (BucketsByWorld.TryGetValue(entry.WorldId, out WorldStorageBucket bucket))
            {
                bucket.Entries.Remove(entry);
                bucket.SnapshotValid = false;
                if (bucket.Entries.Count == 0)
                {
                    BucketsByWorld.Remove(entry.WorldId);
                }
            }

            RemoveEntryIndexes(entry);
        }

        private static void RemoveEntryIndexes(ParticleStorageEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.ParticleStorage != null)
            {
                EntriesByParticleStorage.Remove(entry.ParticleStorage);
            }

            if (entry.BackingStorage != null)
            {
                EntriesByStorage.Remove(entry.BackingStorage);
            }

            if (entry.InstanceId != KPrefabID.InvalidInstanceID &&
                EntriesByInstanceId.TryGetValue(entry.InstanceId, out ParticleStorageEntry indexed) &&
                indexed == entry)
            {
                EntriesByInstanceId.Remove(entry.InstanceId);
            }
        }

        private sealed class ParticleStorageEntry
        {
            public ParticleStorageEntry(
                HighEnergyParticleStorage particleStorage,
                Storage backingStorage,
                Operational operational,
                int instanceId,
                int worldId)
            {
                ParticleStorage = particleStorage;
                BackingStorage = backingStorage;
                Operational = operational;
                InstanceId = instanceId;
                WorldId = worldId;
            }

            public readonly HighEnergyParticleStorage ParticleStorage;
            public readonly Storage BackingStorage;
            public readonly Operational Operational;
            public readonly int InstanceId;
            public readonly int WorldId;

            public bool SnapshotOnline;

            public GameObject GameObject =>
                ParticleStorage != null ? ParticleStorage.gameObject : null;

            public bool IsLive =>
                ParticleStorage != null && ParticleStorage.gameObject != null;

            public bool IsOnline =>
                IsLive && (Operational == null || Operational.IsOperational);
        }

        private sealed class WorldStorageBucket
        {
            public readonly List<ParticleStorageEntry> Entries =
                new List<ParticleStorageEntry>();

            public bool SnapshotValid;
            public float SnapshotAt;
            public float AvailableParticles;
            public float Capacity;

            public void RecordParticleDelta(ParticleStorageEntry entry, float delta)
            {
                if (!SnapshotValid || entry == null || Mathf.Approximately(delta, 0f))
                {
                    return;
                }

                bool online = entry.IsOnline;
                if (online != entry.SnapshotOnline)
                {
                    SnapshotValid = false;
                    return;
                }

                if (online)
                {
                    AvailableParticles = Mathf.Clamp(
                        AvailableParticles + delta,
                        0f,
                        Capacity);
                }
            }
        }
    }
}
