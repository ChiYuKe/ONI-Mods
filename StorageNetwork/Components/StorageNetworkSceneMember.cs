using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    /// <summary>
    /// Registers StorageNetwork prefab instances that do not have a richer runtime component.
    /// </summary>
    public sealed class StorageNetworkSceneMember : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageSceneRegistry.Register(gameObject);
        }

        protected override void OnCleanUp()
        {
            StorageSceneRegistry.Unregister(gameObject);
            base.OnCleanUp();
        }
    }
}
