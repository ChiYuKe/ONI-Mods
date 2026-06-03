using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkGeyserOutput : KMonoBehaviour, ISim200ms
    {
        [MyCmpGet]
        private StorageNetworkEnrollment enrollment = null;

        [MyCmpGet]
        private ElementEmitter emitter = null;

        public void Sim200ms(float dt)
        {
            if (!CanCaptureOutput())
            {
                return;
            }

            ElementConverter.OutputElement output = emitter.outputElement;
            float mass = output.massGenerationRate * dt;
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            float temperature = output.minOutputTemperature > 0f
                ? output.minOutputTemperature
                : GetComponent<PrimaryElement>()?.Temperature ?? 293.15f;
            int diseaseCount = Mathf.RoundToInt(output.addedDiseaseCount * dt);
            Storage specificTarget = enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? enrollment.ResolveGeyserOutputStorage()
                : null;
            int sourceWorldId = GetOutputWorldId();
            StorageSceneSnapshot snapshot = specificTarget == null ? StorageSceneCollector.CollectForWorld(sourceWorldId) : null;
            List<Storage> targets = NetworkStorageTransferService.FindElementOutputTargets(output.elementHash, null, specificTarget, snapshot, sourceWorldId);
            float overflow = StoreElementInNetwork(output.elementHash, mass, temperature, output.addedDiseaseIdx, diseaseCount, targets);
            if (overflow > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                emitter.ForceEmit(overflow, output.addedDiseaseIdx, diseaseCount, temperature);
            }
        }

        private int GetOutputWorldId()
        {
            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        public bool CanCaptureOutput()
        {
            return enrollment != null &&
                   emitter != null &&
                   emitter.IsSimActive &&
                   enrollment.ShouldDirectGeyserOutputToNetwork() &&
                   emitter.outputElement.elementHash != SimHashes.Vacuum &&
                   emitter.outputElement.massGenerationRate > 0f;
        }

        private static float StoreElementInNetwork(
            SimHashes elementHash,
            float mass,
            float temperature,
            byte diseaseIdx,
            int diseaseCount,
            List<Storage> targets)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return mass;
            }

            float remaining = mass;
            if (targets == null || targets.Count == 0)
            {
                return remaining;
            }

            foreach (Storage target in targets)
            {
                if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float amount = Mathf.Min(remaining, Mathf.Max(0f, target.RemainingCapacity()));
                if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                if (element.IsGas)
                {
                    target.AddGasChunk(elementHash, amount, temperature, diseaseIdx, diseaseCount, false, true);
                }
                else if (element.IsLiquid)
                {
                    target.AddLiquid(elementHash, amount, temperature, diseaseIdx, diseaseCount, false, true);
                }
                else if (element.IsSolid)
                {
                    GameObject resource = element.substance.SpawnResource(target.transform.GetPosition(), amount, temperature, diseaseIdx, diseaseCount, true, false, true);
                    target.Store(resource, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                }
                else
                {
                    break;
                }

                remaining -= amount;
            }

            return Mathf.Max(0f, remaining);
        }
    }
}
