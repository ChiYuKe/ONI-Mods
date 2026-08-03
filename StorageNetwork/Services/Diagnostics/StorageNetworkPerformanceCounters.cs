using System.Collections.Generic;
using System.Threading;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkPerformanceCounters
    {
        private static int enabled;
        private static long inventoryIndexRebuilds;
        private static long collectForWorldRebuilds;
        private static long lightweightSceneRebuilds;
        private static long storageInfoConstructions;
        private static long portRequestAttempts;
        private static long networkSourceScans;
        private static long networkSourceFallbackScans;
        private static long fetchBridgeAttempts;
        private static long portNavigationChecks;
        private static long bufferReturnAttempts;
        private static long inputReservationIndexRebuilds;
        private static readonly HashSet<int> activePortIds = new HashSet<int>(32);
        private static readonly object activePortLock = new object();

        public static void RecordInventoryIndexRebuild()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref inventoryIndexRebuilds);
        }

        public static void RecordCollectForWorldRebuild()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref collectForWorldRebuilds);
        }

        public static void RecordLightweightSceneRebuild()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref lightweightSceneRebuilds);
        }

        public static void RecordStorageInfoConstruction()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref storageInfoConstructions);
        }

        public static void RecordPortRequestAttempt(int portInstanceId = 0)
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref portRequestAttempts);
            if (portInstanceId == 0)
            {
                return;
            }

            lock (activePortLock)
            {
                activePortIds.Add(portInstanceId);
            }
        }

        public static void RecordNetworkSourceScan()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref networkSourceScans);
        }

        public static void RecordNetworkSourceFallbackScan()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref networkSourceFallbackScans);
        }

        public static void RecordFetchBridgeAttempt()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref fetchBridgeAttempts);
        }

        public static void RecordPortNavigationCheck()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref portNavigationChecks);
        }

        public static void RecordBufferReturnAttempt()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref bufferReturnAttempts);
        }

        public static void RecordInputReservationIndexRebuild()
        {
            if (Volatile.Read(ref enabled) == 0) return;
            Interlocked.Increment(ref inputReservationIndexRebuilds);
        }

        public static void SetEnabled(bool value)
        {
            Volatile.Write(ref enabled, value ? 1 : 0);
            if (!value)
            {
                ConsumeSnapshot();
            }
        }

        public static StorageNetworkPerformanceSnapshot ConsumeSnapshot()
        {
            int activePortCount;
            lock (activePortLock)
            {
                activePortCount = activePortIds.Count;
                activePortIds.Clear();
            }

            return new StorageNetworkPerformanceSnapshot(
                Interlocked.Exchange(ref inventoryIndexRebuilds, 0L),
                Interlocked.Exchange(ref collectForWorldRebuilds, 0L),
                Interlocked.Exchange(ref lightweightSceneRebuilds, 0L),
                Interlocked.Exchange(ref storageInfoConstructions, 0L),
                Interlocked.Exchange(ref portRequestAttempts, 0L),
                Interlocked.Exchange(ref networkSourceScans, 0L),
                Interlocked.Exchange(ref networkSourceFallbackScans, 0L),
                Interlocked.Exchange(ref fetchBridgeAttempts, 0L),
                Interlocked.Exchange(ref portNavigationChecks, 0L),
                Interlocked.Exchange(ref bufferReturnAttempts, 0L),
                Interlocked.Exchange(ref inputReservationIndexRebuilds, 0L),
                activePortCount);
        }

        public static void ResetRuntimeState()
        {
            Volatile.Write(ref enabled, 0);
            ConsumeSnapshot();
        }
    }

    internal readonly struct StorageNetworkPerformanceSnapshot
    {
        public StorageNetworkPerformanceSnapshot(
            long inventoryIndexRebuilds,
            long collectForWorldRebuilds,
            long lightweightSceneRebuilds,
            long storageInfoConstructions,
            long portRequestAttempts,
            long networkSourceScans,
            long networkSourceFallbackScans,
            long fetchBridgeAttempts,
            long portNavigationChecks,
            long bufferReturnAttempts,
            long inputReservationIndexRebuilds,
            int activePortCount)
        {
            InventoryIndexRebuilds = inventoryIndexRebuilds;
            CollectForWorldRebuilds = collectForWorldRebuilds;
            LightweightSceneRebuilds = lightweightSceneRebuilds;
            StorageInfoConstructions = storageInfoConstructions;
            PortRequestAttempts = portRequestAttempts;
            NetworkSourceScans = networkSourceScans;
            NetworkSourceFallbackScans = networkSourceFallbackScans;
            FetchBridgeAttempts = fetchBridgeAttempts;
            PortNavigationChecks = portNavigationChecks;
            BufferReturnAttempts = bufferReturnAttempts;
            InputReservationIndexRebuilds = inputReservationIndexRebuilds;
            ActivePortCount = activePortCount;
        }

        public long InventoryIndexRebuilds { get; }
        public long CollectForWorldRebuilds { get; }
        public long LightweightSceneRebuilds { get; }
        public long StorageInfoConstructions { get; }
        public long PortRequestAttempts { get; }
        public long NetworkSourceScans { get; }
        public long NetworkSourceFallbackScans { get; }
        public long FetchBridgeAttempts { get; }
        public long PortNavigationChecks { get; }
        public long BufferReturnAttempts { get; }
        public long InputReservationIndexRebuilds { get; }
        public int ActivePortCount { get; }
    }
}
