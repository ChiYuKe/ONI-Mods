using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络核心。它只负责供电判定，不提供储存容量；当前场景至少有一个在线核心时网络才可用。
    /// </summary>
    public sealed class StorageNetworkCore : KMonoBehaviour
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
