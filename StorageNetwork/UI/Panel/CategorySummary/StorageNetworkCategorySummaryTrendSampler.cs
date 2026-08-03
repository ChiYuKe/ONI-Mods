using System.Collections.Generic;
using System;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkCategorySummaryTrendSampler
    {
        private const float SampleIntervalSeconds = 10f;
        private const float StaleSeriesSeconds = 120f;
        private const int MaxSamplesPerSeries = 64;
        private const int ApproximateBytesPerSeries = 2048;
        private const int MaxApproximateBytes = 1024 * 1024;
        private const int MaxSeriesCount = MaxApproximateBytes / ApproximateBytesPerSeries;
        private readonly Dictionary<SampleKey, SampleSeries> samplesByKey = new Dictionary<SampleKey, SampleSeries>();
        private readonly HashSet<SampleKey> observedKeys = new HashSet<SampleKey>();
        private readonly List<SampleKey> keysToRemove = new List<SampleKey>();
        private readonly List<KeyValuePair<SampleKey, SampleSeries>> evictionWorkspace =
            new List<KeyValuePair<SampleKey, SampleSeries>>();

        public void Record(string categoryKey, IEnumerable<StorageNetworkCategorySummaryItemTotal> totals)
        {
            Record(categoryKey, totals, StorageNetworkCycleTime.GetCurrent(), Time.unscaledTime);
        }

        internal void Record(
            string categoryKey,
            IEnumerable<StorageNetworkCategorySummaryItemTotal> totals,
            float currentCycle,
            float realtimeSeconds)
        {
            string normalizedCategory = categoryKey ?? string.Empty;
            observedKeys.Clear();
            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                SampleKey key = new SampleKey(normalizedCategory, total.Key);
                observedKeys.Add(key);
                if (!samplesByKey.TryGetValue(key, out SampleSeries samples))
                {
                    samples = new SampleSeries(MaxSamplesPerSeries);
                    samplesByKey.Add(key, samples);
                }

                samples.Record(currentCycle, realtimeSeconds, total.MassKg);
            }

            keysToRemove.Clear();
            foreach (KeyValuePair<SampleKey, SampleSeries> pair in samplesByKey)
            {
                bool missingFromCurrentCategory =
                    string.Equals(pair.Key.CategoryKey, normalizedCategory, StringComparison.Ordinal) &&
                    !observedKeys.Contains(pair.Key);
                bool stale = realtimeSeconds - pair.Value.LastSeenRealtimeSeconds > StaleSeriesSeconds;
                if (missingFromCurrentCategory || stale)
                {
                    keysToRemove.Add(pair.Key);
                }
            }

            foreach (SampleKey key in keysToRemove)
            {
                samplesByKey.Remove(key);
            }

            EnforceMemoryBudget();
        }

        public float? GetTrendPerCycle(string categoryKey, string itemKey)
        {
            if (!samplesByKey.TryGetValue(new SampleKey(categoryKey, itemKey), out SampleSeries samples) ||
                !samples.TryGetTrend(out float trend))
            {
                return null;
            }

            return trend;
        }

        internal int SeriesCount => samplesByKey.Count;

        internal int ApproximateBytes => samplesByKey.Count * ApproximateBytesPerSeries;

        private void EnforceMemoryBudget()
        {
            if (samplesByKey.Count <= MaxSeriesCount)
            {
                return;
            }

            keysToRemove.Clear();
            evictionWorkspace.Clear();
            foreach (KeyValuePair<SampleKey, SampleSeries> pair in samplesByKey)
            {
                evictionWorkspace.Add(pair);
            }

            evictionWorkspace.Sort(SampleSeriesAgeComparer.Instance);
            int removeCount = samplesByKey.Count - MaxSeriesCount;
            for (int index = 0; index < removeCount; index++)
            {
                keysToRemove.Add(evictionWorkspace[index].Key);
            }

            foreach (SampleKey key in keysToRemove)
            {
                samplesByKey.Remove(key);
            }

            evictionWorkspace.Clear();
        }

        private readonly struct SampleKey : IEquatable<SampleKey>
        {
            public SampleKey(string categoryKey, string itemKey)
            {
                CategoryKey = categoryKey ?? string.Empty;
                ItemKey = itemKey ?? string.Empty;
            }

            public string CategoryKey { get; }

            public string ItemKey { get; }

            public bool Equals(SampleKey other)
            {
                return string.Equals(CategoryKey, other.CategoryKey, StringComparison.Ordinal) &&
                       string.Equals(ItemKey, other.ItemKey, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is SampleKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (StringComparer.Ordinal.GetHashCode(CategoryKey) * 397) ^
                           StringComparer.Ordinal.GetHashCode(ItemKey);
                }
            }
        }

        private sealed class SampleSeries
        {
            private readonly StorageNetworkFixedRingBuffer<MassSample> samples;
            private float lastSampleRealtimeSeconds = float.NegativeInfinity;

            public SampleSeries(int capacity)
            {
                samples = new StorageNetworkFixedRingBuffer<MassSample>(capacity);
            }

            public float LastSeenRealtimeSeconds { get; private set; }

            public void Record(float cycleTime, float realtimeSeconds, float massKg)
            {
                LastSeenRealtimeSeconds = realtimeSeconds;
                if (samples.Count > 0 && realtimeSeconds - lastSampleRealtimeSeconds < SampleIntervalSeconds)
                {
                    return;
                }

                samples.Add(new MassSample(cycleTime, massKg));
                lastSampleRealtimeSeconds = realtimeSeconds;
            }

            public bool TryGetTrend(out float trend)
            {
                trend = 0f;
                if (samples.Count < 2 ||
                    !samples.TryGetFirst(out MassSample first) ||
                    !samples.TryGetLast(out MassSample last))
                {
                    return false;
                }

                float elapsedCycles = last.CycleTime - first.CycleTime;
                if (elapsedCycles < 0.01f)
                {
                    return false;
                }

                trend = (last.MassKg - first.MassKg) / elapsedCycles;
                return true;
            }
        }

        private sealed class SampleSeriesAgeComparer :
            IComparer<KeyValuePair<SampleKey, SampleSeries>>
        {
            public static readonly SampleSeriesAgeComparer Instance =
                new SampleSeriesAgeComparer();

            public int Compare(
                KeyValuePair<SampleKey, SampleSeries> left,
                KeyValuePair<SampleKey, SampleSeries> right)
            {
                int age = left.Value.LastSeenRealtimeSeconds.CompareTo(
                    right.Value.LastSeenRealtimeSeconds);
                if (age != 0)
                {
                    return age;
                }

                int category = string.Compare(
                    left.Key.CategoryKey,
                    right.Key.CategoryKey,
                    StringComparison.Ordinal);
                return category != 0
                    ? category
                    : string.Compare(
                        left.Key.ItemKey,
                        right.Key.ItemKey,
                        StringComparison.Ordinal);
            }
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
