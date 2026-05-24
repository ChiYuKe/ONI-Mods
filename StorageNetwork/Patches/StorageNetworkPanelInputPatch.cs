using HarmonyLib;
using StorageNetwork.UI;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkPanelInputPatch
    {
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.OnKeyDown))]
        public static class PlayerControllerOnKeyDownPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                return !ConsumeStorageNetworkRightClick(e);
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.OnKeyUp))]
        public static class PlayerControllerOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.OnKeyUp))]
        public static class DetailsScreenOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        [HarmonyPatch(typeof(OverlayMenu), nameof(OverlayMenu.OnKeyUp))]
        public static class OverlayMenuOnKeyUpPatch
        {
            public static bool Prefix(KButtonEvent e)
            {
                return !ConsumeStorageNetworkRightClick(e, true);
            }
        }

        private static bool ConsumeStorageNetworkRightClick(KButtonEvent e, bool keyUp = false)
        {
            if (e == null || !e.IsAction(global::Action.MouseRight) || !StorageNetworkPanel.IsOpen())
            {
                return false;
            }

            return false;
        }
    }
}
