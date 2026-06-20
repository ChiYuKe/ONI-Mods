using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkServerCopySettings : KMonoBehaviour
    {
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkServerCopySettings> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkServerCopySettings>((component, data) => component.OnCopySettings(data));

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            if (sourceObject == null || sourceObject == gameObject)
            {
                return;
            }

            Storage sourceStorage = sourceObject.GetComponent<Storage>();
            Storage targetStorage = GetComponent<Storage>();
            if (!StorageNetworkStorageRules.IsServerStorage(sourceStorage) ||
                !StorageNetworkStorageRules.IsServerStorage(targetStorage) ||
                !HasMatchingFilterCategory(sourceStorage, targetStorage))
            {
                return;
            }

            StorageNetworkFilterCopyHelper.CopyFilters(gameObject, sourceObject);
            CopyColdStorageSettings(sourceObject);
        }

        private static bool HasMatchingFilterCategory(Storage source, Storage target)
        {
            if (StorageNetworkStorageRules.IsColdStorageServer(source) &&
                StorageNetworkStorageRules.IsColdStorageServer(target))
            {
                return true;
            }

            return Matches(source, target, API.StorageNetworkTags.CategorySolidPort) ||
                   Matches(source, target, API.StorageNetworkTags.CategoryLiquidPort) ||
                   Matches(source, target, API.StorageNetworkTags.CategoryGasPort);
        }

        private void CopyColdStorageSettings(GameObject sourceObject)
        {
            StorageNetworkColdStorageCooling source = sourceObject != null ? sourceObject.GetComponent<StorageNetworkColdStorageCooling>() : null;
            StorageNetworkColdStorageCooling target = GetComponent<StorageNetworkColdStorageCooling>();
            if (source == null || target == null)
            {
                return;
            }

            target.TargetTemperature = source.TargetTemperature;
        }

        private static bool Matches(Storage source, Storage target, Tag category)
        {
            return HasTag(source, category) && HasTag(target, category);
        }

        private static bool HasTag(Storage storage, Tag tag)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null && prefabId.HasTag(tag);
        }
    }
}
