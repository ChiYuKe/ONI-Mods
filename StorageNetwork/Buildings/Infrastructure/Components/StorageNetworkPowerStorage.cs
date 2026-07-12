using System;
using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPowerStorage : KMonoBehaviour, ISim1000ms, IGameObjectEffectDescriptor
    {
        private const float SecondsPerCycle = 600f;
        private const float FullSnapJoules = 0.5f;

        [SerializeField]
        public float capacityJoules;

        [SerializeField]
        public float joulesLostPerSecond;

        [Serialize]
        private float joulesAvailable;

        private static StatusItem joulesStatusItem;
        private static StatusItem heatStatusItem;
        private Guid joulesStatusHandle = Guid.Empty;
        private Guid heatStatusHandle = Guid.Empty;

        public float JoulesAvailable
        {
            get
            {
                float capacity = CapacityJoules;
                float joules = Mathf.Clamp(joulesAvailable, 0f, capacity);
                return capacity - joules <= JoulesLostPerSecond * 0.25f ? capacity : joules;
            }
        }

        public float RawJoulesAvailable => Mathf.Clamp(joulesAvailable, 0f, CapacityJoules);

        public float CapacityJoules => Mathf.Max(0f, capacityJoules);

        public float JoulesLostPerCycle => Mathf.Max(0f, joulesLostPerSecond);

        public float JoulesLostPerSecond => JoulesLostPerCycle / SecondsPerCycle;

        public float AvailableCapacityJoules => Mathf.Max(0f, CapacityJoules - RawJoulesAvailable);

        public bool IsOnline
        {
            get
            {
                Storage storage = GetComponent<Storage>();
                return StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageSceneRegistry.Register(gameObject);
            if (IsBatteryServerPrefab())
            {
                joulesLostPerSecond = Config.Instance.BatteryServerLeakJoulesPerCycle;
            }

            joulesAvailable = Mathf.Clamp(joulesAvailable, 0f, CapacityJoules);
            AddStatusItems();
        }

        protected override void OnCleanUp()
        {
            RemoveStatusItems();
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            joulesAvailable = Mathf.Clamp(joulesAvailable - JoulesLostPerSecond * dt, 0f, CapacityJoules);
        }

        public float AddEnergy(float joules)
        {
            if (!IsOnline || joules <= 0f)
            {
                return 0f;
            }

            float before = RawJoulesAvailable;
            joulesAvailable += Mathf.Min(joules, AvailableCapacityJoules);
            if (CapacityJoules - joulesAvailable <= FullSnapJoules)
            {
                joulesAvailable = CapacityJoules;
            }

            return RawJoulesAvailable - before;
        }

        public float ConsumeEnergy(float joules)
        {
            if (!IsOnline || joules <= 0f)
            {
                return 0f;
            }

            float consumed = Mathf.Min(joules, RawJoulesAvailable);
            joulesAvailable -= consumed;
            return consumed;
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            return new List<Descriptor>
            {
                new Descriptor(
                    string.Format(global::STRINGS.UI.BUILDINGEFFECTS.BATTERYCAPACITY, GameUtil.GetFormattedJoules(CapacityJoules, "", GameUtil.TimeSlice.None)),
                    string.Format(global::STRINGS.UI.BUILDINGEFFECTS.TOOLTIPS.BATTERYCAPACITY, GameUtil.GetFormattedJoules(CapacityJoules, "", GameUtil.TimeSlice.None)),
                    Descriptor.DescriptorType.Effect,
                    false),
                new Descriptor(
                    string.Format(global::STRINGS.UI.BUILDINGEFFECTS.BATTERYLEAK, FormatJoulesPerCycle(JoulesLostPerCycle)),
                    string.Format(global::STRINGS.UI.BUILDINGEFFECTS.TOOLTIPS.BATTERYLEAK, FormatJoulesPerCycle(JoulesLostPerCycle)),
                    Descriptor.DescriptorType.Effect,
                    false)
            };
        }

        private bool IsBatteryServerPrefab()
        {
            KPrefabID prefabId = GetComponent<KPrefabID>();
            Tag prefabTag = prefabId != null ? prefabId.PrefabTag : Tag.Invalid;
            return prefabTag == SmallBatteryServerConfig.ID ||
                   prefabTag == MediumBatteryServerConfig.ID ||
                   prefabTag == LargeBatteryServerConfig.ID;
        }

        private void AddStatusItems()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null)
            {
                return;
            }

            joulesStatusHandle = selectable.AddStatusItem(GetJoulesStatusItem(), this);
            if (GetSelfHeatKilowatts() > 0f)
            {
                heatStatusHandle = selectable.AddStatusItem(GetHeatStatusItem(), this);
            }
        }

        private void RemoveStatusItems()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null)
            {
                joulesStatusHandle = Guid.Empty;
                heatStatusHandle = Guid.Empty;
                return;
            }

            if (joulesStatusHandle != Guid.Empty)
            {
                selectable.RemoveStatusItem(joulesStatusHandle);
            }

            if (heatStatusHandle != Guid.Empty)
            {
                selectable.RemoveStatusItem(heatStatusHandle);
            }

            joulesStatusHandle = Guid.Empty;
            heatStatusHandle = Guid.Empty;
        }

        private static StatusItem GetJoulesStatusItem()
        {
            if (joulesStatusItem != null)
            {
                return joulesStatusItem;
            }

            joulesStatusItem = new StatusItem(
                "StorageNetworkPowerStorageJoules",
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STORAGE_JOULES_STATUS),
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STORAGE_JOULES_TOOLTIP),
                "",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.Power.ID,
                129022,
                true);
            joulesStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkPowerStorage powerStorage = data as StorageNetworkPowerStorage;
                if (powerStorage == null)
                {
                    return text;
                }

                return text
                    .Replace("{JoulesAvailable}", GameUtil.GetFormattedJoules(powerStorage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None))
                    .Replace("{JoulesCapacity}", GameUtil.GetFormattedJoules(powerStorage.CapacityJoules, "F2", GameUtil.TimeSlice.None))
                    .Replace("{JoulesPercent}", GameUtil.GetFormattedPercent(powerStorage.CapacityJoules > 0f ? powerStorage.RawJoulesAvailable / powerStorage.CapacityJoules * 100f : 0f));
            };

            return joulesStatusItem;
        }

        private static StatusItem GetHeatStatusItem()
        {
            if (heatStatusItem != null)
            {
                return heatStatusItem;
            }

            heatStatusItem = new StatusItem(
                "StorageNetworkPowerStorageHeat",
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STORAGE_HEAT_STATUS),
                Loc.Get(Loc.UI.STORAGE_NETWORK.POWER_STORAGE_HEAT_TOOLTIP),
                "",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.Temperature.ID,
                129022,
                true);
            heatStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkPowerStorage powerStorage = data as StorageNetworkPowerStorage;
                if (powerStorage == null)
                {
                    return text;
                }

                return text.Replace("{Heat}", GameUtil.GetFormattedHeatEnergy(powerStorage.GetSelfHeatKilowatts() * 1000f, GameUtil.HeatEnergyFormatterUnit.Automatic));
            };

            return heatStatusItem;
        }

        private float GetSelfHeatKilowatts()
        {
            Building building = GetComponent<Building>();
            return building?.Def != null ? Mathf.Max(0f, building.Def.SelfHeatKilowattsWhenActive) : 0f;
        }

        private static string FormatJoulesPerCycle(float joules)
        {
            return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.TREND_PER_CYCLE), string.Empty, GameUtil.GetFormattedJoules(joules, "F1", GameUtil.TimeSlice.None));
        }
    }
}
