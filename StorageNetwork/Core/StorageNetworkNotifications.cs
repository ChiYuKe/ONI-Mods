using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkNotifications
    {
        public static void ShowAbnormalOrder(ProductionOrderRecord order)
        {
            if (order == null)
            {
                return;
            }

            Transform focus = GetOrderFocus(order);
            if (focus == null)
            {
                return;
            }

            Notification notification = new Notification(
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.ORDER_ABNORMAL_NOTIFICATION),
                NotificationType.BadMinor,
                BuildAbnormalOrderTooltip,
                "\t• " + BuildAbnormalOrderDetails(order),
                true,
                0f,
                null,
                null,
                focus,
                true,
                false,
                true);

            Notifier notifier = focus.gameObject.AddOrGet<Notifier>();
            notifier.Add(notification, string.Empty);
        }

        public static void ShowInfo(GameObject owner, string message)
        {
            ShowSimple(owner, message, NotificationType.Neutral);
        }

        public static void ShowSuccess(GameObject owner, string message)
        {
            ShowSimple(owner, message, NotificationType.Good);
        }

        public static void ShowWarning(GameObject owner, string message)
        {
            ShowSimple(owner, message, NotificationType.BadMinor);
        }

        public static void ShowError(GameObject owner, string message)
        {
            ShowSimple(owner, message, NotificationType.Bad);
        }

        private static void ShowSimple(GameObject owner, string message, NotificationType type)
        {
            if (owner == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            Notification notification = new Notification(
                message,
                type,
                (notifications, data) => message + notifications.ReduceMessages(false),
                "\t• " + owner.GetProperName(),
                true,
                0f,
                null,
                null,
                null,
                true,
                false,
                false);
            Notifier notifier = owner.AddOrGet<Notifier>();
            notifier.Add(notification, string.Empty);
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
