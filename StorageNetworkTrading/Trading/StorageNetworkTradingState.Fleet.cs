using System.Collections.Generic;
using StorageNetwork.API;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        internal static int TradeWorldId(int worldId)
        {
            var world = ClusterManager.Instance?.GetWorld(worldId);
            return world != null && world.IsModuleInterior ? world.ParentWorldId : worldId;
        }

        internal List<TradingSupplier> LocalVisitors(int worldId) => market.VisitorsAt(TradeWorldId(worldId), Cycle);

        private bool ConfigureRuntimeFleet()
        {
            if (!DlcManager.FeatureClusterSpaceEnabled() || ClusterManager.Instance == null || ClusterGrid.Instance == null) return false;
            var ports = new List<TradingPort>();
            foreach (var world in ClusterManager.Instance.WorldContainers)
            {
                if (world == null || world.IsModuleInterior) continue;
                var entity = world.GetComponent<AsteroidGridEntity>();
                if (entity == null) continue;
                ports.Add(new TradingPort { WorldId = world.id, Q = entity.Location.Q, R = entity.Location.R });
            }
            return market.ConfigureFleet(ports, TradeWorldId(ClusterManager.Instance.activeWorldId), Cycle);
        }
    }
}
