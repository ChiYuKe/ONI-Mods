using KSerialization;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkEnrollment : KMonoBehaviour
    {
        [Serialize]
        public bool IncludedInSceneNetwork;

        [MyCmpGet]
        private Storage storage;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
        }

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkEnrollment> OnRefreshUserMenuDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkEnrollment>((component, data) => component.OnRefreshUserMenu(data));

        private void OnRefreshUserMenu(object data)
        {
            if (storage == null || GetComponent<StorageLocker>() == null)
            {
                return;
            }

            string name = IncludedInSceneNetwork ? "移出网络" : "加入网络";
            string tooltip = IncludedInSceneNetwork
                ? "将这个原版储物箱从储存网络面板中移除。"
                : "将这个原版储物箱加入储存网络面板，并归类为原版储存。";

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
            IncludedInSceneNetwork = !IncludedInSceneNetwork;
            KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));

            if (StorageNetwork.UI.StorageNetworkPanel.IsOpen())
            {
                StorageNetwork.UI.StorageNetworkPanel.Show(storage);
            }
        }
    }
}
