using StorageNetwork.Components;
using StorageNetwork.API;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkProductionSettingsStyle
    {
        public static float GetMaterialAutomationCardHeight(StorageNetworkMaterialRequester requester)
        {
            float height = requester != null && requester.LimitEnabled ? 214f : 176f;
            if (requester != null && !string.IsNullOrEmpty(requester.LastStatus))
            {
                height += 30f;
            }

            return height;
        }

        public static float GetOutputAutomationCardHeight(StorageNetworkMaterialRequester requester)
        {
            float height = 176f;
            if (requester != null && requester.OutputStoreEnabled)
            {
                height += 40f;
            }

            if (requester != null && !string.IsNullOrEmpty(requester.LastOutputStatus))
            {
                height += 30f;
            }

            return height;
        }

        public static Color GetEnabledStatusColor(bool enabled)
        {
            return StorageNetworkPanelPalette.GetEnabledStatusColor(enabled);
        }

        public static Color GetNetworkAutomationColor(bool enabled)
        {
            return StorageNetworkPanelPalette.GetNetworkAutomationColor(enabled);
        }

        public static Color GetOutputStoreColor(bool enabled)
        {
            return StorageNetworkPanelPalette.GetOutputStoreColor(enabled);
        }
    }
}
