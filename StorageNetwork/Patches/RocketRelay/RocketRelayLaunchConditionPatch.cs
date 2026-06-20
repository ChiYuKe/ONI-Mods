using HarmonyLib;
using StorageNetwork.Services;

namespace StorageNetwork.Patches
{
    public static class RocketRelayLaunchConditionPatch
    {
        [HarmonyPatch(typeof(Clustercraft), "RequestLaunch")]
        public static class ClustercraftRequestLaunchPatch
        {
            public static bool Prefix(Clustercraft __instance, bool automated)
            {
                if (__instance == null || !StorageNetworkRocketRelayService.HasStorageNetworkRelay(__instance.ModuleInterface))
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
                if (__result != ProcessCondition.Status.Failure || !StorageNetworkRocketRelayService.HasStorageNetworkRelay(__instance))
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
                if (__result != ProcessCondition.Status.Failure ||
                    !StorageNetworkRocketRelayService.HasStorageNetworkRelayOnModuleField(__instance, "module"))
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
                if (__result != ProcessCondition.Status.Failure ||
                    !StorageNetworkRocketRelayService.HasStorageNetworkRelayOnModuleField(__instance, "rocketModule"))
                {
                    return;
                }

                __result = ProcessCondition.Status.Warning;
            }
        }

    }
}
