using System.Collections.Generic;

namespace StorageNetwork.Trading
{
    internal sealed class TradingStationLocation
    {
        public int Q;
        public int R;
    }

    internal sealed partial class TradingMarket
    {
        public List<TradingStationLocation> StarMapStations = new List<TradingStationLocation>();
    }

    internal sealed partial class StorageNetworkTradingState
    {
        internal void SaveStarMapStations() => Persist();
    }
}
