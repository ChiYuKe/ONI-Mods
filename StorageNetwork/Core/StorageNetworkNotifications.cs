using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkNotifications
    {
        public const string AbnormalOrderNotificationId = "StorageNetwork.AbnormalOrder";

        private static bool customNotificationRegistered;

        public static void Register(NotificationScreen screen)
        {
            if (screen == null || screen.customNotificationPrefabs == null)
            {
                customNotificationRegistered = false;
                return;
            }

            if (screen.customNotificationPrefabs.Any(prefab => prefab != null && prefab.ID == AbnormalOrderNotificationId))
            {
                customNotificationRegistered = true;
                return;
            }

            if (screen.LabelPrefab == null || screen.LabelsFolder == null)
            {
                customNotificationRegistered = false;
                return;
            }

            GameObject prefab = Object.Instantiate(screen.LabelPrefab, screen.transform, false);
            prefab.name = "StorageNetworkAbnormalOrderNotificationPrefab";
            prefab.SetActive(false);
            ConfigureCustomPrefab(prefab);

            screen.customNotificationPrefabs.Add(new NotificationScreen.CustomNotificationPrefabs
            {
                ID = AbnormalOrderNotificationId,
                notificationPrefab = prefab,
                parentFolder = screen.LabelsFolder
            });
            customNotificationRegistered = true;
        }

        public static void ShowAbnormalOrder(ProductionOrderRecord order)
        {
            if (order == null)
            {
                return;
            }

            Transform focus = GetOrderFocus(order);
            Notification notification = new Notification(
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_NOTIFICATION),
                customNotificationRegistered ? NotificationType.Custom : NotificationType.BadMinor,
                BuildAbnormalOrderTooltip,
                BuildAbnormalOrderDetails(order),
                false,
                0f,
                null,
                null,
                focus,
                true,
                false,
                true);

            if (notification.Type == NotificationType.Custom)
            {
                notification.customNotificationID = AbnormalOrderNotificationId;
            }

            Notifier notifier = focus != null
                ? focus.gameObject.AddOrGet<Notifier>()
                : NotificationScreen.Instance != null
                    ? NotificationScreen.Instance.gameObject.AddOrGet<Notifier>()
                    : null;
            notifier?.Add(notification, string.Empty);
        }

        public static void ShowInfo(string message)
        {
            ShowSimple(message, NotificationType.Neutral);
        }

        public static void ShowWarning(string message)
        {
            ShowSimple(message, NotificationType.BadMinor);
        }

        private static void ShowSimple(string message, NotificationType type)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Notification notification = new Notification(
                message,
                type,
                (notifications, data) => message,
                null,
                false,
                0f,
                null,
                null,
                null,
                true,
                false,
                false);
            Notifier notifier = NotificationScreen.Instance != null
                ? NotificationScreen.Instance.gameObject.AddOrGet<Notifier>()
                : null;
            notifier?.Add(notification, string.Empty);
        }

        private static void ConfigureCustomPrefab(GameObject prefab)
        {
            HierarchyReferences references = prefab.GetComponent<HierarchyReferences>();
            if (references == null)
            {
                return;
            }

            Color textColor = GlobalAssets.Instance != null
                ? GlobalAssets.Instance.colorSet.NotificationBad
                : new Color(0.88f, 0.25f, 0.25f, 1f);
            Color backgroundColor = GlobalAssets.Instance != null
                ? GlobalAssets.Instance.colorSet.NotificationBadBG
                : new Color(0.42f, 0.18f, 0.18f, 1f);

            KImage icon = references.GetReference<KImage>("Icon");
            if (icon != null)
            {
                icon.sprite = StorageNetworkSprites.GetOverviewIcon() ?? Assets.GetSprite("status_item_exclamation");
                icon.color = textColor;
            }

            LocText text = references.GetReference<LocText>("Text");
            if (text != null)
            {
                text.color = textColor;
            }

            Button button = references.GetReference<Button>("MainButton");
            if (button != null)
            {
                ColorBlock colors = button.colors;
                colors.normalColor = backgroundColor;
                button.colors = colors;
            }
        }

        private static Transform GetOrderFocus(ProductionOrderRecord order)
        {
            ProductionOrderQueueAssignment assignment = order.QueueAssignments
                .FirstOrDefault(item => item != null && item.Fabricator != null);
            return assignment?.Fabricator?.transform;
        }

        private static string BuildAbnormalOrderTooltip(List<Notification> notifications, object data)
        {
            IEnumerable<string> details = notifications
                .Select(notification => notification.tooltipData as string)
                .Where(detail => !string.IsNullOrEmpty(detail));
            return STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_NOTIFICATION_TOOLTIP) +
                   string.Join("\n", details.ToArray());
        }

        private static string BuildAbnormalOrderDetails(ProductionOrderRecord order)
        {
            string reason = string.IsNullOrEmpty(order.AbnormalReason)
                ? STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_DEFAULT_REASON)
                : order.AbnormalReason;
            return string.Format(
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_NOTIFICATION_DETAIL),
                order.DisplayId,
                order.ProductName,
                GameUtil.GetFormattedMass(order.RequestedAmount),
                GameUtil.GetFormattedMass(order.ProducedAtSubmit),
                reason);
        }
    }
}
