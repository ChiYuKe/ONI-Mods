using System;

namespace StorageNetwork.Trading
{
    internal static class TradingRelationships
    {
        private static readonly decimal[] Rates = { 0.65m, 1.35m, 0.75m, 1.20m, 1.50m, 1m, 0.90m, 0.80m };
        internal static int Temperament(TradingSupplier supplier) => new Random(TradingCharacters.Find(supplier)?.AppearanceSeed ??
            unchecked(supplier.Id * 15485863 ^ 0x56af)).Next(8);
        internal static decimal GrowthRate(TradingSupplier supplier)
        {
            if (supplier.CharacterId == "greyfinch") return 0.60m;
            if (supplier.CharacterId == "oldmi") return 1.10m;
            if (supplier.CharacterId == "lamplow") return 0.75m;
            return Rates[Temperament(supplier)];
        }
        internal static decimal Favor(TradingSupplier supplier) => Math.Max(0, Math.Min(100, supplier.Relationship?.Affection ?? 0));
        internal static int MinimumVisitCycles(TradingSupplier supplier) => 1 + Math.Min(4, (int)(Favor(supplier) / 20m));
        internal static void RollVisit(TradingMarket market, TradingSupplier supplier)
        {
            market.Memory(supplier);
            supplier.VisitMinimumCycles = MinimumVisitCycles(supplier);
            var random = new Random(unchecked(supplier.Id * 7919 ^ 0x731cf));
            supplier.Departure = supplier.Arrival + random.Next(supplier.VisitMinimumCycles, 16);
        }
        internal static decimal BuyFactor(TradingSupplier supplier) => 1m - Favor(supplier) / 100m * 0.02m;
        internal static decimal SellFactor(TradingSupplier supplier) => 1m + Favor(supplier) / 100m * 0.01m;
        internal static decimal CooperationFactor(TradingSupplier supplier, TradingDialogueMemory memory) =>
            Math.Round(1m - 0.02m - (memory.Trust > 0 ? 0.03m : 0m) - Favor(supplier) / 100m * 0.02m * GrowthRate(supplier), 4);

        internal static void Initialize(TradingMarket market, TradingSupplier supplier)
        {
            market.Memory(supplier);
            if (!supplier.RelationshipBaselineSet)
            {
                supplier.RelationshipBaselineSet = true;
                supplier.BaseDeparture = supplier.Departure;
                supplier.BaseBuyLimit = supplier.BuyLimit;
                supplier.BaseSellLimit = supplier.SellLimit;
            }
            ApplyBenefits(supplier);
        }
        private static void ApplyBenefits(TradingSupplier supplier)
        {
            decimal favor = Favor(supplier);
            int quotaTier = (int)Math.Floor(favor / 20m);
            if (quotaTier > supplier.RelationshipQuotaTier)
            {
                decimal increment = (quotaTier - supplier.RelationshipQuotaTier) * 0.1m;
                supplier.BuyLimit += Math.Round(supplier.BaseBuyLimit * increment, 2);
                supplier.SellLimit += Math.Round(supplier.BaseSellLimit * increment, 2);
                supplier.RelationshipQuotaTier = quotaTier;
            }
        }
        internal static void OnSettled(TradingMarket market, TradingSupplier supplier, decimal actualTotal)
        {
            if (actualTotal <= 0) return;
            Initialize(market, supplier);
            TradingDialogueMemory memory = supplier.Relationship;
            // No per-click bonus: splitting the same turnover cannot accelerate progress.
            memory.Affection = Math.Min(100m, Math.Max(0, memory.Affection) + actualTotal / 10000m * GrowthRate(supplier));
            memory.SuccessfulTrades++;
            memory.TotalTradeValue += actualTotal;
            ApplyBenefits(supplier);
        }
        internal static string Stage(TradingSupplier supplier) => TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.REL_STAGE_" +
            Math.Min(4, (int)(Favor(supplier) / 20m)));
        internal static string Tone(TradingSupplier supplier)
        {
            int stage = Math.Min(4, (int)(Favor(supplier) / 20m));
            if (stage == 0) return "";
            string character = supplier.CharacterId;
            if (character == "greyfinch" || character == "oldmi" || character == "lamplow")
                return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.REL_TONE_" + character.ToUpperInvariant() + (stage >= 3 ? "_CLOSE" : "_KNOWN")) + "\n";
            int style = Temperament(supplier) % 3;
            return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.REL_TONE_" + style + (stage >= 3 ? "_CLOSE" : "_KNOWN")) + "\n";
        }
        internal static string Card(TradingSupplier supplier)
        {
            return TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.REL_CARD",
                TradingText.Argument(GrowthRate(supplier), "0.##"),
                TradingText.Argument((1m - BuyFactor(supplier)) * 100, "0.##"), TradingText.Argument((SellFactor(supplier) - 1m) * 100, "0.##"),
                TradingText.Argument(Math.Floor(Favor(supplier) / 20m) * 10, "0"), TradingText.Argument(MinimumVisitCycles(supplier)),
                TradingText.Argument(supplier.Relationship?.SuccessfulTrades ?? 0),
                TradingText.Argument((1m - (supplier.DialogueNegotiated ? supplier.DialoguePriceFactor : CooperationFactor(supplier, supplier.Relationship ?? new TradingDialogueMemory()))) * 100m, "0.##"));
        }
    }
    internal sealed partial class TradingMarket
    {
        internal void RefreshRelationshipPrices(double cycle, System.Collections.Generic.IReadOnlyList<TradingGood> catalog)
        {
            RecordPrices(cycle, catalog, true);
        }
    }
}
