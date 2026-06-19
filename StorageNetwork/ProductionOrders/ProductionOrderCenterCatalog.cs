using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderCenterCatalog
    {
        public static List<StorageNetworkOrderProductionCenter> GetCenters()
        {
            return Object.FindObjectsByType<StorageNetworkOrderProductionCenter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(center => center != null)
                .Distinct()
                .ToList();
        }

        public static IEnumerable<ComplexFabricator> GetFabricators()
        {
            foreach (StorageNetworkOrderProductionCenter center in GetCenters())
            {
                ComplexFabricator fabricator = GetFabricator(center);
                if (fabricator != null)
                {
                    yield return fabricator;
                }
            }
        }

        public static ComplexFabricator GetFabricator(StorageNetworkOrderProductionCenter center)
        {
            return center != null ? center.GetComponent<ComplexFabricator>() : null;
        }

        public static bool IsOrderProductionFabricator(ComplexFabricator fabricator)
        {
            return fabricator != null &&
                   fabricator.GetComponent<StorageNetworkOrderProductionCenter>() != null;
        }

        public static ComplexFabricator FindFabricatorByInstanceId(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            return GetFabricators()
                .FirstOrDefault(fabricator => GetInstanceId(fabricator) == instanceId);
        }

        public static int GetInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
