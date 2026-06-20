using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkParticleServer : KMonoBehaviour
    {
        [MyCmpReq]
        private HighEnergyParticleStorage particleStorage;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageNetworkParticleStorageService.Register(particleStorage);
        }

        protected override void OnCleanUp()
        {
            StorageNetworkParticleStorageService.Unregister(particleStorage);
            base.OnCleanUp();
        }
    }
}
