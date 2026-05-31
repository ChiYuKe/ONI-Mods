using HarmonyLib;
using StorageNetwork.Components;

namespace StorageNetwork.Patches
{
    public static class GeyserElementEmitterPatch
    {
        [HarmonyPatch(typeof(ElementEmitter), "OnSimActivate")]
        public static class ElementEmitterOnSimActivatePatch
        {
            public static bool Prefix(ElementEmitter __instance)
            {
                StorageNetworkGeyserOutput output = __instance.GetComponent<StorageNetworkGeyserOutput>();
                return output == null || !output.CanCaptureOutput();
            }
        }
    }
}
