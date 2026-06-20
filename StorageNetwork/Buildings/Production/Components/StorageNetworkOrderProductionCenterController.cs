using System.Linq;
using UnityEngine;

namespace StorageNetwork.Components
{
#pragma warning disable CS0649
    public sealed class StorageNetworkOrderProductionCenterController : StateMachineComponent<StorageNetworkOrderProductionCenterController.StatesInstance>
    {
        private const string MeterAnimation = "meter_engravingdisk";
        private const string MeterTarget = "meter_target";
        private const string GreenLightSymbol = "green_light";
        private const string RedLightSymbol = "red_light";
        private const int MaxDiskCount = 3;

        [MyCmpGet]
        private StorageNetworkOrderProductionCenter center;

        [MyCmpGet]
        private KBatchedAnimController animController;

        private MeterController diskMeter;
        private int lastDiskCount = -1;
        private bool attemptedMeterCreation;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            diskMeter?.Unlink();
            diskMeter = null;
            smi.StopSM("OnCleanUp");
            base.OnCleanUp();
        }

        public void RefreshDiskMeter()
        {
            EnsureDiskMeter();

            int diskCount = GetDiskCount();
            if (diskCount == lastDiskCount)
            {
                return;
            }

            lastDiskCount = diskCount;
            if (diskMeter != null)
            {
                float percent = Mathf.Clamp01((float)diskCount / MaxDiskCount);
                diskMeter.SetPositionPercent(percent);
            }
        }

        private void EnsureDiskMeter()
        {
            if (diskMeter != null || animController == null)
            {
                return;
            }

            if (!animController.HasAnimation(MeterAnimation))
            {
                if (!attemptedMeterCreation)
                {
                    attemptedMeterCreation = true;
                    Debug.LogWarning("[StorageNetwork] Order production center missing animation '" + MeterAnimation + "'.");
                }

                return;
            }

            if (!HasSymbol(animController, MeterTarget))
            {
                if (!attemptedMeterCreation)
                {
                    attemptedMeterCreation = true;
                    Debug.LogWarning("[StorageNetwork] Order production center missing meter target symbol '" + MeterTarget + "'.");
                }

                return;
            }

            attemptedMeterCreation = true;
            diskMeter = new MeterController(
                animController,
                MeterTarget,
                MeterAnimation,
                Meter.Offset.Infront,
                Grid.SceneLayer.NoLayer,
                MeterTarget,
                GreenLightSymbol,
                RedLightSymbol);
            lastDiskCount = -1;
        }

        private int GetDiskCount()
        {
            return center?.DiskSlots?.Count(slot => slot != null && slot.HasDisk) ?? 0;
        }

        private static bool HasSymbol(KAnimControllerBase controller, string symbolName)
        {
            if (controller == null || string.IsNullOrEmpty(symbolName))
            {
                return false;
            }

            bool visible;
            controller.GetSymbolTransform(new HashedString(symbolName), out visible);
            return visible || controller.GetSymbolVisiblity(new KAnimHashedString(symbolName));
        }

        public sealed class States : GameStateMachine<States, StatesInstance, StorageNetworkOrderProductionCenterController>
        {
            public State idle;

            public override void InitializeStates(out StateMachine.BaseState defaultState)
            {
                defaultState = idle;
                idle
                    .Enter(smi => smi.master.RefreshDiskMeter())
                    .Update("StorageNetwork order center disk meter", (smi, dt) => smi.master.RefreshDiskMeter(), UpdateRate.SIM_1000ms);
            }
        }

        public sealed class StatesInstance : GameStateMachine<States, StatesInstance, StorageNetworkOrderProductionCenterController, object>.GameInstance
        {
            public StatesInstance(StorageNetworkOrderProductionCenterController master)
                : base(master)
            {
            }
        }
    }
#pragma warning restore CS0649
}
