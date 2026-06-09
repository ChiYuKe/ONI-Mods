using HarmonyLib;

namespace MiniBox.BuildingConfig.PowerTransformerSmall
{
    [HarmonyPatch(typeof(PowerTransformerSmallConfig), "CreateBuildingDef")]
    public class SmallPowerTransformerHeatPatch
    {
        public static void Postfix(ref BuildingDef __result)
        {
            bool heatGeneration = ModSettings.Current.EnableSmallPowerTransformerHeatGeneration;
            __result.ExhaustKilowattsWhenActive = heatGeneration ? 0.25f : 0f;
            __result.SelfHeatKilowattsWhenActive = heatGeneration ? 1f : 0f;
        }
    }
}





