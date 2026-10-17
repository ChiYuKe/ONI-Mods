using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.UI;
using StorageNetwork.Core;

internal static class MainThreadOptimizationChecks
{
    public static void Verify(Action<bool, string> check)
    {
        VerifyFrameBudget(check);
        VerifyWakeGate(check);
        var totals = new StorageNetworkIncrementalTotals<int, double>();
        var actual = new Dictionary<int, double>();
        var sources = new Dictionary<int, double>[20];
        for (int i = 0; i < sources.Length; i++)
        {
            sources[i] = new Dictionary<int, double> { [i] = 100d, [100] = i + 0.5d };
            totals.SetSource(i, sources[i]);
        }
        totals.ApplyChanges(actual, (a, b) => a + b);
        check(totals.ApplyChanges(actual, (a, b) => a + b) == 0 && actual.Count == 21 && actual[19] == 100d,
            "an unchanged summary refresh retains every cached material without refolding");
        sources[3][3] = 200;
        totals.SetSource(3, sources[3]);
        check(totals.ApplyChanges(actual, (a, b) => a + b) == 2 && actual[3] == 200,
            "incremental summary folds only two affected keys after one of twenty sources changes");
        var random = new Random(731);
        for (int step = 0; step < 200; step++)
        {
            int source = random.Next(sources.Length), key = random.Next(30);
            if (random.Next(3) == 0) sources[source].Remove(key);
            else sources[source][key] = random.NextDouble() * 100;
            totals.SetSource(source, sources[source]);
            totals.ApplyChanges(actual, (a, b) => a + b);
            var reference = new Dictionary<int, double>();
            foreach (var values in sources)
                foreach (var pair in values)
                    reference[pair.Key] = reference.TryGetValue(pair.Key, out double old) ? old + pair.Value : pair.Value;
            check(actual.Count == reference.Count && actual.All(p => reference[p.Key] == p.Value),
                "incremental summary equals native-order full fold after dynamic source update " + step);
        }
        for (int i = 0; i < sources.Length; i++) totals.SetSource(i, Array.Empty<KeyValuePair<int, double>>());
        totals.ApplyChanges(actual, (a, b) => a + b);
        check(actual.Count == 0, "incremental summary removes keys when their last contribution disappears");

        var order = new StorageNetworkStableDisplayOrder<(string Key, string Name, int Mass)>();
        var rows = new List<(string Key, string Name, int Mass)> { ("b", "Zinc", 1), ("a", "Copper", 2) };
        check(order.Apply(rows, r => r.Key, r => r.Name, StringComparer.Ordinal) && rows[0].Key == "a",
            "display order sorts the initial material structure");
        rows.Clear(); rows.Add(("b", "Zinc", 50)); rows.Add(("a", "Copper", 20));
        check(!order.Apply(rows, r => r.Key, r => r.Name, StringComparer.Ordinal) && rows[0].Mass == 20,
            "mass-only updates reuse display order without sorting");
        rows[1] = ("b", "Aluminum", 50);
        check(order.Apply(rows, r => r.Key, r => r.Name, StringComparer.Ordinal) && rows[0].Key == "b",
            "renamed or localized materials invalidate alphabetical order");
        rows.RemoveAt(0);
        check(order.Apply(rows, r => r.Key, r => r.Name, StringComparer.Ordinal),
            "removing a material rebuilds the display order");
    }

    private static void VerifyFrameBudget(Action<bool, string> check)
    {
        long clock = 0;
        var queue = new StorageNetworkFrameWorkQueue(() => clock, 1000);
        var executed = new List<int>();
        bool visible = false, alive = true;
        queue.Enqueue(() => executed.Add(0), () => alive, () => visible);
        queue.Enqueue(() => { executed.Add(1); clock += 2; }, () => true, () => true);
        queue.Enqueue(() => executed.Add(2), () => true, () => true);
        check(queue.Run(8, 1) == 1 && executed.SequenceEqual(new[] { 1 }),
            "frame queue yields after its time budget and does not block behind hidden rows");
        visible = true;
        check(queue.Run(1, 1) == 1 && executed[1] == 2 && queue.Count == 1,
            "frame queue enforces row count and preserves ready-work order");
        alive = false;
        check(queue.Run(8, 1) == 0 && queue.Count == 0,
            "destroyed row work is discarded before touching dead objects");
        queue.Enqueue(() => queue.Enqueue(() => executed.Add(4), () => true, () => true), () => true, () => true);
        check(queue.Run(8, 1) == 1 && queue.Count == 1,
            "nested row construction waits until a later frame");
        queue.Clear();
        check(queue.Count == 0, "clearing a view releases queued construction callbacks");
        visible = false;
        queue.Enqueue(() => executed.Add(5), () => true, () => visible);
        check(queue.Run(2, 1) == 0 && queue.Count == 1,
            "hidden cached rows keep their pending construction work");
        visible = true;
        check(queue.Run(2, 1) == 1 && executed[executed.Count - 1] == 5,
            "showing a cached row resumes its construction instead of leaving an empty placeholder");
    }

    private static void VerifyWakeGate(Action<bool, string> check)
    {
        var gate = new StorageNetworkRequestWakeGate();
        check(gate.ShouldAttempt(1, 0), "new requests do not sleep");
        gate.RecordResult(false, 1, 0);
        check(!gate.ShouldAttempt(1, 1) && !gate.ShouldAttempt(1, 4.99f),
            "unchanged failed requests avoid repeated searches until fallback");
        check(gate.ShouldAttempt(2, 1), "a relevant mutation wakes a failed request immediately");
        check(gate.ShouldAttempt(1, 5), "fallback polling remains available without events");
        gate.RecordResult(true, 1, 5);
        check(gate.ShouldAttempt(1, 5), "successful transfers preserve their existing next-tick behavior");
        gate.RecordResult(false, 1, 5); gate.Reset();
        check(gate.ShouldAttempt(1, 5), "owner, filter and settings changes explicitly wake requests");
    }
}
