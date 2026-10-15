using System;

namespace StorageNetwork.Trading
{
    internal static class TradingPrecision
    {
        internal static void ApplyMinimum(TradingQuote quote, double minimum, decimal balance)
        {
            quote.MinimumAmount = minimum;
            if (quote.MaximumAmount > 0 && quote.MaximumAmount < minimum)
            {
                quote.MaximumAmount = quote.Amount = 0;
                quote.Total = 0;
                quote.BalanceAfter = balance;
                quote.Reason = (quote.LimitReason ?? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_BF7330B5535A")) + TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_2B92EB18DD51", TradingText.Argument((object)(minimum), "0.###", 0), TradingText.Argument((object)((quote.Good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_421F465DAEE8") : " kg")), null, 0));
            }
            else if (quote.Amount > 0 && quote.Amount < minimum)
            {
                quote.Amount = 0;
                quote.Total = 0;
                quote.BalanceAfter = balance;
                quote.Reason = TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_7EF691373E38", TradingText.Argument((object)(minimum), "0.###", 0), TradingText.Argument((object)((quote.Good.ByUnit ? TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_421F465DAEE8") : " kg")), null, 0));
            }
        }

        internal static double MinimumTransfer(float storedMass, float massPerUnit, bool byUnit, float nativeMinimum)
        {
            if (float.IsNaN(storedMass) || float.IsInfinity(storedMass) || storedMass < 0 ||
                (byUnit && (float.IsNaN(massPerUnit) || float.IsInfinity(massPerUnit) || massPerUnit <= 0))) return double.PositiveInfinity;
            // Subtracting less than one mass ULP from a large float stack can
            // leave its mass unchanged, even though Pickupable creates a split.
            float nextMass = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(storedMass) + 1);
            float aboveMinimum = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(nativeMinimum) + 1);
            double minimumMass = Math.Max((double)nextMass - storedMass, aboveMinimum);
            double units = minimumMass / (byUnit ? massPerUnit : 1);
            return byUnit ? Math.Max(1, Math.Ceiling(units)) : Math.Max(0.001, Math.Ceiling(units * 1000d) / 1000d);
        }
    }
}
