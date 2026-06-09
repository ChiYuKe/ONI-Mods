using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.MineralDeoxidizer
{
    [HarmonyPatch(typeof(MineralDeoxidizerConfig), "ConfigureBuildingTemplate")]
    internal class MineralDeoxidizerOutputPatch
    {
        private static void Postfix(GameObject go)
        {
            var cfg = ModSettings.Current;
            go.AddOrGet<ElementConverter>().outputElements = new ElementConverter.OutputElement[]
            {
                new ElementConverter.OutputElement(cfg.MineralDeoxidizerOxygenOutputKgPerSecond, SimHashes.Oxygen, cfg.MineralDeoxidizerOutputTemperature + 273.15f, false, false, 0f, 1f, 1f, byte.MaxValue, 0, true)
            };
            Prioritizable.AddRef(go);
        }
    }

    [HarmonyPatch(typeof(MineralDeoxidizerConfig), "CreateBuildingDef")]
    public class MineralDeoxidizerBuildingDefPatch
    {
        public static void Postfix(ref BuildingDef __result)
        {
            var cfg = ModSettings.Current;
            bool heatGeneration = cfg.EnableMineralDeoxidizerHeatGeneration;
            __result.EnergyConsumptionWhenActive = cfg.MineralDeoxidizerPowerConsumption;
            __result.Floodable = cfg.MineralDeoxidizerCanFlood;
            __result.Overheatable = cfg.MineralDeoxidizerCanOverheat;
            __result.ExhaustKilowattsWhenActive = heatGeneration ? 0.5f : 0f;
            __result.SelfHeatKilowattsWhenActive = heatGeneration ? 1f : 0f;
        }
    }
}





