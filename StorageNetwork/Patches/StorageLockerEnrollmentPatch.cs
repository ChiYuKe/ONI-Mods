using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Patches
{
    public static class StorageLockerEnrollmentPatch
    {
        [HarmonyPatch(typeof(StorageLockerConfig), nameof(StorageLockerConfig.ConfigureBuildingTemplate))]
        public static class StorageLockerConfigConfigureBuildingTemplatePatch
        {
            public static void Postfix(UnityEngine.GameObject go)
            {
                go.AddOrGet<StorageNetworkEnrollment>();
            }
        }

    }
}
