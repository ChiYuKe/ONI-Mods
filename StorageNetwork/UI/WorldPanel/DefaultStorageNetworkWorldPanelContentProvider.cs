using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
using UnityEngine;
using static StorageNetwork.STRINGS;

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
            List<string> orderUsages = GetRelatedOrderUsages(fabricator);

            string lineOne = requester != null && requester.RequestEnabled
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_MATERIAL_REQUEST_ON)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_MATERIAL_REQUEST_OFF);
            string lineTwo = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_STATUS), requester != null && !string.IsNullOrEmpty(requester.LastStatus)
                ? requester.LastStatus
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_WAITING_QUEUE));
            string lineThree = orderUsages.Count == 0
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_ORDER_EMPTY)
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_ORDER_LIST), string.Join(" / ", orderUsages.ToArray()));

            return new StorageNetworkWorldPanelContent(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_PRODUCTION_TITLE), lineOne, lineTwo, lineThree);
        }

        private StorageNetworkWorldPanelContent BuildStorageContent(GameObject target, Storage storage)
        {
            StorageSceneSnapshot snapshot = StorageSceneCollector.Collect();
            string lineOne = string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_NETWORK_CAPACITY),
                GameUtil.GetFormattedMass(snapshot.TotalStoredKg),
                GameUtil.GetFormattedMass(snapshot.TotalCapacityKg));
            string lineTwo = storage != null
                ? string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_BUILDING_STORAGE),
                    GameUtil.GetFormattedMass(StorageItemUtility.GetStoredMass(storage)),
                    GameUtil.GetFormattedMass(storage.capacityKg))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_BUILDING_NO_STORAGE);

            StorageNetworkStorageConnector connector = target.GetComponent<StorageNetworkStorageConnector>();
            string lineThree = connector != null && !string.IsNullOrEmpty(connector.LastOutputStatus)
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_OUTPUT_STATUS), connector.LastOutputStatus)
                : string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_CONNECTED_BUILDINGS), snapshot.Storages.Count);

            return new StorageNetworkWorldPanelContent(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.WORLD_STORAGE_TITLE), lineOne, lineTwo, lineThree);
        }

        private List<string> GetRelatedOrderUsages(ComplexFabricator fabricator)
        {
            return orderService.GetActiveOrderUsagesForFabricator(fabricator, 3).ToList();
        }
    }
}
