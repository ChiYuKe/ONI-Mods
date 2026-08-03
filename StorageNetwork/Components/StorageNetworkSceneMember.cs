using StorageNetwork.Core;
using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    /// <summary>
    /// Registers StorageNetwork prefab instances that do not have a richer runtime component.
    /// </summary>
    public sealed class StorageNetworkSceneMember : KMonoBehaviour
    {
        [MyCmpGet]
        private Storage storage = null;

        [MyCmpGet]
        private Operational operational = null;

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkSceneMember>
            OnOperationalChangedDelegate =
                new EventSystem.IntraObjectHandler<StorageNetworkSceneMember>(
                    (component, data) => component.OnOperationalChanged(data));

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageSceneRegistry.Register(gameObject);
            if (operational != null)
            {
                Subscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            }
        }

        protected override void OnCleanUp()
        {
            if (operational != null)
            {
                Unsubscribe((int)GameHashes.OperationalChanged, OnOperationalChangedDelegate);
            }

            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        private void OnOperationalChanged(object data)
        {
            if (storage != null)
            {
                StorageNetworkContentIndexService.Invalidate(storage);
                StorageNetworkParticleStorageService.Invalidate(storage);
            }

            StorageSceneRegistry.InvalidateCapabilities(storage);
        }
    }
}
