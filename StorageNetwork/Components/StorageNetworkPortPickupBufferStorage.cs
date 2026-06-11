using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPortPickupBufferStorage : KMonoBehaviour, ISim1000ms
    {
        public static readonly Tag BufferStorageId = new Tag("StorageNetwork_PortDuplicantPickupBuffer");

        private const string BufferObjectName = "StorageNetworkDuplicantPickupBuffer";
        private const float DefaultCapacityKg = 2000f;
        private const float IdleReturnSeconds = 120f;
        private const float ReturnCheckIntervalSeconds = 5f;

        private Storage mainStorage;
        private Storage bufferStorage;
        private StorageNetworkPort port;
        private float idleSeconds;
        private float returnCheckSeconds;
        private bool pickupStateDirty = true;
        private bool contentsDirty = true;
        private bool hasAppliedPickupState;
        private bool lastAppliedAllowPickup;

        public Storage BufferStorage
        {
            get
            {
                EnsureComponents();
                return bufferStorage;
            }
        }

        public static void ConfigurePrefab(GameObject go, float capacityKg)
        {
            if (go == null)
            {
                return;
            }

            go.AddOrGet<StorageNetworkPortPickupBufferStorage>();
            Storage main = FindMainStorage(go);
            Storage buffer = FindBufferStorage(go);
            if (buffer == null)
            {
                buffer = GetOrCreateBufferObject(go).AddOrGet<Storage>();
            }

            ConfigureStorage(buffer, Mathf.Max(DefaultCapacityKg, capacityKg, main != null ? main.capacityKg : 0f), allowPickup: true, syncStoredItems: true);
        }

        public static bool IsPickupBufferStorage(Storage storage)
        {
            return storage != null && storage.storageID == BufferStorageId;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureComponents();
            MarkPickupStateDirty();
            ApplyDirtyState();
        }

        protected override void OnCleanUp()
        {
            TryReturnToNetwork();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            EnsureComponents();
            if (bufferStorage == null)
            {
                return;
            }

            ApplyDirtyState();
            if (!HasStoredItems())
            {
                idleSeconds = 0f;
                returnCheckSeconds = 0f;
                return;
            }

            returnCheckSeconds += dt;
            if (returnCheckSeconds < ReturnCheckIntervalSeconds)
            {
                return;
            }

            float elapsed = returnCheckSeconds;
            returnCheckSeconds = 0f;

            if (!IsPickupAllowed())
            {
                idleSeconds = 0f;
                TryReturnToNetwork();
                return;
            }

            if (HasReservedItems())
            {
                idleSeconds = 0f;
                return;
            }

            idleSeconds += elapsed;
            if (idleSeconds >= IdleReturnSeconds)
            {
                idleSeconds = 0f;
                TryReturnToNetwork();
            }
        }

        public void MarkTouched()
        {
            idleSeconds = 0f;
            returnCheckSeconds = 0f;
        }

        public void MarkPickupStateDirty()
        {
            pickupStateDirty = true;
        }

        public void MarkContentsChanged()
        {
            idleSeconds = 0f;
            returnCheckSeconds = 0f;
            contentsDirty = true;
            EnsureComponents();
            ApplyDirtyState();
        }

        public void OnManualOperationChanged(bool allowed)
        {
            EnsureComponents();
            MarkPickupStateDirty();
            ApplyDirtyState();
            if (!allowed)
            {
                TryReturnToNetwork();
            }
        }

        public Storage GetBufferStorage()
        {
            EnsureComponents();
            return bufferStorage;
        }

        public Storage GetMainStorage()
        {
            EnsureComponents();
            return mainStorage;
        }

        public void TryReturnToNetwork()
        {
            EnsureComponents();
            if (bufferStorage == null || bufferStorage.items == null || bufferStorage.items.Count == 0)
            {
                return;
            }

            StorageNetworkPerformanceCounters.RecordBufferReturnAttempt();
            NetworkStorageTransferService.TransferStoredItemsToNetwork(
                bufferStorage,
                new[] { mainStorage, bufferStorage });

            MarkContentsChanged();
        }

        private void EnsureComponents()
        {
            port ??= GetComponent<StorageNetworkPort>();
            mainStorage ??= FindMainStorage(gameObject);
            bufferStorage ??= FindBufferStorage(gameObject);
            if (bufferStorage == null && port != null && port.Kind == StorageNetworkPortKind.SolidOutput)
            {
                bufferStorage = GetOrCreateBufferObject(gameObject).AddOrGet<Storage>();
                MarkPickupStateDirty();
            }

            if (isSpawned && bufferStorage != null && !bufferStorage.isSpawned)
            {
                bufferStorage.Spawn();
            }
        }

        private void ApplyDirtyState()
        {
            if (bufferStorage == null)
            {
                return;
            }

            bool allowPickup = IsPickupAllowed();
            if (!hasAppliedPickupState || allowPickup != lastAppliedAllowPickup)
            {
                pickupStateDirty = true;
            }

            if (!pickupStateDirty && !contentsDirty)
            {
                return;
            }

            if (pickupStateDirty)
            {
                ConfigureBufferStorage(allowPickup, syncStoredItems: true);
                lastAppliedAllowPickup = allowPickup;
                hasAppliedPickupState = true;
                pickupStateDirty = false;
                contentsDirty = false;
                return;
            }

            StorageNetworkPortPickupState.SyncStoredItems(bufferStorage, allowPickup);
            contentsDirty = false;
        }

        private bool IsPickupAllowed()
        {
            return port != null && port.IsManualDuplicantOperationAllowed();
        }

        private void ConfigureBufferStorage(bool allowPickup, bool syncStoredItems)
        {
            if (bufferStorage == null)
            {
                return;
            }

            float capacityKg = Mathf.Max(DefaultCapacityKg, mainStorage != null ? mainStorage.capacityKg : 0f, bufferStorage.capacityKg);
            ConfigureStorage(bufferStorage, capacityKg, allowPickup, syncStoredItems);
        }

        private static void ConfigureStorage(Storage storage, float capacityKg, bool allowPickup, bool syncStoredItems)
        {
            if (storage == null)
            {
                return;
            }

            storage.storageID = BufferStorageId;
            storage.capacityKg = Mathf.Max(DefaultCapacityKg, capacityKg);
            storage.showInUI = false;
            storage.allowClearable = false;
            storage.allowItemRemoval = allowPickup;
            storage.showDescriptor = false;
            storage.storageFilters = new List<Tag>();
            storage.fetchCategory = allowPickup ? Storage.FetchCategory.GeneralStorage : Storage.FetchCategory.Building;
            storage.showCapacityStatusItem = false;
            storage.showCapacityAsMainStatus = false;
            storage.storageFullMargin = 1f;
            if (syncStoredItems)
            {
                StorageNetworkPortPickupState.SyncStoredItems(storage, allowPickup);
            }
        }

        private bool HasStoredItems()
        {
            return bufferStorage?.items != null && bufferStorage.items.Count > 0;
        }

        private bool HasReservedItems()
        {
            if (bufferStorage?.items == null)
            {
                return false;
            }

            foreach (GameObject item in bufferStorage.items)
            {
                Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
                if (pickupable != null && pickupable.ReservedAmount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return true;
                }
            }

            return false;
        }

        public static Storage FindMainStorage(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            foreach (Storage storage in go.GetComponents<Storage>())
            {
                if (!IsPickupBufferStorage(storage))
                {
                    return storage;
                }
            }

            return null;
        }

        public static Storage FindBufferStorage(GameObject go)
        {
            if (go == null)
            {
                return null;
            }

            foreach (Storage storage in go.GetComponents<Storage>())
            {
                if (IsPickupBufferStorage(storage))
                {
                    ConfigureBufferIdentity(storage.gameObject);
                    return storage;
                }
            }

            foreach (Storage storage in go.GetComponentsInChildren<Storage>(true))
            {
                if (storage != null && storage.gameObject != go && IsPickupBufferStorage(storage))
                {
                    ConfigureBufferIdentity(storage.gameObject);
                    return storage;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateBufferObject(GameObject owner)
        {
            Transform existing = owner != null ? owner.transform.Find(BufferObjectName) : null;
            GameObject bufferObject = existing != null ? existing.gameObject : null;
            if (bufferObject == null)
            {
                bufferObject = new GameObject(BufferObjectName);
            }

            bufferObject.transform.SetParent(owner.transform, false);
            bufferObject.transform.localPosition = Vector3.zero;
            bufferObject.transform.localRotation = Quaternion.identity;
            bufferObject.transform.localScale = Vector3.one;
            bufferObject.layer = owner.layer;
            ConfigureBufferIdentity(bufferObject);
            return bufferObject;
        }

        private static void ConfigureBufferIdentity(GameObject bufferObject)
        {
            if (bufferObject == null)
            {
                return;
            }

            KPrefabID prefabId = bufferObject.AddOrGet<KPrefabID>();
            prefabId.PrefabTag = BufferStorageId;
            prefabId.SaveLoadTag = BufferStorageId;
            prefabId.AddTag(BufferStorageId);
            prefabId.UpdateSaveLoadTag();
        }
    }
}
