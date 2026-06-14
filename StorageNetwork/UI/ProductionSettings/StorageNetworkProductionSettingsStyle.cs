using StorageNetwork.Components;
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
            return enabled
                ? new Color(0.28f, 0.48f, 0.34f, 1f)
                : new Color(0.52f, 0.38f, 0.30f, 1f);
        }

        public static Color GetNetworkAutomationColor(bool enabled)
        {
            return enabled
                ? new Color(0.28f, 0.48f, 0.34f, 1f)
                : new Color(0.50f, 0.42f, 0.34f, 1f);
        }

        public static Color GetOutputStoreColor(bool enabled)
        {
            return enabled
                ? new Color(0.28f, 0.48f, 0.34f, 1f)
                : new Color(0.48f, 0.45f, 0.36f, 1f);
        }
    }
}
