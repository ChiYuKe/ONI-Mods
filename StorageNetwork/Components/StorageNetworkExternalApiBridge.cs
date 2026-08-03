using System;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 安装在附属模组对象上的运行时桥接组件。负责把公开接口接入主模组的场景注册和用户菜单刷新流程。
    /// </summary>
    public sealed class StorageNetworkExternalApiBridge : KMonoBehaviour
    {
        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private Operational operational = null;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageSceneRegistry.Register(gameObject);
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            if (operational != null)
            {
                Subscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            }
        }

        protected override void OnCleanUp()
        {
            if (operational != null)
            {
                Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            }

            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkExternalApiBridge> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkExternalApiBridge>((component, data) => component.OnRefreshUserMenu(data));

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkExternalApiBridge> OnOperationalChangedDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkExternalApiBridge>((component, data) => component.OnOperationalChanged(data));

        private void OnOperationalChanged(object data)
        {
            if (storage != null)
            {
                StorageNetworkContentIndexService.Invalidate(storage);
                StorageNetworkParticleStorageService.Invalidate(storage);
            }

            StorageSceneRegistry.InvalidateCapabilities(storage);
        }

        private void OnRefreshUserMenu(object data)
        {
            IStorageNetworkEnrollable enrollable = StorageNetworkMembership.GetExternalEnrollable(gameObject);
            if (enrollable == null)
            {
                return;
            }

            bool included = enrollable.IsStorageNetworkIncluded();
            string name = included
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_REMOVE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_ADD);
            string tooltip = included
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_REMOVE_TOOLTIP)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.ENROLL_ADD_TOOLTIP);

            KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                "action_switch_toggle",
                name,
                () => ToggleEnrollment(enrollable),
                global::Action.NumActions,
                null,
                null,
                null,
                tooltip,
                true);

            GameObject target = enrollable.GetStorageNetworkEnrollmentTarget() ?? gameObject;
            Game.Instance.userMenu.AddButton(target, button, 1f);
        }

        private void ToggleEnrollment(IStorageNetworkEnrollable enrollable)
        {
            if (enrollable == null)
            {
                return;
            }

            GameObject target = enrollable.GetStorageNetworkEnrollmentTarget() ?? gameObject;
            enrollable.SetStorageNetworkIncluded(!enrollable.IsStorageNetworkIncluded());
            StorageSceneRegistry.InvalidateMembership(this.storage);
            StorageNetworkApi.RefreshObjectUi(target);
            KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));

            Storage storage = target.GetComponent<Storage>();
            if (StorageNetworkPanel.IsOpen())
            {
                StorageNetworkPanel.Show(storage);
            }
        }
    }
}
