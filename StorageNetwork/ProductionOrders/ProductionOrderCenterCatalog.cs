using System.Collections.Generic;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderCenterCatalog
    {
        private static readonly List<StorageNetworkOrderProductionCenter> Centers = new List<StorageNetworkOrderProductionCenter>();
        private static readonly List<ComplexFabricator> Fabricators = new List<ComplexFabricator>();
        private static readonly Dictionary<StorageNetworkOrderProductionCenter, ComplexFabricator> FabricatorsByCenter =
            new Dictionary<StorageNetworkOrderProductionCenter, ComplexFabricator>();
        private static readonly Dictionary<int, ComplexFabricator> FabricatorsByInstanceId =
            new Dictionary<int, ComplexFabricator>();
        private static readonly Dictionary<ComplexFabricator, int> FabricatorInstanceIds =
            new Dictionary<ComplexFabricator, int>();
        private static readonly Dictionary<StorageNetworkOrderProductionCenter, int> CenterInstanceIds =
            new Dictionary<StorageNetworkOrderProductionCenter, int>();
        private static readonly HashSet<ComplexFabricator> FabricatorSet =
            new HashSet<ComplexFabricator>();
        private static int version;

        public static int Version => version;

        public static void Register(StorageNetworkOrderProductionCenter center)
        {
            if (center != null && !Centers.Contains(center))
            {
                Centers.Add(center);
                int centerInstanceId = ResolveInstanceId(center);
                if (centerInstanceId != KPrefabID.InvalidInstanceID)
                {
                    CenterInstanceIds[center] = centerInstanceId;
                }

                ComplexFabricator fabricator = GetFabricator(center);
                if (fabricator != null && FabricatorSet.Add(fabricator))
                {
                    FabricatorsByCenter[center] = fabricator;
                    Fabricators.Add(fabricator);
                    int instanceId = ResolveInstanceId(fabricator);
                    if (instanceId != KPrefabID.InvalidInstanceID)
                    {
                        FabricatorInstanceIds[fabricator] = instanceId;
                        FabricatorsByInstanceId[instanceId] = fabricator;
                    }
                }
                version++;
            }
        }

        public static void Unregister(StorageNetworkOrderProductionCenter center)
        {
            if (!Centers.Remove(center))
            {
                return;
            }

            FabricatorsByCenter.TryGetValue(center, out ComplexFabricator fabricator);
            FabricatorsByCenter.Remove(center);
            CenterInstanceIds.Remove(center);
            if (fabricator != null)
            {
                FabricatorSet.Remove(fabricator);
                Fabricators.Remove(fabricator);
                int instanceId = GetInstanceId(fabricator);
                FabricatorInstanceIds.Remove(fabricator);
                if (instanceId != KPrefabID.InvalidInstanceID &&
                    FabricatorsByInstanceId.TryGetValue(instanceId, out ComplexFabricator registered) &&
                    ReferenceEquals(registered, fabricator))
                {
                    FabricatorsByInstanceId.Remove(instanceId);
                }
            }

            version++;
        }

        public static void InvalidateRecipes()
        {
            version++;
        }

        public static IReadOnlyList<StorageNetworkOrderProductionCenter> GetCenters()
        {
            PruneInvalidCenters();
            return Centers;
        }

        public static IReadOnlyList<ComplexFabricator> GetFabricators()
        {
            PruneInvalidCenters();
            return Fabricators;
        }

        public static ComplexFabricator GetFabricator(StorageNetworkOrderProductionCenter center)
        {
            return center != null ? center.GetComponent<ComplexFabricator>() : null;
        }

        public static bool IsOrderProductionFabricator(ComplexFabricator fabricator)
        {
            return fabricator != null && FabricatorSet.Contains(fabricator);
        }

        public static ComplexFabricator FindFabricatorByInstanceId(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            return FabricatorsByInstanceId.TryGetValue(instanceId, out ComplexFabricator fabricator) &&
                   fabricator != null
                ? fabricator
                : null;
        }

        public static int GetInstanceId(Component component)
        {
            if (component is ComplexFabricator fabricator &&
                FabricatorInstanceIds.TryGetValue(fabricator, out int fabricatorInstanceId))
            {
                return fabricatorInstanceId;
            }

            if (component is StorageNetworkOrderProductionCenter center &&
                CenterInstanceIds.TryGetValue(center, out int centerInstanceId))
            {
                return centerInstanceId;
            }

            return ResolveInstanceId(component);
        }

        private static int ResolveInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        public static void ResetRuntimeState()
        {
            Centers.Clear();
            Fabricators.Clear();
            FabricatorsByCenter.Clear();
            FabricatorsByInstanceId.Clear();
            FabricatorInstanceIds.Clear();
            CenterInstanceIds.Clear();
            FabricatorSet.Clear();
            version++;
        }

        private static void PruneInvalidCenters()
        {
            for (int i = Centers.Count - 1; i >= 0; i--)
            {
                if (Centers[i] == null)
                {
                    StorageNetworkOrderProductionCenter center = Centers[i];
                    FabricatorsByCenter.TryGetValue(center, out ComplexFabricator fabricator);
                    FabricatorsByCenter.Remove(center);
                    CenterInstanceIds.Remove(center);
                    if (!ReferenceEquals(fabricator, null))
                    {
                        FabricatorSet.Remove(fabricator);
                        int instanceId = GetInstanceId(fabricator);
                        if (instanceId != KPrefabID.InvalidInstanceID)
                        {
                            FabricatorsByInstanceId.Remove(instanceId);
                        }

                        FabricatorInstanceIds.Remove(fabricator);
                    }

                    Centers.RemoveAt(i);
                    if (!ReferenceEquals(fabricator, null))
                    {
                        Fabricators.Remove(fabricator);
                    }
                    version++;
                }
            }
        }
    }
}
