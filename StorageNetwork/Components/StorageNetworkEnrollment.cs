using KSerialization;
using System;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkEnrollment : KMonoBehaviour
    {
        [Serialize]
        public bool IncludedInSceneNetwork;

        [MyCmpGet]
        private Storage storage;

        private Guid connectedStatusHandle = Guid.Empty;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            RefreshConnectedStatus();
        }

        protected override void OnCleanUp()
        {
            RemoveConnectedStatus();
            base.OnCleanUp();
        }

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkEnrollment> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkEnrollment>((component, data) => component.OnRefreshUserMenu(data));

        private void OnRefreshUserMenu(object data)
        {
            if (!CanShowEnrollmentButton())
            {
                return;
            }

            string name = IncludedInSceneNetwork
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_REMOVE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_ADD);
            string tooltip = IncludedInSceneNetwork
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_REMOVE_TOOLTIP)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_ADD_TOOLTIP);

            KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                "action_switch_toggle",
                name,
                ToggleEnrollment,
                global::Action.NumActions,
                null,
                null,
                null,
                tooltip,
                true);

            Game.Instance.userMenu.AddButton(gameObject, button, 1f);
        }

        private void ToggleEnrollment()
        {
            SetIncludedInSceneNetwork(!IncludedInSceneNetwork);
        }

        public void SetIncludedInSceneNetwork(bool included)
        {
            if (IncludedInSceneNetwork == included)
            {
                return;
            }

            IncludedInSceneNetwork = included;
            RefreshConnectedStatus();
            KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));

            if (StorageNetwork.UI.StorageNetworkPanel.IsOpen())
            {
                StorageNetwork.UI.StorageNetworkPanel.Show(storage);
            }
        }

        public bool CanShowInEnrollableList()
        {
            return CanShowEnrollmentButton();
        }

        private bool CanShowEnrollmentButton()
        {
            return storage != null && (IsStorageLocker() || IsComplexRecipeBuilding());
        }

        public bool IsStorageLocker()
        {
            return GetComponent<StorageLocker>() != null;
        }

        public bool IsComplexRecipeBuilding()
        {
            return GetComponent<ComplexFabricator>() != null;
        }

        private void RefreshConnectedStatus()
        {
            if (IncludedInSceneNetwork)
            {
                if (connectedStatusHandle == Guid.Empty)
                {
                    KSelectable selectable = GetComponent<KSelectable>();
                    if (selectable != null)
                    {
                        connectedStatusHandle = selectable.AddStatusItem(CreateConnectedStatusItem(), this);
                    }
                }
            }
            else
            {
                RemoveConnectedStatus();
            }
        }

        private void RemoveConnectedStatus()
        {
            if (connectedStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(connectedStatusHandle);
            }

            connectedStatusHandle = Guid.Empty;
        }

        private static StatusItem CreateConnectedStatusItem()
        {
            return new StatusItem(
                "StorageNetworkConnected",
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_STATUS),
                Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_STATUS_TOOLTIP),
                "status_item_check",
                StatusItem.IconType.Info,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);
        }
    }
}
