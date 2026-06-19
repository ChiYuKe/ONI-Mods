using StorageNetwork.API;
using StorageNetwork.Buildings;
using System.Collections.Generic;

namespace StorageNetwork.Components
{
#pragma warning disable CS0649
    public sealed class StorageNetworkOrderProductionCenterDiskInstallWorkable : KMonoBehaviour
    {
        [MyCmpGet]
        private StorageNetworkOrderProductionCenter center;

        private Storage storage;

        private FetchList2 fetchList;

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkOrderProductionCenterDiskInstallWorkable> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkOrderProductionCenterDiskInstallWorkable>((component, data) => component.OnStorageChange(data));

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RemoveAccidentalFilterable();
            storage = StorageNetworkOrderProductionCenterStorageHelper.GetDiskInstallStorage(gameObject);
            ConfigureStorage();
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
        }

        protected override void OnCleanUp()
        {
            fetchList?.Cancel("StorageNetwork order production center cleaned up");
            fetchList = null;
            center?.ClearPendingDiskInstall();
            base.OnCleanUp();
        }

        public void CreateInstallChore()
        {
            if (center == null || storage == null || !center.IsQueuedDiskStillValid())
            {
                center?.ClearPendingDiskInstall();
                return;
            }

            fetchList?.Cancel("StorageNetwork order production center disk install replaced");
            fetchList = new FetchList2(storage, Db.Get().ChoreTypes.StorageFetch);
            fetchList.ShowStatusItem = false;
            fetchList.SetPriorityMod(5);

            int requestCount = 0;
            foreach (Tag requiredTag in center.GetPendingInstallRequiredTags())
            {
                if (!requiredTag.IsValid)
                {
                    continue;
                }

                fetchList.Add(
                    new HashSet<Tag> { StorageNetworkEngravingDiskConfig.ID },
                    requiredTag,
                    null,
                    1f,
                    Operational.State.Functional);
                requestCount++;
            }

            if (requestCount <= 0)
            {
                fetchList = null;
                center.ClearPendingDiskInstall();
                return;
            }

            fetchList.Submit(OnFetchComplete, false);
        }

        private void OnStorageChange(object data)
        {
            if (center == null)
            {
                return;
            }

            if (center.CompleteDeliveredDiskInstall())
            {
                fetchList = null;
            }
        }

        private void ConfigureStorage()
        {
            if (storage == null)
            {
                return;
            }

            storage.capacityKg = StorageNetworkOrderProductionCenterStorageHelper.DiskInstallStorageCapacityKg;
            storage.showInUI = false;
            storage.allowItemRemoval = false;
            storage.allowUIItemRemoval = false;
            storage.fetchCategory = Storage.FetchCategory.Building;
            storage.storageFilters = new System.Collections.Generic.List<Tag> { StorageNetworkEngravingDiskConfig.ID };
            storage.SetDefaultStoredItemModifiers(Storage.StandardSealedStorage);
        }

        private void RemoveAccidentalFilterable()
        {
            TreeFilterable filterable = GetComponent<TreeFilterable>();
            if (filterable != null)
            {
                Destroy(filterable);
            }
        }

        private void OnFetchComplete()
        {
            fetchList = null;
            if (center == null)
            {
                return;
            }

            center.CompleteDeliveredDiskInstall();
        }
    }
#pragma warning restore CS0649
}
