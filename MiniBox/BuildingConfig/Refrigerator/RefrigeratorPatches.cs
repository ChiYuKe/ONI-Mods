using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.Refrigerator
{
    [HarmonyPatch(typeof(RefrigeratorConfig), "CreateBuildingDef")]
    internal class RefrigeratorBuildingDefPatch
    {
        private static void Postfix(ref BuildingDef __result)
        {
            var cfg = ModSettings.Current;
            __result.EnergyConsumptionWhenActive = cfg.RefrigeratorPowerConsumption;
            __result.Floodable = cfg.RefrigeratorCanFlood;
            __result.Overheatable = cfg.RefrigeratorCanOverheat;
        }
    }

    [HarmonyPatch(typeof(RefrigeratorConfig), "DoPostConfigureComplete")]
    internal class RefrigeratorStoragePatch
    {
        private static void Postfix(GameObject go)
        {
            Storage storage = go.AddOrGet<Storage>();
            storage.capacityKg = ModSettings.Current.RefrigeratorCapacityKg;
        }
    }
}





