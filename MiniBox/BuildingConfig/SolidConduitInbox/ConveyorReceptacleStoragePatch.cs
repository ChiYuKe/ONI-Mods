using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.SolidConduitInbox
{
    [HarmonyPatch(typeof(SolidConduitInboxConfig), "DoPostConfigureComplete")]
    public class ConveyorReceptacleStoragePatch
    {
        public static void Postfix(ref GameObject go)
        {
            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = ModSettings.Current.SolidConduitInboxCapacityKg;
        }
    }
}





