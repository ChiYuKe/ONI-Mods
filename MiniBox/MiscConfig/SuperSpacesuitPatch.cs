using HarmonyLib;
using TUNING;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class EnableSuperSpaceSuit
    {
        private static void Prefix()
        {
            bool superAtmosuit = ModSettings.Current.EnableSuperSpaceSuit;
            EQUIPMENT.SUITS.ATMOSUIT_DIGGING = superAtmosuit ? 200 : 10;
            EQUIPMENT.SUITS.ATMOSUIT_INSULATION = superAtmosuit ? 3000 : 50;
            EQUIPMENT.SUITS.ATMOSUIT_SCALDING = superAtmosuit ? 6000 : 1000;
            EQUIPMENT.SUITS.ATMOSUIT_ATHLETICS = superAtmosuit ? 100 : -6;
            EQUIPMENT.SUITS.ATMOSUIT_THERMAL_CONDUCTIVITY_BARRIER = superAtmosuit ? 10f : 0.2f;
        }
    }
}





