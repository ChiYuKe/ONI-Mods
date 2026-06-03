using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Patches
{
    public static class RocketRelayLaunchConditionPatch
    {
        [HarmonyPatch(typeof(Clustercraft), "RequestLaunch")]
        public static class ClustercraftRequestLaunchPatch
        {
            public static bool Prefix(Clustercraft __instance, bool automated)
            {
                if (__instance == null || !RocketHasStorageNetworkRelay(__instance.ModuleInterface))
                {
                    return true;
                }

                if (__instance.HasTag(GameTags.RocketNotOnGround) || __instance.LaunchRequested)
                {
                    return false;
                }

                if (!__instance.CheckPreppedForLaunch())
                {
                    return false;
                }

                __instance.Launch(automated);
                return false;
            }
        }

        [HarmonyPatch(typeof(ConditionHasNosecone), "EvaluateCondition")]
        public static class ConditionHasNoseconeEvaluateConditionPatch
        {
            public static void Postfix(ConditionHasNosecone __instance, ref ProcessCondition.Status __result)
            {
                if (__result != ProcessCondition.Status.Failure || !RocketHasStorageNetworkRelay(__instance))
                {
                    return;
                }

                __result = ProcessCondition.Status.Ready;
            }
        }

        [HarmonyPatch(typeof(ConditionHasControlStation), "EvaluateCondition")]
        public static class ConditionHasControlStationEvaluateConditionPatch
        {
            public static void Postfix(ConditionHasControlStation __instance, ref ProcessCondition.Status __result)
            {
                if (__result != ProcessCondition.Status.Failure || !RocketHasStorageNetworkRelay(__instance, "module"))
                {
                    return;
                }

                __result = ProcessCondition.Status.Warning;
            }
        }

        [HarmonyPatch(typeof(ConditionPilotOnBoard), "EvaluateCondition")]
        public static class ConditionPilotOnBoardEvaluateConditionPatch
        {
            public static void Postfix(ConditionPilotOnBoard __instance, ref ProcessCondition.Status __result)
            {
                if (__result != ProcessCondition.Status.Failure || !RocketHasStorageNetworkRelay(__instance, "rocketModule"))
                {
                    return;
                }

                __result = ProcessCondition.Status.Warning;
            }
        }

        private static bool RocketHasStorageNetworkRelay(ConditionHasNosecone condition)
        {
            LaunchableRocketCluster launchable = Traverse.Create(condition)
                .Field("launchable")
                .GetValue<LaunchableRocketCluster>();
            if (launchable?.parts == null)
            {
                return false;
            }

            foreach (Ref<RocketModuleCluster> partRef in launchable.parts)
            {
                RocketModuleCluster part = partRef?.Get();
                if (part != null && part.GetComponent<StorageNetworkRelayModule>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool RocketHasStorageNetworkRelay(object condition, string rocketModuleFieldName)
        {
            RocketModuleCluster module = Traverse.Create(condition)
                .Field(rocketModuleFieldName)
                .GetValue<RocketModuleCluster>();
            CraftModuleInterface craftInterface = module != null ? module.CraftInterface : null;
            return RocketHasStorageNetworkRelay(craftInterface);
        }

        private static bool RocketHasStorageNetworkRelay(CraftModuleInterface craftInterface)
        {
            if (craftInterface?.ClusterModules == null)
            {
                return false;
            }

            foreach (Ref<RocketModuleCluster> partRef in craftInterface.ClusterModules)
            {
                RocketModuleCluster part = partRef?.Get();
                if (part != null && part.GetComponent<StorageNetworkRelayModule>() != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
