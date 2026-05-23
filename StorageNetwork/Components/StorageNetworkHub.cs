using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.UI;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络中心组件。
    /// 挂在 Storage Network Hub 建筑上，作为储存网络的核心连接节点。
    /// 负责注册到 StorageNetworkRegistry，定时构建网络快照，
    /// 统计当前网络中连接的储存建筑、总容量和总储存量。
    /// 同时实现侧边栏按钮，用于打开储存网络详情面板。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class StorageNetworkHub : KMonoBehaviour, ISim1000ms, ISidescreenButtonControl, IStorageNetworkConnectable
    {
        [Serialize]
        private bool allowsNetworkPull = true;

        private StorageNetworkSnapshot snapshot = StorageNetworkSnapshot.Empty;

        public int Cell => Grid.PosToCell(this);

        public int InputCell => Cell;

        public int OutputCell => Grid.CellRight(Cell);

        public string DisplayName => gameObject.GetProperName();

        public Storage Storage => null;

        public bool AllowsNetworkPull
        {
            get => allowsNetworkPull;
            set => allowsNetworkPull = value;
        }

        public bool CanShareStorage => true;

        public IReadOnlyList<StorageNetworkStorageInfo> ConnectedStorages => snapshot.Storages;

        public float TotalCapacityKg => snapshot.TotalCapacityKg;

        public float TotalStoredKg => snapshot.TotalStoredKg;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageNetworkRegistry.Register(this);
            gameObject.AddOrGet<StorageNetworkPortVisualizer>();
            RefreshNetwork();
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            StorageNetworkPanel.CloseIfTarget(this);
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshNetwork();
        }

        public void RefreshNetwork()
        {
            snapshot = StorageNetworkRegistry.BuildSnapshot(this);
        }

        public string SidescreenButtonText => STRINGS.UI.STORAGE_NETWORK.VIEW_NETWORK_BUTTON;

        public string SidescreenButtonTooltip => STRINGS.UI.STORAGE_NETWORK.VIEW_NETWORK_TOOLTIP;

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenEnabled()
        {
            return true;
        }

        public bool SidescreenButtonInteractable()
        {
            return true;
        }

        public void OnSidescreenButtonPressed()
        {
            StorageNetworkPanel.Show(this);
        }

        public int HorizontalGroupID()
        {
            return -1;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 20;
        }
    }
}
