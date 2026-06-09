using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.GasReservoir
{
    [HarmonyPatch(typeof(GasReservoirConfig), "ConfigureBuildingTemplate")]
    public class GasReservoirStoragePatch
    {
        public static void Postfix(ref GameObject go)
        {
            Storage storage = BuildingTemplates.CreateDefaultStorage(go, false);
            storage.capacityKg = ModSettings.Current.GasReservoirCapacityKg;
            storage.SetDefaultStoredItemModifiers(Storage.StandardInsulatedStorage);
            storage.allowUIItemRemoval = true;

            ConduitConsumer conduitConsumer = go.AddOrGet<ConduitConsumer>();
            conduitConsumer.capacityKG = storage.capacityKg;
        }
    }

    [HarmonyPatch(typeof(GasReservoirConfig), "CreateBuildingDef")]
    public class GasReservoirAttributesPatch
    {
        public static void Postfix(ref BuildingDef __result)
        {
            var cfg = ModSettings.Current;
            __result.PermittedRotations = PermittedRotations.R360;
            __result.BuildLocationRule = BuildLocationRule.Anywhere;
            __result.ContinuouslyCheckFoundation = cfg.GasReservoirRequiresFoundation;
            __result.Overheatable = cfg.GasReservoirCanOverheat;
        }
    }
}





