using HarmonyLib;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(MopTool), "OnPrefabInit")]
    public class MopToolCapacityPatch
    {
        public static void Postfix()
        {
            MopTool.maxMopAmt = ModSettings.Current.UnlimitedMopping ? 900000000f : 150f;
        }
    }
}





