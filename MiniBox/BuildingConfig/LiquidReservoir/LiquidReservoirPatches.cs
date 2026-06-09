using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.LiquidReservoir
{
    [HarmonyPatch(typeof(LiquidReservoirConfig), "ConfigureBuildingTemplate")]
    public class LiquidReservoirStoragePatch
    {
        public static void Postfix(ref GameObject go)
        {
            Storage storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.capacityKg = ModSettings.Current.LiquidReservoirCapacityKg;
            storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            storage.allowUIItemRemoval = true;

            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.capacityKG = storage.capacityKg;
        }
    }

    [HarmonyPatch(typeof(LiquidReservoirConfig), "CreateBuildingDef")]
    public class LiquidReservoirBuildingDefPatch
    {
        public static void Postfix(ref BuildingDef __result)
        {
            var cfg = ModSettings.Current;
            __result.PermittedRotations = PermittedRotations.R360;
            __result.BuildLocationRule = BuildLocationRule.Anywhere;
            __result.ContinuouslyCheckFoundation = cfg.LiquidReservoirRequiresFoundation;
            __result.Overheatable = cfg.LiquidReservoirCanOverheat;
        }
    }
}





