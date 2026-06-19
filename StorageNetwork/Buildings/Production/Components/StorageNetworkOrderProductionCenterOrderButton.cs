using StorageNetwork.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkOrderProductionCenterOrderButton : KMonoBehaviour, ISidescreenButtonControl
    {
        [MyCmpGet]
        private StorageNetworkOrderProductionCenter center = null;

        public string SidescreenButtonText => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_BUTTON);

        public string SidescreenButtonTooltip => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_TOOLTIP);

        public string SidescreenTitle => Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ORDER_SECTION_TITLE);

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenButtonInteractable()
        {
            return center != null;
        }

        public bool SidescreenEnabled()
        {
            return true;
        }

        public void OnSidescreenButtonPressed()
        {
            StorageNetworkPanel.ShowOrderProductionCenter(center);
        }

        public int HorizontalGroupID()
        {
            return 1003;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 10;
        }
    }
}
