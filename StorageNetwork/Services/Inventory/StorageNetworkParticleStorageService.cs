using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkParticleStorageService
    {
        private static readonly HashSet<HighEnergyParticleStorage> Storages = new HashSet<HighEnergyParticleStorage>();

        public static void Reset()
        {
            Storages.Clear();
        }

        public static void Register(HighEnergyParticleStorage storage)
        {
            if (storage != null)
            {
                Storages.Add(storage);
            }
        }

        public static void Unregister(HighEnergyParticleStorage storage)
        {
            if (storage != null)
            {
                Storages.Remove(storage);
            }
        }

        public static float Store(GameObject source, float amount)
        {
            if (source == null || amount <= 0f)
            {
                return 0f;
            }

            int worldId = source.GetMyWorldId();
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            float moved = 0f;
            foreach (HighEnergyParticleStorage storage in GetLiveStorages())
            {
                if (amount - moved <= 0f)
                {
                    break;
                }

                if (storage.gameObject == source || storage.gameObject.GetMyWorldId() != worldId || !IsOnline(storage))
                {
                    continue;
                }

                moved += storage.Store(amount - moved);
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
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            if (specificSource != null)
            {
                return Consume(specificSource.GetComponent<HighEnergyParticleStorage>(), amount, requester, worldId);
            }

            float moved = 0f;
            foreach (HighEnergyParticleStorage storage in GetLiveStorages())
            {
                if (amount - moved <= 0f)
                {
                    break;
                }

                if (storage.gameObject == requester || storage.gameObject.GetMyWorldId() != worldId || !IsOnline(storage))
                {
                    continue;
                }

                moved += storage.ConsumeAndGet(amount - moved);
            }

            return moved;
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
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return 0f;
            }

            if (specificSource != null)
            {
                return GetAvailable(specificSource.GetComponent<HighEnergyParticleStorage>(), requester, worldId);
            }

            float available = 0f;
            foreach (HighEnergyParticleStorage storage in GetLiveStorages())
            {
                if (storage.gameObject == requester || storage.gameObject.GetMyWorldId() != worldId || !IsOnline(storage))
                {
                    continue;
                }

                available += storage.Particles;
            }

            return available;
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
                return GetCapacity(specificSource.GetComponent<HighEnergyParticleStorage>(), requester, worldId);
            }

            float capacity = 0f;
            foreach (HighEnergyParticleStorage storage in GetLiveStorages())
            {
                if (storage.gameObject == requester || storage.gameObject.GetMyWorldId() != worldId || !IsOnline(storage))
                {
                    continue;
                }

                capacity += storage.Capacity();
            }

            return capacity;
        }

        private static float Consume(HighEnergyParticleStorage storage, float amount, GameObject requester, int worldId)
        {
            if (!IsUsableSource(storage, requester, worldId))
            {
                return 0f;
            }

            return storage.ConsumeAndGet(amount);
        }

        private static float GetAvailable(HighEnergyParticleStorage storage, GameObject requester, int worldId)
        {
            return IsUsableSource(storage, requester, worldId) ? storage.Particles : 0f;
        }

        private static float GetCapacity(HighEnergyParticleStorage storage, GameObject requester, int worldId)
        {
            return IsUsableSource(storage, requester, worldId) ? storage.Capacity() : 0f;
        }

        private static List<HighEnergyParticleStorage> GetLiveStorages()
        {
            List<HighEnergyParticleStorage> live = new List<HighEnergyParticleStorage>();
            foreach (HighEnergyParticleStorage storage in Storages)
            {
                if (storage != null && storage.gameObject != null)
                {
                    live.Add(storage);
                }
            }

            if (live.Count != Storages.Count)
            {
                Storages.Clear();
                foreach (HighEnergyParticleStorage storage in live)
                {
                    Storages.Add(storage);
                }
            }

            return live;
        }

        private static bool IsOnline(HighEnergyParticleStorage storage)
        {
            Operational operational = storage.GetComponent<Operational>();
            return operational == null || operational.IsOperational;
        }

        private static bool IsUsableSource(HighEnergyParticleStorage storage, GameObject requester, int worldId)
        {
            return storage != null &&
                   storage.gameObject != null &&
                   storage.gameObject != requester &&
                   storage.gameObject.GetMyWorldId() == worldId &&
                   IsOnline(storage);
        }
    }
}
