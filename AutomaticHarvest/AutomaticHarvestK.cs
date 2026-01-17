using System;
using System.Collections.Generic;
using System.Linq;
using Klei.AI;
using KSerialization;
using TUNING;
using UnityEngine;
using static AutomaticHarvest.Reservoir;
using static MercuryLight;
using static STRINGS.BUILDINGS.PREFABS;
using static STRINGS.ELEMENTS;

namespace AutomaticHarvest
{
    public class AutomaticHarvestK : StateMachineComponent<AutomaticHarvestK.StatesInstance>
    {
        [Serialize]
        public bool isEnabled = true;

        [Serialize]
        public Anim_Au anim_au { get; private set; }





        [MyCmpGet]
        private Storage storage;

        [MyCmpGet]
        private RangeVisualizer visualizer;

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


        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();


        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            base.Subscribe<AutomaticHarvestK>(493375141, OnRefreshUserMenuDelegate);
            testgun();






            base.smi.StartSM();
        }


        public bool IsFull
        {
            get
            {
                // 确保 capacityKg 不为零，避免除以零的错误
                if (storage.capacityKg <= 0f)
                {
                    return false; // 如果容量为零或负数，逻辑上认为不满或无法存储
                }

                // 比较当前质量是否大于或等于最大容量
                return storage.MassStored() >= storage.capacityKg;
            }
        }



        private GameObject arm_go;
        private KBatchedAnimController arm_anim_ctrl;
        private int gameCell;
        private KAnimLink link;

        public void testgun()
        {
            KBatchedAnimController component = base.GetComponent<KBatchedAnimController>();
            string text = component.name + ".gun";
            this.arm_go = new GameObject(text);
            this.arm_go.SetActive(false);
            this.arm_go.transform.parent = component.transform;
            this.arm_go.AddComponent<KPrefabID>().PrefabTag = new Tag(text);
            this.arm_anim_ctrl = this.arm_go.AddComponent<KBatchedAnimController>();
            this.arm_anim_ctrl.AnimFiles = new KAnimFile[] { component.AnimFiles[0] };
            this.arm_anim_ctrl.initialAnim = "gun";
            this.arm_anim_ctrl.isMovable = true;
            this.arm_anim_ctrl.sceneLayer = Grid.SceneLayer.SceneMAX;
            component.SetSymbolVisiblity("gun_target", false);
            bool flag;
            Vector3 vector = component.GetSymbolTransform(new HashedString("gun_target"), out flag).GetColumn(3);
            vector.z = -35f;
            this.arm_go.transform.SetPosition(vector);
            this.arm_go.SetActive(true);
            this.gameCell = Grid.PosToCell(this.arm_go);
            this.link = new KAnimLink(component, this.arm_anim_ctrl);

            this.storage.fxPrefix = Storage.FXPrefix.PickedUp;



        }

        protected override void OnCleanUp()
        {
            base.smi.StopSM("OnCleanUp");
            base.OnCleanUp();
        }


        public enum Anim_Au
        {
            on,
            off,
            Green
        }





        private static readonly EventSystem.IntraObjectHandler<AutomaticHarvestK> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<AutomaticHarvestK>(delegate (AutomaticHarvestK component, object data)
        {
            component.OnRefreshUserMenu(data);
        });

        private void OnRefreshUserMenu(object data)
        {
            // 清空按钮
            KIconButtonMenu.ButtonInfo emptyButton = new KIconButtonMenu.ButtonInfo("action_empty_contents", STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_CLEAR, delegate
            {

                Debug.Log($"当前位置{transform.position}");

                // 确保 DropAll 调用参数正确
                storage.DropAll(false, false, default(Vector3), true, null);
            }, global::Action.NumActions, null, null, null, STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_CLEAR_TOOLTIP, true);
            Game.Instance.userMenu.AddButton(base.gameObject, emptyButton, 1f);
        }















        // ------------------ 状态机定义 ------------------

