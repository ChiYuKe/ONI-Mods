using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRelayModule : KMonoBehaviour
    {
        [MyCmpGet]
        private RocketModuleCluster module = null;

        private Clustercraft craft;
        private bool isInSpace;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RefreshCraftReference();
            RefreshInSpaceState();
            StorageNetworkRocketRelayService.Register(this, module);
            StorageNetworkRocketRelayService.SetInSpace(this, isInSpace);
            StorageSceneRegistry.Register(gameObject);
            Subscribe((int)GameHashes.ClustercraftStateChanged, OnRocketStateChangedDelegate);
        }

        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.ClustercraftStateChanged, OnRocketStateChangedDelegate);
            StorageNetworkRocketRelayService.Unregister(this);
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }

        public bool IsInSpace()
        {
            if (craft == null)
            {
                RefreshCraftReference();
                RefreshInSpaceState();
                StorageNetworkRocketRelayService.Register(this, module);
                StorageNetworkRocketRelayService.SetInSpace(this, isInSpace);
            }

            return isInSpace;
        }

        private void OnRocketStateChanged(object data)
        {
            RefreshCraftReference();
            bool previous = isInSpace;
            RefreshInSpaceState();
            StorageNetworkRocketRelayService.Register(this, module);
            StorageNetworkRocketRelayService.SetInSpace(this, isInSpace);
            if (previous != isInSpace)
            {
                StorageSceneRegistry.InvalidateConnectivity();
            }
        }

        private void RefreshCraftReference()
        {
            CraftModuleInterface craftInterface = module != null ? module.CraftInterface : null;
            Clustercraft currentCraft = craftInterface != null
                ? craftInterface.GetComponent<Clustercraft>()
                : null;
            if (craft != currentCraft)
            {
                craft = currentCraft;
            }
        }

        private void RefreshInSpaceState()
        {
            isInSpace = craft != null &&
                craft.Status == Clustercraft.CraftStatus.InFlight;
        }

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkRelayModule> OnRocketStateChangedDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkRelayModule>((component, data) => component.OnRocketStateChanged(data));
    }
}
