using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPowerOverlayBatterySyncPatch
    {
        [HarmonyPatch(typeof(BatteryUI), nameof(BatteryUI.SetContent))]
        public static class BatteryUISetContentPatch
        {
            private static readonly AccessTools.FieldRef<BatteryUI, LocText> CurrentKJLabelRef =
                CreateFieldRef<LocText>("currentKJLabel");
            private static readonly AccessTools.FieldRef<BatteryUI, LocText> UnitLabelRef =
                CreateFieldRef<LocText>("unitLabel");
            private static readonly AccessTools.FieldRef<BatteryUI, Image> BatteryBGRef =
                CreateFieldRef<Image>("batteryBG");
            private static readonly AccessTools.FieldRef<BatteryUI, Image> BatteryMeterRef =
                CreateFieldRef<Image>("batteryMeter");

            public static void Postfix(BatteryUI __instance, Battery bat)
            {
                if (__instance == null || bat == null || !ShouldForceWhite(bat))
                {
                    return;
                }

                Color color = Color.white;

                Image batteryBG = BatteryBGRef != null ? BatteryBGRef(__instance) : null;
                if (batteryBG != null)
                {
                    batteryBG.color = color;
                }

                Image batteryMeter = BatteryMeterRef != null ? BatteryMeterRef(__instance) : null;
                if (batteryMeter != null)
                {
                    batteryMeter.color = color;
                }

                LocText currentKJLabel = CurrentKJLabelRef != null ? CurrentKJLabelRef(__instance) : null;
                if (currentKJLabel != null)
                {
                    currentKJLabel.color = color;
                }

                LocText unitLabel = UnitLabelRef != null ? UnitLabelRef(__instance) : null;
                if (unitLabel != null)
                {
                    unitLabel.color = color;
                }
            }

            private static bool ShouldForceWhite(Battery bat)
            {
                return StorageNetworkPowerOverlayBattery.ShouldForceWhiteUi(bat);
            }

            private static AccessTools.FieldRef<BatteryUI, T> CreateFieldRef<T>(string fieldName)
                where T : class
            {
                try
                {
                    return AccessTools.FieldRefAccess<BatteryUI, T>(fieldName);
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
