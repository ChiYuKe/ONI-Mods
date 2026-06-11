using System;
using KSerialization;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkFilterState : KMonoBehaviour
    {
        [Serialize]
        private bool defaultInitialized;

        [Serialize]
        private bool userConfigured;

        [ThreadStatic]
        private static int suppressUserConfigurationDepth;

        public bool DefaultInitialized => defaultInitialized;

        public bool UserConfigured => userConfigured;

        public static StorageNetworkFilterState Ensure(TreeFilterable filterable)
        {
            if (filterable == null)
            {
                return null;
            }

            StorageNetworkFilterState state = filterable.gameObject.AddOrGet<StorageNetworkFilterState>();
            Storage storage = filterable.GetFilterStorage();
            if (storage != null && storage.gameObject != filterable.gameObject)
            {
                storage.gameObject.AddOrGet<StorageNetworkFilterState>();
            }

            return state;
        }

        public void MarkDefaultInitialized()
        {
            defaultInitialized = true;
        }

        public void MarkUserConfigured()
        {
            userConfigured = true;
            defaultInitialized = true;
        }

        public static IDisposable SuppressUserConfigurationTracking()
        {
            suppressUserConfigurationDepth++;
            return new SuppressionScope();
        }

        public static void MarkUserConfigured(TreeFilterable filterable)
        {
            if (suppressUserConfigurationDepth > 0 || filterable == null)
            {
                return;
            }

            Storage storage = filterable.GetFilterStorage();
            StorageNetworkFilterState state = filterable.GetComponent<StorageNetworkFilterState>();
            StorageNetworkFilterState storageState = null;
            if (state == null && storage != null)
            {
                state = storage.GetComponent<StorageNetworkFilterState>();
            }

            state?.MarkUserConfigured();
            if (storage != null)
            {
                storageState = storage.GetComponent<StorageNetworkFilterState>();
                if (storageState != null && storageState != state)
                {
                    storageState.MarkUserConfigured();
                }
            }
        }

        private sealed class SuppressionScope : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                suppressUserConfigurationDepth = Math.Max(0, suppressUserConfigurationDepth - 1);
            }
        }
    }
}
