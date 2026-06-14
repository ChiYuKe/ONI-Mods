using System;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed partial class StorageNetworkMaterialRequester
    {
        private void RefreshMaterialRequestStatus()
        {
            if (materialRequestStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                materialRequestStatusHandle = selectable.AddStatusItem(GetMaterialRequestStatusItem(), this);
            }
        }

        private void RemoveMaterialRequestStatus()
        {
            if (materialRequestStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(materialRequestStatusHandle);
            }

            materialRequestStatusHandle = Guid.Empty;
        }

        private static StatusItem GetMaterialRequestStatusItem()
        {
            if (materialRequestStatusItem != null)
            {
                return materialRequestStatusItem;
            }

            materialRequestStatusItem = new StatusItem(
                "StorageNetworkMaterialRequest",
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            materialRequestStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkMaterialRequester requester = data as StorageNetworkMaterialRequester;
                string status = requester != null && !string.IsNullOrEmpty(requester.LastStatus)
                    ? requester.LastStatus
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_ITEM);
                return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_TOOLTIP), status);
            };

            return materialRequestStatusItem;
        }
    }
}
