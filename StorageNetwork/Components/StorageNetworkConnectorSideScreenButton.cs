using StorageNetwork.Core;
using StorageNetwork.UI;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkConnectorSideScreenButton : KMonoBehaviour, ISidescreenButtonControl
    {
        private StorageNetworkStorageConnector connector;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            connector = GetComponent<StorageNetworkStorageConnector>();
        }

        public string SidescreenButtonText => STRINGS.UI.STORAGE_NETWORK.VIEW_NETWORK_BUTTON;

        public string SidescreenButtonTooltip
        {
            get
            {
                StorageNetworkHub hub = GetConnectedHub();
                return hub != null
                    ? STRINGS.UI.STORAGE_NETWORK.VIEW_CONNECTED_NETWORK_TOOLTIP
                    : STRINGS.UI.STORAGE_NETWORK.VIEW_CONNECTED_NETWORK_UNAVAILABLE_TOOLTIP;
            }
        }

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenEnabled()
        {
            return !StorageNetworkUiOptions.UseTitleBarNetworkButton && GetConnector() != null;
        }

        public bool SidescreenButtonInteractable()
        {
            return GetConnectedHub() != null;
        }

        public void OnSidescreenButtonPressed()
        {
            StorageNetworkHub hub = GetConnectedHub();
            Storage storage = GetConnector()?.Storage;
            if (hub != null)
            {
                StorageNetworkPanel.Show(hub, storage);
            }
        }

        public int HorizontalGroupID()
        {
            return -1;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 21;
        }

        private StorageNetworkStorageConnector GetConnector()
        {
            if (connector == null)
            {
                connector = GetComponent<StorageNetworkStorageConnector>();
            }

            return connector;
        }

        private StorageNetworkHub GetConnectedHub()
        {
            return StorageNetworkRegistry.GetConnectedHub(GetConnector());
        }
    }
}
