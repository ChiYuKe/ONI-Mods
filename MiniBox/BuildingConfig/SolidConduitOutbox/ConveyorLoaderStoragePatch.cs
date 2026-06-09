using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.SolidConduitOutbox
{
    [HarmonyPatch(typeof(SolidConduitOutboxConfig), "ConfigureBuildingTemplate")]
    public class ConveyorLoaderStoragePatch
    {
        private static void Postfix(ref GameObject go)
        {
            float capacityKg = ModSettings.Current.SolidConduitOutboxCapacityKg;
            Storage storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.capacityKg = capacityKg;
            go.AddOrGet<SolidConduitConsumer>().capacityKG = capacityKg;
        }
    }
}





