using System.Collections.Generic;
using StorageNetwork.Core;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private int GetCurrentNetworkWorldId()
        {
            if (networkWorldId >= 0)
            {
                return networkWorldId;
            }

            networkWorldId = orderCenterScope != null
                ? StorageNetworkWorldUtility.GetObjectWorldId(orderCenterScope.gameObject)
                : GetActiveWorldId();
            return networkWorldId;
        }

        private static int GetActiveWorldId()
        {
            return ClusterManager.Instance != null
                ? ClusterManager.Instance.activeWorldId
                : -1;
        }

        private int SetNetworkWorldForPlan(
            IList<ComplexFabricator> fabricators,
            int preferredWorldId = -1)
        {
            if (preferredWorldId >= 0)
            {
                networkWorldId = preferredWorldId;
                return networkWorldId;
            }

            if (orderCenterScope != null)
            {
                int scopedWorldId = StorageNetworkWorldUtility.GetObjectWorldId(orderCenterScope.gameObject);
                if (scopedWorldId >= 0)
                {
                    networkWorldId = scopedWorldId;
                    return networkWorldId;
                }
            }

            if (fabricators != null)
            {
                for (int i = 0; i < fabricators.Count; i++)
                {
                    ComplexFabricator fabricator = fabricators[i];
                    if (!StorageSceneRegistry.IsLive(fabricator))
                    {
                        continue;
                    }

                    int fabricatorWorldId = StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject);
                    if (fabricatorWorldId >= 0 && StorageSceneRegistry.HasOnlineCoreInWorld(fabricatorWorldId))
                    {
                        networkWorldId = fabricatorWorldId;
                        return networkWorldId;
                    }
                }
            }

            networkWorldId = GetActiveWorldId();
            return networkWorldId;
        }

        private static int GetOrderNetworkWorldId(ProductionOrderRecord order)
        {
            int fallbackWorldId = -1;
            if (order?.QueueAssignments != null)
            {
                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    ComplexFabricator fabricator = assignment?.Fabricator;
                    if (!StorageSceneRegistry.IsLive(fabricator))
                    {
                        continue;
                    }

                    int worldId = StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject);
                    if (worldId >= 0)
                    {
                        if (StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
                        {
                            return worldId;
                        }

                        if (fallbackWorldId < 0)
                        {
                            fallbackWorldId = worldId;
                        }
                    }
                }
            }

            return fallbackWorldId >= 0 ? fallbackWorldId : GetActiveWorldId();
        }

        private static bool IsFabricatorReachableFromWorld(
            ComplexFabricator fabricator,
            int destinationWorldId)
        {
            if (!IsOrderProductionFabricator(fabricator) ||
                !StorageSceneRegistry.IsLive(fabricator) ||
                destinationWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
            {
                return false;
            }

            int fabricatorWorldId = StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject);
            return fabricatorWorldId >= 0 &&
                   StorageSceneRegistry.HasOnlineCoreInWorld(fabricatorWorldId) &&
                   (fabricatorWorldId == destinationWorldId ||
                    StorageSceneRegistry.IsCrossPlanetRelayOnline());
        }

        private static bool IsSourceUsableForDestination(
            Storage source,
            Storage destination,
            Tag tag)
        {
            if (source == null || destination == null || source == destination ||
                tag == Tag.Invalid || !StorageSceneRegistry.IsLive(destination))
            {
                return false;
            }

            int destinationWorldId = StorageNetworkWorldUtility.GetObjectWorldId(destination.gameObject);
            return ProductionNetworkInventoryCache.IsUsableSource(source, destinationWorldId) &&
                   source.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private bool IsOrderReachableFromCurrentWorld(ProductionOrderRecord order)
        {
            if (order?.QueueAssignments == null)
            {
                return false;
            }

            int worldId = GetCurrentNetworkWorldId();
            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (IsFabricatorReachableFromWorld(assignment?.Fabricator, worldId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsRouteReachableFromWorld(RecipeDisplayInfo route, int worldId)
        {
            if (route.Fabricators == null)
            {
                return false;
            }

            for (int i = 0; i < route.Fabricators.Count; i++)
            {
                if (IsFabricatorReachableFromWorld(route.Fabricators[i], worldId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
