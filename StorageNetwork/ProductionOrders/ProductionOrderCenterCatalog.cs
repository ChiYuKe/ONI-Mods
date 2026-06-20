using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderCenterCatalog
    {
        private static readonly List<StorageNetworkOrderProductionCenter> Centers = new List<StorageNetworkOrderProductionCenter>();

        public static void Register(StorageNetworkOrderProductionCenter center)
        {
            if (center != null && !Centers.Contains(center))
            {
                Centers.Add(center);
            }
        }

        public static void Unregister(StorageNetworkOrderProductionCenter center)
        {
            Centers.Remove(center);
        }

        public static List<StorageNetworkOrderProductionCenter> GetCenters()
        {
            PruneInvalidCenters();
            if (Centers.Count == 0)
            {
                foreach (StorageNetworkOrderProductionCenter center in Object.FindObjectsByType<StorageNetworkOrderProductionCenter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    Register(center);
                }
            }

            return new List<StorageNetworkOrderProductionCenter>(Centers);
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

        private static void PruneInvalidCenters()
        {
            for (int i = Centers.Count - 1; i >= 0; i--)
            {
                if (Centers[i] == null)
                {
                    Centers.RemoveAt(i);
                }
            }
        }
    }
}
