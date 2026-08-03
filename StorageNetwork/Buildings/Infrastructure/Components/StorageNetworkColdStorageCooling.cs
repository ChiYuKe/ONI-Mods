using KSerialization;
using System.Collections.Generic;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkColdStorageCooling : KMonoBehaviour, ISingleSliderControl, IGameObjectEffectDescriptor
    {
        public const float DefaultTargetTemperature = 274.15f;
        public const float MinTargetTemperature = 253.15f;
        public const float MaxTargetTemperature = 274.15f;
        public const float ThermalConductivity = 10f;
        public const float HeatCapacity = 400f;
        public const float EnergySaverPowerWatts = 20f;
        public const float MaxCoolingPowerWatts = 1000f;
        public const float BaseCoolingHeatKW = 0.375f;
        public const float MaxCoolingHeatKW = 1f;
        public static readonly float EnergySaverHeatKW = 0f;
        public static readonly List<Storage.StoredItemModifier> StoredItemModifiers = new List<Storage.StoredItemModifier>
        {
            Storage.StoredItemModifier.Hide
        };

        [Serialize]
        private float coolingRate = DefaultTargetTemperature;
        private bool energySaving;
        private float appliedWattage = float.NaN;
        private System.Action<SimTemperatureTransfer> applyToItemOnRegistered;

        [MyCmpGet]
        private EnergyConsumer energyConsumer = null;

        [MyCmpGet]
        private Building building = null;

        [MyCmpGet]
        private Operational operational = null;

        [MyCmpGet]
        private Storage storage = null;

        public bool IsEnergySaving => energySaving;

        public float TargetTemperature
        {
            get => NormalizeTemperature(coolingRate);
            set
            {
                float normalized = NormalizeTemperature(value);
                if (Mathf.Approximately(coolingRate, normalized))
                {
                    return;
                }

                coolingRate = normalized;
                ApplyEnergyProfile();
                if (!ApplyToRunningController())
                {
                    ApplyToStoredItems();
                }
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            applyToItemOnRegistered = ApplyToItem;
            coolingRate = NormalizeTemperature(IsLegacyCoolingRate(coolingRate) ? DefaultTargetTemperature : coolingRate);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChanged);
            storage?.SetDefaultStoredItemModifiers(StoredItemModifiers);
            ApplyEnergyProfile();
            if (!ApplyToRunningController())
            {
                ApplyToStoredItems();
            }
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.OnStorageChange, OnStorageChanged);
            base.OnCleanUp();
        }

        private void OnStorageChanged(object data)
        {
            if (data is GameObject item)
            {
                if (storage?.items != null &&
                    storage.items.Contains(item) &&
                    operational?.IsActive == true)
                {
                    ApplyToItem(EnsureItemCanExchangeTemperature(item));
                }

                return;
            }

            ApplyToStoredItems();
        }

        public void ResetToDefault()
        {
            TargetTemperature = DefaultTargetTemperature;
        }

        public static float NormalizeTemperature(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = DefaultTargetTemperature;
            }

            return Mathf.Clamp(value, GetMinTargetTemperature(), GetMaxTargetTemperature());
        }

        public static float NormalizeDisplayTemperature(float value)
        {
            return GameUtil.GetConvertedTemperature(NormalizeTemperature(GameUtil.GetTemperatureConvertedToKelvin(value)), true);
        }

        public void ApplyToItem(SimTemperatureTransfer transfer)
        {
            if (transfer == null || !Sim.IsValidHandle(transfer.SimHandle))
            {
                return;
            }

            float targetTemperature = GetItemCoolingTargetTemperature(transfer);
            SimMessages.ModifyElementChunkTemperatureAdjuster(transfer.SimHandle, targetTemperature, HeatCapacity, ThermalConductivity);
        }

        private float GetItemCoolingTargetTemperature(SimTemperatureTransfer transfer)
        {
            PrimaryElement primaryElement = transfer.GetComponent<PrimaryElement>();
            if (primaryElement == null)
            {
                return TargetTemperature;
            }

            return Mathf.Min(TargetTemperature, primaryElement.Temperature);
        }

        private bool ApplyToRunningController()
        {
            StorageNetworkColdStorageController.Instance instance =
                gameObject.GetSMI<StorageNetworkColdStorageController.Instance>();
            if (instance == null)
            {
                return false;
            }

            instance.RefreshTemperatureAdjuster();
            return true;
        }

        public void ApplyEnergyProfile()
        {
            if (energyConsumer == null)
            {
                return;
            }

            float wattage = energySaving ? GetEnergySaverPowerWatts() : GetCoolingPowerWatts();
            if (Mathf.Approximately(appliedWattage, wattage))
            {
                return;
            }

            appliedWattage = wattage;
            energyConsumer.BaseWattageRating = wattage;
        }

        public void SetEnergySaving(bool value)
        {
            if (energySaving == value)
            {
                return;
            }

            energySaving = value;
            ApplyEnergyProfile();
        }

        public float GetEnergySaverPowerWatts()
        {
            float baseWatts = building?.Def?.EnergyConsumptionWhenActive ?? 0f;
            if (baseWatts <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(Config.Instance.ColdStorageEnergySaverWatts, baseWatts);
        }

        public float GetCoolingPowerWatts()
        {
            float baseWatts = building?.Def?.EnergyConsumptionWhenActive ?? 0f;
            if (baseWatts <= 0f)
            {
                return 0f;
            }

            return Mathf.Lerp(baseWatts, Config.Instance.ColdStorageMaxCoolingWatts, GetCoolingIntensity());
        }

        public float GetCoolingHeatKW()
        {
            return Mathf.Lerp(BaseCoolingHeatKW, Config.Instance.ColdStorageMaxCoolingHeatKW, GetCoolingIntensity());
        }

        public float GetCurrentHeatKW()
        {
            return energySaving ? EnergySaverHeatKW : GetCoolingHeatKW();
        }

        public string GetFormattedCoolingHeat()
        {
            return GameUtil.GetFormattedHeatEnergy(GetCoolingHeatKW() * 1000f, GameUtil.HeatEnergyFormatterUnit.Automatic);
        }

        public string GetFormattedCurrentHeat()
        {
            return GameUtil.GetFormattedHeatEnergy(GetCurrentHeatKW() * 1000f, GameUtil.HeatEnergyFormatterUnit.Automatic);
        }

        private float GetCoolingIntensity()
        {
            return Mathf.InverseLerp(GetMaxTargetTemperature(), GetMinTargetTemperature(), TargetTemperature);
        }

        public void ApplyToStoredItems()
        {
            if (operational == null || storage == null || !operational.IsActive)
            {
                return;
            }

            foreach (GameObject item in storage.items)
            {
                if (item != null)
                {
                    SimTemperatureTransfer transfer = EnsureItemCanExchangeTemperature(item);
                    ApplyToItem(transfer);
                }
            }
        }

        private SimTemperatureTransfer EnsureItemCanExchangeTemperature(GameObject item)
        {
            Storage.MakeItemSealed(item, false, false);
            Storage.MakeItemTemperatureInsulated(item, false, false);
            SimTemperatureTransfer transfer = item.GetComponent<SimTemperatureTransfer>();
            if (transfer == null)
            {
                return null;
            }

            transfer.onSimRegistered = (System.Action<SimTemperatureTransfer>)System.Delegate.Remove(
                transfer.onSimRegistered,
                applyToItemOnRegistered);
            transfer.onSimRegistered = (System.Action<SimTemperatureTransfer>)System.Delegate.Combine(
                transfer.onSimRegistered,
                applyToItemOnRegistered);
            transfer.enabled = true;
            return transfer;
        }

        public string SliderTitleKey => "STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_SIDE_SCREEN_TITLE";

        public string SliderUnits => GameUtil.GetTemperatureUnitSuffix();

        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        public float GetSliderMin(int index)
        {
            return GameUtil.GetConvertedTemperature(GetMinTargetTemperature(), true);
        }

        public float GetSliderMax(int index)
        {
            return GameUtil.GetConvertedTemperature(GetMaxTargetTemperature(), true);
        }

        public float GetSliderValue(int index)
        {
            return GameUtil.GetConvertedTemperature(TargetTemperature, true);
        }

        public void SetSliderValue(float value, int index)
        {
            TargetTemperature = GameUtil.GetTemperatureConvertedToKelvin(value);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_DESC";
        }

        public string GetSliderTooltip(int index)
        {
            return Strings.Get(GetSliderTooltipKey(index));
        }

        public string GetFormattedTargetTemperature()
        {
            return GameUtil.GetFormattedTemperature(TargetTemperature, GameUtil.TimeSlice.None, GameUtil.TemperatureInterpretation.Absolute, true, false);
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> descriptors = SimulatedTemperatureAdjuster.GetDescriptors(TargetTemperature);
            Descriptor heatDescriptor = default;
            string formattedHeat = GetFormattedCoolingHeat();
            heatDescriptor.SetupDescriptor(
                string.Format(global::STRINGS.UI.BUILDINGEFFECTS.HEATGENERATED, formattedHeat),
                string.Format(global::STRINGS.UI.BUILDINGEFFECTS.TOOLTIPS.HEATGENERATED, formattedHeat),
                Descriptor.DescriptorType.Effect);
            descriptors.Add(heatDescriptor);

            descriptors.Add(new Descriptor(
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_POWER), GetCoolingPowerWatts().ToString("0.#")),
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.COLD_STORAGE_COOLING_POWER_TOOLTIP), GetCoolingPowerWatts().ToString("0.#"), GetEnergySaverPowerWatts().ToString("0.#")),
                Descriptor.DescriptorType.Effect,
                false));
            return descriptors;
        }

        private static bool IsLegacyCoolingRate(float value)
        {
            return value <= 0f || value > GetMaxTargetTemperature() + 50f;
        }

        public static float GetMinTargetTemperature()
        {
            return Config.Instance.ColdStorageMinTemperatureC + 273.15f;
        }

        public static float GetMaxTargetTemperature()
        {
            return Config.Instance.ColdStorageMaxTemperatureC + 273.15f;
        }
    }
}
