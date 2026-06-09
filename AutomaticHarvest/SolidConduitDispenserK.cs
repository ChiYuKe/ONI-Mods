using System.Collections.Generic;
using KSerialization;
using UnityEngine;
using static AutomaticHarvest.Reservoir;



namespace AutomaticHarvest
{
#pragma warning disable CS0649
    public class SolidConduitDispenserK : KMonoBehaviour, ISaveLoadable, IConduitDispenser
    {
        [SerializeField]
        public SimHashes[] elementFilter;

        [SerializeField]
        public bool invertElementFilter;

        [SerializeField]
        public bool alwaysDispense;

        [SerializeField]
        public bool useSecondaryOutput;

        [SerializeField]
        public bool solidOnly;

        private static readonly Operational.Flag OutputConduitFlag = new Operational.Flag("output_conduit", Operational.Flag.Type.Functional);

        [MyCmpReq]
        private Operational operational;

        [MyCmpGet]
        private Reservoir reservoir;

        [MyCmpReq]
        AutomaticHarvestK automaticHarvestK;

        [MyCmpReq]
        public Storage storage;

        private HandleVector<int>.Handle partitionerEntry;

        private int utilityCell = -1;

        private bool dispensing;

        private int round_robin_index;

        public Storage Storage => storage;

        public ConduitType ConduitType => ConduitType.Solid;

        public SolidConduitFlow.ConduitContents ConduitContents => GetConduitFlow().GetContents(utilityCell);

        public bool IsDispensing => dispensing;


        [Serialize]
        public bool isEnabled = true;

        public bool IsConnected
        {
            get
            {
                GameObject gameObject = Grid.Objects[utilityCell, 20];
                if (gameObject != null)
                {
                    return gameObject.GetComponent<BuildingComplete>() != null;
                }

                return false;
            }
        }

        public SolidConduitFlow GetConduitFlow()
        {
            return Game.Instance.solidConduitFlow;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            utilityCell = GetOutputCell();
            ScenePartitionerLayer layer = GameScenePartitioner.Instance.objectLayers[20];
            partitionerEntry = GameScenePartitioner.Instance.Add("SolidConduitConsumer.OnSpawn", base.gameObject, utilityCell, layer, OnConduitConnectionChanged);
            GetConduitFlow().AddConduitUpdater(ConduitUpdate, ConduitFlowPriority.Dispense);
            OnConduitConnectionChanged(null);
        }

        protected override void OnCleanUp()
        {
            GetConduitFlow().RemoveConduitUpdater(ConduitUpdate);
            GameScenePartitioner.Instance.Free(ref partitionerEntry);
            base.OnCleanUp();
        }

        private void OnConduitConnectionChanged(object data)
        {
            dispensing = dispensing && IsConnected;
            Trigger(-2094018600, BoxedBools.Box(true));
        }







        private static readonly EventSystem.IntraObjectHandler<SolidConduitDispenserK> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<SolidConduitDispenserK>(delegate (SolidConduitDispenserK component, object data)
        {
            component.OnRefreshUserMenu(data);
        });

        private void OnRefreshUserMenu(object data)
        {
            KIconButtonMenu.ButtonInfo toggleButton;

            if (this.isEnabled)
            {
                toggleButton = new KIconButtonMenu.ButtonInfo(
                    "action_building_disabled",
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_DEACTIVATE,
                    new System.Action(this.DisableHarvester),
                    global::Action.NumActions,
                    null, null, null,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_DEACTIVATE_TOOLTIP,
                    true
                );
            }
            else
            {
                toggleButton = new KIconButtonMenu.ButtonInfo(
                    "action_priority", // icon_category_shipping  action_harvest
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_ACTIVATE,
                    new System.Action(this.EnableHarvester),
                    global::Action.NumActions,
                    null, null, null,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.BUTTON.USERMENU_ACTIVATE_TOOLTIP,
                    true
                );
            }

            Game.Instance.userMenu.AddButton(base.gameObject, toggleButton, 0.5f);
        }

