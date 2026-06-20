using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPowerOverlayBatterySyncPatch
    {
        [HarmonyPatch(typeof(OverlayModes.Power), nameof(OverlayModes.Power.Update))]
        public static class PowerOverlayUpdatePatch
        {
            public static void Prefix()
            {
                foreach (Battery battery in global::Components.Batteries.Items)
                {
                    StorageNetworkPowerOverlayBattery overlayBattery = battery as StorageNetworkPowerOverlayBattery;
                    if (overlayBattery != null)
                    {
                        overlayBattery.SyncFromPowerStorage(false);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BatteryUI), nameof(BatteryUI.SetContent))]
        public static class BatteryUISetContentPatch
        {
            private static readonly System.Reflection.FieldInfo CurrentKJLabelField =
                AccessTools.Field(typeof(BatteryUI), "currentKJLabel");

            private static readonly System.Reflection.FieldInfo UnitLabelField =
                AccessTools.Field(typeof(BatteryUI), "unitLabel");

            private static readonly System.Reflection.FieldInfo BatteryBGField =
                AccessTools.Field(typeof(BatteryUI), "batteryBG");

            private static readonly System.Reflection.FieldInfo BatteryMeterField =
                AccessTools.Field(typeof(BatteryUI), "batteryMeter");

            public static void Postfix(BatteryUI __instance, Battery bat)
            {
                if (__instance == null || bat == null || !ShouldForceWhite(bat))
                {
                    return;
                }

                Color color = Color.white;

                Image batteryBG = BatteryBGField.GetValue(__instance) as Image;
                if (batteryBG != null)
                {
                    batteryBG.color = color;
                }

                Image batteryMeter = BatteryMeterField.GetValue(__instance) as Image;
                if (batteryMeter != null)
                {
                    batteryMeter.color = color;
                }

                LocText currentKJLabel = CurrentKJLabelField.GetValue(__instance) as LocText;
                if (currentKJLabel != null)
                {
                    currentKJLabel.color = color;
                }

                LocText unitLabel = UnitLabelField.GetValue(__instance) as LocText;
                if (unitLabel != null)
                {
                    unitLabel.color = color;
                }
            }

            private static bool ShouldForceWhite(Battery bat)
            {
                if (bat is StorageNetworkPowerOverlayBattery)
                {
                    return true;
                }

                return bat.GetComponent<StorageNetworkPowerInputPortConsumer>() != null;
            }
        }
    }
}
