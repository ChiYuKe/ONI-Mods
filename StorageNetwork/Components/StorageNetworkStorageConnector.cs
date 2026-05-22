using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    public class StorageNetworkStorageConnector : KMonoBehaviour, IStorageNetworkConnectable
    {
        private Storage storage;

        public int Cell => Grid.PosToCell(this);

        public string DisplayName => gameObject.GetProperName();

        public Storage Storage => storage;

        public bool CanShareStorage => storage != null && storage.enabled;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            storage = GetComponent<Storage>();
        }
    }
}
