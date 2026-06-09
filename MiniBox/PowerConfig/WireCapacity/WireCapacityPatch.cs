using HarmonyLib;

namespace MiniBox.PowerConfig.WireCapacity
{
    [HarmonyPatch(typeof(Wire), "GetMaxWattageAsFloat")]
    internal class WireCapacityPatch
    {
        public static void Postfix(ref float __result, Wire.WattageRating rating)
        {
            var cfg = ModSettings.Current;
            switch (rating)
            {
                case Wire.WattageRating.Max500:
                    __result = 500f;
                    break;
                case Wire.WattageRating.Max1000:
                    __result = cfg.WireLoadKw * 1000f;
                    break;
                case Wire.WattageRating.Max2000:
                    __result = cfg.ConductiveWireLoadKw * 1000f;
                    break;
                case Wire.WattageRating.Max20000:
                    __result = cfg.HighLoadWireLoadKw * 1000f;
                    break;
                case Wire.WattageRating.Max50000:
                    __result = cfg.HighLoadConductiveWireLoadKw * 1000f;
                    break;
                default:
                    __result = 0f;
                    break;
            }
        }
    }
}





