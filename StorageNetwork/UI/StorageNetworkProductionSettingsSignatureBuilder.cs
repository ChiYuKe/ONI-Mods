using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkProductionSettingsSignatureBuilder
    {
        public static string BuildProduction(Storage storage, ComplexFabricator fabricator)
        {
            StorageNetworkMaterialRequester requester = storage != null ? storage.GetComponent<StorageNetworkMaterialRequester>() : null;
            StorageNetworkStorageConnector connector = storage != null ? storage.GetComponent<StorageNetworkStorageConnector>() : null;
            StorageNetworkEnergyGeneratorRequester energyRequester = storage != null ? storage.GetComponent<StorageNetworkEnergyGeneratorRequester>() : null;
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
                connector != null && connector.OutputStoreEnabled ? "conn1" : "conn0",
                energyRequester != null && energyRequester.RequestEnabled ? "energyReq1" : "energyReq0",
                energyRequester != null ? energyRequester.Mode.ToString() : "0",
                energyRequester != null ? energyRequester.SourceStorageInstanceId.ToString() : "0",
                energyRequester != null && energyRequester.LimitEnabled ? "energyLim1" : "energyLim0",
                requester != null && !string.IsNullOrEmpty(requester.LastStatus) ? "matStatus1" : "matStatus0",
                requester != null && !string.IsNullOrEmpty(requester.LastOutputStatus) ? "reqOutStatus1" : "reqOutStatus0",
                connector != null && !string.IsNullOrEmpty(connector.LastOutputStatus) ? "connOutStatus1" : "connOutStatus0",
                energyRequester != null && !string.IsNullOrEmpty(energyRequester.LastStatus) ? "energyStatus1" : "energyStatus0",
                itemSignature);
        }

        public static string BuildMinion(MinionIdentity minion, Storage storage)
        {
            return string.Join(
                "~",
                minion != null ? minion.GetInstanceID().ToString() : "null",
                Config.Instance.IsMinionAllowedRequestMaterialsFromNetwork(minion) ? "allow1" : "allow0",
                BuildItemSignature(storage, null));
        }

        private static string BuildItemSignature(Storage storage, ComplexFabricator fabricator)
        {
            return string.Join("|", StorageNetworkProductionStorageCollector.GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .GroupBy(GetStoredItemKey)
                .OrderBy(group => group.Key)
                .Select(group => group.Key));
        }

        private static string GetStoredItemKey(GameObject item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            KPrefabID prefabId = item.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                return prefabId.PrefabID().ToString();
            }

            PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
            return primaryElement != null ? primaryElement.ElementID.ToString() : item.name;
        }
    }
}