        public class States : GameStateMachine<States, StatesInstance, AutomaticHarvestK>
        {
            public override void InitializeStates(out StateMachine.BaseState default_state)
            {
                default_state = this.off;
                this.root.DoNothing();
                this.off
                    .Enter(delegate (StatesInstance smi)
                    {
                        smi.master.reservoir.RefreshHstatusLight(HstatusLight.Red);
                        smi.master.operational.SetActive(false, false);
                        smi.master.anim_au = Anim_Au.off;

                    })
                    .PlayAnim("off")
                    .Transition(this.on, (StatesInstance smi) => !smi.master.storage.IsFull() && smi.master.energyConsumer.IsPowered, UpdateRate.SIM_200ms);



                this.on
                    .Enter(delegate (StatesInstance smi)
                    {
                        if (!smi.master.dispenser.isEnabled)
                            smi.master.reservoir.RefreshHstatusLight(HstatusLight.Yellow);
                        else
                            smi.master.reservoir.RefreshHstatusLight(HstatusLight.Green);

                        smi.master.anim_au = Anim_Au.on;
                    })
                    .DefaultState(this.on.idle)
                    //.Update(delegate (StatesInstance smi, float dt)
                    //{
                    //    smi.CheckPlants();
                    //    GameObject firstPlant = smi.plantsToHarvest.FirstOrDefault();
                    //    if (firstPlant != null)
                    //    {
                    //        // 使用植物的世界位置来设置枪口的旋转
                    //        smi.PointGunAt(firstPlant.transform.position);
                    //    }

                    //    smi.HarvestPlantsIfNeeded();

                    //}, UpdateRate.SIM_4000ms, false)
                    .Transition(this.off, (StatesInstance smi) => smi.master.storage.IsFull() || !smi.master.energyConsumer.IsPowered, UpdateRate.SIM_200ms);

                this.on.idle
                    .PlayAnim("on")
                    .Enter(smi => smi.master.operational.SetActive(false))
                    .Update(delegate (StatesInstance smi, float dt)
                    {
                        smi.CheckPlants(); // 持续扫描
                    }, UpdateRate.SIM_1000ms)
                    // 如果发现有植物，跳转到 working
                    .Transition(this.on.working, (StatesInstance smi) => smi.plantsToHarvest.Count > 0, UpdateRate.SIM_200ms);

                this.on.working
                    .Enter(delegate (StatesInstance smi)
                    {
                        smi.master.operational.SetActive(true);
                        // 指向第一个目标
                        GameObject target = smi.plantsToHarvest.FirstOrDefault();
                        if (target != null)
                        {
                            smi.PointGunAt(target.transform.position);
                        }
                    })
                    .ScheduleGoTo(0.5f, this.on.post_harvest);

                this.on.post_harvest
                    .Enter(delegate (StatesInstance smi)
                    {
                        // b执行收获：只收列表第一个
                        smi.HarvestSinglePlant();
                        // 重新扫描一次，看看还有没有剩下的
                        smi.CheckPlants();
                    })
                    // 如果还有，回到 working 状态处理下一个目标
                    .Transition(this.on.working, (StatesInstance smi) => smi.plantsToHarvest.Count > 0, UpdateRate.SIM_200ms)
                    // 如果没了，回到 idle 待机
                    .Transition(this.on.idle, (StatesInstance smi) => smi.plantsToHarvest.Count == 0, UpdateRate.SIM_200ms);
            }
            

            public State off;
            public ReadyStates on;

            public class OnStates : State
            {
                public State dnd;   // 禁用/待命状态
                public State working;  // 工作状态
            }
            public class  ReadyStates : State
            {
                public State idle;  
                public State working;
                public State post_harvest;
            }
               
        }



        // ------------------ 状态机实例 ------------------

        public class StatesInstance : GameStateMachine<States, StatesInstance, AutomaticHarvestK, object>.GameInstance
        {
            private const float DropAmountKg = 20f;
            public List<GameObject> plantsToHarvest; // 用于存储扫描结果

            [Serialize]
            public Reservoir.HstatusLight HstatusLight { get; set; }

            public StatesInstance(AutomaticHarvestK master)
                : base(master)
            {
                this.plantsToHarvest = new List<GameObject>();
            }



            public void PointGunAt(Vector3 targetPosition)
            {
               
                Vector3 armPosition = master.arm_go.gameObject.transform.position;

                Vector3 direction = targetPosition - armPosition;


                // 计算目标方向的旋转角度
                float angleRadians = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float angleoffset = angleRadians + 90;

                // 获取 KBatchedAnimController
                KBatchedAnimController kBatchedAnimController = master.arm_go.gameObject.GetComponent<KBatchedAnimController>();
                // kBatchedAnimController.Rotation = angleoffset;

                // 设置 arm_go 的旋转
                 master.arm_go.transform.rotation = Quaternion.Euler(0f, 0f, angleoffset);

                // 确保播放动画（如果需要）
                kBatchedAnimController.Play("gun_harvest", KAnim.PlayMode.Once);
            }





            /// <summary>
            /// 检查是否达到最大阈值
            /// </summary>
            public bool IsThreshold()
            {
                return master.logic.activated;  
            }


            /// <summary>
            /// 获取可收获植物并添加到列表里
            /// </summary>
            public void CheckPlants()
            {
                plantsToHarvest = master.autoPlantHarvester?.ScanPlants() ?? new List<GameObject>();
            }

            /// <summary>
            ///  执行收获
            /// </summary>
            public void HarvestPlantsIfNeeded()
            {
                // 确保 plantsToHarvest 不为 null 且有植物可以收获
                if (plantsToHarvest != null && plantsToHarvest.Count > 0)
                {
                    // 调用收获和存储植物的方法
                    master.autoPlantHarvester.HarvestAndStorePlants(plantsToHarvest);
                    plantsToHarvest.Clear();
                }
            }

            /// <summary>
            /// 收获单个植物
            /// </summary>
            public void HarvestSinglePlant()
            {
                if (plantsToHarvest != null && plantsToHarvest.Count > 0)
                {
                    GameObject target = plantsToHarvest[0];
                    if (target != null)
                    {
                        // 这里只传入包含单个目标的 List 
                        master.autoPlantHarvester.HarvestAndStorePlants(new List<GameObject> { target });
                    }
                    // 处理完后从待收获列表中移除
                    plantsToHarvest.RemoveAt(0);
                }
            }

            /// <summary>
            /// 获取范围内是否有植物
            /// </summary>
            public bool CheckPlantsAll()
            {

                return master.autoPlantHarvester?.ScanPlants(false).Count > 0;
            }

            public void UpdateLight()
            {
                // 根据是否有植物决定指示灯颜色
                HstatusLight lightStatus = CheckPlantsAll()
                    ? Reservoir.HstatusLight.Green
                    : Reservoir.HstatusLight.Yellow;

                // 更新指示灯状态
                smi.master.reservoir.RefreshHstatusLight(lightStatus);
            }

         
        }
    }

}
