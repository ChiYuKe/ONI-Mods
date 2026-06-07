using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkRocketRelayService
    {
        public static bool HasStorageNetworkRelay(ConditionHasNosecone condition)
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

        public static bool HasStorageNetworkRelayOnModuleField(object condition, string rocketModuleFieldName)
        {
            RocketModuleCluster module = Traverse.Create(condition)
                .Field(rocketModuleFieldName)
                .GetValue<RocketModuleCluster>();
            CraftModuleInterface craftInterface = module != null ? module.CraftInterface : null;
            return HasStorageNetworkRelay(craftInterface);
        }

        public static bool HasStorageNetworkRelay(CraftModuleInterface craftInterface)
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
