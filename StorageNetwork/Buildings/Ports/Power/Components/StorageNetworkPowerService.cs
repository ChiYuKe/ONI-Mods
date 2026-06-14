using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkPowerService
    {
        private const float EpsilonJoules = 0.01f;
        public static bool IsNetworkOnlineForWorld(int worldId)
        {
            return StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
        }

        public static float GetStoredJoules(int worldId)
        {
            float total = 0f;
            foreach (StorageNetworkPowerStorage battery in GetReachablePowerStorages(worldId))
            {
                total += battery.RawJoulesAvailable;
            }

            return total;
        }

        public static float GetCapacityJoules(int worldId)
        {
            float total = 0f;
            foreach (StorageNetworkPowerStorage battery in GetReachablePowerStorages(worldId))
            {
                total += battery.CapacityJoules;
            }

            return total;
        }

        public static float GetAvailableCapacityJoules(int worldId)
        {
            float total = 0f;
            foreach (StorageNetworkPowerStorage battery in GetReachablePowerStorages(worldId))
            {
                total += battery.AvailableCapacityJoules;
            }

            return total;
        }

        public static float GetJoulesLostPerCycle(int worldId)
        {
            float total = 0f;
            foreach (StorageNetworkPowerStorage battery in GetReachablePowerStorages(worldId))
            {
                total += battery.JoulesLostPerCycle;
            }

            return total;
        }

        public static float AddEnergy(int worldId, float joules)
        {
            if (joules <= 0f || !IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            return AddEnergyEvenly(GetReachablePowerStorages(worldId), joules);
        }

        public static float ConsumeEnergy(int worldId, float joules)
        {
            if (joules <= 0f || !IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            return ConsumeEnergyEvenly(GetReachablePowerStorages(worldId), joules);
        }

        public static float ConsumeEnergy(StorageNetworkPowerStorage source, float joules)
        {
            if (source == null || joules <= 0f)
            {
                return 0f;
            }

            int worldId = source.gameObject != null ? source.gameObject.GetMyWorldId() : -1;
            if (!IsNetworkOnlineForWorld(worldId))
            {
                return 0f;
            }

            return source.ConsumeEnergy(joules);
        }

        private static float AddEnergyEvenly(IEnumerable<StorageNetworkPowerStorage> storages, float joules)
        {
            List<StorageNetworkPowerStorage> batteries = storages
                .Where(battery => battery != null && battery.CapacityJoules > 0f && battery.AvailableCapacityJoules > EpsilonJoules)
                .ToList();
            float remaining = joules;
            while (remaining > EpsilonJoules && batteries.Count > 0)
            {
                batteries.RemoveAll(battery => battery.AvailableCapacityJoules <= EpsilonJoules);
                if (batteries.Count == 0)
                {
                    break;
                }

                float share = remaining / batteries.Count;
                float accepted = 0f;
                foreach (StorageNetworkPowerStorage battery in batteries)
                {
                    accepted += battery.AddEnergy(share);
                }

                if (accepted <= EpsilonJoules)
                {
                    break;
                }

                remaining -= accepted;
            }

            return joules - remaining;
        }

        private static float ConsumeEnergyEvenly(IEnumerable<StorageNetworkPowerStorage> storages, float joules)
        {
            List<StorageNetworkPowerStorage> batteries = storages
                .Where(battery => battery != null && battery.CapacityJoules > 0f && battery.RawJoulesAvailable > EpsilonJoules)
                .ToList();
            float remaining = joules;
            while (remaining > EpsilonJoules && batteries.Count > 0)
            {
                batteries.RemoveAll(battery => battery.RawJoulesAvailable <= EpsilonJoules);
                if (batteries.Count == 0)
                {
                    break;
                }

                float share = remaining / batteries.Count;
                float consumed = 0f;
                foreach (StorageNetworkPowerStorage battery in batteries)
                {
                    consumed += battery.ConsumeEnergy(share);
                }

                if (consumed <= EpsilonJoules)
                {
                    break;
                }

                remaining -= consumed;
            }

            return joules - remaining;
        }

        private static System.Collections.Generic.IEnumerable<StorageNetworkPowerStorage> GetReachablePowerStorages(int worldId)
        {
            bool crossPlanetRelayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            foreach (StorageNetworkPowerStorage battery in StorageSceneRegistry.GetPowerStorages())
            {
                if (battery == null || battery.gameObject == null || !battery.IsOnline)
                {
                    continue;
                }

                if (!crossPlanetRelayOnline && worldId >= 0 && battery.gameObject.GetMyWorldId() != worldId)
                {
                    continue;
                }

                yield return battery;
            }
        }
    }
}
