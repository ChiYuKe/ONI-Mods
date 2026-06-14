using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkPowerOverlayBattery : Battery
    {
        private const float MaxNativeBatteryUiCapacity = 60000f;

        private static readonly System.Reflection.FieldInfo JoulesAvailableField =
            AccessTools.Field(typeof(Battery), "joulesAvailable");

        private static readonly System.Reflection.FieldInfo PreviousJoulesAvailableField =
            AccessTools.Field(typeof(Battery), "PreviousJoulesAvailable");

        private static readonly System.Reflection.FieldInfo PowerCellField =
            AccessTools.Field(typeof(Battery), "<PowerCell>k__BackingField");

        [MyCmpGet]
        private StorageNetworkPowerStorage powerStorage = null;

        private float lastUiJoules;

        public float RealJoulesAvailable => powerStorage != null ? powerStorage.RawJoulesAvailable : 0f;

        protected override void OnSpawn()
        {
            global::Components.Batteries.Add(this);
            SyncFromPowerStorage(true);
        }

        protected override void OnCleanUp()
        {
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
            Building building = GetComponent<Building>();
            if (building != null)
            {
                PowerCellField.SetValue(this, Grid.PosToCell(building.transform.GetPosition()));
            }

            float previousJoules = resetPrevious
                ? Mathf.Max(0f, joules - 1f)
                : lastUiJoules;
            if (joules < previousJoules)
            {
                previousJoules = Mathf.Max(0f, joules - 1f);
            }

            PreviousJoulesAvailableField.SetValue(this, previousJoules);
            JoulesAvailableField.SetValue(this, joules);
            lastUiJoules = joules;
        }
    }
}
