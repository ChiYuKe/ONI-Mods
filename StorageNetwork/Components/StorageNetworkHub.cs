using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.UI;

namespace StorageNetwork.Components
{
    public class StorageNetworkHub : KMonoBehaviour, ISim1000ms, ISidescreenButtonControl
    {
        private StorageNetworkSnapshot snapshot = StorageNetworkSnapshot.Empty;

        public IReadOnlyList<StorageNetworkStorageInfo> ConnectedStorages => snapshot.Storages;

        public float TotalCapacityKg => snapshot.TotalCapacityKg;

        public float TotalStoredKg => snapshot.TotalStoredKg;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageNetworkRegistry.Register(this);
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
