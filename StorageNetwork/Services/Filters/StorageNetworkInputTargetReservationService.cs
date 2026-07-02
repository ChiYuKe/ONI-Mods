using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkInputTargetReservationService
    {
        public static bool IsReservedForAutoInput(Storage target, Storage currentInputStorage)
        {
            if (!IsReservableTarget(target) || !IsInputReservationSource(currentInputStorage))
            {
                return false;
            }

            foreach (InputTargetReservation reservation in GetReservationsForTarget(target))
            {
                if (reservation.InputStorage != null && reservation.InputStorage != currentInputStorage)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasReservedAutoOutputCandidate(GameObject item, Storage sourceStorage, int sourceWorldId)
        {
            if (item == null || sourceStorage == null)
            {
                return false;
            }

            StorageItemUtility.StorageMatchTags matchTags = StorageItemUtility.GetStorageMatchTagsNonAlloc(item);
            HashSet<Storage> excluded = StorageTargetSelector.BuildExclusionSet(new[] { sourceStorage });
            foreach (Storage target in StorageSceneCollector.CollectLightweightForWorld(sourceWorldId).Storages)
            {
                if (StorageTargetSelector.IsAutoOutputCandidateIgnoringReservation(target, item, matchTags, excluded, sourceWorldId) &&
                    IsReservedForAutoInput(target, sourceStorage))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<InputTargetReservation> GetReservationsForTarget(Storage target)
        {
            List<InputTargetReservation> reservations = new List<InputTargetReservation>();
            if (!IsReservableTarget(target))
            {
                return reservations;
            }

            int targetInstanceId = GetStorageInstanceId(target);
            if (targetInstanceId == KPrefabID.InvalidInstanceID)
            {
                return reservations;
            }

            foreach (Storage inputStorage in StorageSceneRegistry.GetStorages())
            {
                if (!StorageSceneRegistry.IsLive(inputStorage) || inputStorage == target)
                {
                    continue;
                }

                AddSolidInputReservation(inputStorage, target, targetInstanceId, reservations);
                AddLiquidInputReservation(inputStorage, target, targetInstanceId, reservations);
                AddGasInputReservation(inputStorage, target, targetInstanceId, reservations);
            }

            return reservations;
        }

        public static bool ClearReservation(InputTargetReservation reservation)
        {
            return reservation != null && reservation.Clear();
        }

        public static int ClearReservationsForTarget(Storage target)
        {
            int cleared = 0;
            foreach (InputTargetReservation reservation in GetReservationsForTarget(target))
            {
                if (ClearReservation(reservation))
                {
                    cleared++;
                }
            }

            return cleared;
        }

        public static List<InputTargetReservation> GetOutputSourceReservationsForTarget(Storage target)
        {
            List<InputTargetReservation> reservations = new List<InputTargetReservation>();
            if (!IsReservableTarget(target))
            {
                return reservations;
            }

            int targetInstanceId = GetStorageInstanceId(target);
            if (targetInstanceId == KPrefabID.InvalidInstanceID)
            {
                return reservations;
            }

            foreach (Storage outputStorage in StorageSceneRegistry.GetStorages())
            {
                if (!StorageSceneRegistry.IsLive(outputStorage) || outputStorage == target)
                {
                    continue;
                }

                AddSolidOutputReservation(outputStorage, target, targetInstanceId, reservations);
                AddLiquidOutputReservation(outputStorage, target, targetInstanceId, reservations);
                AddGasOutputReservation(outputStorage, target, targetInstanceId, reservations);
            }

            return reservations;
        }

        public static int ClearOutputSourceReservationsForTarget(Storage target)
        {
            int cleared = 0;
            foreach (InputTargetReservation reservation in GetOutputSourceReservationsForTarget(target))
            {
                if (ClearReservation(reservation))
                {
                    cleared++;
                }
            }

            return cleared;
        }

        private static void AddSolidInputReservation(Storage inputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkSolidInputPortIngress ingress = inputStorage.GetComponent<StorageNetworkSolidInputPortIngress>();
            if (ingress == null ||
                ingress.CurrentInputStoreMode != StorageNetwork.Components.StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage ||
                ingress.InputStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                inputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_PORT_INPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(ingress))
                    {
                        return false;
                    }

                    ingress.UseAutomaticInputStorage();
                    return true;
                }));
        }

        private static void AddLiquidInputReservation(Storage inputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkLiquidInputPortIngress ingress = inputStorage.GetComponent<StorageNetworkLiquidInputPortIngress>();
            if (ingress == null ||
                ingress.CurrentInputStoreMode != StorageNetwork.Components.StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage ||
                ingress.InputStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                inputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_PORT_INPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(ingress))
                    {
                        return false;
                    }

                    ingress.UseAutomaticInputStorage();
                    return true;
                }));
        }

        private static void AddGasInputReservation(Storage inputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkGasInputPortIngress ingress = inputStorage.GetComponent<StorageNetworkGasInputPortIngress>();
            if (ingress == null ||
                ingress.CurrentInputStoreMode != StorageNetwork.Components.StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage ||
                ingress.InputStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                inputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_PORT_INPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(ingress))
                    {
                        return false;
                    }

                    ingress.UseAutomaticInputStorage();
                    return true;
                }));
        }

        private static void AddSolidOutputReservation(Storage outputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkSolidOutputPortEgress egress = outputStorage.GetComponent<StorageNetworkSolidOutputPortEgress>();
            if (egress == null ||
                egress.CurrentSourceMode != StorageNetwork.Components.StorageNetworkMaterialRequester.RequestMode.SpecificStorage ||
                egress.SourceStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                outputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_PORT_OUTPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(egress))
                    {
                        return false;
                    }

                    egress.UseAutomaticSourceStorage();
                    return true;
                }));
        }

        private static void AddLiquidOutputReservation(Storage outputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkLiquidOutputPortEgress egress = outputStorage.GetComponent<StorageNetworkLiquidOutputPortEgress>();
            if (egress == null ||
                egress.CurrentSourceMode != StorageNetwork.Components.StorageNetworkMaterialRequester.RequestMode.SpecificStorage ||
                egress.SourceStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                outputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LIQUID_PORT_OUTPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(egress))
                    {
                        return false;
                    }

                    egress.UseAutomaticSourceStorage();
                    return true;
                }));
        }

        private static void AddGasOutputReservation(Storage outputStorage, Storage target, int targetInstanceId, List<InputTargetReservation> reservations)
        {
            StorageNetworkGasOutputPortEgress egress = outputStorage.GetComponent<StorageNetworkGasOutputPortEgress>();
            if (egress == null ||
                egress.CurrentSourceMode != StorageNetwork.Components.StorageNetworkMaterialRequester.RequestMode.SpecificStorage ||
                egress.SourceStorageInstanceId != targetInstanceId)
            {
                return;
            }

            reservations.Add(CreateReservation(
                outputStorage,
                target,
                Loc.Get(Loc.UI.STORAGE_NETWORK.GAS_PORT_OUTPUT_STATUS),
                () =>
                {
                    if (!StorageSceneRegistry.IsLive(egress))
                    {
                        return false;
                    }

                    egress.UseAutomaticSourceStorage();
                    return true;
                }));
        }

        private static InputTargetReservation CreateReservation(Storage inputStorage, Storage target, string inputTypeName, System.Func<bool> clear)
        {
            string properName = inputStorage != null ? inputStorage.GetProperName() : inputTypeName;
            string displayName = string.IsNullOrEmpty(properName)
                ? inputTypeName
                : string.Format("{0} - {1}", inputTypeName, properName);

            return new InputTargetReservation(
                inputStorage,
                inputStorage != null ? inputStorage.gameObject : null,
                target,
                inputTypeName,
                displayName,
                clear);
        }

        private static bool IsReservableTarget(Storage target)
        {
            return StorageSceneRegistry.IsLive(target) &&
                   StorageNetworkStorageRules.IsServerStorage(target) &&
                   StorageNetworkStorageRules.IsConnectedNetworkStorage(target) &&
                   !StorageNetworkStorageRules.IsNetworkPortStorage(target) &&
                   !StorageNetworkStorageRules.IsPowerStorageServer(target) &&
                   !StorageNetworkStorageRules.IsParticleStorageServer(target);
        }

        private static bool IsInputReservationSource(Storage storage)
        {
            return StorageSceneRegistry.IsLive(storage) &&
                   (StorageNetworkStorageRules.IsSolidInputPort(storage) ||
                    StorageNetworkStorageRules.IsLiquidInputPort(storage) ||
                    StorageNetworkStorageRules.IsGasInputPort(storage));
        }

        private static int GetStorageInstanceId(Storage target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
