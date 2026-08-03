using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkPowerOverlayBattery : Battery
    {
        private const float MaxNativeBatteryUiCapacity = 60000f;

        private static readonly AccessTools.FieldRef<Battery, float> JoulesAvailableRef =
            CreateFieldRef<float>("joulesAvailable");
        private static readonly AccessTools.FieldRef<Battery, float> PreviousJoulesAvailableRef =
            CreateFieldRef<float>("PreviousJoulesAvailable");
        private static readonly AccessTools.FieldRef<Battery, int> PowerCellRef =
            CreateFieldRef<int>("<PowerCell>k__BackingField");

        private static readonly FieldInfo JoulesAvailableField =
            AccessTools.Field(typeof(Battery), "joulesAvailable");
        private static readonly FieldInfo PreviousJoulesAvailableField =
            AccessTools.Field(typeof(Battery), "PreviousJoulesAvailable");
        private static readonly FieldInfo PowerCellField =
            AccessTools.Field(typeof(Battery), "<PowerCell>k__BackingField");
        private static readonly HashSet<Battery> WhiteUiBatteries = new HashSet<Battery>();

        [MyCmpGet]
        private StorageNetworkPowerStorage powerStorage = null;

        [MyCmpGet]
        private Building building = null;

        private float lastUiJoules;

        public float RealJoulesAvailable => powerStorage != null ? powerStorage.RawJoulesAvailable : 0f;

        protected override void OnSpawn()
        {
            global::Components.Batteries.Add(this);
            RegisterWhiteUiBattery(this);
            SyncFromPowerStorage(true);
        }

        protected override void OnCleanUp()
        {
            UnregisterWhiteUiBattery(this);
            global::Components.Batteries.Remove(this);
        }

        public override void EnergySim200ms(float dt)
        {
            SyncFromPowerStorage(false);
        }

        public void SyncFromPowerStorage(bool resetPrevious)
        {
            if (powerStorage == null)
            {
                return;
            }

            float realCapacity = Mathf.Max(1f, powerStorage.CapacityJoules);
            float uiCapacity = Mathf.Min(realCapacity, MaxNativeBatteryUiCapacity);
            float joules = realCapacity > 0f
                ? Mathf.Clamp01(RealJoulesAvailable / realCapacity) * uiCapacity
                : 0f;
            capacity = uiCapacity;
            joulesLostPerSecond = 0f;
            chargeWattage = 0f;
            powerSortOrder = 1000;
            if (building != null)
            {
                SetPrivateField(
                    PowerCellRef,
                    PowerCellField,
                    this,
                    Grid.PosToCell(building.transform.GetPosition()));
            }

            float previousJoules = resetPrevious
                ? Mathf.Max(0f, joules - 1f)
                : lastUiJoules;
            if (joules < previousJoules)
            {
                previousJoules = Mathf.Max(0f, joules - 1f);
            }

            SetPrivateField(
                PreviousJoulesAvailableRef,
                PreviousJoulesAvailableField,
                this,
                previousJoules);
            SetPrivateField(
                JoulesAvailableRef,
                JoulesAvailableField,
                this,
                joules);
            lastUiJoules = joules;
        }

        internal static void RegisterWhiteUiBattery(Battery battery)
        {
            if (battery != null)
            {
                WhiteUiBatteries.Add(battery);
            }
        }

        internal static void UnregisterWhiteUiBattery(Battery battery)
        {
            if (battery != null)
            {
                WhiteUiBatteries.Remove(battery);
            }
        }

        internal static bool ShouldForceWhiteUi(Battery battery)
        {
            return battery != null && WhiteUiBatteries.Contains(battery);
        }

        internal static void ResetRuntimeState()
        {
            WhiteUiBatteries.Clear();
        }

        private static AccessTools.FieldRef<Battery, T> CreateFieldRef<T>(string fieldName)
        {
            try
            {
                return AccessTools.FieldRefAccess<Battery, T>(fieldName);
            }
            catch
            {
                return null;
            }
        }

        private static void SetPrivateField<T>(
            AccessTools.FieldRef<Battery, T> fieldRef,
            FieldInfo fallback,
            Battery battery,
            T value)
        {
            if (fieldRef != null)
            {
                fieldRef(battery) = value;
                return;
            }

            fallback?.SetValue(battery, value);
        }
    }
}
