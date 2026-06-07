using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络核心。每个星球只能建造一个；该星球的核心在线时，本地储存网络才可用。
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
