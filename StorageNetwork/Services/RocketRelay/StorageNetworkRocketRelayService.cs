using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkRocketRelayService
    {
        private static readonly AccessTools.FieldRef<ConditionHasNosecone, LaunchableRocketCluster> NoseconeLaunchableRef =
            CreateFieldRef<ConditionHasNosecone, LaunchableRocketCluster>("launchable", "nosecone launch condition");
        private static readonly AccessTools.FieldRef<ConditionHasControlStation, RocketModuleCluster> ControlStationModuleRef =
            CreateFieldRef<ConditionHasControlStation, RocketModuleCluster>("module", "control-station launch condition");
        private static readonly AccessTools.FieldRef<ConditionPilotOnBoard, RocketModuleCluster> PilotRocketModuleRef =
            CreateFieldRef<ConditionPilotOnBoard, RocketModuleCluster>("rocketModule", "pilot launch condition");

        private static readonly Dictionary<CraftModuleInterface, int> RelayCountsByCraft =
            new Dictionary<CraftModuleInterface, int>();
        private static readonly Dictionary<StorageNetworkRelayModule, CraftModuleInterface> CraftByRelay =
            new Dictionary<StorageNetworkRelayModule, CraftModuleInterface>();
        private static readonly HashSet<StorageNetworkRelayModule> KnownRelays =
            new HashSet<StorageNetworkRelayModule>();
        private static readonly HashSet<StorageNetworkRelayModule> RelaysInSpace =
            new HashSet<StorageNetworkRelayModule>();

        public static void ResetRuntimeState()
        {
            RelayCountsByCraft.Clear();
            CraftByRelay.Clear();
            KnownRelays.Clear();
            RelaysInSpace.Clear();
        }

        public static void Register(
            StorageNetworkRelayModule relay,
            RocketModuleCluster module)
        {
            if (relay == null)
            {
                return;
            }

            KnownRelays.Add(relay);

            CraftModuleInterface craft = module != null ? module.CraftInterface : null;
            if (CraftByRelay.TryGetValue(relay, out CraftModuleInterface previousCraft))
            {
                if (previousCraft == craft)
                {
                    return;
                }

                DecrementRelayCount(previousCraft);
                CraftByRelay.Remove(relay);
            }

            if (craft == null)
            {
                return;
            }

            CraftByRelay[relay] = craft;
            RelayCountsByCraft[craft] = RelayCountsByCraft.TryGetValue(craft, out int count)
                ? count + 1
                : 1;
        }

        public static void Unregister(StorageNetworkRelayModule relay)
        {
            if (ReferenceEquals(relay, null))
            {
                return;
            }

            KnownRelays.Remove(relay);
            RelaysInSpace.Remove(relay);
            if (!CraftByRelay.TryGetValue(relay, out CraftModuleInterface craft))
            {
                return;
            }

            CraftByRelay.Remove(relay);
            DecrementRelayCount(craft);
        }

        public static void SetInSpace(StorageNetworkRelayModule relay, bool inSpace)
        {
            if (relay == null || !KnownRelays.Contains(relay))
            {
                return;
            }

            if (inSpace)
            {
                RelaysInSpace.Add(relay);
            }
            else
            {
                RelaysInSpace.Remove(relay);
            }
        }

        public static bool HasRelayInSpace()
        {
            return RelaysInSpace.Count > 0;
        }

        public static bool HasStorageNetworkRelay(ConditionHasNosecone condition)
        {
            if (condition == null || NoseconeLaunchableRef == null)
            {
                return false;
            }

            return HasStorageNetworkRelay(NoseconeLaunchableRef(condition));
        }

        public static bool HasStorageNetworkRelay(ConditionHasControlStation condition)
        {
            if (condition == null || ControlStationModuleRef == null)
            {
                return false;
            }

            return HasStorageNetworkRelay(ControlStationModuleRef(condition));
        }

        public static bool HasStorageNetworkRelay(ConditionPilotOnBoard condition)
        {
            if (condition == null || PilotRocketModuleRef == null)
            {
                return false;
            }

            return HasStorageNetworkRelay(PilotRocketModuleRef(condition));
        }

        public static bool HasStorageNetworkRelay(LaunchableRocketCluster launchable)
        {
            if (launchable == null)
            {
                return false;
            }

            // CraftInterface can change during load and module-stack rebuilds. This is a
            // low-frequency launch-condition path, so resolving it directly is safest.
            RocketModuleCluster module = launchable.GetComponent<RocketModuleCluster>();
            return HasStorageNetworkRelay(module != null ? module.CraftInterface : null);
        }

        public static bool HasStorageNetworkRelay(RocketModuleCluster module)
        {
            return HasStorageNetworkRelay(module != null ? module.CraftInterface : null);
        }

        public static bool HasStorageNetworkRelay(CraftModuleInterface craftInterface)
        {
            if (craftInterface == null)
            {
                return false;
            }

            if (!RelayCountsByCraft.TryGetValue(craftInterface, out int count) || count <= 0)
            {
                ReindexCraft(craftInterface);
                return RelayCountsByCraft.TryGetValue(craftInterface, out count) && count > 0;
            }

            return true;
        }

        private static void ReindexCraft(CraftModuleInterface craft)
        {
            if (craft == null || craft.ClusterModules == null)
            {
                return;
            }

            for (int i = 0; i < craft.ClusterModules.Count; i++)
            {
                Ref<RocketModuleCluster> partRef = craft.ClusterModules[i];
                RocketModuleCluster module = partRef != null ? partRef.Get() : null;
                StorageNetworkRelayModule relay = module != null
                    ? module.GetComponent<StorageNetworkRelayModule>()
                    : null;
                if (relay != null && KnownRelays.Contains(relay))
                {
                    Register(relay, module);
                }
            }
        }

        private static void DecrementRelayCount(CraftModuleInterface craft)
        {
            if (craft == null || !RelayCountsByCraft.TryGetValue(craft, out int count))
            {
                return;
            }

            if (count <= 1)
            {
                RelayCountsByCraft.Remove(craft);
                return;
            }

            RelayCountsByCraft[craft] = count - 1;
        }

        private static AccessTools.FieldRef<TInstance, TField> CreateFieldRef<TInstance, TField>(
            string fieldName,
            string featureName)
        {
            try
            {
                return AccessTools.FieldRefAccess<TInstance, TField>(fieldName);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "[StorageNetwork] Disabled optional " + featureName +
                    " integration because field '" + fieldName +
                    "' could not be bound: " + exception.Message);
                return null;
            }
        }
    }
}
