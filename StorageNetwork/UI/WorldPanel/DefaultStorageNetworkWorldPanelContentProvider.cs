using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.UI.WorldPanel
{
    /// <summary>
    /// StorageNetwork 默认世界文字面板内容。只负责生成文字，不直接创建或操作 UI。
    /// </summary>
    internal sealed class DefaultStorageNetworkWorldPanelContentProvider : IStorageNetworkWorldPanelContentProvider
    {
        private readonly ProductionOrderService orderService = new ProductionOrderService();

        /// <summary>
        /// 按建筑类型生成显示内容，外部提供器可用更高优先级覆盖这里。
        /// </summary>
        public bool TryBuild(GameObject target, out StorageNetworkWorldPanelContent content)
        {
            content = null;
            if (target == null)
            {
                return false;
            }

            ComplexFabricator fabricator = target.GetComponent<ComplexFabricator>();
            if (fabricator != null)
            {
                content = BuildFabricatorContent(target, fabricator);
                return true;
            }

            content = BuildStorageContent(target, target.GetComponent<Storage>());
            return true;
        }

        private StorageNetworkWorldPanelContent BuildFabricatorContent(GameObject target, ComplexFabricator fabricator)
        {
            StorageNetworkMaterialRequester requester = target.GetComponent<StorageNetworkMaterialRequester>();
            List<ProductionOrderRecord> relatedOrders = GetRelatedOrders(fabricator);

            string lineOne = requester != null && requester.RequestEnabled ? "材料请求：开启" : "材料请求：关闭";
            string lineTwo = "状态：" + (requester != null && !string.IsNullOrEmpty(requester.LastStatus)
                ? requester.LastStatus
                : "等待生产队列");
            string lineThree = relatedOrders.Count == 0
                ? "订单：当前建筑没有活动订单"
                : "订单：" + string.Join(" / ", relatedOrders.Select(FormatOrder).ToArray());

            return new StorageNetworkWorldPanelContent("Storage Network 生产接入", lineOne, lineTwo, lineThree);
        }

        private StorageNetworkWorldPanelContent BuildStorageContent(GameObject target, Storage storage)
        {
            StorageSceneSnapshot snapshot = StorageSceneCollector.Collect();
            string lineOne = string.Format(
                "网络容量：{0} / {1}",
                GameUtil.GetFormattedMass(snapshot.TotalStoredKg),
                GameUtil.GetFormattedMass(snapshot.TotalCapacityKg));
            string lineTwo = storage != null
                ? string.Format(
                    "本建筑：{0} / {1}",
                    GameUtil.GetFormattedMass(StorageItemUtility.GetStoredMass(storage)),
                    GameUtil.GetFormattedMass(storage.capacityKg))
                : "本建筑：未检测到 Storage";

            StorageNetworkStorageConnector connector = target.GetComponent<StorageNetworkStorageConnector>();
            string lineThree = connector != null && !string.IsNullOrEmpty(connector.LastOutputStatus)
                ? "输出：" + connector.LastOutputStatus
                : string.Format("接入建筑：{0}", snapshot.Storages.Count);

            return new StorageNetworkWorldPanelContent("Storage Network 储存接入", lineOne, lineTwo, lineThree);
        }

        private List<ProductionOrderRecord> GetRelatedOrders(ComplexFabricator fabricator)
        {
            orderService.LoadOrdersForDisplay();
            return orderService.Orders
                .Where(order => IsActiveOrder(order) && order.QueueAssignments.Any(assignment => assignment.Fabricator == fabricator))
                .OrderBy(order => order.DisplayId)
                .Take(3)
                .ToList();
        }

        private static bool IsActiveOrder(ProductionOrderRecord order)
        {
            return order != null &&
                   order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Cancelled &&
                   order.State != ProductionOrderState.Abnormal;
        }

        private static string FormatOrder(ProductionOrderRecord order)
        {
            return string.Format("#{0} {1} {2}", order.DisplayId, order.ProductName, GameUtil.GetFormattedMass(order.RequestedAmount));
        }
    }
}
