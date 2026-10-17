using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.Trading
{
    internal sealed class TradingPort
    {
        public int WorldId, Q, R;
    }

    internal sealed class TradingVoyage
    {
        public TradingSupplier Visitor;
        public int FromQ, FromR, ToQ, ToR, WorldId;
        public double Start, Arrival;
        public bool Outbound;
        public bool WaitingForDestination;
        public double Progress(double cycle) => Math.Max(0, Math.Min(1, (cycle - Start) / Math.Max(.001, Arrival - Start)));
    }

    internal sealed partial class TradingMarket
    {
        internal const int MaximumVisitorsPerWorld = 5;
        public bool FleetEnabled;
        public double NextFleetDispatch;
        public List<TradingPort> TradePorts = new List<TradingPort>();
        public List<TradingVoyage> Voyages = new List<TradingVoyage>();

        internal bool ConfigureFleet(List<TradingPort> ports, int migrationWorld, double cycle)
        {
            if (ports == null || ports.Count == 0) return false;
            bool changed = !FleetEnabled;
            TradePorts = TradePorts ?? new List<TradingPort>();
            Voyages = Voyages ?? new List<TradingVoyage>();
            foreach (var port in ports)
            {
                var existing = TradePorts.Find(p => p.WorldId == port.WorldId);
                if (existing == null) { TradePorts.Add(port); changed = true; }
                else if (existing.Q != port.Q || existing.R != port.R)
                { existing.Q = port.Q; existing.R = port.R; changed = true; }
            }
            if (!FleetEnabled)
            {
                FleetEnabled = true;
                UpcomingVisit = null;
                NextFleetDispatch = cycle;
                var home = TradePorts.Find(p => p.WorldId == migrationWorld) ?? TradePorts[0];
                // Preserve existing visits and money limits when upgrading an old save.
                foreach (var supplier in Suppliers)
                {
                    supplier.VisitWorldId = home.WorldId;
                    Voyages.Add(new TradingVoyage { Visitor = supplier, WorldId = home.WorldId,
                        FromQ = home.Q + 4, FromR = home.R, ToQ = home.Q, ToR = home.R,
                        Start = supplier.Arrival - 4, Arrival = supplier.Arrival });
                }
            }
            return changed;
        }

        internal List<TradingSupplier> VisitorsAt(int worldId, double cycle) => Suppliers
            .Where(s => s.Arrival <= cycle && s.Departure > cycle && (!FleetEnabled || s.VisitWorldId == worldId))
            .OrderBy(s => s.Id).ToList();

        internal bool IsVisiting(TradingSupplier supplier, int worldId, double cycle) => supplier != null &&
            supplier.Arrival <= cycle && supplier.Departure > cycle && (!FleetEnabled || supplier.VisitWorldId == worldId);

        private bool RefreshFleet(double cycle, IReadOnlyList<TradingGood> catalog)
        {
            bool changed = PrepareMarket(cycle);
            if (QuoteSignalVersion < 1) { QuoteSignalVersion = 1; changed = true; }
            Voyages = Voyages ?? new List<TradingVoyage>();
            foreach (var voyage in Voyages.ToList())
            {
                var supplier = voyage.Visitor;
                if (supplier == null) { Voyages.Remove(voyage); changed = true; continue; }
                // JSON stores both lists; rebind to the authoritative docked instance after loading.
                var docked = Suppliers.Find(s => s.Id == supplier.Id);
                if (docked != null) voyage.Visitor = supplier = docked;
                if (voyage.Outbound)
                {
                    // Upgrade old exit-to-empty-space legs into a planet-to-planet itinerary.
                    BeginWaiting(voyage, cycle);
                    TryRouteToNextPlanet(voyage, cycle);
                    changed = true;
                    continue;
                }
                if (voyage.WaitingForDestination)
                {
                    changed |= TryRouteToNextPlanet(voyage, cycle);
                    continue;
                }
                var destination = TradePorts.Find(p => p.WorldId == voyage.WorldId);
                if (destination != null && (voyage.ToQ != destination.Q || voyage.ToR != destination.R))
                {
                    // The destination world is authoritative, never a nearby empty hex.
                    voyage.ToQ = destination.Q; voyage.ToR = destination.R;
                    changed = true;
                }
                if (cycle < voyage.Arrival) continue;
                if (cycle >= supplier.Departure)
                {
                    Suppliers.RemoveAll(s => s.Id == supplier.Id);
                    BeginWaiting(voyage, cycle);
                    TryRouteToNextPlanet(voyage, cycle);
                    changed = true;
                    continue;
                }
                if (docked == null)
                {
                    if (VisitorsAt(voyage.WorldId, cycle).Count >= MaximumVisitorsPerWorld)
                    {
                        BeginWaiting(voyage, cycle);
                        TryRouteToNextPlanet(voyage, cycle);
                        changed = true;
                        continue;
                    }
                    Suppliers.Add(supplier); changed = true;
                }
                changed |= Restock(supplier, cycle, catalog);
                changed |= TradingDialogue.Assign(supplier);
                supplier.Pressure = Pressure;
            }
            changed |= Suppliers.RemoveAll(s => s.Departure <= cycle) > 0;
            if (cycle >= NextFleetDispatch && TradePorts.Count > 0)
            {
                var available = TradePorts.Where(p => DockedCount(p.WorldId, cycle) < MaximumVisitorsPerWorld).ToList();
                if (available.Count > 0)
                {
                    int id = NextId++;
                    var random = new Random(unchecked(id * 7919));
                    var port = available[random.Next(available.Count)];
                    int dq = random.Next(2) == 0 ? -4 : 4;
                    int dr = random.Next(-2, 3);
                    // Publish a saved itinerary before the merchant appears on the star map.
                    double launch = cycle + 2;
                    double arrival = launch + HexDistance(port.Q + dq, port.R + dr, port.Q, port.R);
                    var visitor = new TradingSupplier { Id = id, VisitWorldId = port.WorldId,
                        Specialty = ChooseSpecialty(id, catalog), Arrival = arrival, Departure = arrival,
                        BuyLimit = random.Next(20, 81) * 1000m, SellLimit = random.Next(20, 81) * 1000m };
                    TradingCharacters.Assign(visitor, Suppliers.Concat(Voyages.Where(v => v.Visitor != null).Select(v => v.Visitor)).ToList(), arrival);
                    TradingRelationships.RollVisit(this, visitor);
                    TradingRelationships.Initialize(this, visitor);
                    TradingDialogue.Assign(visitor);
                    FillStock(visitor, catalog, random);
                    visitor.InitialStock = new Dictionary<string, double>(visitor.Stock);
                    visitor.LastRestockCycle = (int)Math.Floor(arrival);
                    Voyages.Add(new TradingVoyage { Visitor = visitor, WorldId = port.WorldId,
                        FromQ = port.Q + dq, FromR = port.R + dr, ToQ = port.Q, ToR = port.R,
                        Start = launch, Arrival = arrival });
                }
                NextFleetDispatch = cycle + 2 + new Random(NextId).Next(3);
                changed = true;
            }
            var next = Voyages.Where(v => !v.Outbound && !v.WaitingForDestination && v.Arrival > cycle).OrderBy(v => v.Arrival).FirstOrDefault();
            UpcomingVisit = next == null ? null : new TradingArrivalPlan { ExpectedCycle = next.Arrival, Visitor = next.Visitor,
                WorldId = next.WorldId, LaunchCycle = next.Start,
                RouteKey = next.Visitor.Id + ":" + next.WorldId + ":" + next.Start.ToString("R", System.Globalization.CultureInfo.InvariantCulture) };
            foreach (var supplier in Suppliers) AttachInventory(supplier);
            if (changed) PruneInventoryHistory();
            return RecordPrices(cycle, catalog, changed) || changed;
        }

        private int DockedCount(int worldId, double cycle) => Suppliers.Count(s =>
            s.VisitWorldId == worldId && s.Arrival <= cycle && s.Departure > cycle);

        private void BeginWaiting(TradingVoyage voyage, double cycle)
        {
            var port = TradePorts.Find(p => p.WorldId == voyage.WorldId);
            int q = port?.Q ?? voyage.FromQ, r = port?.R ?? voyage.FromR;
            voyage.FromQ = voyage.ToQ = q;
            voyage.FromR = voyage.ToR = r;
            voyage.Start = voyage.Arrival = cycle;
            voyage.Outbound = false;
            voyage.WaitingForDestination = true;
        }

        private bool TryRouteToNextPlanet(TradingVoyage voyage, double cycle)
        {
            var available = TradePorts.Where(p => p.WorldId != voyage.WorldId &&
                DockedCount(p.WorldId, cycle) < MaximumVisitorsPerWorld).ToList();
            if (available.Count == 0) return false;
            var random = new Random(unchecked(voyage.Visitor.Id * 7919 ^ (int)Math.Floor(cycle * 1000)));
            var target = available[random.Next(available.Count)];
            var supplier = voyage.Visitor;
            double duration = Math.Max(1, HexDistance(voyage.FromQ, voyage.FromR, target.Q, target.R));
            voyage.WorldId = supplier.VisitWorldId = target.WorldId;
            voyage.ToQ = target.Q; voyage.ToR = target.R;
            voyage.Start = cycle;
            voyage.Arrival = supplier.Arrival = cycle + duration;
            voyage.WaitingForDestination = false;
            TradingRelationships.RollVisit(this, supplier);
            // Carry stock and monetary usage forward; changing planets cannot reset trade limits.
            supplier.BaseDeparture = supplier.Departure;
            return true;
        }

        internal static int HexDistance(int q1, int r1, int q2, int r2) =>
            (Math.Abs(q1 - q2) + Math.Abs(r1 - r2) + Math.Abs(q1 + r1 - q2 - r2)) / 2;
    }
}
