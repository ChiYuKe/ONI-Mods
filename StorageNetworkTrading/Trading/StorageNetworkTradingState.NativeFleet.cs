using System.Collections.Generic;
using System.Linq;
using StorageNetworkTrading.UI;

namespace StorageNetwork.Trading
{
    internal sealed partial class StorageNetworkTradingState
    {
        private readonly Dictionary<int, TradingMerchantClusterEntity> nativeMerchantEntities = new Dictionary<int, TradingMerchantClusterEntity>();

        private bool SynchronizeNativeFleet()
        {
            if (!market.FleetEnabled || ClusterGrid.Instance == null || market.Voyages == null) return false;
            var active = new HashSet<int>(market.Voyages.Where(v => v.Visitor != null).Select(v => v.Visitor.Id));
            foreach (int id in nativeMerchantEntities.Keys.Where(id => !active.Contains(id)).ToList())
            {
                var entity = nativeMerchantEntities[id];
                if (entity != null) entity.gameObject.DeleteObject();
                nativeMerchantEntities.Remove(id);
            }
            bool changed = false;
            foreach (var voyage in market.Voyages)
            {
                if (voyage.Visitor == null) continue;
                if (Cycle < voyage.Start) continue;
                int oldQ = voyage.FromQ, oldR = voyage.FromR, endQ = voyage.ToQ, endR = voyage.ToR;
                if (!nativeMerchantEntities.TryGetValue(voyage.Visitor.Id, out var entity) || entity == null)
                {
                    entity = TradingMerchantClusterEntity.Create(transform, voyage);
                    nativeMerchantEntities[voyage.Visitor.Id] = entity;
                }
                else entity.Synchronize(voyage);
                changed |= oldQ != voyage.FromQ || oldR != voyage.FromR || endQ != voyage.ToQ || endR != voyage.ToR;
            }
            return changed;
        }

        private void ClearNativeFleet()
        {
            foreach (var entity in nativeMerchantEntities.Values)
                if (entity != null) entity.gameObject.DeleteObject();
            nativeMerchantEntities.Clear();
        }
    }
}
