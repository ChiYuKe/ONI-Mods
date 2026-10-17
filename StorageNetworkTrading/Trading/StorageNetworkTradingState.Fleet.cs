using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        private readonly HashSet<int> worldsWithBuiltCore = new HashSet<int>();
        private static readonly Tag BuiltCoreTag = new Tag("StorageNetworkCore");
        private double lastCoreEligibilityScan = -1;
        internal static int TradeWorldId(int worldId)
        {
            var world = ClusterManager.Instance?.GetWorld(worldId);
            return world != null && world.IsModuleInterior ? world.ParentWorldId : worldId;
        }

        internal List<TradingSupplier> LocalVisitors(int worldId) => market.VisitorsAt(TradeWorldId(worldId), Cycle);

        private bool ConfigureRuntimeFleet()
        {
            if (!DlcManager.FeatureClusterSpaceEnabled() || ClusterManager.Instance == null || ClusterGrid.Instance == null) return false;
            double seconds = Cycle * 600;
            if (lastCoreEligibilityScan < 0 || seconds < lastCoreEligibilityScan || seconds >= lastCoreEligibilityScan + 1)
            {
                lastCoreEligibilityScan = seconds;
                worldsWithBuiltCore.Clear();
                // Require a completed core, not a construction blueprint or an online power supply.
                foreach (var building in global::Components.BuildingCompletes.Items)
                {
                    if (building == null) continue;
                    var id = building.GetComponent<KPrefabID>();
                    if (id != null && id.PrefabTag == BuiltCoreTag)
                        worldsWithBuiltCore.Add(building.GetMyWorldId());
                }
            }
            var ports = new List<TradingPort>();
            var populations = new Dictionary<int, int>();
            foreach (var minion in global::Components.LiveMinionIdentities.Items)
            {
                if (minion == null) continue;
                int worldId = minion.GetMyWorldId();
                populations.TryGetValue(worldId, out int count);
                populations[worldId] = count + 1;
            }
            foreach (var world in ClusterManager.Instance.WorldContainers)
            {
                if (world == null || world.IsModuleInterior) continue;
                var entity = world.GetComponent<AsteroidGridEntity>();
                if (entity == null) continue;
                populations.TryGetValue(world.id, out int population);
                ports.Add(new TradingPort { WorldId = world.id, Q = entity.Location.Q, R = entity.Location.R, Population = population,
                    Eligible = world.IsDiscovered && worldsWithBuiltCore.Contains(world.id) });
            }
            bool changed = market.ConfigureFleet(ports, TradeWorldId(ClusterManager.Instance.activeWorldId), Cycle);
            foreach (var voyage in market.Voyages)
            {
                if (voyage.Visitor == null || voyage.Outbound || ports.Any(p => p.WorldId == voyage.WorldId && p.Eligible)) continue;
                if (nativeMerchantEntities.TryGetValue(voyage.Visitor.Id, out var ship) && ship != null)
                { voyage.FromQ = ship.Location.Q; voyage.FromR = ship.Location.R; }
            }
            if (market.ExitPort == null)
            {
                var tear = ClusterGrid.Instance.cellContents.Values.SelectMany(entities => entities).OfType<TemporalTear>().FirstOrDefault();
                if (tear != null)
                {
                    market.ExitPort = new TradingPort { WorldId = -1, Q = tear.Location.Q, R = tear.Location.R };
                    changed = true;
                }
            }
            return changed;
        }
    }
}
