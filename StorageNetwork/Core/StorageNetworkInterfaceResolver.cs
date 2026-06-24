using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkInterfaceResolver
    {
        public static StorageNetworkStorageFlags GetStorageFlags(Storage storage)
        {
            if (storage == null)
            {
                return StorageNetworkStorageFlags.None;
            }

            StorageNetworkStorageFlags flags = GetTagFlags(storage);
            foreach (IStorageNetworkStorageFlagsProvider provider in GetComponents<IStorageNetworkStorageFlagsProvider>(storage.gameObject))
            {
                flags |= provider.GetStorageNetworkStorageFlags(storage);
            }

            return flags;
        }

        public static bool HasStorageFlag(Storage storage, StorageNetworkStorageFlags flag)
        {
            return (GetStorageFlags(storage) & flag) == flag;
        }

        public static IStorageNetworkEnrollable GetEnrollable(GameObject gameObject)
        {
            return GetComponents<IStorageNetworkEnrollable>(gameObject)
                .FirstOrDefault(enrollable => enrollable.CanShowStorageNetworkEnrollmentButton());
        }

        public static bool HasExternalStorageNetworkInterface(GameObject gameObject)
        {
            return GetComponents<IStorageNetworkStorageFlagsProvider>(gameObject).Any() ||
                   GetComponents<IStorageNetworkSettingsButtonProvider>(gameObject).Any() ||
                   GetComponents<IStorageNetworkEnrollable>(gameObject).Any(enrollable => !(enrollable is StorageNetworkEnrollment));
        }

        public static StorageNetworkSettingsButtonState GetSettingsButtonState(Storage storage)
        {
            foreach (IStorageNetworkSettingsButtonProvider provider in GetComponents<IStorageNetworkSettingsButtonProvider>(storage?.gameObject))
            {
                StorageNetworkSettingsButtonState state = provider.GetStorageNetworkSettingsButtonState(storage);
                if (state != null)
                {
                    return state;
                }
            }

            bool visible = HasStorageFlag(storage, StorageNetworkStorageFlags.ShowSettingsButton);
            return new StorageNetworkSettingsButtonState(visible, visible);
        }

        public static IStorageNetworkSettingsPanelProvider GetSettingsPanelProvider(Storage storage)
        {
            return GetComponents<IStorageNetworkSettingsPanelProvider>(storage?.gameObject)
                .FirstOrDefault();
        }

        public static void InstallExternalApiBridgeIfNeeded(GameObject gameObject)
        {
            if (gameObject == null ||
                gameObject.GetComponent<StorageNetworkExternalApiBridge>() != null ||
                !HasExternalStorageNetworkInterface(gameObject))
            {
                return;
            }

            gameObject.AddOrGet<StorageNetworkExternalApiBridge>();
        }

        private static StorageNetworkStorageFlags GetTagFlags(Storage storage)
        {
            KPrefabID prefabId = storage.GetComponent<KPrefabID>();
            if (prefabId == null)
            {
                return StorageNetworkStorageFlags.None;
            }

            StorageNetworkStorageFlags flags = StorageNetworkStorageFlags.None;
            AddTagFlag(prefabId, StorageNetworkTags.ModStorage, StorageNetworkStorageFlags.NetworkStorage, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.ServerStorage, StorageNetworkStorageFlags.ServerStorage, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.ShowSettingsButton, StorageNetworkStorageFlags.ShowSettingsButton, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryModStorage, StorageNetworkStorageFlags.CategoryModStorage, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryInputPort, StorageNetworkStorageFlags.InputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryOutputPort, StorageNetworkStorageFlags.OutputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategorySolidPort, StorageNetworkStorageFlags.SolidPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryLiquidPort, StorageNetworkStorageFlags.LiquidPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryGasPort, StorageNetworkStorageFlags.GasPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryPowerPort, StorageNetworkStorageFlags.PowerPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryParticlePort, StorageNetworkStorageFlags.ParticlePort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategorySolidInputPort, StorageNetworkStorageFlags.SolidInputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategorySolidOutputPort, StorageNetworkStorageFlags.SolidOutputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryLiquidInputPort, StorageNetworkStorageFlags.LiquidInputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryLiquidOutputPort, StorageNetworkStorageFlags.LiquidOutputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryGasInputPort, StorageNetworkStorageFlags.GasInputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryGasOutputPort, StorageNetworkStorageFlags.GasOutputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryPowerInputPort, StorageNetworkStorageFlags.PowerInputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryPowerOutputPort, StorageNetworkStorageFlags.PowerOutputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryParticleInputPort, StorageNetworkStorageFlags.ParticleInputPort, ref flags);
            AddTagFlag(prefabId, StorageNetworkTags.CategoryParticleOutputPort, StorageNetworkStorageFlags.ParticleOutputPort, ref flags);
            return flags;
        }

        private static void AddTagFlag(KPrefabID prefabId, Tag tag, StorageNetworkStorageFlags flag, ref StorageNetworkStorageFlags flags)
        {
            if (prefabId.HasTag(tag))
            {
                flags |= flag;
            }
        }

        private static T[] GetComponents<T>(GameObject gameObject)
            where T : class
        {
            return gameObject == null
                ? new T[0]
                : gameObject.GetComponents<Component>().OfType<T>().ToArray();
        }
    }
}
