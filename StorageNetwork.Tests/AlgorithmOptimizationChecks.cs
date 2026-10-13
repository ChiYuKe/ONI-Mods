using System;
using System.Collections.Generic;
using StorageNetwork.Services;

internal static class AlgorithmOptimizationChecks
{
    public static void Verify(Action<bool, string> check)
    {
        VerifySparsePrefixes(check);
        VerifyActualDeltas(check);
        VerifyCounters(check);
    }

    private static void VerifySparsePrefixes(Action<bool, string> check)
    {
        Random random = new Random(1731);
        for (int trial = 0; trial < 50; trial++)
        {
            var index = new StorageNetworkSparsePrefixIndex<int>();
            var legacy = new Dictionary<int, Dictionary<int, float>>();
            var prefix = new Dictionary<int, float>();
            int entries = 0;
            for (int port = 0; port < 120; port++)
            {
                // Include repeated IDs to verify the legacy overwrite behavior.
                int id = port / 2;
                var reserved = new Dictionary<int, float>();
                for (int tag = 0; tag < 40; tag++)
                    if (random.Next(10) == 0) reserved[tag] = random.Next(1, 200) * 0.125f;
                if (reserved.Count == 0) continue; // Native snapshot excludes empty ports.
                legacy[id] = new Dictionary<int, float>(prefix);
                foreach (var pair in reserved)
                {
                    prefix.TryGetValue(pair.Key, out float prior);
                    prefix[pair.Key] = prior + pair.Value;
                }
                index.AddPort(id, reserved, 0.001f);
                entries += reserved.Count;
            }
            bool equal = true;
            foreach (var port in legacy)
                for (int tag = 0; tag < 41; tag++)
                {
                    port.Value.TryGetValue(tag, out float expected);
                    equal &= index.GetEarlierAmount(port.Key, tag) == expected;
                }
            check(equal, "sparse runtime prefixes match copied legacy snapshots, including duplicate IDs and absent tags");
            check(index.GetEarlierAmount(-100, 0) == 0f, "missing/empty port preserves zero reservation behavior");
            check(index.EntryCount == entries, "sparse prefix memory contains only actual port-tag records");
        }
        var stress = new StorageNetworkSparsePrefixIndex<int>();
        for (int port = 0; port < 1000; port++)
            stress.AddPort(port, new Dictionary<int, float> { [port] = 1f }, 0.001f);
        check(stress.EntryCount == 1000 && stress.GetEarlierAmount(999, 0) == 1f &&
              stress.GetEarlierAmount(999, 999) == 0f,
            "1,000 distinct seeds use 1,000 sparse entries instead of 499,500 copied prefix entries");
    }

    private static void VerifyActualDeltas(Action<bool, string> check)
    {
        const float minimum = 0.001f;
        var amounts = new Dictionary<int, float> { [1] = 1.0005f, [2] = 50f };
        float actual = StorageNetworkAmountDelta.Apply(amounts, 1, -1f, minimum);
        check(!amounts.ContainsKey(1) && actual == -1.0005f && amounts[2] == 50f,
            "delta reports the discarded threshold residue without touching unrelated tags");
        check(StorageNetworkAmountDelta.Apply(amounts, 3, 0.0005f, minimum) == 0f &&
              !amounts.ContainsKey(3), "sub-threshold new contribution is not added to world totals");
        check(StorageNetworkAmountDelta.Apply(amounts, 3, float.NaN, minimum) == 0f &&
              !amounts.ContainsKey(3), "missing-key NaN delta preserves legacy non-insertion behavior");
        var legacy = new Dictionary<int, float>();
        amounts.Clear();
        Random random = new Random(209);
        bool equivalent = true;
        for (int step = 0; step < 20000; step++)
        {
            int key = random.Next(100);
            float delta = random.Next(-20, 21) * 0.125f;
            legacy.TryGetValue(key, out float before);
            if (Math.Abs(delta) > float.Epsilon)
            {
                if (legacy.ContainsKey(key))
                {
                    float updated = before + delta;
                    if (updated <= minimum) legacy.Remove(key);
                    else legacy[key] = updated;
                }
                else if (delta > minimum) legacy.Add(key, delta);
            }
            float change = StorageNetworkAmountDelta.Apply(amounts, key, delta, minimum);
            legacy.TryGetValue(key, out float expected);
            amounts.TryGetValue(key, out float result);
            equivalent &= result == expected && change == expected - before && amounts.Count == legacy.Count;
        }
        check(equivalent, "20,000 actual runtime delta updates match legacy threshold behavior and true aggregate differences");
        amounts.Clear();
        check(StorageNetworkAmountDelta.Apply(amounts, 1, 0f, minimum) == 0f && amounts.Count == 0,
            "zero delta preserves an empty record after reset");
    }

    private static void VerifyCounters(Action<bool, string> check)
    {
        StorageNetworkPerformanceCounters.ResetRuntimeState();
        StorageNetworkPerformanceCounters.RecordScannedItems(20);
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().ScannedItems == 0,
            "disabled algorithm counters perform no writes");
        StorageNetworkPerformanceCounters.SetEnabled(true);
        StorageNetworkPerformanceCounters.RecordScannedItems(20);
        StorageNetworkPerformanceCounters.RecordUpdatedTags(3);
        StorageNetworkPerformanceCounters.RecordSourceBucketSorts();
        StorageNetworkPerformanceCounters.RecordFarmingSnapshotEntries(4);
        StorageNetworkPerformanceCounters.RecordTextMeshRebuilds(2);
        var snapshot = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(snapshot.ScannedItems == 20 && snapshot.UpdatedTags == 3 && snapshot.SourceBucketSorts == 1 &&
              snapshot.FarmingSnapshotEntries == 4 && snapshot.TextMeshRebuilds == 2,
            "algorithm diagnostics publish real operation counts");
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().UpdatedTags == 0,
            "algorithm diagnostics reset between measurement windows");
        StorageNetworkPerformanceCounters.ResetRuntimeState();
    }
}
