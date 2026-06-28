using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkInterfaceResolver
    {
        private static readonly Dictionary<string, StorageNetworkCategoryDescriptor> KnownCategories =
            new Dictionary<string, StorageNetworkCategoryDescriptor>();
        private static readonly Dictionary<int, ComponentInterfaceCache> ComponentCaches =
            new Dictionary<int, ComponentInterfaceCache>();
        private static readonly Dictionary<int, StorageNetworkStorageFlags> TagFlagCache =
            new Dictionary<int, StorageNetworkStorageFlags>();
        private static readonly List<int> DeadComponentCacheKeys = new List<int>();
        private static int lastComponentCachePruneFrame = -1;
        private static float lastComponentCachePruneAt = -1f;

        public static void ResetRuntimeState()
        {
            KnownCategories.Clear();
            ComponentCaches.Clear();
            TagFlagCache.Clear();
            DeadComponentCacheKeys.Clear();
            lastComponentCachePruneFrame = -1;
            lastComponentCachePruneAt = -1f;
        }

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
                   GetComponents<IStorageNetworkCategoryProvider>(gameObject).Any() ||
                   GetComponents<IStorageNetworkDisplayProvider>(gameObject).Any() ||
                   GetComponents<IStorageNetworkStorageRowButtonProvider>(gameObject).Any() ||
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

        public static StorageNetworkCategoryDescriptor GetCategory(Storage storage)
        {
            foreach (IStorageNetworkCategoryProvider provider in GetComponents<IStorageNetworkCategoryProvider>(storage?.gameObject))
            {
                StorageNetworkCategoryDescriptor descriptor = provider.GetStorageNetworkCategory(storage);
                if (descriptor != null && !string.IsNullOrEmpty(descriptor.Key))
                {
                    KnownCategories[descriptor.Key] = descriptor;
                    return descriptor;
                }
            }

            return null;
        }

        public static StorageNetworkCategoryDescriptor GetKnownCategory(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            return KnownCategories.TryGetValue(key, out StorageNetworkCategoryDescriptor descriptor)
                ? descriptor
                : null;
        }

        public static StorageNetworkDisplayInfo GetDisplayInfo(Storage storage)
        {
            foreach (IStorageNetworkDisplayProvider provider in GetComponents<IStorageNetworkDisplayProvider>(storage?.gameObject))
            {
                StorageNetworkDisplayInfo displayInfo = provider.GetStorageNetworkDisplayInfo(storage);
                if (displayInfo != null)
                {
                    return displayInfo;
                }
            }

            return null;
        }

        public static IEnumerable<StorageNetworkStorageRowButton> GetStorageRowButtons(Storage storage)
        {
            foreach (IStorageNetworkStorageRowButtonProvider provider in GetComponents<IStorageNetworkStorageRowButtonProvider>(storage?.gameObject))
            {
                IEnumerable<StorageNetworkStorageRowButton> buttons = provider.GetStorageNetworkStorageRowButtons(storage);
                if (buttons == null)
                {
                    continue;
                }

                foreach (StorageNetworkStorageRowButton button in buttons)
                {
                    if (button != null && !string.IsNullOrEmpty(button.Id) && button.OnClick != null)
                    {
                        yield return button;
                    }
                }
            }
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

            int cacheKey = prefabId.GetInstanceID();
            if (TagFlagCache.TryGetValue(cacheKey, out StorageNetworkStorageFlags cachedFlags))
            {
                return cachedFlags;
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
            TagFlagCache[cacheKey] = flags;
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
            if (gameObject == null)
            {
                return new T[0];
            }

            PruneDeadComponentCaches();
            return GetOrCreateComponentCache(gameObject).GetComponents<T>();
        }

        private static ComponentInterfaceCache GetOrCreateComponentCache(GameObject gameObject)
        {
            int instanceId = gameObject.GetInstanceID();
            if (ComponentCaches.TryGetValue(instanceId, out ComponentInterfaceCache cache) &&
                cache.IsFor(gameObject))
            {
                return cache;
            }

            cache = new ComponentInterfaceCache(gameObject);
            ComponentCaches[instanceId] = cache;
            return cache;
        }

        private static void PruneDeadComponentCaches()
        {
            if (lastComponentCachePruneFrame == Time.frameCount)
            {
                return;
            }

            if (lastComponentCachePruneAt >= 0f && Time.unscaledTime - lastComponentCachePruneAt < 1f)
            {
                return;
            }

            lastComponentCachePruneFrame = Time.frameCount;
            lastComponentCachePruneAt = Time.unscaledTime;
            DeadComponentCacheKeys.Clear();
            foreach (KeyValuePair<int, ComponentInterfaceCache> pair in ComponentCaches)
            {
                if (!pair.Value.IsLive)
                {
                    DeadComponentCacheKeys.Add(pair.Key);
                }
            }

            foreach (int key in DeadComponentCacheKeys)
            {
                ComponentCaches.Remove(key);
            }

            DeadComponentCacheKeys.Clear();
        }

        private sealed class ComponentInterfaceCache
        {
            private readonly GameObject gameObject;
            private readonly Component[] components;
            private readonly Dictionary<System.Type, object> typedComponents =
                new Dictionary<System.Type, object>();

            public ComponentInterfaceCache(GameObject gameObject)
            {
                this.gameObject = gameObject;
                components = gameObject != null ? gameObject.GetComponents<Component>() : new Component[0];
            }

            public bool IsLive => gameObject != null;

            public bool IsFor(GameObject candidate)
            {
                return gameObject == candidate;
            }

            public T[] GetComponents<T>()
                where T : class
            {
                System.Type type = typeof(T);
                if (typedComponents.TryGetValue(type, out object cached))
                {
                    return (T[])cached;
                }

                List<T> matches = new List<T>();
                foreach (Component component in components)
                {
                    if (component is T match)
                    {
                        matches.Add(match);
                    }
                }

                T[] result = matches.ToArray();
                typedComponents[type] = result;
                return result;
            }
        }
    }
}
