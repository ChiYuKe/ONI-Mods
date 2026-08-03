using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal enum StorageNetworkPerformanceArea
    {
        General,
        Registry,
        ContentIndex,
        SourceSelection,
        TargetSelection,
        Transfer,
        Reservation,
        Power,
        ProductionMaintenance,
        ProductionPlanning,
        LogicDiy,
        MainPanel,
        CategorySummary,
        OrderEditor,
        Tracking,
        LiquidSideScreen,
        WorldPanel,
        Layout,
        GlobalHarmony,
        Count
    }

    internal static class StorageNetworkFrameProfileTool
    {
        private const string EnableFileName = "FrameProfileTool.enabled";
        private const string LogPrefix = "[StorageNetwork][FrameProfile]";
        private const int HistogramBucketCount = 15;
        private static readonly double[] HistogramUpperBoundsMs =
        {
            0.05d,
            0.1d,
            0.25d,
            0.5d,
            1d,
            2d,
            3d,
            5d,
            10d,
            20d,
            50d,
            100d,
            250d,
            500d
        };
        private static readonly long[] AreaCalls = new long[(int)StorageNetworkPerformanceArea.Count];
        private static readonly long[] AreaTicks = new long[(int)StorageNetworkPerformanceArea.Count];
        private static readonly long[] AreaMaxTicks = new long[(int)StorageNetworkPerformanceArea.Count];
        private static readonly long[] AreaHistogram =
            new long[(int)StorageNetworkPerformanceArea.Count * HistogramBucketCount];
        private static string modPath;
        private static long currentFrameWorkTicks;
        private static long currentFrameAllocatedBytes;
        private static int frameProfilerEnabled;
        private static int allocationTrackingSupported = -1;

        [ThreadStatic]
        private static int workScopeDepth;

        public static void RecordWork(long elapsedTicks, long allocatedBytes)
        {
            if (Volatile.Read(ref frameProfilerEnabled) == 0)
            {
                return;
            }

            if (elapsedTicks > 0)
            {
                Interlocked.Add(ref currentFrameWorkTicks, elapsedTicks);
            }

            if (allocatedBytes > 0)
            {
                Interlocked.Add(ref currentFrameAllocatedBytes, allocatedBytes);
            }
        }

        /// <summary>
        /// Measures a known hot path without patching every method in the assembly.
        /// Nested scopes contribute only their outermost duration so callers can
        /// safely instrument transfer, index, and scene work independently.
        /// </summary>
        public static WorkScope BeginWork()
        {
            return BeginWork(StorageNetworkPerformanceArea.General);
        }

        public static WorkScope BeginWork(StorageNetworkPerformanceArea area)
        {
            if (Volatile.Read(ref frameProfilerEnabled) == 0)
            {
                return default;
            }

            bool isRoot = workScopeDepth++ == 0;
            bool trackAllocations = Volatile.Read(ref allocationTrackingSupported) == 1;
            return new WorkScope(
                true,
                isRoot,
                Stopwatch.GetTimestamp(),
                isRoot && trackAllocations ? GetAllocatedBytesForCurrentThread() : 0L,
                area,
                trackAllocations);
        }

        public static void SetModPath(string path)
        {
            modPath = path;
        }

        public static void InstallIfEnabled(Game game)
        {
            if (game == null || game.gameObject == null || !IsEnabled())
            {
                return;
            }

            Volatile.Write(ref frameProfilerEnabled, 1);
            Volatile.Write(
                ref allocationTrackingSupported,
                DetectAllocationTrackingSupport() ? 1 : 0);
            StorageNetworkPerformanceCounters.SetEnabled(true);
            FrameProfileBehaviour profiler = game.gameObject.GetComponent<FrameProfileBehaviour>();
            if (profiler == null)
            {
                profiler = game.gameObject.AddComponent<FrameProfileBehaviour>();
            }

            profiler.ResetWindow();
            Debug.Log(LogPrefix + " enabled. Reporting every 60s.");
        }

        public static void ResetRuntimeState()
        {
            Volatile.Write(ref frameProfilerEnabled, 0);
            Volatile.Write(ref allocationTrackingSupported, -1);
            StorageNetworkPerformanceCounters.SetEnabled(false);
            Interlocked.Exchange(ref currentFrameWorkTicks, 0L);
            Interlocked.Exchange(ref currentFrameAllocatedBytes, 0L);
            ResetAreaCounters();
            workScopeDepth = 0;
            FrameProfileBehaviour profiler = Game.Instance != null
                ? Game.Instance.gameObject.GetComponent<FrameProfileBehaviour>()
                : null;
            if (profiler != null)
            {
                UnityEngine.Object.Destroy(profiler);
            }
        }

        private static bool IsEnabled()
        {
            return File.Exists(Path.Combine(GetConfigDirectory(), EnableFileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, EnableFileName)));
        }

        private static string GetConfigDirectory()
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", "StorageNetwork");
            }
            catch
            {
                return string.Empty;
            }
        }

        internal static void RecordHarmonyWork(long elapsedTicks, long allocatedBytes)
        {
            RecordWork(elapsedTicks, allocatedBytes);
            RecordArea(StorageNetworkPerformanceArea.GlobalHarmony, elapsedTicks);
        }

        private static void EndWork(
            bool isRoot,
            long startedTicks,
            long startedAllocatedBytes,
            StorageNetworkPerformanceArea area,
            bool trackAllocations)
        {
            workScopeDepth = Math.Max(0, workScopeDepth - 1);
            long elapsedTicks = Stopwatch.GetTimestamp() - startedTicks;
            RecordArea(area, elapsedTicks);
            if (!isRoot)
            {
                return;
            }

            long allocatedBytes = trackAllocations
                ? Math.Max(0L, GetAllocatedBytesForCurrentThread() - startedAllocatedBytes)
                : 0L;
            RecordWork(elapsedTicks, allocatedBytes);
        }

        private static void RecordArea(StorageNetworkPerformanceArea area, long elapsedTicks)
        {
            int areaIndex = (int)area;
            if (elapsedTicks <= 0 ||
                areaIndex < 0 ||
                areaIndex >= (int)StorageNetworkPerformanceArea.Count)
            {
                return;
            }

            Interlocked.Increment(ref AreaCalls[areaIndex]);
            Interlocked.Add(ref AreaTicks[areaIndex], elapsedTicks);
            long observedMax = Volatile.Read(ref AreaMaxTicks[areaIndex]);
            while (elapsedTicks > observedMax)
            {
                long previous = Interlocked.CompareExchange(
                    ref AreaMaxTicks[areaIndex],
                    elapsedTicks,
                    observedMax);
                if (previous == observedMax)
                {
                    break;
                }

                observedMax = previous;
            }

            double elapsedMs = elapsedTicks * 1000d / Stopwatch.Frequency;
            int bucket = HistogramUpperBoundsMs.Length;
            for (int i = 0; i < HistogramUpperBoundsMs.Length; i++)
            {
                if (elapsedMs <= HistogramUpperBoundsMs[i])
                {
                    bucket = i;
                    break;
                }
            }

            Interlocked.Increment(
                ref AreaHistogram[areaIndex * HistogramBucketCount + bucket]);
        }

        private static List<PerformanceAreaSnapshot> ConsumeAreaSnapshots()
        {
            List<PerformanceAreaSnapshot> snapshots =
                new List<PerformanceAreaSnapshot>((int)StorageNetworkPerformanceArea.Count);
            for (int areaIndex = 0;
                 areaIndex < (int)StorageNetworkPerformanceArea.Count;
                 areaIndex++)
            {
                long calls = Interlocked.Exchange(ref AreaCalls[areaIndex], 0L);
                long ticks = Interlocked.Exchange(ref AreaTicks[areaIndex], 0L);
                long maxTicks = Interlocked.Exchange(ref AreaMaxTicks[areaIndex], 0L);
                if (calls <= 0)
                {
                    ClearHistogram(areaIndex);
                    continue;
                }

                long p95Target = Math.Max(1L, (long)Math.Ceiling(calls * 0.95d));
                long p99Target = Math.Max(1L, (long)Math.Ceiling(calls * 0.99d));
                long cumulative = 0L;
                int p95Bucket = HistogramBucketCount - 1;
                int p99Bucket = HistogramBucketCount - 1;
                bool p95Found = false;
                bool p99Found = false;
                for (int bucket = 0; bucket < HistogramBucketCount; bucket++)
                {
                    cumulative += Interlocked.Exchange(
                        ref AreaHistogram[areaIndex * HistogramBucketCount + bucket],
                        0L);
                    if (!p95Found && cumulative >= p95Target)
                    {
                        p95Bucket = bucket;
                        p95Found = true;
                    }

                    if (!p99Found && cumulative >= p99Target)
                    {
                        p99Bucket = bucket;
                        p99Found = true;
                    }
                }

                snapshots.Add(new PerformanceAreaSnapshot(
                    (StorageNetworkPerformanceArea)areaIndex,
                    calls,
                    ticks,
                    maxTicks,
                    GetBucketUpperBound(p95Bucket),
                    GetBucketUpperBound(p99Bucket)));
            }

            snapshots.Sort((left, right) => right.TotalTicks.CompareTo(left.TotalTicks));
            return snapshots;
        }

        private static void ResetAreaCounters()
        {
            for (int areaIndex = 0;
                 areaIndex < (int)StorageNetworkPerformanceArea.Count;
                 areaIndex++)
            {
                Interlocked.Exchange(ref AreaCalls[areaIndex], 0L);
                Interlocked.Exchange(ref AreaTicks[areaIndex], 0L);
                Interlocked.Exchange(ref AreaMaxTicks[areaIndex], 0L);
                ClearHistogram(areaIndex);
            }
        }

        private static void ClearHistogram(int areaIndex)
        {
            for (int bucket = 0; bucket < HistogramBucketCount; bucket++)
            {
                Interlocked.Exchange(
                    ref AreaHistogram[areaIndex * HistogramBucketCount + bucket],
                    0L);
            }
        }

        private static double GetBucketUpperBound(int bucket)
        {
            return bucket >= 0 && bucket < HistogramUpperBoundsMs.Length
                ? HistogramUpperBoundsMs[bucket]
                : double.PositiveInfinity;
        }

        private static bool DetectAllocationTrackingSupport()
        {
            try
            {
                long before = GC.GetAllocatedBytesForCurrentThread();
                byte[] probe = new byte[64];
                GC.KeepAlive(probe);
                long after = GC.GetAllocatedBytesForCurrentThread();
                return after > before;
            }
            catch (MissingMethodException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }

        private static long GetAllocatedBytesForCurrentThread()
        {
            try
            {
                return GC.GetAllocatedBytesForCurrentThread();
            }
            catch (MissingMethodException)
            {
                return 0L;
            }
            catch (NotSupportedException)
            {
                return 0L;
            }
        }

        internal readonly struct WorkScope : IDisposable
        {
            private readonly bool active;
            private readonly bool isRoot;
            private readonly long startedTicks;
            private readonly long startedAllocatedBytes;
            private readonly StorageNetworkPerformanceArea area;
            private readonly bool trackAllocations;

            public WorkScope(
                bool active,
                bool isRoot,
                long startedTicks,
                long startedAllocatedBytes,
                StorageNetworkPerformanceArea area,
                bool trackAllocations)
            {
                this.active = active;
                this.isRoot = isRoot;
                this.startedTicks = startedTicks;
                this.startedAllocatedBytes = startedAllocatedBytes;
                this.area = area;
                this.trackAllocations = trackAllocations;
            }

            public void Dispose()
            {
                if (active)
                {
                    EndWork(
                        isRoot,
                        startedTicks,
                        startedAllocatedBytes,
                        area,
                        trackAllocations);
                }
            }
        }

        private sealed class FrameProfileBehaviour : MonoBehaviour
        {
            private readonly List<float> frameTimesMs = new List<float>(4096);
            private readonly List<float> storageNetworkTimesMs = new List<float>(4096);
            private readonly List<long> storageNetworkAllocatedBytes = new List<long>(4096);
            private float windowStartedAt;
            private float totalMs;
            private float maxMs;
            private int hitchOver33;
            private int hitchOver50;
            private int hitchOver100;
            private int hitchOver200;
            private int gen0CollectionsAtStart;
            private long managedBytesAtStart;

            public void ResetWindow()
            {
                frameTimesMs.Clear();
                storageNetworkTimesMs.Clear();
                storageNetworkAllocatedBytes.Clear();
                windowStartedAt = Time.unscaledTime;
                totalMs = 0f;
                maxMs = 0f;
                hitchOver33 = 0;
                hitchOver50 = 0;
                hitchOver100 = 0;
                hitchOver200 = 0;
                gen0CollectionsAtStart = GC.CollectionCount(0);
                managedBytesAtStart = GC.GetTotalMemory(false);
            }

            private void Update()
            {
                StorageNetworkHarmonyProfileTool.ResetCurrentThreadDepth();
                float frameMs = Time.unscaledDeltaTime * 1000f;
                if (frameMs <= 0f || float.IsNaN(frameMs) || float.IsInfinity(frameMs))
                {
                    return;
                }

                long workTicks = Interlocked.Exchange(ref currentFrameWorkTicks, 0L);
                long allocatedBytes = Interlocked.Exchange(ref currentFrameAllocatedBytes, 0L);
                float storageNetworkMs = (float)(workTicks * 1000d / Stopwatch.Frequency);
                RecordFrame(frameMs, storageNetworkMs, allocatedBytes);
            }

            private void RecordFrame(float frameMs, float storageNetworkMs, long allocatedBytes)
            {
                frameTimesMs.Add(frameMs);
                storageNetworkTimesMs.Add(storageNetworkMs);
                storageNetworkAllocatedBytes.Add(Math.Max(0L, allocatedBytes));
                totalMs += frameMs;
                if (frameMs > maxMs)
                {
                    maxMs = frameMs;
                }

                if (frameMs > 33.333f)
                {
                    hitchOver33++;
                }

                if (frameMs > 50f)
                {
                    hitchOver50++;
                }

                if (frameMs > 100f)
                {
                    hitchOver100++;
                }

                if (frameMs > 200f)
                {
                    hitchOver200++;
                }

                if (Time.unscaledTime - windowStartedAt >= 60f)
                {
                    LogWindow();
                    ResetWindow();
                }
            }

            private void LogWindow()
            {
                int frames = frameTimesMs.Count;
                if (frames <= 0)
                {
                    return;
                }

                frameTimesMs.Sort();
                float elapsedSeconds = Mathf.Max(0.001f, Time.unscaledTime - windowStartedAt);
                float avgMs = totalMs / frames;
                float p95 = GetPercentile(0.95f);
                float p99 = GetPercentile(0.99f);
                storageNetworkTimesMs.Sort();
                float storageNetworkP95 = GetPercentile(storageNetworkTimesMs, 0.95f);
                float storageNetworkP99 = GetPercentile(storageNetworkTimesMs, 0.99f);
                float storageNetworkMax = storageNetworkTimesMs.Count > 0
                    ? storageNetworkTimesMs[storageNetworkTimesMs.Count - 1]
                    : 0f;
                storageNetworkAllocatedBytes.Sort();
                long storageNetworkAllocatedTotal = 0L;
                foreach (long allocatedBytes in storageNetworkAllocatedBytes)
                {
                    storageNetworkAllocatedTotal += allocatedBytes;
                }
                long storageNetworkAllocatedP95 = GetPercentile(storageNetworkAllocatedBytes, 0.95f);
                long storageNetworkAllocatedP99 = GetPercentile(storageNetworkAllocatedBytes, 0.99f);
                long storageNetworkAllocatedMax = storageNetworkAllocatedBytes.Count > 0
                    ? storageNetworkAllocatedBytes[storageNetworkAllocatedBytes.Count - 1]
                    : 0L;
                float fps = frames / elapsedSeconds;
                int gen0Collections = GC.CollectionCount(0) - gen0CollectionsAtStart;
                long managedBytesDelta = GC.GetTotalMemory(false) - managedBytesAtStart;

                Debug.Log(string.Format(
                    "{0} {1:F1}s frames={2} fps={3:F1} avg={4:F2}ms p95={5:F2}ms p99={6:F2}ms max={7:F2}ms >33ms={8} >50ms={9} >100ms={10} >200ms={11}",
                    LogPrefix,
                    elapsedSeconds,
                    frames,
                    fps,
                    avgMs,
                    p95,
                    p99,
                    maxMs,
                    hitchOver33,
                    hitchOver50,
                    hitchOver100,
                    hitchOver200));

                if (Volatile.Read(ref allocationTrackingSupported) == 1)
                {
                    Debug.Log(string.Format(
                        "{0} modCpu p95={1:F3}ms p99={2:F3}ms max={3:F3}ms modAlloc total={4}B p95={5}B p99={6}B max={7}B gen0={8} managedDelta={9}B targetP95<2ms,p99<3ms",
                        LogPrefix,
                        storageNetworkP95,
                        storageNetworkP99,
                        storageNetworkMax,
                        storageNetworkAllocatedTotal,
                        storageNetworkAllocatedP95,
                        storageNetworkAllocatedP99,
                        storageNetworkAllocatedMax,
                        gen0Collections,
                        managedBytesDelta));
                }
                else
                {
                    Debug.Log(string.Format(
                        "{0} modCpu p95={1:F3}ms p99={2:F3}ms max={3:F3}ms modAlloc=unsupported gen0={4} managedDelta={5}B targetP95<2ms,p99<3ms",
                        LogPrefix,
                        storageNetworkP95,
                        storageNetworkP99,
                        storageNetworkMax,
                        gen0Collections,
                        managedBytesDelta));
                }

                List<PerformanceAreaSnapshot> areaSnapshots = ConsumeAreaSnapshots();
                int areaCount = Mathf.Min(8, areaSnapshots.Count);
                for (int i = 0; i < areaCount; i++)
                {
                    PerformanceAreaSnapshot area = areaSnapshots[i];
                    Debug.Log(string.Format(
                        "{0} area={1} calls={2} total={3:F3}ms avg={4:F4}ms p95<={5}ms p99<={6}ms max={7:F3}ms",
                        LogPrefix,
                        area.Area,
                        area.Calls,
                        area.TotalMilliseconds,
                        area.AverageMilliseconds,
                        FormatBucket(area.P95UpperMilliseconds),
                        FormatBucket(area.P99UpperMilliseconds),
                        area.MaxMilliseconds));
                }

                StorageNetworkPerformanceSnapshot counters = StorageNetworkPerformanceCounters.ConsumeSnapshot();
                Debug.Log(string.Format(
                    "{0} counters inventoryRebuilds={1} worldSnapshotRebuilds={2} lightweightRebuilds={3} storageInfo={4} portRequests={5} activePorts={6} sourceScans={7} sourceFallbackScans={8} fetchBridges={9} navigationChecks={10} bufferReturns={11} reservationRebuilds={12}",
                    LogPrefix,
                    counters.InventoryIndexRebuilds,
                    counters.CollectForWorldRebuilds,
                    counters.LightweightSceneRebuilds,
                    counters.StorageInfoConstructions,
                    counters.PortRequestAttempts,
                    counters.ActivePortCount,
                    counters.NetworkSourceScans,
                    counters.NetworkSourceFallbackScans,
                    counters.FetchBridgeAttempts,
                    counters.PortNavigationChecks,
                    counters.BufferReturnAttempts,
                    counters.InputReservationIndexRebuilds));
            }

            private float GetPercentile(float percentile)
            {
                return GetPercentile(frameTimesMs, percentile);
            }

            private static float GetPercentile(List<float> values, float percentile)
            {
                if (values == null || values.Count == 0)
                {
                    return 0f;
                }

                int index = Mathf.Clamp(
                    Mathf.CeilToInt(values.Count * percentile) - 1,
                    0,
                    values.Count - 1);
                return values[index];
            }

            private static long GetPercentile(List<long> values, float percentile)
            {
                if (values == null || values.Count == 0)
                {
                    return 0L;
                }

                int index = Mathf.Clamp(
                    Mathf.CeilToInt(values.Count * percentile) - 1,
                    0,
                    values.Count - 1);
                return values[index];
            }

            private static string FormatBucket(double milliseconds)
            {
                return double.IsPositiveInfinity(milliseconds)
                    ? "500+"
                    : milliseconds.ToString("0.###");
            }
        }

        private readonly struct PerformanceAreaSnapshot
        {
            public PerformanceAreaSnapshot(
                StorageNetworkPerformanceArea area,
                long calls,
                long totalTicks,
                long maxTicks,
                double p95UpperMilliseconds,
                double p99UpperMilliseconds)
            {
                Area = area;
                Calls = calls;
                TotalTicks = totalTicks;
                MaxTicks = maxTicks;
                P95UpperMilliseconds = p95UpperMilliseconds;
                P99UpperMilliseconds = p99UpperMilliseconds;
            }

            public StorageNetworkPerformanceArea Area { get; }
            public long Calls { get; }
            public long TotalTicks { get; }
            public long MaxTicks { get; }
            public double P95UpperMilliseconds { get; }
            public double P99UpperMilliseconds { get; }
            public double TotalMilliseconds => TotalTicks * 1000d / Stopwatch.Frequency;
            public double AverageMilliseconds => Calls > 0 ? TotalMilliseconds / Calls : 0d;
            public double MaxMilliseconds => MaxTicks * 1000d / Stopwatch.Frequency;
        }
    }
}
