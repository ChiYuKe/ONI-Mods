using System.Collections.Generic;
using System.Linq;
using KSerialization;
using UnityEngine;

namespace AutomaticHarvest
{
#pragma warning disable CS0649
    public class AutomaticHarvestK : StateMachineComponent<AutomaticHarvestK.StatesInstance>
    {
        private const float PlantScanIntervalSeconds = 2f;

        [Serialize]
        public bool isEnabled = true;

        [Serialize]
        public Anim_Au anim_au { get; private set; }

        [MyCmpGet]
        private Storage storage;

        [MyCmpGet]
        private AutomaticHarvestLogic logic;

        [MyCmpGet]
        private SolidConduitDispenserK dispenser;

        [MyCmpGet]
        private Reservoir reservoir;

        [MyCmpGet]
        private AutoPlantHarvester autoPlantHarvester;

        [MyCmpGet]
        private Operational operational;

        [MyCmpGet]
        private EnergyConsumer energyConsumer;

        private GameObject harvestArm;
        private KBatchedAnimController harvestArmController;
        private KAnimLink harvestArmLink;

        public enum Anim_Au
        {
            On,
            Off,
            Green
        }

        public bool IsFull => storage.capacityKg > 0f && storage.MassStored() >= storage.capacityKg;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            CreateHarvestArm();
            smi.StartSM();
        }

        protected override void OnCleanUp()
        {
            smi.StopSM("OnCleanUp");
            base.OnCleanUp();
        }

        private void CreateHarvestArm()
        {
            KBatchedAnimController mainController = GetComponent<KBatchedAnimController>();
            string armName = mainController.name + ".gun";

            harvestArm = new GameObject(armName);
            harvestArm.SetActive(false);
            harvestArm.transform.parent = mainController.transform;
            harvestArm.AddComponent<KPrefabID>().PrefabTag = new Tag(armName);

            harvestArmController = harvestArm.AddComponent<KBatchedAnimController>();
            harvestArmController.AnimFiles = new[] { mainController.AnimFiles[0] };
            harvestArmController.initialAnim = "gun";
            harvestArmController.isMovable = true;
            harvestArmController.sceneLayer = Grid.SceneLayer.SceneMAX;

            mainController.SetSymbolVisiblity("gun_target", false);
            Vector3 armPosition = mainController.GetSymbolTransform(new HashedString("gun_target"), out _).GetColumn(3);
            armPosition.z = -35f;
            harvestArm.transform.SetPosition(armPosition);
            harvestArm.SetActive(true);

            harvestArmLink = new KAnimLink(mainController, harvestArmController);
            storage.fxPrefix = Storage.FXPrefix.PickedUp;
        }

        private static readonly EventSystem.IntraObjectHandler<AutomaticHarvestK> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<AutomaticHarvestK>((component, data) => component.OnRefreshUserMenu(data));

        private void OnRefreshUserMenu(object data)
        {
            KIconButtonMenu.ButtonInfo emptyButton = new KIconButtonMenu.ButtonInfo(
                "action_empty_contents",
                STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_CLEAR,
                () => storage.DropAll(false, false, default(Vector3), true, null),
                global::Action.NumActions,
                null,
                null,
                null,
                STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_CLEAR_TOOLTIP,
                true);

            Game.Instance.userMenu.AddButton(gameObject, emptyButton, 1f);
        }

        public class States : GameStateMachine<States, StatesInstance, AutomaticHarvestK>
        {
            public State off;
            public ReadyStates on;

            public override void InitializeStates(out StateMachine.BaseState defaultState)
            {
                defaultState = off;
                root.DoNothing();

                off
                    .Enter(smi =>
                    {
                        smi.master.reservoir.RefreshHstatusLight(Reservoir.HstatusLight.Red);
                        smi.master.operational.SetActive(false, false);
                        smi.master.anim_au = Anim_Au.Off;
                    })
                    .PlayAnim("off")
                    .Transition(on, smi => !smi.master.storage.IsFull() && smi.master.energyConsumer.IsPowered, UpdateRate.SIM_200ms);

                on
                    .Enter(smi =>
                    {
                        smi.master.reservoir.RefreshHstatusLight(
                            smi.master.dispenser.isEnabled
                                ? Reservoir.HstatusLight.Green
                                : Reservoir.HstatusLight.Yellow);

                        smi.master.anim_au = Anim_Au.On;
                    })
                    .DefaultState(on.idle)
                    .Transition(off, smi => smi.master.storage.IsFull() || !smi.master.energyConsumer.IsPowered, UpdateRate.SIM_200ms);

                on.idle
                    .PlayAnim("on")
                    .Enter(smi => smi.master.operational.SetActive(false))
                    .Update((smi, dt) => smi.CheckPlants(), UpdateRate.SIM_1000ms)
                    .Transition(on.working, smi => smi.plantsToHarvest.Count > 0, UpdateRate.SIM_200ms);

                on.working
                    .Enter(smi =>
                    {
                        smi.master.operational.SetActive(true);
                        GameObject target = smi.plantsToHarvest.FirstOrDefault();
                        if (target != null)
                        {
                            smi.PointGunAt(target.transform.position);
                        }
                    })
                    .ScheduleGoTo(0.5f, on.post_harvest);

                on.post_harvest
                    .Enter(smi =>
                    {
                        smi.HarvestSinglePlant();
                        smi.CheckPlants(true);
                    })
                    .Transition(on.working, smi => smi.plantsToHarvest.Count > 0, UpdateRate.SIM_200ms)
                    .Transition(on.idle, smi => smi.plantsToHarvest.Count == 0, UpdateRate.SIM_200ms);
            }

            public class ReadyStates : State
            {
                public State idle;
                public State working;
                public State post_harvest;
            }
        }

        public class StatesInstance : GameStateMachine<States, StatesInstance, AutomaticHarvestK, object>.GameInstance
        {
            public List<GameObject> plantsToHarvest;
            private float nextScanTime;

            public StatesInstance(AutomaticHarvestK master)
                : base(master)
            {
                plantsToHarvest = new List<GameObject>();
                nextScanTime = Time.time + Mathf.Abs(master.GetInstanceID() % 1000) / 1000f * PlantScanIntervalSeconds;
            }

            public void PointGunAt(Vector3 targetPosition)
            {
                Vector3 armPosition = master.harvestArm.transform.position;
                Vector3 direction = targetPosition - armPosition;
                float angleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;

                master.harvestArm.transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);
                master.harvestArmController.Play("gun_harvest", KAnim.PlayMode.Once);
            }

            public bool IsThreshold()
            {
                return master.logic.activated;
            }

            public void CheckPlants(bool force = false)
            {
                if (!force && Time.time < nextScanTime)
                {
                    return;
                }

                plantsToHarvest = master.autoPlantHarvester?.ScanPlants() ?? new List<GameObject>();
                nextScanTime = Time.time + PlantScanIntervalSeconds;
            }

            public void HarvestSinglePlant()
            {
                if (plantsToHarvest == null || plantsToHarvest.Count == 0)
                {
                    return;
                }

                GameObject target = plantsToHarvest[0];
                if (target != null)
                {
                    master.autoPlantHarvester.HarvestAndStorePlants(new List<GameObject> { target });
                }

                plantsToHarvest.RemoveAt(0);
            }
        }
    }
#pragma warning restore CS0649
}
