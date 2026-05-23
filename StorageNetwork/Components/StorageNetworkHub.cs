using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.UI;

namespace StorageNetwork.Components
{
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
