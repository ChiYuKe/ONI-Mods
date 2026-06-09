using HarmonyLib;

namespace MiniBox.BuildingConfig.PowerTransformer
{
    [HarmonyPatch(typeof(PowerTransformerConfig), "CreateBuildingDef")]
    public class PowerTransformerHeatPatch
    {
        public static void Postfix(ref BuildingDef __result)
        {
            bool heatGeneration = ModSettings.Current.EnablePowerTransformerHeatGeneration;
            __result.ExhaustKilowattsWhenActive = heatGeneration ? 0.25f : 0f;
            __result.SelfHeatKilowattsWhenActive = heatGeneration ? 1f : 0f;
        }
    }
}





