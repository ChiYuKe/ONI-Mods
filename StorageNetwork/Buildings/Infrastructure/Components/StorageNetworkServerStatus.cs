using System;
using KSerialization;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkServerStatus : KMonoBehaviour
    {
        private static StatusItem connectedStatusItem;
        private Guid connectedStatusHandle = Guid.Empty;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                connectedStatusHandle = selectable.AddStatusItem(GetConnectedStatusItem(), this);
            }
        }

        protected override void OnCleanUp()
        {
            if (connectedStatusHandle != Guid.Empty)
            {
                KSelectable selectable = GetComponent<KSelectable>();
                if (selectable != null)
                {
                    selectable.RemoveStatusItem(connectedStatusHandle);
                }
            }

            connectedStatusHandle = Guid.Empty;
            base.OnCleanUp();
        }

        private static StatusItem GetConnectedStatusItem()
        {
            if (connectedStatusItem != null)
            {
                return connectedStatusItem;
            }

            connectedStatusItem = new StatusItem(
                "StorageNetworkServerConnected",
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_STATUS),
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_STATUS_TOOLTIP),
                "status_item_check",
                StatusItem.IconType.Info,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);
            return connectedStatusItem;
        }
    }
}
