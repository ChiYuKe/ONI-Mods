using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace StorageNetwork.Components
{
    /// <summary>
    /// Incrementally merges legacy liquid and gas chunks that were stored as separate objects.
    /// New conduit input uses Storage.AddLiquid/AddGasChunk and therefore arrives consolidated.
    /// </summary>
    public sealed class StorageNetworkFluidStorageCompactor : KMonoBehaviour, ISim1000ms
    {
        private const int MaxItemsPerTick = 64;
        private const double MaxMillisecondsPerTick = 0.5d;
        private const float CompletedRescanSeconds = 30f;

        [MyCmpGet]
        private Storage storage = null;

        private readonly Dictionary<SimHashes, Pickupable> canonicalChunks =
            new Dictionary<SimHashes, Pickupable>();

        private int scanIndex;
        private bool mergedDuringPass;
        private bool completed;
        private float completedRescanTimer;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ResetPass();
        }

        public void Sim1000ms(float dt)
        {
            if (storage?.items == null)
            {
                return;
            }

            if (completed)
            {
                completedRescanTimer -= dt;
                if (completedRescanTimer > 0f)
                {
                    return;
                }

                ResetPass();
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            int processed = 0;
            while (scanIndex < storage.items.Count &&
                   processed < MaxItemsPerTick &&
                   stopwatch.Elapsed.TotalMilliseconds < MaxMillisecondsPerTick)
            {
                GameObject item = storage.items[scanIndex];
                processed++;
                if (!TryGetFluidChunk(item, out PrimaryElement primaryElement, out Pickupable pickupable))
                {
                    scanIndex++;
                    continue;
                }

                if (!canonicalChunks.TryGetValue(primaryElement.ElementID, out Pickupable canonical) || canonical == null)
                {
                    canonicalChunks[primaryElement.ElementID] = pickupable;
                    scanIndex++;
                    continue;
                }

                if (canonical == pickupable)
                {
                    scanIndex++;
                    continue;
                }

                int countBefore = storage.items.Count;
                if (canonical.TryAbsorb(pickupable, hide_effects: true, allow_cross_storage: false))
                {
                    mergedDuringPass = true;
                    if (storage.items.Count >= countBefore &&
                        scanIndex < storage.items.Count &&
                        storage.items[scanIndex] == item)
                    {
                        scanIndex++;
                    }

                    continue;
                }

                scanIndex++;
            }

            if (scanIndex < storage.items.Count)
            {
                return;
            }

            if (mergedDuringPass)
            {
                ResetPass();
                return;
            }

            completed = true;
            completedRescanTimer = CompletedRescanSeconds;
            canonicalChunks.Clear();
        }

        private void ResetPass()
        {
            scanIndex = 0;
            mergedDuringPass = false;
            completed = false;
            completedRescanTimer = 0f;
            canonicalChunks.Clear();
        }

        private static bool TryGetFluidChunk(
            GameObject item,
            out PrimaryElement primaryElement,
            out Pickupable pickupable)
        {
            primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            pickupable = item != null ? item.GetComponent<Pickupable>() : null;
            if (primaryElement == null || pickupable == null || pickupable.wasAbsorbed)
            {
                return false;
            }

            Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
            return element != null && (element.IsLiquid || element.IsGas);
        }
    }
}
