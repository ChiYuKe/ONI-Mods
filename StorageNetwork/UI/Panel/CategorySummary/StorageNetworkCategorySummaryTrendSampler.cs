using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkCategorySummaryTrendSampler
    {
        private readonly Dictionary<string, Queue<MassSample>> samplesByKey = new Dictionary<string, Queue<MassSample>>();

        public void Record(string categoryKey, IEnumerable<StorageNetworkCategorySummaryItemTotal> totals)
        {
            float currentCycle = StorageNetworkCycleTime.GetCurrent();
            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                string key = BuildSampleKey(categoryKey, total.Key);
                if (!samplesByKey.TryGetValue(key, out Queue<MassSample> samples))
                {
                    samples = new Queue<MassSample>();
                    samplesByKey.Add(key, samples);
                }

                samples.Enqueue(new MassSample(currentCycle, total.MassKg));
                while (samples.Count > 1 && currentCycle - samples.Peek().CycleTime > 1f)
                {
                    samples.Dequeue();
                }
            }
        }

        public float? GetTrendPerCycle(string categoryKey, string itemKey)
        {
            if (!samplesByKey.TryGetValue(BuildSampleKey(categoryKey, itemKey), out Queue<MassSample> samples) ||
                samples.Count < 2)
            {
                return null;
            }

            MassSample first = samples.Peek();
            MassSample last = samples.Last();
            float elapsedCycles = last.CycleTime - first.CycleTime;
            if (elapsedCycles < 0.01f)
            {
                return null;
            }

            return (last.MassKg - first.MassKg) / elapsedCycles;
        }

        private static string BuildSampleKey(string categoryKey, string itemKey)
        {
            return (categoryKey ?? string.Empty) + "|" + (itemKey ?? string.Empty);
        }

        private readonly struct MassSample
        {
            public MassSample(float cycleTime, float massKg)
            {
                CycleTime = cycleTime;
                MassKg = massKg;
            }

            public float CycleTime { get; }

            public float MassKg { get; }
        }
    }
}
