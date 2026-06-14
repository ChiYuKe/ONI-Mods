using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal readonly struct StorageNetworkPanelHealthMetrics
    {
        private StorageNetworkPanelHealthMetrics(
            float remainingCapacityKg,
            float fillRatio,
            int activeOrders,
            int waitingOrders,
            int abnormalOrders,
            int offlineServers)
        {
            RemainingCapacityKg = remainingCapacityKg;
            FillRatio = fillRatio;
            ActiveOrders = activeOrders;
            WaitingOrders = waitingOrders;
            AbnormalOrders = abnormalOrders;
            OfflineServers = offlineServers;
        }

        internal float RemainingCapacityKg { get; }

        internal float FillRatio { get; }

        internal int ActiveOrders { get; }

        internal int WaitingOrders { get; }

        internal int AbnormalOrders { get; }

        internal int OfflineServers { get; }

        internal static StorageNetworkPanelHealthMetrics Create(
            StorageSceneSnapshot snapshot,
            IEnumerable<ProductionOrderRecord> orders,
            Func<StorageInfo, bool> isOfflineNetworkServer)
        {
            float remainingCapacity = snapshot != null
                ? Mathf.Max(0f, snapshot.TotalCapacityKg - snapshot.TotalStoredKg)
                : 0f;
            float fillRatio = snapshot != null && snapshot.TotalCapacityKg > 0f
                ? snapshot.TotalStoredKg / snapshot.TotalCapacityKg
                : 0f;

            IEnumerable<ProductionOrderRecord> orderList = orders ?? Enumerable.Empty<ProductionOrderRecord>();
            IReadOnlyList<StorageInfo> storages = snapshot?.Storages ?? StorageSceneSnapshot.Empty.Storages;

            return new StorageNetworkPanelHealthMetrics(
                remainingCapacity,
                fillRatio,
                orderList.Count(IsActiveOrder),
                orderList.Count(order => order != null && order.State == ProductionOrderState.WaitingMaterials),
                orderList.Count(order => order != null && order.State == ProductionOrderState.Abnormal),
                storages.Count(storage => isOfflineNetworkServer != null && isOfflineNetworkServer(storage)));
        }

        private static bool IsActiveOrder(ProductionOrderRecord order)
        {
            return order != null &&
                   order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }
    }
}