        private void EnableHarvester()
        {
            this.isEnabled = true;
            switch (automaticHarvestK.anim_au)
            {
                case AutomaticHarvestK.Anim_Au.On:
                    reservoir.RefreshHstatusLight(HstatusLight.Green);
                    break;
                case AutomaticHarvestK.Anim_Au.Off:
                    reservoir.RefreshHstatusLight(HstatusLight.Red);
                    break;
                default:
                    reservoir.RefreshHstatusLight(HstatusLight.Yellow);
                    break;
            }
            // 触发事件以通知状态机切换状态
            Trigger((int)GameHashes.RefreshUserMenu, null);
        }

        private void DisableHarvester()
        {
            this.isEnabled = false;
            reservoir.RefreshHstatusLight(HstatusLight.Yellow);
            // 触发事件以通知状态机切换状态
            Trigger((int)GameHashes.RefreshUserMenu, null);
        }

















        /// -----------------------------    --------------------------------------------------------------


        private void ConduitUpdate(float dt)
        {
            bool flag = false;

            // 如果 isEnabled 为 false，则不进行任何物品运输
            if (!isEnabled)
            {
                dispensing = false;
                return;
            }
            operational.SetFlag(OutputConduitFlag, true);
            if (operational.IsOperational || alwaysDispense)
            {
                SolidConduitFlow conduitFlow = GetConduitFlow();
                if (conduitFlow.HasConduit(utilityCell) && conduitFlow.IsConduitEmpty(utilityCell))
                {
                    Pickupable pickupable = FindSuitableItem();
                    if ((bool)pickupable)
                    {
                        if (pickupable.PrimaryElement.Mass > 20f)
                        {
                            pickupable = pickupable.Take(Mathf.Max(20f, pickupable.PrimaryElement.MassPerUnit));
                        }

                        conduitFlow.AddPickupable(utilityCell, pickupable);
                        flag = true;
                    }
                }
            }

            storage.storageNetworkID = GetConnectedNetworkID();
            dispensing = flag;
        }

        private bool isSolid(GameObject o)
        {
            PrimaryElement component = o.GetComponent<PrimaryElement>();
            if (component == null)
            {
                return false;
            }

            if (component.Element.IsSolid)
            {
                return true;
            }

            if ((double)component.MassPerUnit != 1.0)
            {
                return true;
            }

            return false;
        }

        private Pickupable FindSuitableItem()
        {
            List<GameObject> items = storage.items;
            if (items.Count < 1)
            {
                return null;
            }

            round_robin_index %= items.Count;
            GameObject gameObject = items[round_robin_index];
            round_robin_index++;
            if (solidOnly && !isSolid(gameObject))
            {
                bool flag = false;
                int num = 0;
                while (!flag && num < items.Count)
                {
                    gameObject = items[(round_robin_index + num) % items.Count];
                    if (isSolid(gameObject))
                    {
                        flag = true;
                    }

                    num++;
                }

                if (!flag)
                {
                    return null;
                }
            }

            if (!gameObject)
            {
                return null;
            }

            return gameObject.GetComponent<Pickupable>();
        }

        private int GetConnectedNetworkID()
        {
            GameObject gameObject = Grid.Objects[utilityCell, 20];
            SolidConduit solidConduit = ((gameObject != null) ? gameObject.GetComponent<SolidConduit>() : null);
            return ((solidConduit != null) ? solidConduit.GetNetwork() : null)?.id ?? (-1);
        }

        private int GetOutputCell()
        {
            Building component = GetComponent<Building>();
            if (useSecondaryOutput)
            {
                ISecondaryOutput[] components = GetComponents<ISecondaryOutput>();
                foreach (ISecondaryOutput secondaryOutput in components)
                {
                    if (secondaryOutput.HasSecondaryConduitType(ConduitType.Solid))
                    {
                        return Grid.OffsetCell(component.NaturalBuildingCell(), secondaryOutput.GetSecondaryConduitOffset(ConduitType.Solid));
                    }
                }

                return Grid.OffsetCell(component.NaturalBuildingCell(), CellOffset.none);
            }

            return component.GetUtilityOutputCell();
        }
    }
#pragma warning restore CS0649
}
