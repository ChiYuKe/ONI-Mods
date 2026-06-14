using UnityEngine;
using STRINGS;
using System;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkColdStorageController : GameStateMachine<StorageNetworkColdStorageController, StorageNetworkColdStorageController.Instance>
    {
        public State inoperational;
        public OperationalStates operational;

        public override void InitializeStates(out StateMachine.BaseState defaultState)
        {
            defaultState = inoperational;
            inoperational.EventTransition(GameHashes.OperationalChanged, operational, IsOperational);
            operational.DefaultState(operational.steady)
                .EventTransition(GameHashes.OperationalChanged, inoperational, Not(IsOperational))
                .Enter(smi => smi.SetActive(true))
                .Exit(smi => smi.SetActive(false));
            operational.cooling
                .Enter(smi => smi.cooling.SetEnergySaving(false))
                .Enter(smi => smi.ShowModeStatus(Instance.GetCoolingStatusItem()))
                .Update("Cold storage cooling exhaust", (smi, dt) => smi.ApplyCoolingExhaust(dt), UpdateRate.SIM_200ms)
                .UpdateTransition(operational.steady, AllFoodCool, UpdateRate.SIM_4000ms);
            operational.steady
                .Enter(smi => smi.cooling.SetEnergySaving(true))
                .Enter(smi => smi.ShowModeStatus(Instance.GetSteadyStatusItem()))
                .Exit(smi => smi.cooling.SetEnergySaving(false))
                .Update("Cold storage steady exhaust", (smi, dt) => smi.ApplySteadyExhaust(dt), UpdateRate.SIM_200ms)
                .UpdateTransition(operational.cooling, AnyWarmFood, UpdateRate.SIM_4000ms);
        }

        private bool IsOperational(Instance smi)
        {
            return smi.operational.IsOperational;
        }

        private bool AllFoodCool(Instance smi, float dt)
        {
            foreach (GameObject item in smi.storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement != null && primaryElement.Temperature >= smi.cooling.TargetTemperature + 0.1f)
                {
                    return false;
                }
            }

            return true;
        }

        private bool AnyWarmFood(Instance smi, float dt)
        {
            foreach (GameObject item in smi.storage.items)
            {
                if (item == null)
                {
                    continue;
                }

                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement != null && primaryElement.Temperature >= smi.cooling.TargetTemperature + 2f)
                {
                    return true;
                }
            }

            return false;
        }

        public class OperationalStates : State
        {
            public State cooling;
            public State steady;
        }

        public class Def : StateMachine.BaseDef
        {
        }

        public new sealed class Instance : GameStateMachine<StorageNetworkColdStorageController, Instance, IStateMachineTarget, object>.GameInstance
        {
            [MyCmpReq]
            public Operational operational;

            [MyCmpReq]
            public Storage storage;

            [MyCmpReq]
            public StorageNetworkColdStorageCooling cooling;

            private readonly HandleVector<int>.Handle structureTemperature;
            private readonly SimulatedTemperatureAdjuster temperatureAdjuster;
            private Guid modeStatusHandle = Guid.Empty;
            private Guid heatStatusHandle = Guid.Empty;
            private static StatusItem coolingStatusItem;
            private static StatusItem steadyStatusItem;
            private static StatusItem heatStatusItem;

            public Instance(IStateMachineTarget master, Def def)
                : base(master)
            {
                temperatureAdjuster = new SimulatedTemperatureAdjuster(
                    cooling.TargetTemperature,
                    StorageNetworkColdStorageCooling.HeatCapacity,
                    StorageNetworkColdStorageCooling.ThermalConductivity,
                    storage);
                structureTemperature = GameComps.StructureTemperatures.GetHandle(gameObject);
            }

            protected override void OnCleanUp()
            {
                ClearColdStorageStatusItems();
                temperatureAdjuster.CleanUp();
                base.OnCleanUp();
            }

            public void SetActive(bool active)
            {
                operational.SetActive(active, false);
                if (active)
                {
                    ShowHeatStatus();
                    cooling.SetEnergySaving(false);
                    cooling.ApplyEnergyProfile();
                    cooling.ApplyToStoredItems();
                    RefreshTemperatureAdjuster();
                    return;
                }

                cooling.SetEnergySaving(false);
                ClearColdStorageStatusItems();
            }

            public void ShowModeStatus(StatusItem statusItem)
            {
                KSelectable selectable = GetComponent<KSelectable>();
                if (selectable == null || statusItem == null)
                {
                    return;
                }

                if (modeStatusHandle != Guid.Empty)
                {
                    selectable.RemoveStatusItem(modeStatusHandle);
                }

                modeStatusHandle = selectable.AddStatusItem(statusItem, this);
            }

            private void ShowHeatStatus()
            {
                if (heatStatusHandle != Guid.Empty)
                {
                    return;
                }

                KSelectable selectable = GetComponent<KSelectable>();
                if (selectable != null)
                {
                    heatStatusHandle = selectable.AddStatusItem(GetHeatStatusItem(), this);
                }
            }

            private void ClearColdStorageStatusItems()
            {
                KSelectable selectable = GetComponent<KSelectable>();
                if (selectable == null)
                {
                    modeStatusHandle = Guid.Empty;
                    heatStatusHandle = Guid.Empty;
                    return;
                }

                if (modeStatusHandle != Guid.Empty)
                {
                    selectable.RemoveStatusItem(modeStatusHandle);
                    modeStatusHandle = Guid.Empty;
                }

                if (heatStatusHandle != Guid.Empty)
                {
                    selectable.RemoveStatusItem(heatStatusHandle);
                    heatStatusHandle = Guid.Empty;
                }
            }

            public void RefreshTemperatureAdjuster()
            {
                TraverseTemperatureAdjuster.SetValues(
                    temperatureAdjuster,
                    cooling.TargetTemperature,
                    StorageNetworkColdStorageCooling.HeatCapacity,
                    StorageNetworkColdStorageCooling.ThermalConductivity);
                cooling.ApplyToStoredItems();
            }

            public void ApplyCoolingExhaust(float dt)
            {
                cooling.ApplyEnergyProfile();
                GameComps.StructureTemperatures.ProduceEnergy(
                    structureTemperature,
                    cooling.GetCoolingHeatKW() * dt,
                    BUILDING.STATUSITEMS.OPERATINGENERGY.FOOD_TRANSFER,
                    dt);
            }

            public void ApplySteadyExhaust(float dt)
            {
                cooling.ApplyEnergyProfile();
                if (StorageNetworkColdStorageCooling.EnergySaverHeatKW <= 0f)
                {
                    return;
                }

                GameComps.StructureTemperatures.ProduceEnergy(
                    structureTemperature,
                    StorageNetworkColdStorageCooling.EnergySaverHeatKW * dt,
                    BUILDING.STATUSITEMS.OPERATINGENERGY.FOOD_TRANSFER,
                    dt);
            }

            public static StatusItem GetCoolingStatusItem()
            {
                if (coolingStatusItem != null)
                {
                    return coolingStatusItem;
                }

                coolingStatusItem = new StatusItem(
                    "StorageNetworkColdStorageCooling",
                    "正在制冷",
                    "冷库服务器正在将内容物冷却到目标温度。",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    false);
                return coolingStatusItem;
            }

            public static StatusItem GetSteadyStatusItem()
            {
                if (steadyStatusItem != null)
                {
                    return steadyStatusItem;
                }

                steadyStatusItem = new StatusItem(
                    "StorageNetworkColdStorageSteady",
                    "节能模式: {Power}",
                    "内容物已经达到目标温度，冷库服务器正在以节能模式维持温度。",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    false);
                steadyStatusItem.resolveStringCallback = (text, data) =>
                {
                    Instance instance = data as Instance;
                    string power = instance?.cooling != null
                        ? GameUtil.GetFormattedWattage(instance.cooling.GetEnergySaverPowerWatts())
                        : string.Empty;
                    return text.Replace("{Power}", power);
                };
                return steadyStatusItem;
            }

            public static StatusItem GetHeatStatusItem()
            {
                if (heatStatusItem != null)
                {
                    return heatStatusItem;
                }

                heatStatusItem = new StatusItem(
                    "StorageNetworkColdStorageHeatGenerated",
                    "产热: {Heat}/秒",
                    "冷库服务器降温时产生的热量。目标温度越低，产热越高。",
                    string.Empty,
                    StatusItem.IconType.Info,
                    NotificationType.Neutral,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    false);
                heatStatusItem.resolveStringCallback = (text, data) =>
                {
                    Instance instance = data as Instance;
                    string heat = instance?.cooling != null ? instance.cooling.GetFormattedCurrentHeat() : string.Empty;
                    return text.Replace("{Heat}", heat);
                };
                return heatStatusItem;
            }
        }

        private static class TraverseTemperatureAdjuster
        {
            public static void SetValues(SimulatedTemperatureAdjuster adjuster, float temperature, float heatCapacity, float thermalConductivity)
            {
                HarmonyLib.Traverse traverse = HarmonyLib.Traverse.Create(adjuster);
                traverse.Field("temperature").SetValue(temperature);
                traverse.Field("heatCapacity").SetValue(heatCapacity);
                traverse.Field("thermalConductivity").SetValue(thermalConductivity);
            }
        }
    }
}
