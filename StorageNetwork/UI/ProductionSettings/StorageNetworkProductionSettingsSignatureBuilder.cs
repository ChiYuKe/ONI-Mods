using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Services;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkProductionSettingsSignatureBuilder
    {
        public static string BuildProduction(Storage storage, ComplexFabricator fabricator)
        {
            StorageNetworkMaterialRequester requester = storage != null ? storage.GetComponent<StorageNetworkMaterialRequester>() : null;
            StorageNetworkStorageConnector connector = storage != null ? storage.GetComponent<StorageNetworkStorageConnector>() : null;
            StorageNetworkEnergyGeneratorRequester energyRequester = storage != null ? storage.GetComponent<StorageNetworkEnergyGeneratorRequester>() : null;
            StorageNetworkLiquidInputPortIngress liquidInput = storage != null ? storage.GetComponent<StorageNetworkLiquidInputPortIngress>() : null;
            StorageNetworkLiquidOutputPortEgress liquidOutput = storage != null ? storage.GetComponent<StorageNetworkLiquidOutputPortEgress>() : null;
            StorageNetworkGasInputPortIngress gasInput = storage != null ? storage.GetComponent<StorageNetworkGasInputPortIngress>() : null;
            StorageNetworkGasOutputPortEgress gasOutput = storage != null ? storage.GetComponent<StorageNetworkGasOutputPortEgress>() : null;
            StorageNetworkSolidInputPortIngress solidInput = storage != null ? storage.GetComponent<StorageNetworkSolidInputPortIngress>() : null;
            StorageNetworkSolidOutputPortEgress solidOutput = storage != null ? storage.GetComponent<StorageNetworkSolidOutputPortEgress>() : null;
            StorageNetworkPowerInputPortConsumer powerInput = storage != null ? storage.GetComponent<StorageNetworkPowerInputPortConsumer>() : null;
            StorageNetworkPowerOutputPortGenerator powerOutput = storage != null ? storage.GetComponent<StorageNetworkPowerOutputPortGenerator>() : null;
            StorageNetworkParticleInputPortIngress particleInput = storage != null ? storage.GetComponent<StorageNetworkParticleInputPortIngress>() : null;
            StorageNetworkParticleOutputPortEgress particleOutput = storage != null ? storage.GetComponent<StorageNetworkParticleOutputPortEgress>() : null;
            StorageNetworkColdStorageCooling coldStorageCooling = storage != null ? storage.GetComponent<StorageNetworkColdStorageCooling>() : null;
            string itemSignature = BuildItemSignature(storage, fabricator);

            return string.Join(
                "~",
                storage != null ? storage.GetInstanceID().ToString() : "null",
                requester != null && requester.RequestEnabled ? "req1" : "req0",
                requester != null ? requester.Mode.ToString() : "0",
                requester != null ? requester.SourceStorageInstanceId.ToString() : "0",
                requester != null && requester.LimitEnabled ? "lim1" : "lim0",
                requester != null && requester.OutputStoreEnabled ? "out1" : "out0",
                requester != null ? requester.OutputStoreModeValue.ToString() : "0",
                requester != null ? requester.OutputStorageInstanceId.ToString() : "0",
                connector != null && connector.IsOutputStoreEnabled() ? "conn1" : "conn0",
                connector != null ? connector.OutputStoreModeValue.ToString() : "0",
                connector != null ? connector.OutputStorageInstanceId.ToString() : "0",
                energyRequester != null && energyRequester.RequestEnabled ? "energyReq1" : "energyReq0",
                energyRequester != null ? energyRequester.Mode.ToString() : "0",
                energyRequester != null ? energyRequester.SourceStorageInstanceId.ToString() : "0",
                energyRequester != null && energyRequester.LimitEnabled ? "energyLim1" : "energyLim0",
                requester != null && !string.IsNullOrEmpty(requester.LastStatus) ? "matStatus1" : "matStatus0",
                requester != null && !string.IsNullOrEmpty(requester.LastOutputStatus) ? "reqOutStatus1" : "reqOutStatus0",
                connector != null && !string.IsNullOrEmpty(connector.LastOutputStatus) ? "connOutStatus1" : "connOutStatus0",
                energyRequester != null && !string.IsNullOrEmpty(energyRequester.LastStatus) ? "energyStatus1" : "energyStatus0",
                liquidInput != null && liquidInput.InputStoreEnabled ? "liquidIn1" : "liquidIn0",
                liquidInput != null ? liquidInput.InputStoreModeValue.ToString() : "0",
                liquidInput != null ? liquidInput.InputStorageInstanceId.ToString() : "0",
                liquidInput != null && !string.IsNullOrEmpty(liquidInput.LastStatus) ? liquidInput.LastStatus : "liquidInStatus0",
                liquidOutput != null && liquidOutput.OutputRequestEnabled ? "liquidOut1" : "liquidOut0",
                liquidOutput != null ? liquidOutput.SourceModeValue.ToString() : "0",
                liquidOutput != null ? liquidOutput.SourceStorageInstanceId.ToString() : "0",
                liquidOutput != null ? liquidOutput.OutputElementHash.ToString() : "0",
                liquidOutput != null && liquidOutput.OutputLimitEnabled ? "liquidOutLimit1" : "liquidOutLimit0",
                liquidOutput != null ? liquidOutput.OutputLimitKg.ToString("0.###") : "0",
                liquidOutput != null ? liquidOutput.OutputLimitUsedKg.ToString("0.###") : "0",
                liquidOutput != null ? liquidOutput.GetRequestRateKgPerSecond().ToString("0.###") : "0",
                liquidOutput != null && !string.IsNullOrEmpty(liquidOutput.LastStatus) ? liquidOutput.LastStatus : "liquidOutStatus0",
                gasInput != null && gasInput.InputStoreEnabled ? "gasIn1" : "gasIn0",
                gasInput != null ? gasInput.InputStoreModeValue.ToString() : "0",
                gasInput != null ? gasInput.InputStorageInstanceId.ToString() : "0",
                gasInput != null && !string.IsNullOrEmpty(gasInput.LastStatus) ? gasInput.LastStatus : "gasInStatus0",
                gasOutput != null && gasOutput.OutputRequestEnabled ? "gasOut1" : "gasOut0",
                gasOutput != null ? gasOutput.SourceModeValue.ToString() : "0",
                gasOutput != null ? gasOutput.SourceStorageInstanceId.ToString() : "0",
                gasOutput != null ? gasOutput.OutputElementHash.ToString() : "0",
                gasOutput != null && gasOutput.OutputLimitEnabled ? "gasOutLimit1" : "gasOutLimit0",
                gasOutput != null ? gasOutput.OutputLimitKg.ToString("0.###") : "0",
                gasOutput != null ? gasOutput.OutputLimitUsedKg.ToString("0.###") : "0",
                gasOutput != null ? gasOutput.GetRequestRateKgPerSecond().ToString("0.###") : "0",
                gasOutput != null && !string.IsNullOrEmpty(gasOutput.LastStatus) ? gasOutput.LastStatus : "gasOutStatus0",
                solidInput != null && solidInput.InputStoreEnabled ? "solidIn1" : "solidIn0",
                solidInput != null ? solidInput.InputStoreModeValue.ToString() : "0",
                solidInput != null ? solidInput.InputStorageInstanceId.ToString() : "0",
                solidInput != null && !string.IsNullOrEmpty(solidInput.LastStatus) ? solidInput.LastStatus : "solidInStatus0",
                solidOutput != null && solidOutput.OutputRequestEnabled ? "solidOut1" : "solidOut0",
                solidOutput != null ? solidOutput.SourceModeValue.ToString() : "0",
                solidOutput != null ? solidOutput.SourceStorageInstanceId.ToString() : "0",
                solidOutput != null ? solidOutput.OutputItemTagName ?? "solidOutFilterAny" : "solidOutFilter0",
                solidOutput != null && solidOutput.OutputLimitEnabled ? "solidOutLimit1" : "solidOutLimit0",
                solidOutput != null ? solidOutput.OutputLimitKg.ToString("0.###") : "0",
                solidOutput != null ? solidOutput.OutputLimitUsedKg.ToString("0.###") : "0",
                solidOutput != null ? solidOutput.GetRequestRateKgPerSecond().ToString("0.###") : "0",
                solidOutput != null && !string.IsNullOrEmpty(solidOutput.LastStatus) ? solidOutput.LastStatus : "solidOutStatus0",
                powerInput != null ? powerInput.GetInputWattsSetting().ToString("0.###") : "powerIn0",
                powerInput != null ? powerInput.InputStoreModeValue.ToString() : "powerInMode0",
                powerInput != null ? powerInput.InputStorageInstanceId.ToString() : "powerInTarget0",
                powerOutput != null ? powerOutput.GetOutputWattsSetting().ToString("0.###") : "powerOut0",
                powerOutput != null ? powerOutput.SourceModeValue.ToString() : "powerOutMode0",
                powerOutput != null ? powerOutput.SourceStorageInstanceId.ToString() : "powerOutSource0",
                powerOutput != null && powerOutput.OutputLimitEnabled ? "powerOutLimit1" : "powerOutLimit0",
                powerOutput != null ? powerOutput.OutputLimitJoules.ToString("0.###") : "0",
                powerOutput != null ? powerOutput.OutputLimitUsedJoules.ToString("0.###") : "0",
                particleInput != null && particleInput.InputStoreEnabled ? "particleIn1" : "particleIn0",
                particleOutput != null && particleOutput.OutputRequestEnabled ? "particleOut1" : "particleOut0",
                particleOutput != null ? particleOutput.SourceModeValue.ToString() : "particleOutMode0",
                particleOutput != null ? particleOutput.SourceStorageInstanceId.ToString() : "particleOutSource0",
                particleOutput != null ? particleOutput.Direction.ToString() : "particleDirection0",
                particleOutput != null ? particleOutput.ParticleThreshold.ToString("0.###") : "particleThreshold0",
                particleOutput != null && particleOutput.OutputLimitEnabled ? "particleLimit1" : "particleLimit0",
                particleOutput != null ? particleOutput.OutputLimitParticles.ToString("0.###") : "particleLimitAmount0",
                particleOutput != null ? particleOutput.OutputLimitUsedParticles.ToString("0.###") : "particleLimitUsed0",
                particleInput != null || particleOutput != null ? StorageNetworkParticleStorageService.GetAvailable(storage.gameObject).ToString("0.###") : "particleAvailable0",
                particleInput != null || particleOutput != null ? StorageNetworkParticleStorageService.GetCapacity(storage.gameObject).ToString("0.###") : "particleCapacity0",
                coldStorageCooling != null ? coldStorageCooling.TargetTemperature.ToString("0.###") : "cold0",
                itemSignature);
        }

        private static string BuildItemSignature(Storage storage, ComplexFabricator fabricator)
        {
            return string.Join("|", StorageNetworkProductionStorageCollector.GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .GroupBy(StorageItemUtility.GetStoredItemKey)
                .OrderBy(group => group.Key)
                .Select(group => group.Key));
        }

    }
}
