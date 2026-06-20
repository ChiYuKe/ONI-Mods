using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkOrderProductionCenterEngraveTool
    {
        private static StorageNetworkOrderProductionCenter activeCenter;
        private static HoverTextConfiguration hoverTextConfiguration;
        private static string previousToolName;
        private static string previousActionName;
        private static string previousToolNameStringKey;
        private static string previousActionStringKey;
        private static bool hasHoverTextOverride;
        private static bool failureWarningShown;
        private static int successCount;

        public static bool Active => activeCenter != null;

        public static void Begin(StorageNetworkOrderProductionCenter center)
        {
            activeCenter = center;
            failureWarningShown = false;
            successCount = 0;
            if (CopySettingsTool.Instance != null && center != null)
            {
                ApplyHoverTextOverride(CopySettingsTool.Instance);
                CopySettingsTool.Instance.SetSourceObject(center.gameObject);
                PlayerController.Instance.ActivateTool(CopySettingsTool.Instance);
            }

            if (center != null)
            {
                StorageNetworkNotifications.ShowInfo(center.gameObject, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_STARTED));
            }
        }

        public static void CancelIfOwner(StorageNetworkOrderProductionCenter center)
        {
            if (activeCenter == center)
            {
                Reset();
            }
        }

        public static bool HandleSelected(GameObject target)
        {
            if (activeCenter == null)
            {
                return false;
            }

            StorageNetworkOrderProductionCenter center = activeCenter;
            if (center.TryEngraveFrom(target, out string message))
            {
                successCount++;
                StorageNetworkNotifications.ShowSuccess(center.gameObject, message);
                return true;
            }

            if (!failureWarningShown && successCount == 0)
            {
                failureWarningShown = true;
                StorageNetworkNotifications.ShowWarning(center.gameObject, message);
            }

            return true;
        }

        public static bool TryHandleCopyTarget(GameObject source, GameObject target)
        {
            if (activeCenter == null || source == null || target == null || source != activeCenter.gameObject)
            {
                return false;
            }

            return HandleSelected(target);
        }

        public static void HandleCopyToolDeactivated()
        {
            Reset();
        }

        private static void Reset()
        {
            activeCenter = null;
            failureWarningShown = false;
            successCount = 0;
            RestoreHoverTextOverride();
        }

        private static void ApplyHoverTextOverride(CopySettingsTool tool)
        {
            HoverTextConfiguration hoverText = tool != null ? tool.GetComponent<HoverTextConfiguration>() : null;
            if (hoverText == null)
            {
                return;
            }

            if (!hasHoverTextOverride)
            {
                hoverTextConfiguration = hoverText;
                previousToolName = hoverText.ToolName;
                previousActionName = hoverText.ActionName;
                previousToolNameStringKey = hoverText.ToolNameStringKey;
                previousActionStringKey = hoverText.ActionStringKey;
                hasHoverTextOverride = true;
            }

            hoverText.ToolNameStringKey = string.Empty;
            hoverText.ActionStringKey = string.Empty;
            hoverText.ToolName = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_TOOLNAME).ToUpper();
            hoverText.ActionName = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_ACTION).ToUpper();
        }

        private static void RestoreHoverTextOverride()
        {
            if (!hasHoverTextOverride || hoverTextConfiguration == null)
            {
                hasHoverTextOverride = false;
                hoverTextConfiguration = null;
                return;
            }

            hoverTextConfiguration.ToolName = previousToolName;
            hoverTextConfiguration.ActionName = previousActionName;
            hoverTextConfiguration.ToolNameStringKey = previousToolNameStringKey;
            hoverTextConfiguration.ActionStringKey = previousActionStringKey;
            hoverTextConfiguration.ConfigureHoverScreen();
            hoverTextConfiguration = null;
            previousToolName = null;
            previousActionName = null;
            previousToolNameStringKey = null;
            previousActionStringKey = null;
            hasHoverTextOverride = false;
        }
    }
}
