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
                if (MergeFluidChunk(canonical, pickupable))
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

                if (!HasMergeRoom(canonical))
                {
                    canonicalChunks[primaryElement.ElementID] = pickupable;
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

        private bool MergeFluidChunk(Pickupable canonical, Pickupable other)
        {
            if (canonical == null ||
                other == null ||
                canonical == other ||
                canonical.wasAbsorbed ||
                other.wasAbsorbed)
            {
                return false;
            }

            PrimaryElement canonicalElement = canonical.GetComponent<PrimaryElement>();
            PrimaryElement otherElement = other.GetComponent<PrimaryElement>();
            if (canonicalElement == null ||
                otherElement == null ||
                canonicalElement.ElementID != otherElement.ElementID)
            {
                return false;
            }

            float canonicalMass = Mathf.Max(0f, canonicalElement.Mass);
            float otherMass = Mathf.Max(0f, otherElement.Mass);
            if (otherMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                RemoveMergedChunk(other.gameObject);
                return true;
            }

            float mergeRoom = Mathf.Max(0f, PrimaryElement.MAX_MASS - canonicalMass);
            if (mergeRoom <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            float transferMass = Mathf.Min(otherMass, mergeRoom);
            float finalTemperature = canonicalMass > 0f
                ? GameUtil.GetFinalTemperature(canonicalElement.Temperature, canonicalMass, otherElement.Temperature, transferMass)
                : otherElement.Temperature;

            canonicalElement.KeepZeroMassObject = canonicalElement.KeepZeroMassObject || otherElement.KeepZeroMassObject;
            canonicalElement.SetMassTemperature(canonicalMass + transferMass, finalTemperature);
            if (otherElement.DiseaseIdx != byte.MaxValue && otherElement.DiseaseCount > 0)
            {
                int transferredDisease = transferMass + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= otherMass
                    ? otherElement.DiseaseCount
                    : Mathf.RoundToInt(otherElement.DiseaseCount * Mathf.Clamp01(transferMass / otherMass));
                if (transferredDisease > 0)
                {
                    canonicalElement.AddDisease(otherElement.DiseaseIdx, transferredDisease, "StorageNetworkFluidStorageCompactor.MergeFluidChunk");
                    otherElement.ModifyDiseaseCount(-transferredDisease, "StorageNetworkFluidStorageCompactor.MergeFluidChunk");
                }
            }

            GameObject canonicalObject = canonical.gameObject;
            if (transferMass + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= otherMass)
            {
                RemoveMergedChunk(other.gameObject);
            }
            else
            {
                otherElement.SetMassTemperature(otherMass - transferMass, otherElement.Temperature);
            }

            storage.Trigger(-1697596308, canonicalObject);
            storage.OnStorageChange?.Invoke(canonicalObject);
            storage.Trigger(-778359855, storage);
            return true;
        }

        private static bool HasMergeRoom(Pickupable pickupable)
        {
            PrimaryElement primaryElement = pickupable != null ? pickupable.GetComponent<PrimaryElement>() : null;
            return primaryElement != null &&
                   PrimaryElement.MAX_MASS - Mathf.Max(0f, primaryElement.Mass) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private void RemoveMergedChunk(GameObject item)
        {
            if (item == null)
            {
                return;
            }

            if (storage != null && storage.items != null)
            {
                storage.items.Remove(item);
            }

            Pickupable pickupable = item.GetComponent<Pickupable>();
            if (pickupable != null)
            {
                pickupable.storage = null;
            }

            Util.KDestroyGameObject(item);
        }
    }
}
