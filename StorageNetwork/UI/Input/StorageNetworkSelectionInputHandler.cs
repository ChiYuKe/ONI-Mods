using UnityEngine;
using StorageNetwork.Components;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkSelectionInputHandler
    {
        public static void HandleSelectToolPostfix()
        {
            if (IsShiftLeftClick())
            {
                StorageNetworkWorldTextPanel.HandleSelectionClick();
            }
        }

        private static bool IsShiftLeftClick()
        {
            return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                   (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0));
        }
    }
}
