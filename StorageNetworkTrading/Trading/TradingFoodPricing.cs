using System;

namespace StorageNetwork.Trading
{
    internal static class TradingFoodPricing
    {
        internal const decimal IngredientPrice = 8m;

        // Food is traded by item. Quality sets the price band; energy adds up to
        // 50% within that band, below the next quality tier's 75% step.
        internal static decimal BasePrice(int quality, float caloriesPerUnit = 0)
        {
            decimal energy = (decimal)Math.Max(0d, caloriesPerUnit / 1000000d);
            decimal energyFactor = 1m + 0.5m * energy / (energy + 2m);
            return Math.Max(0.01m, Math.Round(28m * (decimal)Math.Pow(1.75d, quality) * energyFactor, 2));
        }
    }
}
