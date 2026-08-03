using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Services
{
    internal static class StorageNetworkInputTargetReservationService
    {
        private static readonly Dictionary<int, List<InputTargetReservation>> inputReservationsByTarget = new Dictionary<int, List<InputTargetReservation>>();
        private static readonly Dictionary<int, List<InputTargetReservation>> outputReservationsByTarget = new Dictionary<int, List<InputTargetReservation>>();
        private static readonly List<InputTargetReservation> emptyReservations = new List<InputTargetReservation>(0);
        private static readonly List<Storage> storageWorkspace = new List<Storage>();
        private static readonly Dictionary<int, Storage> targetWorkspace = new Dictionary<int, Storage>();
        private static readonly List<InputTargetReservation> shadowReservations =
            new List<InputTargetReservation>();
        private static int reservationVersion;
        private static int builtReservationVersion = -1;
        private static int builtRegistryVersion = -1;
        private static int builtCapabilityVersion = -1;

        internal static int Version => reservationVersion;

        internal static bool HasInputReservations
        {
            get
            {
                EnsureInputReservationIndex();
                return inputReservationsByTarget.Count > 0;
            }
        }

        public static void Invalidate()
        {
            unchecked
            {
                reservationVersion++;
            }
        }

        public static void ResetRuntimeState()
        {
            inputReservationsByTarget.Clear();
            outputReservationsByTarget.Clear();
            storageWorkspace.Clear();
            targetWorkspace.Clear();
            shadowReservations.Clear();
            reservationVersion = 0;
            builtReservationVersion = -1;
            builtRegistryVersion = -1;
            builtCapabilityVersion = -1;
        }

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
            if (!IsReservableTarget(target))
            {
                return emptyReservations;
            }

            int targetInstanceId = GetStorageInstanceId(target);
            if (targetInstanceId == KPrefabID.InvalidInstanceID)
            {
                return emptyReservations;
            }

            EnsureInputReservationIndex();
            List<InputTargetReservation> indexed = inputReservationsByTarget.TryGetValue(
                    targetInstanceId,
                    out List<InputTargetReservation> reservations)
                ? reservations
                : emptyReservations;
            return ValidateReservations(
                target,
                targetInstanceId,
                indexed,
                outputReservations: false);
        }

        private static void EnsureInputReservationIndex()
        {
            int registryVersion = StorageSceneRegistry.MembershipVersion;
            int capabilityVersion = StorageSceneRegistry.CapabilityVersion;
            if (builtReservationVersion == reservationVersion &&
                builtRegistryVersion == registryVersion &&
                builtCapabilityVersion == capabilityVersion)
            {
                return;
            }

            using StorageNetworkFrameProfileTool.WorkScope reservationScope =
                StorageNetworkFrameProfileTool.BeginWork(
                    StorageNetworkPerformanceArea.Reservation);
            builtReservationVersion = reservationVersion;
            builtRegistryVersion = registryVersion;
            builtCapabilityVersion = capabilityVersion;
            StorageNetworkPerformanceCounters.RecordInputReservationIndexRebuild();
            inputReservationsByTarget.Clear();
            outputReservationsByTarget.Clear();
            storageWorkspace.Clear();
            targetWorkspace.Clear();
            foreach (Storage storage in StorageSceneRegistry.GetStorages())
            {
                storageWorkspace.Add(storage);
            }

            foreach (Storage storage in storageWorkspace)
            {
                if (IsReservableTarget(storage))
                {
                    targetWorkspace[GetStorageInstanceId(storage)] = storage;
                }
            }

            foreach (Storage inputStorage in storageWorkspace)
            {
                if (!StorageSceneRegistry.IsLive(inputStorage)) continue;
                int targetId = KPrefabID.InvalidInstanceID;
                StorageNetworkSolidInputPortIngress solid = inputStorage.GetComponent<StorageNetworkSolidInputPortIngress>();
                StorageNetworkLiquidInputPortIngress liquid = inputStorage.GetComponent<StorageNetworkLiquidInputPortIngress>();
                StorageNetworkGasInputPortIngress gas = inputStorage.GetComponent<StorageNetworkGasInputPortIngress>();
                if (solid != null && solid.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage) targetId = solid.InputStorageInstanceId;
                else if (liquid != null && liquid.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage) targetId = liquid.InputStorageInstanceId;
                else if (gas != null && gas.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage) targetId = gas.InputStorageInstanceId;
                if (!targetWorkspace.TryGetValue(targetId, out Storage target)) continue;
                if (!inputReservationsByTarget.TryGetValue(targetId, out List<InputTargetReservation> reservations))
                {
                    reservations = new List<InputTargetReservation>();
                    inputReservationsByTarget[targetId] = reservations;
                }
                AddSolidInputReservation(inputStorage, target, targetId, reservations);
                AddLiquidInputReservation(inputStorage, target, targetId, reservations);
                AddGasInputReservation(inputStorage, target, targetId, reservations);
            }

            foreach (Storage outputStorage in storageWorkspace)
            {
                if (!StorageSceneRegistry.IsLive(outputStorage))
                {
                    continue;
                }

                int targetId = GetSpecificOutputSourceId(outputStorage);
                if (!targetWorkspace.TryGetValue(targetId, out Storage target))
                {
                    continue;
                }

                if (!outputReservationsByTarget.TryGetValue(targetId, out List<InputTargetReservation> reservations))
                {
                    reservations = new List<InputTargetReservation>();
                    outputReservationsByTarget[targetId] = reservations;
                }

                AddSolidOutputReservation(outputStorage, target, targetId, reservations);
                AddLiquidOutputReservation(outputStorage, target, targetId, reservations);
                AddGasOutputReservation(outputStorage, target, targetId, reservations);
            }

            storageWorkspace.Clear();
            targetWorkspace.Clear();
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
            if (!IsReservableTarget(target))
            {
                return emptyReservations;
            }

            int targetInstanceId = GetStorageInstanceId(target);
            if (targetInstanceId == KPrefabID.InvalidInstanceID)
            {
                return emptyReservations;
            }

            EnsureInputReservationIndex();
            List<InputTargetReservation> indexed = outputReservationsByTarget.TryGetValue(
                targetInstanceId,
                out List<InputTargetReservation> reservations)
                ? reservations
                : emptyReservations;
            return ValidateReservations(
                target,
                targetInstanceId,
                indexed,
                outputReservations: true);
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
                StorageNetworkReservationKind.SolidInput));
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
                StorageNetworkReservationKind.LiquidInput));
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
                StorageNetworkReservationKind.GasInput));
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
                StorageNetworkReservationKind.SolidOutput));
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
                StorageNetworkReservationKind.LiquidOutput));
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
                StorageNetworkReservationKind.GasOutput));
        }

        private static InputTargetReservation CreateReservation(
            Storage inputStorage,
            Storage target,
            string inputTypeName,
            StorageNetworkReservationKind kind)
        {
            return new InputTargetReservation(
                inputStorage,
                target,
                inputTypeName,
                kind);
        }

        private static List<InputTargetReservation> ValidateReservations(
            Storage target,
            int targetInstanceId,
            List<InputTargetReservation> indexed,
            bool outputReservations)
        {
            int version = unchecked(
                (reservationVersion * 397) ^
                StorageSceneRegistry.MembershipVersion ^
                StorageSceneRegistry.CapabilityVersion);
            int worldId = StorageTargetSelector.GetObjectWorldId(target.gameObject);
            if (!StorageNetworkShadowValidationService.ShouldValidate(
                    StorageNetworkShadowArea.Reservation,
                    worldId,
                    version))
            {
                return indexed;
            }

            shadowReservations.Clear();
            foreach (Storage portStorage in StorageSceneRegistry.GetStorages())
            {
                if (!StorageSceneRegistry.IsLive(portStorage))
                {
                    continue;
                }

                if (outputReservations)
                {
                    AddSolidOutputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                    AddLiquidOutputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                    AddGasOutputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                }
                else
                {
                    AddSolidInputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                    AddLiquidInputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                    AddGasInputReservation(
                        portStorage,
                        target,
                        targetInstanceId,
                        shadowReservations);
                }
            }

            if (ReservationsEquivalent(indexed, shadowReservations))
            {
                shadowReservations.Clear();
                StorageNetworkShadowValidationService.ReportMatch(
                    StorageNetworkShadowArea.Reservation,
                    worldId,
                    version);
                return indexed;
            }

            List<InputTargetReservation> native =
                new List<InputTargetReservation>(shadowReservations);
            shadowReservations.Clear();
            if (outputReservations)
            {
                outputReservationsByTarget[targetInstanceId] = native;
            }
            else
            {
                inputReservationsByTarget[targetInstanceId] = native;
            }

            StorageNetworkShadowValidationService.ReportMismatch(
                StorageNetworkShadowArea.Reservation,
                worldId,
                version,
                unchecked((targetInstanceId * 397) ^ (outputReservations ? 1 : 0)),
                $"target={targetInstanceId}, indexedCount={indexed.Count}, " +
                $"nativeCount={native.Count}");
            Invalidate();
            return native;
        }

        private static bool ReservationsEquivalent(
            List<InputTargetReservation> left,
            List<InputTargetReservation> right)
        {
            if (left == null || right == null || left.Count != right.Count)
            {
                return left == right;
            }

            foreach (InputTargetReservation expected in left)
            {
                bool found = false;
                foreach (InputTargetReservation actual in right)
                {
                    if (expected == null
                        ? actual == null
                        : actual != null &&
                          expected.InputStorage == actual.InputStorage &&
                          expected.Kind == actual.Kind)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
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

        private static int GetSpecificOutputSourceId(Storage outputStorage)
        {
            StorageNetworkSolidOutputPortEgress solid =
                outputStorage.GetComponent<StorageNetworkSolidOutputPortEgress>();
            if (solid != null &&
                solid.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                return solid.SourceStorageInstanceId;
            }

            StorageNetworkLiquidOutputPortEgress liquid =
                outputStorage.GetComponent<StorageNetworkLiquidOutputPortEgress>();
            if (liquid != null &&
                liquid.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                return liquid.SourceStorageInstanceId;
            }

            StorageNetworkGasOutputPortEgress gas =
                outputStorage.GetComponent<StorageNetworkGasOutputPortEgress>();
            return gas != null &&
                   gas.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? gas.SourceStorageInstanceId
                : KPrefabID.InvalidInstanceID;
        }
    }
}
