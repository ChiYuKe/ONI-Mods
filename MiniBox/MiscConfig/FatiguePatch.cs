using HarmonyLib;
using TUNING;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    public class FatiguePatch
    {
        private const float DefaultStaminaUsedPerSecond = -0.11666667f;

        private static void Prefix()
        {
            DUPLICANTSTATS.STANDARD.BaseStats.STAMINA_USED_PER_SECOND =
                ModSettings.Current.DisableFatigue ? 0f : DefaultStaminaUsedPerSecond;
        }
    }
}





