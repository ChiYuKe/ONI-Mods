using UnityEngine;
using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    public sealed class SceneStorageBoxMarker : KMonoBehaviour
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
