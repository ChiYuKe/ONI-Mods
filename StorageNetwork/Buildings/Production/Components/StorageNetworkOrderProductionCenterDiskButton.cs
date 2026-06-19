using StorageNetwork.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkOrderProductionCenterDiskButton : KMonoBehaviour, ISidescreenButtonControl
    {
        [MyCmpGet]
        private StorageNetworkOrderProductionCenter center = null;

        public string SidescreenButtonText => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE);

        public string SidescreenButtonTooltip => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TOOLTIP);

        public string SidescreenTitle => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE);

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenButtonInteractable()
        {
            return center != null;
        }

        public bool SidescreenEnabled()
        {
            return false;
        }

        public void OnSidescreenButtonPressed()
        {
        }

        public int HorizontalGroupID()
        {
            return 1002;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 15;
        }
    }
}
