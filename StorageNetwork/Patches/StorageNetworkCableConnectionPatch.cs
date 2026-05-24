using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using System.Collections.Generic;

namespace StorageNetwork.Patches
{
    [HarmonyPatch(typeof(Deconstructable), "OnCompleteWork")]
    public static class StorageNetworkCableDeconstructRefreshScopePatch
    {
        public static void Prefix(Deconstructable __instance, ref bool __state)
        {
            StorageNetworkCable cable = __instance != null ? __instance.GetComponent<StorageNetworkCable>() : null;
            __state = cable != null;
            if (__state)
            {
                StorageNetworkCableTileRefreshQueue.Begin();
            }
        }

        public static void Postfix(bool __state)
        {
            if (__state)
            {
                StorageNetworkCableTileRefreshQueue.End();
            }
        }
    }

    [HarmonyPatch(typeof(TileVisualizer), nameof(TileVisualizer.RefreshCell), typeof(int), typeof(ObjectLayer), typeof(ObjectLayer))]
    public static class StorageNetworkCableTileRefreshPatch
    {
        public static bool Prefix(int cell, ObjectLayer tile_layer, ObjectLayer replacement_layer)
        {
            if (!StorageNetworkCableTileRefreshQueue.ShouldDefer(tile_layer, replacement_layer))
            {
                return true;
            }

            StorageNetworkCableTileRefreshQueue.Queue(cell, tile_layer, replacement_layer);
            return false;
        }
    }

    public static class StorageNetworkCableTileRefreshQueue
    {
        private const int RefreshesPerFrame = 32;
        private static readonly Queue<RefreshKey> PendingRefreshes = new Queue<RefreshKey>();
        private static readonly HashSet<RefreshKey> PendingRefreshKeys = new HashSet<RefreshKey>();
        private static int suppressDepth;
        private static bool flushQueued;
        private static bool flushing;

        public static void Begin()
        {
            suppressDepth++;
        }

        public static void End()
        {
            if (suppressDepth > 0)
            {
                suppressDepth--;
            }
        }

        public static bool ShouldDefer(ObjectLayer tileLayer, ObjectLayer replacementLayer)
        {
            return !flushing &&
                suppressDepth > 0 &&
                tileLayer == ObjectLayer.LogicWireTile &&
                replacementLayer == ObjectLayer.ReplacementLogicWire;
        }

        public static void Queue(int cell, ObjectLayer tileLayer, ObjectLayer replacementLayer)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            RefreshKey key = new RefreshKey(cell, tileLayer, replacementLayer);
            if (!PendingRefreshKeys.Add(key))
            {
                return;
            }

            PendingRefreshes.Enqueue(key);
            QueueFlush();
        }

        private static void QueueFlush()
        {
            if (flushQueued)
            {
                return;
            }

            flushQueued = true;
            if (GameScheduler.Instance != null)
            {
                GameScheduler.Instance.ScheduleNextFrame(
                    "StorageNetworkCableTileRefresh",
                    _ => Flush());
                return;
            }

            Flush();
        }

        private static void Flush()
        {
            flushQueued = false;
            flushing = true;
            int processed = 0;
            while (PendingRefreshes.Count > 0 && processed < RefreshesPerFrame)
            {
                RefreshKey key = PendingRefreshes.Dequeue();
                PendingRefreshKeys.Remove(key);
                TileVisualizer.RefreshCell(key.Cell, key.TileLayer, key.ReplacementLayer);
                processed++;
            }

            flushing = false;
            if (PendingRefreshes.Count > 0)
            {
                QueueFlush();
            }
        }

        private struct RefreshKey
        {
            public readonly int Cell;
            public readonly ObjectLayer TileLayer;
            public readonly ObjectLayer ReplacementLayer;

            public RefreshKey(int cell, ObjectLayer tileLayer, ObjectLayer replacementLayer)
            {
                Cell = cell;
                TileLayer = tileLayer;
                ReplacementLayer = replacementLayer;
            }
        }
    }

    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), "OnSpawn")]
    public static class StorageNetworkCableVisualizerOnSpawnPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance)
        {
            StorageNetworkCable cable = __instance.GetComponent<StorageNetworkCable>();
            if (cable == null)
            {
                return true;
            }

            __instance.connectionManager = null;
            cable.RefreshSelfAndNeighbours();
            return false;
        }
    }

    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), "OnCleanUp")]
    public static class StorageNetworkCableVisualizerOnCleanUpPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance)
        {
            return __instance.GetComponent<StorageNetworkCable>() == null;
        }
    }

    [HarmonyPatch(typeof(KAnimGraphTileVisualizer), nameof(KAnimGraphTileVisualizer.UpdateConnections))]
    public static class StorageNetworkCableConnectionPatch
    {
        public static bool Prefix(KAnimGraphTileVisualizer __instance, UtilityConnections new_connections)
        {
            StorageNetworkCable cable = __instance.GetComponent<StorageNetworkCable>();
            if (cable == null)
            {
                return true;
            }

            __instance.Connections = new_connections;
            StorageNetworkRegistry.MarkDirty();
            cable.RefreshSelfAndNeighbours();
            return false;
        }
    }
}
