using System.Collections.Generic;

namespace StorageNetwork.Trading
{
    internal sealed class TradingGroupMessage
    {
        public int SupplierId;
        public string Name, Text, Specialty;
        public bool Player;
        public TradingAppearance Appearance;
        public double Cycle;
    }
    internal sealed partial class TradingMarket
    {
        public TradingGroupChat GroupChat = new TradingGroupChat();
    }
}
