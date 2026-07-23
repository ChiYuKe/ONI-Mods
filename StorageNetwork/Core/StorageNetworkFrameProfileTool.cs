using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkFrameProfileTool
    {
        private const string EnableFileName = "FrameProfileTool.enabled";
        private const string LogPrefix = "[StorageNetwork][FrameProfile]";
        private static string modPath;
        private static long currentFrameWorkTicks;
        private static long currentFrameAllocatedBytes;
        private static int frameProfilerEnabled;

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
            Interlocked.Exchange(ref currentFrameWorkTicks, 0L);
            Interlocked.Exchange(ref currentFrameAllocatedBytes, 0L);
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

                StorageNetworkPerformanceSnapshot counters = StorageNetworkPerformanceCounters.ConsumeSnapshot();
                Debug.Log(string.Format(
                    "{0} counters inventoryRebuilds={1} worldSnapshotRebuilds={2} lightweightRebuilds={3} storageInfo={4} portRequests={5} activePorts={6} sourceScans={7} fetchBridges={8} navigationChecks={9} bufferReturns={10} reservationRebuilds={11}",
                    LogPrefix,
                    counters.InventoryIndexRebuilds,
                    counters.CollectForWorldRebuilds,
                    counters.LightweightSceneRebuilds,
                    counters.StorageInfoConstructions,
                    counters.PortRequestAttempts,
                    counters.ActivePortCount,
                    counters.NetworkSourceScans,
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
        }
    }
}
