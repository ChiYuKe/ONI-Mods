using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRelayModule : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageSceneRegistry.Register(gameObject);
            Subscribe((int)GameHashes.ClustercraftStateChanged, OnRocketStateChangedDelegate);
        }

        protected override void OnCleanUp()
        {
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public bool IsInSpace()
        {
            RocketModuleCluster module = GetComponent<RocketModuleCluster>();
            Clustercraft craft = module != null && module.CraftInterface != null
                ? module.CraftInterface.GetComponent<Clustercraft>()
                : null;
            return craft != null && craft.Status == Clustercraft.CraftStatus.InFlight;
        }

        private void OnRocketStateChanged(object data)
        {
            StorageSceneRegistry.Invalidate();
        }

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkRelayModule> OnRocketStateChangedDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkRelayModule>((component, data) => component.OnRocketStateChanged(data));
    }
}
