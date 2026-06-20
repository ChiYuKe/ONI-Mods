using System.Collections.Generic;
using System.Linq;
using KSerialization;
using StorageNetwork.API;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkOrderProductionCenter : KMonoBehaviour
    {
        [Serialize]
        private List<string> engravedRecipeIds = new List<string>();

        [Serialize]
        private List<EngravingDiskSlot> diskSlots = new List<EngravingDiskSlot>();

        [MyCmpGet]
        private StorageNetworkOrderProductionCenterFabricator fabricator = null;

        private Storage diskInstallStorage = null;

        private readonly List<PendingDiskInstall> pendingDiskInstalls = new List<PendingDiskInstall>();

        public IReadOnlyList<string> EngravedRecipeIds => GetEngravedRecipeIds();

        public IReadOnlyList<EngravingDiskSlot> DiskSlots => diskSlots;

        public bool HasBlankDiskSlot => FindBlankDiskSlot() != null;

        public bool HasPendingDiskInstall => pendingDiskInstalls.Count > 0;

        public float CurrentPowerWatts => GetPowerWattsForCoreCount(fabricator != null ? fabricator.ActiveCoreCount : 0);

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ProductionOrderCenterCatalog.Register(this);
            RemoveDynamicRecipeUnsafeStatusManager();
            EnsureDiskSlots();
            StorageNetworkOrderProductionCenterStorageHelper.RestoreFabricatorStorageCapacity(fabricator);
            diskInstallStorage = StorageNetworkOrderProductionCenterStorageHelper.GetDiskInstallStorage(gameObject);
            gameObject.AddOrGet<StorageNetworkOrderProductionCenterDiskInstallWorkable>();
            MigrateLegacyRecipesToDisk();
            RefreshFabricatorRecipes();
            RefreshDiskMeter();
            RefreshPowerDemand();
        }

        protected override void OnCleanUp()
        {
            ProductionOrderCenterCatalog.Unregister(this);
            StorageNetworkOrderProductionCenterEngraveTool.CancelIfOwner(this);
            base.OnCleanUp();
        }

        public void BeginEngraving()
        {
            if (!HasBlankDiskSlot)
            {
                StorageNetworkNotifications.ShowWarning(gameObject, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_NO_DISK));
                return;
            }

            StorageNetworkOrderProductionCenterEngraveTool.Begin(this);
        }

        public bool TryEngraveFrom(GameObject target, out string message)
        {
            message = string.Empty;
            ComplexFabricator source = target != null ? target.GetComponent<ComplexFabricator>() : null;
            if (source == null || source == fabricator)
            {
                message = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_NO_RECIPES);
                return false;
            }

            List<ComplexRecipe> sourceRecipes = source.GetRecipes()
                .Where(recipe => recipe != null && !string.IsNullOrEmpty(recipe.id))
                .ToList();
            if (sourceRecipes.Count == 0)
            {
                message = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_NO_RECIPES);
                return false;
            }

            List<string> newRecipeIds = sourceRecipes
                .Select(recipe => recipe.id)
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
            if (newRecipeIds.Count == 0)
            {
                message = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_DUPLICATE);
                return false;
            }

            EngravingDiskSlot targetSlot = FindBlankDiskSlot();
            if (targetSlot == null)
            {
                message = Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_NO_DISK);
                return false;
            }

            targetSlot.RecipeIds.AddRange(newRecipeIds);
            targetSlot.DeduplicateRecipeIds();
            RefreshFabricatorRecipes();
            DropSourceContents(source);
            DestroySourceBuilding(target);
            RefreshPowerDemand();
            message = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_SUCCESS), newRecipeIds.Count);
            return true;
        }

        public bool InsertDiskFromWorld()
        {
            EnsureDiskSlots();
            int slotIndex = diskSlots.FindIndex(item => item != null && !item.HasDisk);
            return InsertDiskFromWorld(slotIndex);
        }

        public bool InsertDiskFromWorld(int slotIndex)
        {
            EnsureDiskSlots();
            if (slotIndex < 0 || slotIndex >= diskSlots.Count)
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[slotIndex];
            if (slot == null || slot.HasDisk)
            {
                return false;
            }

            StorageNetworkEngravingDisk disk = FindNearestLooseDisk();
            if (disk == null)
            {
                return false;
            }

            slot.HasDisk = true;
            slot.RecipeIds = new List<string>(disk.EngravedRecipeIds ?? new List<string>());
            slot.DeduplicateRecipeIds();
            Util.KDestroyGameObject(disk.gameObject);
            RefreshFabricatorRecipes();
            RefreshDiskMeter();
            RefreshPowerDemand();
            return true;
        }

        public bool QueueDiskInstall(int slotIndex, StorageNetworkEngravingDisk disk)
        {
            EnsureDiskSlots();
            if (disk == null || slotIndex < 0 || slotIndex >= diskSlots.Count)
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[slotIndex];
            if (slot == null || slot.HasDisk)
            {
                return false;
            }

            if (IsSlotPendingDiskInstall(slotIndex) || IsDiskPendingInstall(disk))
            {
                return false;
            }

            Pickupable pickupable = disk.GetComponent<Pickupable>();
            if (pickupable == null)
            {
                return false;
            }

            if (!PrepareDiskForMinionPickup(disk, pickupable))
            {
                return false;
            }

            PendingDiskInstall pending = new PendingDiskInstall(slotIndex, disk);
            pendingDiskInstalls.Add(pending);
            KPrefabID prefabID = disk.GetComponent<KPrefabID>();
            prefabID?.AddTag(StorageNetworkTags.SelectedEngravingDisk, false);
            prefabID?.AddTag(pending.RequiredTag, false);
            gameObject.AddOrGet<StorageNetworkOrderProductionCenterDiskInstallWorkable>()?.CreateInstallChore();
            return true;
        }

        private bool PrepareDiskForMinionPickup(StorageNetworkEngravingDisk disk, Pickupable pickupable)
        {
            if (disk == null || pickupable == null)
            {
                return false;
            }

            Storage sourceStorage = pickupable.storage;
            if (sourceStorage == null)
            {
                return true;
            }

            if (!StorageNetworkStorageRules.IsServerStorage(sourceStorage))
            {
                return true;
            }

            Storage outputPort = FindDiskDeliveryOutputPort(sourceStorage);
            if (outputPort == null)
            {
                return false;
            }

            return sourceStorage.Transfer(disk.gameObject, outputPort, block_events: false, hide_popups: true);
        }

        private Storage FindDiskDeliveryOutputPort(Storage sourceStorage)
        {
            int worldId = StorageTargetSelector.GetObjectWorldId(gameObject);
            foreach (Storage storage in StorageSceneCollector.CollectLightweightForWorld(worldId).Storages)
            {
                if (storage == null ||
                    storage == sourceStorage ||
                    !StorageNetworkStorageRules.IsSolidOutputPort(storage) ||
                    storage.RemainingCapacity() <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                StorageNetworkSolidOutputPortEgress egress = storage.GetComponent<StorageNetworkSolidOutputPortEgress>();
                if (egress == null || !egress.AllowManualOperation)
                {
                    continue;
                }

                Tag selected = egress.GetSelectedOutputTag() ?? Tag.Invalid;
                if (selected != Tag.Invalid && selected != StorageNetworkEngravingDiskConfig.ID)
                {
                    continue;
                }

                return storage;
            }

            return null;
        }

        public bool CompleteDeliveredDiskInstall()
        {
            if (!HasPendingDiskInstall)
            {
                return false;
            }

            StorageNetworkEngravingDisk disk = FindDeliveredPendingDisk();
            PendingDiskInstall pending = FindPendingDiskInstall(disk);
            if (pending == null)
            {
                return false;
            }

            int slotIndex = pending.SlotIndex;
            pendingDiskInstalls.Remove(pending);
            RemovePendingTags(pending);
            return InsertSpecificDisk(slotIndex, disk);
        }

        public void ClearPendingDiskInstall()
        {
            foreach (PendingDiskInstall pending in pendingDiskInstalls)
            {
                RemovePendingTags(pending);
            }

            pendingDiskInstalls.Clear();
        }

        public bool CancelPendingDiskInstall(int slotIndex)
        {
            PendingDiskInstall pending = pendingDiskInstalls.FirstOrDefault(item => item != null && item.SlotIndex == slotIndex);
            if (pending == null)
            {
                return false;
            }

            RemovePendingTags(pending);
            pendingDiskInstalls.Remove(pending);
            return true;
        }

        public bool IsQueuedDiskStillValid()
        {
            PruneInvalidPendingDiskInstalls();
            return HasPendingDiskInstall;
        }

        public IReadOnlyList<StorageNetworkEngravingDisk> GetPendingInstallDisks()
        {
            PruneInvalidPendingDiskInstalls();
            return pendingDiskInstalls
                .Select(pending => pending.Disk)
                .Where(disk => disk != null)
                .ToList();
        }

        public IReadOnlyList<Tag> GetPendingInstallRequiredTags()
        {
            PruneInvalidPendingDiskInstalls();
            return pendingDiskInstalls
                .Select(pending => pending.RequiredTag)
                .ToList();
        }

        public bool IsSlotWaitingForDiskDelivery(int slotIndex)
        {
            PruneInvalidPendingDiskInstalls();
            return IsSlotPendingDiskInstall(slotIndex);
        }

        private bool IsPendingDiskInstallValid(PendingDiskInstall pending)
        {
            if (pending == null || pending.Disk == null)
            {
                return false;
            }

            EnsureDiskSlots();
            if (pending.SlotIndex < 0 || pending.SlotIndex >= diskSlots.Count)
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[pending.SlotIndex];
            if (slot == null || slot.HasDisk)
            {
                return false;
            }

            Pickupable pickupable = pending.Disk.GetComponent<Pickupable>();
            return pickupable != null;
        }

        private void PruneInvalidPendingDiskInstalls()
        {
            if (!HasPendingDiskInstall)
            {
                return;
            }

            for (int i = pendingDiskInstalls.Count - 1; i >= 0; i--)
            {
                PendingDiskInstall pending = pendingDiskInstalls[i];
                if (IsPendingDiskInstallValid(pending))
                {
                    continue;
                }

                RemovePendingTags(pending);
                pendingDiskInstalls.RemoveAt(i);
            }
        }

        private bool IsSlotPendingDiskInstall(int slotIndex)
        {
            return pendingDiskInstalls.Any(pending => pending != null && pending.SlotIndex == slotIndex);
        }

        private bool IsDiskPendingInstall(StorageNetworkEngravingDisk disk)
        {
            if (disk == null)
            {
                return false;
            }

            return pendingDiskInstalls.Any(pending => pending != null && pending.Disk == disk);
        }

        public bool EjectDisk(int slotIndex)
        {
            EnsureDiskSlots();
            if (slotIndex < 0 || slotIndex >= diskSlots.Count || !diskSlots[slotIndex].HasDisk)
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[slotIndex];
            GameObject diskPrefab = Assets.GetPrefab(StorageNetworkEngravingDiskConfig.ID);
            if (diskPrefab == null)
            {
                Debug.LogWarning("[StorageNetwork] Engraving disk prefab is not registered; cannot eject disk.");
                return false;
            }

            GameObject diskObject = GameUtil.KInstantiate(diskPrefab, transform.GetPosition(), Grid.SceneLayer.Ore);
            if (diskObject != null)
            {
                diskObject.SetActive(true);
                diskObject.GetComponent<StorageNetworkEngravingDisk>()?.SetRecipeIds(slot.RecipeIds);
            }

            slot.Clear();
            RefreshFabricatorRecipes();
            RefreshDiskMeter();
            RefreshPowerDemand();
            return true;
        }

        public bool RemoveRecipeFromDiskSlot(int slotIndex, string recipeId)
        {
            EnsureDiskSlots();
            if (slotIndex < 0 || slotIndex >= diskSlots.Count || string.IsNullOrEmpty(recipeId))
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[slotIndex];
            if (slot == null || !slot.HasDisk || slot.RecipeIds == null)
            {
                return false;
            }

            bool removed = slot.RecipeIds.RemoveAll(id => id == recipeId) > 0;
            if (!removed)
            {
                return false;
            }

            slot.DeduplicateRecipeIds();
            RefreshFabricatorRecipes();
            return true;
        }

        private List<string> GetEngravedRecipeIds()
        {
            EnsureDiskSlots();
            return diskSlots
                .Where(slot => slot != null && slot.HasDisk)
                .SelectMany(slot => slot.RecipeIds ?? new List<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
        }

        private void RefreshFabricatorRecipes()
        {
            if (fabricator != null)
            {
                fabricator.SetEngravedRecipeIds(EngravedRecipeIds);
            }
        }

        private void EnsureDiskSlots()
        {
            if (diskSlots == null)
            {
                diskSlots = new List<EngravingDiskSlot>();
            }

            while (diskSlots.Count < 3)
            {
                diskSlots.Add(new EngravingDiskSlot());
            }

            if (diskSlots.Count > 3)
            {
                diskSlots = diskSlots.Take(3).ToList();
            }

            foreach (EngravingDiskSlot slot in diskSlots)
            {
                slot?.DeduplicateRecipeIds();
            }
        }

        private void MigrateLegacyRecipesToDisk()
        {
            if (engravedRecipeIds == null || engravedRecipeIds.Count == 0)
            {
                return;
            }

            EngravingDiskSlot slot = diskSlots.FirstOrDefault(item => item != null && item.HasDisk) ?? diskSlots[0];
            slot.HasDisk = true;
            slot.RecipeIds.AddRange(engravedRecipeIds);
            slot.DeduplicateRecipeIds();
            engravedRecipeIds.Clear();
            RefreshDiskMeter();
        }

        private void RefreshDiskMeter()
        {
            gameObject.AddOrGet<StorageNetworkOrderProductionCenterController>()?.RefreshDiskMeter();
        }

        private EngravingDiskSlot FindBlankDiskSlot()
        {
            EnsureDiskSlots();
            return diskSlots.FirstOrDefault(slot => slot != null && slot.HasDisk && (slot.RecipeIds == null || slot.RecipeIds.Count == 0));
        }

        private StorageNetworkEngravingDisk FindNearestLooseDisk()
        {
            StorageNetworkEngravingDisk best = null;
            float bestDistance = float.MaxValue;
            foreach (StorageNetworkEngravingDisk disk in FindAvailableDisks())
            {
                float distance = Vector3.SqrMagnitude(disk.transform.GetPosition() - transform.GetPosition());
                if (distance < bestDistance)
                {
                    best = disk;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private bool InsertSpecificDisk(int slotIndex, StorageNetworkEngravingDisk disk)
        {
            EnsureDiskSlots();
            if (disk == null || slotIndex < 0 || slotIndex >= diskSlots.Count)
            {
                return false;
            }

            EngravingDiskSlot slot = diskSlots[slotIndex];
            if (slot == null || slot.HasDisk)
            {
                return false;
            }

            Pickupable pickupable = disk.GetComponent<Pickupable>();
            if (pickupable == null)
            {
                return false;
            }

            Storage sourceStorage = pickupable.storage;
            if (sourceStorage != null)
            {
                sourceStorage.Drop(disk.gameObject, true);
            }

            slot.HasDisk = true;
            slot.RecipeIds = new List<string>(disk.EngravedRecipeIds ?? new List<string>());
            slot.DeduplicateRecipeIds();
            disk.GetComponent<KPrefabID>()?.RemoveTag(StorageNetworkTags.SelectedEngravingDisk);
            Util.KDestroyGameObject(disk.gameObject);
            RefreshFabricatorRecipes();
            RefreshDiskMeter();
            RefreshPowerDemand();
            return true;
        }

        private void RefreshPowerDemand()
        {
            EnergyConsumer energyConsumer = GetComponent<EnergyConsumer>();
            if (energyConsumer == null)
            {
                return;
            }

            float watts = CurrentPowerWatts;
            energyConsumer.BaseWattageRating = watts;
            Building building = GetComponent<Building>();
            if (building?.Def != null)
            {
                building.Def.EnergyConsumptionWhenActive = watts;
            }
        }

        private static float GetPowerWattsForCoreCount(int coreCount)
        {
            int extraCoreCount = Mathf.Max(0, coreCount - 1);
            return StorageNetworkOrderProductionCenterConfig.BasePowerWatts +
                extraCoreCount * StorageNetworkOrderProductionCenterConfig.ExtraCorePowerWatts;
        }

        private StorageNetworkEngravingDisk FindDeliveredPendingDisk()
        {
            if (diskInstallStorage == null || diskInstallStorage.items == null)
            {
                return null;
            }

            foreach (GameObject item in diskInstallStorage.items.ToList())
            {
                StorageNetworkEngravingDisk disk = item != null ? item.GetComponent<StorageNetworkEngravingDisk>() : null;
                KPrefabID prefabID = item != null ? item.GetComponent<KPrefabID>() : null;
                PendingDiskInstall pending = FindPendingDiskInstall(disk);
                if (disk != null && prefabID != null && pending != null && prefabID.HasTag(pending.RequiredTag))
                {
                    return disk;
                }
            }

            return null;
        }

        private PendingDiskInstall FindPendingDiskInstall(StorageNetworkEngravingDisk disk)
        {
            if (disk == null)
            {
                return null;
            }

            return pendingDiskInstalls.FirstOrDefault(pending => pending != null && pending.Disk == disk);
        }

        private static void RemovePendingTags(PendingDiskInstall pending)
        {
            KPrefabID prefabID = pending?.Disk?.GetComponent<KPrefabID>();
            if (prefabID == null)
            {
                return;
            }

            prefabID.RemoveTag(StorageNetworkTags.SelectedEngravingDisk);
            prefabID.RemoveTag(pending.RequiredTag);
        }

        public static List<StorageNetworkEngravingDisk> FindLooseDisks()
        {
            return Object.FindObjectsByType<StorageNetworkEngravingDisk>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(disk =>
                {
                    if (disk == null)
                    {
                        return false;
                    }

                    Pickupable pickupable = disk.GetComponent<Pickupable>();
                    KPrefabID prefabID = disk.GetComponent<KPrefabID>();
                    return pickupable != null &&
                        pickupable.storage == null &&
                        prefabID?.HasTag(StorageNetworkTags.SelectedEngravingDisk) != true;
                })
                .OrderBy(disk => disk.GetProperName())
                .ToList();
        }

        public static List<StorageNetworkEngravingDisk> FindAvailableDisks()
        {
            Dictionary<int, StorageNetworkEngravingDisk> disks = new Dictionary<int, StorageNetworkEngravingDisk>();
            foreach (StorageNetworkEngravingDisk disk in FindLooseDisks())
            {
                AddAvailableDisk(disks, disk);
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                foreach (GameObject item in info.StoredItems ?? Enumerable.Empty<GameObject>())
                {
                    StorageNetworkEngravingDisk disk = item != null ? item.GetComponent<StorageNetworkEngravingDisk>() : null;
                    AddAvailableDisk(disks, disk);
                }
            }

            return disks.Values.OrderBy(disk => disk.GetProperName()).ToList();
        }

        private static void AddAvailableDisk(Dictionary<int, StorageNetworkEngravingDisk> disks, StorageNetworkEngravingDisk disk)
        {
            if (disk == null)
            {
                return;
            }

            Pickupable pickupable = disk.GetComponent<Pickupable>();
            KPrefabID prefabID = disk.GetComponent<KPrefabID>();
            if (pickupable == null || prefabID?.HasTag(StorageNetworkTags.SelectedEngravingDisk) == true)
            {
                return;
            }

            disks[disk.GetInstanceID()] = disk;
        }

        private void RemoveDynamicRecipeUnsafeStatusManager()
        {
            FabricatorIngredientStatusManager statusManager = GetComponent<FabricatorIngredientStatusManager>();
            if (statusManager != null)
            {
                Destroy(statusManager);
            }
        }

        private static void DropSourceContents(ComplexFabricator source)
        {
            source.inStorage?.DropAll(false, false);
            source.buildStorage?.DropAll(false, false);
            source.outStorage?.DropAll(false, false);
        }

        private static void DestroySourceBuilding(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Vector3 buildingPosition = target.transform.GetPosition();
            Game.Instance.SpawnFX(
                SpawnFXHashes.MeteorImpactMetal,
                buildingPosition,
                0f);

            Deconstructable deconstructable = target != null ? target.GetComponent<Deconstructable>() : null;
            if (deconstructable != null)
            {
                deconstructable.ForceDestroyAndGetMaterials();
                return;
            }

            Util.KDestroyGameObject(target);
        }

        [SerializationConfig(MemberSerialization.OptIn)]
        public sealed class EngravingDiskSlot
        {
            [Serialize]
            public bool HasDisk;

            [Serialize]
            public List<string> RecipeIds = new List<string>();

            public void Clear()
            {
                HasDisk = false;
                RecipeIds.Clear();
            }

            public void DeduplicateRecipeIds()
            {
                RecipeIds = (RecipeIds ?? new List<string>())
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Distinct()
                    .ToList();
            }
        }

        private sealed class PendingDiskInstall
        {
            public readonly int SlotIndex;
            public readonly StorageNetworkEngravingDisk Disk;
            public readonly Tag RequiredTag;

            public PendingDiskInstall(int slotIndex, StorageNetworkEngravingDisk disk)
            {
                SlotIndex = slotIndex;
                Disk = disk;
                RequiredTag = new Tag(string.Format("{0}_{1}", StorageNetworkTags.SelectedEngravingDiskTagName, disk.GetInstanceID()));
            }
        }
    }
}
