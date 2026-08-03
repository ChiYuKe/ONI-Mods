using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkPowerService
    {
        private const float EpsilonJoules = 0.01f;
        private const float AggregateLifetimeSeconds = 0.2f;

        private static readonly PowerAggregateCache AggregateCache =
            new PowerAggregateCache();
        private static bool aggregateDirty = true;
        private static int powerVersion;

        public static bool IsNetworkOnlineForWorld(int worldId)
        {
            return GetAggregateView(worldId).NetworkOnline;
        }

        public static float GetStoredJoules(int worldId)
        {
            return GetAggregateView(worldId).Reachable.SharedStoredJoules;
        }

        public static float GetCapacityJoules(int worldId)
        {
            return GetAggregateView(worldId).Reachable.SharedCapacityJoules;
        }

        public static float GetAvailableCapacityJoules(int worldId)
        {
            return GetAggregateView(worldId).Reachable.SharedAvailableCapacityJoules;
        }

        public static float GetAvailableChargeCapacityJoules(int worldId)
        {
            PowerAggregateState reachable = GetAggregateView(worldId).Reachable;
            return StorageNetworkPowerReserveMetrics.GetAvailableChargeCapacityJoules(
                reachable.SharedAvailableCapacityJoules,
                reachable.CoreAvailableCapacityJoules);
        }

        public static float GetJoulesLostPerCycle(int worldId)
        {
            return GetAggregateView(worldId).Reachable.JoulesLostPerCycle;
        }

        public static StorageNetworkPowerSnapshot GetSnapshot(int worldId)
        {
            PowerAggregateView view = GetAggregateView(worldId);
            if (!view.NetworkOnline)
            {
                return ValidateSnapshot(
                    worldId,
                    StorageNetworkPowerSnapshot.Offline,
                    includeCoreReserve: false);
            }

            PowerAggregateState reachable = view.Reachable;
            StorageNetworkPowerSnapshot snapshot = new StorageNetworkPowerSnapshot(
                true,
                reachable.SharedStoredJoules,
                reachable.SharedCapacityJoules,
                reachable.SharedAvailableCapacityJoules,
                reachable.JoulesLostPerCycle);
            return ValidateSnapshot(worldId, snapshot, includeCoreReserve: false);
        }

        public static StorageNetworkPowerSnapshot GetAutomationSnapshot(int worldId)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
            {
                PowerAggregateView view = GetAggregateView(worldId);
                if (!view.NetworkOnline)
                {
                    return ValidateSnapshot(
                        worldId,
                        StorageNetworkPowerSnapshot.Offline,
                        includeCoreReserve: true);
                }

                PowerAggregateState reachable = view.Reachable;
                StorageNetworkPowerSnapshot snapshot = new StorageNetworkPowerSnapshot(
                    true,
                    reachable.SharedStoredJoules + reachable.CoreStoredJoules,
                    reachable.SharedCapacityJoules + reachable.CoreCapacityJoules,
                    StorageNetworkPowerReserveMetrics.GetAvailableChargeCapacityJoules(
                        reachable.SharedAvailableCapacityJoules,
                        reachable.CoreAvailableCapacityJoules),
                    reachable.JoulesLostPerCycle);
                return ValidateSnapshot(worldId, snapshot, includeCoreReserve: true);
            }
        }

        public static float AddEnergy(int worldId, float joules)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
            {
                PowerAggregateView view = GetAggregateView(worldId);
                if (joules <= 0f || !view.NetworkOnline)
                {
                    return 0f;
                }

                float storedInNetworkBatteries = AddEnergyEvenly(view.Reachable.Storages, joules);
                float remaining = joules - storedInNetworkBatteries;
                if (remaining <= EpsilonJoules)
                {
                    return storedInNetworkBatteries;
                }

                return storedInNetworkBatteries +
                    AddEnergyToCoreInternalBatteries(view.Reachable.Cores, remaining);
            }
        }

        public static float ConsumeEnergy(int worldId, float joules)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
            {
                PowerAggregateView view = GetAggregateView(worldId);
                if (joules <= 0f || !view.NetworkOnline)
                {
                    return 0f;
                }

                return ConsumeEnergyEvenly(view.Reachable.Storages, joules);
            }
        }

        public static float AddEnergy(StorageNetworkPowerStorage target, float joules)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
            {
                if (target == null || joules <= 0f)
                {
                    return 0f;
                }

                float stored = target.AddEnergy(joules);
                RecordStorageEnergyDelta(target, stored);
                return stored;
            }
        }

        public static float ConsumeEnergy(StorageNetworkPowerStorage source, float joules)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
            {
                if (source == null || joules <= 0f)
                {
                    return 0f;
                }

                int worldId = source.gameObject != null ? source.gameObject.GetMyWorldId() : -1;
                if (!GetAggregateView(worldId).NetworkOnline)
                {
                    return 0f;
                }

                float consumed = source.ConsumeEnergy(joules);
                RecordStorageEnergyDelta(source, -consumed);
                return consumed;
            }
        }

        internal static void InvalidateAggregate()
        {
            aggregateDirty = true;
            unchecked
            {
                powerVersion++;
            }
        }

        private static float AddEnergyEvenly(List<StorageNetworkPowerStorage> batteries, float joules)
        {
            float remaining = joules;
            while (remaining > EpsilonJoules)
            {
                int eligibleCount = 0;
                for (int i = 0; i < batteries.Count; i++)
                {
                    StorageNetworkPowerStorage battery = batteries[i];
                    if (battery != null &&
                        battery.CapacityJoules > 0f &&
                        battery.AvailableCapacityJoules > 0f)
                    {
                        eligibleCount++;
                    }
                }

                if (eligibleCount == 0)
                {
                    break;
                }

                float share = remaining / eligibleCount;
                float accepted = 0f;
                for (int i = 0; i < batteries.Count; i++)
                {
                    StorageNetworkPowerStorage battery = batteries[i];
                    if (battery == null ||
                        battery.CapacityJoules <= 0f ||
                        battery.AvailableCapacityJoules <= 0f)
                    {
                        continue;
                    }

                    float stored = battery.AddEnergy(share);
                    accepted += stored;
                    RecordStorageEnergyDelta(battery, stored);
                }

                if (accepted <= 0f)
                {
                    break;
                }

                remaining -= accepted;
            }

            return joules - Mathf.Max(0f, remaining);
        }

        private static float AddEnergyToCoreInternalBatteries(List<StorageNetworkCore> cores, float joules)
        {
            float remaining = joules;
            while (remaining > EpsilonJoules)
            {
                int eligibleCount = 0;
                for (int i = 0; i < cores.Count; i++)
                {
                    StorageNetworkCore core = cores[i];
                    if (core != null && core.InternalBatteryAvailableCapacityJoules > 0f)
                    {
                        eligibleCount++;
                    }
                }

                if (eligibleCount == 0)
                {
                    break;
                }

                float share = remaining / eligibleCount;
                float accepted = 0f;
                for (int i = 0; i < cores.Count; i++)
                {
                    StorageNetworkCore core = cores[i];
                    if (core == null || core.InternalBatteryAvailableCapacityJoules <= 0f)
                    {
                        continue;
                    }

                    float stored = core.AddInternalBatteryEnergy(share);
                    accepted += stored;
                    RecordCoreEnergyDelta(core, stored);
                }

                if (accepted <= 0f)
                {
                    break;
                }

                remaining -= accepted;
            }

            return joules - Mathf.Max(0f, remaining);
        }

        private static float ConsumeEnergyEvenly(List<StorageNetworkPowerStorage> batteries, float joules)
        {
            float remaining = joules;
            while (remaining > EpsilonJoules)
            {
                int eligibleCount = 0;
                for (int i = 0; i < batteries.Count; i++)
                {
                    StorageNetworkPowerStorage battery = batteries[i];
                    if (battery != null &&
                        battery.CapacityJoules > 0f &&
                        battery.RawJoulesAvailable > 0f)
                    {
                        eligibleCount++;
                    }
                }

                if (eligibleCount == 0)
                {
                    break;
                }

                float share = remaining / eligibleCount;
                float consumed = 0f;
                for (int i = 0; i < batteries.Count; i++)
                {
                    StorageNetworkPowerStorage battery = batteries[i];
                    if (battery == null ||
                        battery.CapacityJoules <= 0f ||
                        battery.RawJoulesAvailable <= 0f)
                    {
                        continue;
                    }

                    float taken = battery.ConsumeEnergy(share);
                    consumed += taken;
                    RecordStorageEnergyDelta(battery, -taken);
                }

                if (consumed <= 0f)
                {
                    break;
                }

                remaining -= consumed;
            }

            return joules - Mathf.Max(0f, remaining);
        }

        private static PowerAggregateView GetAggregateView(int worldId)
        {
            PowerAggregateCache cache = GetAggregateCache();
            PowerAggregateState local = worldId < 0
                ? cache.Global
                : cache.GetWorldOrEmpty(worldId);
            PowerAggregateState reachable = worldId < 0 || cache.CrossPlanetRelayOnline
                ? cache.Global
                : local;
            return new PowerAggregateView(local.NetworkOnline, reachable);
        }

        private static PowerAggregateCache GetAggregateCache()
        {
            // Unity's scaled time follows simulation speed, so a 200 ms cache window
            // remains one SIM_200ms epoch at 1x and 3x instead of several sim ticks.
            float now = Time.time;
            int registryVersion = StorageSceneRegistry.Version;
            if (aggregateDirty ||
                AggregateCache.RegistryVersion != registryVersion ||
                now < AggregateCache.BuiltAt ||
                now - AggregateCache.BuiltAt >= AggregateLifetimeSeconds)
            {
                using (StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.Power))
                {
                    BuildAggregateCache(now, registryVersion, AggregateCache);
                    aggregateDirty = false;
                }
            }

            return AggregateCache;
        }

        private static void BuildAggregateCache(
            float now,
            int registryVersion,
            PowerAggregateCache cache)
        {
            cache.Reset(
                now,
                registryVersion,
                StorageSceneRegistry.IsCrossPlanetRelayOnline());

            foreach (StorageNetworkPowerStorage battery in StorageSceneRegistry.GetPowerStorages())
            {
                if (battery == null || battery.gameObject == null || !battery.IsOnline)
                {
                    continue;
                }

                int worldId = battery.gameObject.GetMyWorldId();
                PowerAggregateState local = cache.GetOrCreateWorld(worldId);
                local.AddStorage(battery);
                cache.Global.AddStorage(battery);
                cache.StorageWorlds[battery] = worldId;
            }

            foreach (StorageNetworkCore core in StorageSceneRegistry.GetCores())
            {
                if (core == null || core.gameObject == null)
                {
                    continue;
                }

                int worldId = core.gameObject.GetMyWorldId();
                PowerAggregateState local = cache.GetOrCreateWorld(worldId);
                local.AddCore(core);
                cache.Global.AddCore(core);
                cache.CoreWorlds[core] = worldId;
            }

        }

        internal static void RecordStorageEnergyDelta(StorageNetworkPowerStorage battery, float delta)
        {
            if (battery == null || Mathf.Abs(delta) <= 0f)
            {
                return;
            }

            unchecked
            {
                powerVersion++;
            }

            if (aggregateDirty)
            {
                return;
            }

            if (!AggregateCache.StorageWorlds.TryGetValue(battery, out int worldId))
            {
                aggregateDirty = true;
                return;
            }

            AggregateCache.Global.ApplyStorageEnergyDelta(delta);
            AggregateCache.GetWorldOrEmpty(worldId).ApplyStorageEnergyDelta(delta);
        }

        internal static void RecordCoreEnergyDelta(StorageNetworkCore core, float delta)
        {
            if (core == null || Mathf.Abs(delta) <= 0f)
            {
                return;
            }

            unchecked
            {
                powerVersion++;
            }

            if (aggregateDirty)
            {
                return;
            }

            if (!AggregateCache.CoreWorlds.TryGetValue(core, out int worldId))
            {
                aggregateDirty = true;
                return;
            }

            AggregateCache.Global.ApplyCoreEnergyDelta(delta);
            PowerAggregateState local = AggregateCache.GetWorldOrEmpty(worldId);
            local.ApplyCoreEnergyDelta(delta);
            if (core.IsNetworkOnline)
            {
                local.NetworkOnline = true;
                AggregateCache.Global.NetworkOnline = true;
            }
        }

        private static StorageNetworkPowerSnapshot ValidateSnapshot(
            int worldId,
            StorageNetworkPowerSnapshot indexed,
            bool includeCoreReserve)
        {
            int version = unchecked(
                (powerVersion * 397) ^
                StorageSceneRegistry.ConnectivityVersion ^
                StorageSceneRegistry.CapabilityVersion);
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.PowerSnapshot,
                    worldId,
                    version))
            {
                return indexed;
            }

            StorageNetworkPowerSnapshot native = BuildNativeSnapshot(
                worldId,
                includeCoreReserve);
            if (indexed.NetworkOnline == native.NetworkOnline &&
                StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.StoredJoules,
                    native.StoredJoules) &&
                StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.CapacityJoules,
                    native.CapacityJoules) &&
                StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.AvailableCapacityJoules,
                    native.AvailableCapacityJoules) &&
                StorageNetworkShadowValidationService.ApproximatelyEqual(
                    indexed.JoulesLostPerCycle,
                    native.JoulesLostPerCycle))
            {
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.PowerSnapshot,
                    worldId,
                    version);
                return indexed;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.PowerSnapshot,
                worldId,
                version,
                includeCoreReserve ? 1 : 0,
                $"indexed={indexed.StoredJoules:0.###}/{indexed.CapacityJoules:0.###}, " +
                $"native={native.StoredJoules:0.###}/{native.CapacityJoules:0.###}");
            InvalidateAggregate();
            return native;
        }

        private static StorageNetworkPowerSnapshot BuildNativeSnapshot(
            int worldId,
            bool includeCoreReserve)
        {
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            bool networkOnline = false;
            float sharedStored = 0f;
            float sharedCapacity = 0f;
            float sharedAvailable = 0f;
            float lostPerCycle = 0f;
            float coreStored = 0f;
            float coreCapacity = 0f;
            float coreAvailable = 0f;

            foreach (StorageNetworkCore core in StorageSceneRegistry.GetCores())
            {
                if (!StorageSceneRegistry.IsLive(core) || core.gameObject == null)
                {
                    continue;
                }

                int coreWorldId = core.gameObject.GetMyWorldId();
                if (worldId < 0 || coreWorldId == worldId)
                {
                    networkOnline |= core.IsNetworkOnline;
                }

                if (includeCoreReserve &&
                    (worldId < 0 || relayOnline || coreWorldId == worldId))
                {
                    coreStored += core.InternalBatteryJoulesAvailable;
                    coreCapacity += StorageNetworkCore.InternalBatteryCapacityJoules;
                    coreAvailable += core.InternalBatteryAvailableCapacityJoules;
                }
            }

            if (!networkOnline)
            {
                return StorageNetworkPowerSnapshot.Offline;
            }

            foreach (StorageNetworkPowerStorage battery in StorageSceneRegistry.GetPowerStorages())
            {
                if (!StorageSceneRegistry.IsLive(battery) ||
                    battery.gameObject == null ||
                    !battery.IsOnline)
                {
                    continue;
                }

                int batteryWorldId = battery.gameObject.GetMyWorldId();
                if (worldId >= 0 && !relayOnline && batteryWorldId != worldId)
                {
                    continue;
                }

                sharedStored += battery.RawJoulesAvailable;
                sharedCapacity += battery.CapacityJoules;
                sharedAvailable += battery.AvailableCapacityJoules;
                lostPerCycle += battery.JoulesLostPerCycle;
            }

            float available = includeCoreReserve
                ? StorageNetworkPowerReserveMetrics.GetAvailableChargeCapacityJoules(
                    sharedAvailable,
                    coreAvailable)
                : sharedAvailable;
            return new StorageNetworkPowerSnapshot(
                true,
                sharedStored + (includeCoreReserve ? coreStored : 0f),
                sharedCapacity + (includeCoreReserve ? coreCapacity : 0f),
                available,
                lostPerCycle);
        }

        private readonly struct PowerAggregateView
        {
            public PowerAggregateView(bool networkOnline, PowerAggregateState reachable)
            {
                NetworkOnline = networkOnline;
                Reachable = reachable;
            }

            public bool NetworkOnline { get; }

            public PowerAggregateState Reachable { get; }
        }

        private sealed class PowerAggregateCache
        {
            private readonly Dictionary<int, PowerAggregateState> worlds =
                new Dictionary<int, PowerAggregateState>();

            public void Reset(
                float builtAt,
                int registryVersion,
                bool crossPlanetRelayOnline)
            {
                BuiltAt = builtAt;
                RegistryVersion = registryVersion;
                CrossPlanetRelayOnline = crossPlanetRelayOnline;
                Global.Reset();
                StorageWorlds.Clear();
                CoreWorlds.Clear();
                foreach (PowerAggregateState state in worlds.Values)
                {
                    state.Reset();
                }
            }

            public readonly PowerAggregateState Global = new PowerAggregateState();
            public readonly Dictionary<StorageNetworkPowerStorage, int> StorageWorlds =
                new Dictionary<StorageNetworkPowerStorage, int>();
            public readonly Dictionary<StorageNetworkCore, int> CoreWorlds =
                new Dictionary<StorageNetworkCore, int>();

            public float BuiltAt { get; private set; } = float.NegativeInfinity;

            public int RegistryVersion { get; private set; } = int.MinValue;

            public bool CrossPlanetRelayOnline { get; private set; }

            public PowerAggregateState GetOrCreateWorld(int worldId)
            {
                if (!worlds.TryGetValue(worldId, out PowerAggregateState state))
                {
                    state = new PowerAggregateState();
                    worlds.Add(worldId, state);
                }

                return state;
            }

            public PowerAggregateState GetWorldOrEmpty(int worldId)
            {
                return worlds.TryGetValue(worldId, out PowerAggregateState state)
                    ? state
                    : PowerAggregateState.Empty;
            }
        }

        private sealed class PowerAggregateState
        {
            public static readonly PowerAggregateState Empty = new PowerAggregateState();

            public readonly List<StorageNetworkPowerStorage> Storages =
                new List<StorageNetworkPowerStorage>();
            public readonly List<StorageNetworkCore> Cores =
                new List<StorageNetworkCore>();

            public bool NetworkOnline;
            public float SharedStoredJoules;
            public float SharedCapacityJoules;
            public float SharedAvailableCapacityJoules;
            public float JoulesLostPerCycle;
            public float CoreStoredJoules;
            public float CoreCapacityJoules;
            public float CoreAvailableCapacityJoules;

            public void AddStorage(StorageNetworkPowerStorage battery)
            {
                Storages.Add(battery);
                SharedStoredJoules += battery.RawJoulesAvailable;
                SharedCapacityJoules += battery.CapacityJoules;
                SharedAvailableCapacityJoules += battery.AvailableCapacityJoules;
                JoulesLostPerCycle += battery.JoulesLostPerCycle;
            }

            public void AddCore(StorageNetworkCore core)
            {
                Cores.Add(core);
                CoreStoredJoules += core.InternalBatteryJoulesAvailable;
                CoreCapacityJoules += StorageNetworkCore.InternalBatteryCapacityJoules;
                CoreAvailableCapacityJoules += core.InternalBatteryAvailableCapacityJoules;
                NetworkOnline |= core.IsNetworkOnline;
            }

            public void ApplyStorageEnergyDelta(float delta)
            {
                SharedStoredJoules = Mathf.Clamp(
                    SharedStoredJoules + delta,
                    0f,
                    SharedCapacityJoules);
                SharedAvailableCapacityJoules = Mathf.Clamp(
                    SharedAvailableCapacityJoules - delta,
                    0f,
                    SharedCapacityJoules);
            }

            public void ApplyCoreEnergyDelta(float delta)
            {
                CoreStoredJoules = Mathf.Clamp(
                    CoreStoredJoules + delta,
                    0f,
                    CoreCapacityJoules);
                CoreAvailableCapacityJoules = Mathf.Clamp(
                    CoreAvailableCapacityJoules - delta,
                    0f,
                    CoreCapacityJoules);
            }

            public void Reset()
            {
                Storages.Clear();
                Cores.Clear();
                NetworkOnline = false;
                SharedStoredJoules = 0f;
                SharedCapacityJoules = 0f;
                SharedAvailableCapacityJoules = 0f;
                JoulesLostPerCycle = 0f;
                CoreStoredJoules = 0f;
                CoreCapacityJoules = 0f;
                CoreAvailableCapacityJoules = 0f;
            }
        }
    }

    internal readonly struct StorageNetworkPowerSnapshot
    {
        public static readonly StorageNetworkPowerSnapshot Offline =
            new StorageNetworkPowerSnapshot(false, 0f, 0f, 0f, 0f);

        public StorageNetworkPowerSnapshot(
            bool networkOnline,
            float storedJoules,
            float capacityJoules,
            float availableCapacityJoules,
            float joulesLostPerCycle)
        {
            NetworkOnline = networkOnline;
            StoredJoules = storedJoules;
            CapacityJoules = capacityJoules;
            AvailableCapacityJoules = availableCapacityJoules;
            JoulesLostPerCycle = joulesLostPerCycle;
        }

        public bool NetworkOnline { get; }

        public float StoredJoules { get; }

        public float CapacityJoules { get; }

        public float AvailableCapacityJoules { get; }

        public float JoulesLostPerCycle { get; }
    }
}
