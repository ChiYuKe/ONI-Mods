using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkFloatingChatHost : MonoBehaviour
    {
        internal StorageNetworkPanel Owner;
        private void Update()
        {
            if (Owner == null) { Destroy(gameObject); return; }
            Owner.UpdateTradingGroupChat(Time.unscaledDeltaTime);
        }
    }
}
