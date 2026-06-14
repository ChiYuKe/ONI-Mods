using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkSolidOutputPortManualOperationButton : KMonoBehaviour, ISidescreenButtonControl
    {
        [MyCmpGet]
        private StorageNetworkSolidOutputPortEgress egress = null;

        public string SidescreenButtonText => egress != null && egress.AllowManualOperation
            ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_ALLOWED)
            : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_FORBIDDEN);

        public string SidescreenButtonTooltip => egress != null && egress.AllowManualOperation
            ? Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_MANUAL_ALLOWED_TOOLTIP)
            : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_MANUAL_FORBIDDEN_TOOLTIP);

        public string SidescreenTitle => Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_REQUEST_TITLE);

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenEnabled()
        {
            return egress != null && egress.PortStorage != null;
        }

        public bool SidescreenButtonInteractable()
        {
            return egress != null && egress.PortStorage != null;
        }

        public void OnSidescreenButtonPressed()
        {
            if (egress == null)
            {
                return;
            }

            egress.SetAllowManualOperation(!egress.AllowManualOperation);
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
