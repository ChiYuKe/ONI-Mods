using System.Collections.Generic;
using System.Linq;
using System;
using KSerialization;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkMaterialRequester : KMonoBehaviour, ISim1000ms
    {
        public enum RequestMode
        {
            SearchNetwork = 0,
            SpecificStorage = 1
        }

        public enum OutputStoreMode
        {
            AutoNetwork = 0,
            SpecificStorage = 1
        }

        [Serialize]
        public bool RequestEnabled;

        [Serialize]
        public int Mode;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public bool LimitEnabled;

        [Serialize]
        public float LimitKg = 1000f;

        [Serialize]
        public float RequestedKg;

        [Serialize]
        public bool OutputStoreEnabled;

        [Serialize]
        public int OutputStoreModeValue;

        [Serialize]
        public int OutputStorageInstanceId = KPrefabID.InvalidInstanceID;

        [MyCmpGet]
        private ComplexFabricator fabricator;

        private static StatusItem materialRequestStatusItem;
        private Guid materialRequestStatusHandle = Guid.Empty;
        private float requestCooldown;
        private string lastStatus;
        private string lastOutputStatus;

        public string LastStatus => lastStatus;
        public string LastOutputStatus => lastOutputStatus;

        public RequestMode CurrentMode
        {
            get => (RequestMode)Mathf.Clamp(Mode, 0, 1);
            set => Mode = (int)value;
        }

        public OutputStoreMode CurrentOutputStoreMode
        {
            get => (OutputStoreMode)Mathf.Clamp(OutputStoreModeValue, 0, 1);
            set => OutputStoreModeValue = (int)value;
        }

        public void Sim1000ms(float dt)
        {
            EnsureFabricator();
            if (fabricator == null)
            {
                lastStatus = string.Empty;
                lastOutputStatus = string.Empty;
                RemoveMaterialRequestStatus();
                return;
            }

            StoreOutputsToNetwork();

            if (!RequestEnabled || fabricator.inStorage == null)
            {
                RemoveMaterialRequestStatus();
                return;
            }

            RefreshMaterialRequestStatus();
            if (requestCooldown > 0f)
            {
                requestCooldown -= dt;
                return;
            }

            ComplexRecipe recipe = GetRecipeToRequest();
            if (recipe == null || recipe.ingredients == null)
            {
                lastStatus = "没有可请求材料的排队配方";
                requestCooldown = 5f;
                return;
            }

            float remainingLimit = LimitEnabled ? Mathf.Max(0f, LimitKg - RequestedKg) : float.MaxValue;
            if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = "已达到请求限额";
                requestCooldown = 2f;
                return;
            }

            bool requestedAny = false;
            bool movedAny = false;
            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float targetAmount = GetTargetIngredientAmount(recipe, ingredient);
                float availableInBuilding = GetAmountAvailableInFabricator(ingredient.material);
                float missing = Mathf.Max(0f, targetAmount - availableInBuilding);
                if (missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                requestedAny = true;
                float moved = RequestIngredient(ingredient.material, Mathf.Min(missing, remainingLimit));
                if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    lastStatus = string.Format("缺少 {0}，网络中没有可用来源", GetTagDisplayName(ingredient.material));
                    requestCooldown = 5f;
                    break;
                }

                movedAny = true;
                RequestedKg += moved;
                lastStatus = string.Format("已请求 {0} {1}", GameUtil.GetFormattedMass(moved), GetTagDisplayName(ingredient.material));
                remainingLimit -= moved;
            }

            if (!requestedAny)
            {
                lastStatus = "当前配方材料已满足";
                requestCooldown = 2f;
            }
            else if (!movedAny && requestCooldown <= 0f)
            {
                requestCooldown = 5f;
            }
        }

        public Storage ResolveSourceStorage()
        {
            if (SourceStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            return StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .FirstOrDefault(storage => GetStorageInstanceId(storage) == SourceStorageInstanceId);
        }

        public Storage ResolveOutputStorage()
        {
            if (OutputStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            return StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .FirstOrDefault(storage => GetStorageInstanceId(storage) == OutputStorageInstanceId);
        }

        public void SetSourceStorage(Storage storage)
        {
            SourceStorageInstanceId = GetStorageInstanceId(storage);
        }

        public void SetOutputStorage(Storage storage)
        {
            OutputStorageInstanceId = GetStorageInstanceId(storage);
        }

        public float GetRequestedAmountForDisplay()
        {
            return RequestedKg;
        }

        public void ResetRequestedAmount()
        {
            RequestedKg = 0f;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureFabricator();
        }

        protected override void OnCleanUp()
        {
            RemoveMaterialRequestStatus();
            base.OnCleanUp();
        }

        public void ForceStoreProducedOutputs(IEnumerable<GameObject> producedOutputs)
        {
            EnsureFabricator();
            if (!OutputStoreEnabled)
            {
                lastOutputStatus = string.Empty;
                return;
            }

            float totalMoved = 0f;
            string lastBlockedItem = null;
            if (producedOutputs != null)
            {
                foreach (GameObject output in producedOutputs.Where(output => output != null).ToList())
                {
                    if (TryStoreLooseOutput(output, out float moved, out string blockedItem))
                    {
                        totalMoved += moved;
                    }
                    else if (!string.IsNullOrEmpty(blockedItem))
                    {
                        lastBlockedItem = blockedItem;
                    }
                }
            }

            totalMoved += StoreOutputsFromOutputStorage(out string outputStorageBlockedItem);
            if (!string.IsNullOrEmpty(outputStorageBlockedItem))
            {
                lastBlockedItem = outputStorageBlockedItem;
            }

            UpdateOutputStatus(totalMoved, lastBlockedItem, "等待下一批成品");
        }

        private void RefreshMaterialRequestStatus()
        {
            if (materialRequestStatusHandle != Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                materialRequestStatusHandle = selectable.AddStatusItem(GetMaterialRequestStatusItem(), this);
            }
        }

        private void RemoveMaterialRequestStatus()
        {
            if (materialRequestStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(materialRequestStatusHandle);
            }

            materialRequestStatusHandle = Guid.Empty;
        }

        private static StatusItem GetMaterialRequestStatusItem()
        {
            if (materialRequestStatusItem != null)
            {
                return materialRequestStatusItem;
            }

            materialRequestStatusItem = new StatusItem(
                "StorageNetworkMaterialRequest",
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Good,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            materialRequestStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkMaterialRequester requester = data as StorageNetworkMaterialRequester;
                string status = requester != null && !string.IsNullOrEmpty(requester.LastStatus)
                    ? requester.LastStatus
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_ITEM);
                return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS_TOOLTIP), status);
            };

            return materialRequestStatusItem;
        }

        private ComplexRecipe GetRecipeToRequest()
        {
            if (fabricator.CurrentWorkingOrder != null)
            {
                return fabricator.CurrentWorkingOrder;
            }

            if (fabricator.NextOrder != null)
            {
                return fabricator.NextOrder;
            }

            return fabricator.GetRecipes()
                .Where(recipe => recipe != null && fabricator.IsRecipeQueued(recipe))
                .OrderByDescending(recipe => fabricator.GetRecipeQueueCount(recipe) == ComplexFabricator.QUEUE_INFINITE)
                .ThenBy(recipe => recipe.GetUIName(false))
                .FirstOrDefault(NeedsAnyIngredient);
        }

        private bool NeedsAnyIngredient(ComplexRecipe recipe)
        {
            return recipe.ingredients != null &&
                   recipe.ingredients.Any(ingredient => GetTargetIngredientAmount(recipe, ingredient) - GetAmountAvailableInFabricator(ingredient.material) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT);
        }

        private float GetTargetIngredientAmount(ComplexRecipe recipe, ComplexRecipe.RecipeElement ingredient)
        {
            return ingredient.amount * GetRequestOrderCount(recipe);
        }

        private int GetRequestOrderCount(ComplexRecipe recipe)
        {
            if (recipe == null || fabricator == null)
            {
                return 1;
            }

            int count = 0;
            if (fabricator.IsRecipeQueued(recipe))
            {
                count = fabricator.GetRecipeQueueCount(recipe);
                if (count == ComplexFabricator.QUEUE_INFINITE)
                {
                    count = 2;
                }
            }

            if (fabricator.CurrentWorkingOrder == recipe || fabricator.NextOrder == recipe)
            {
                count = Mathf.Max(count, 1);
            }

            return Mathf.Clamp(count, 1, 99);
        }

        private float RequestIngredient(Tag tag, float amount)
        {
            float moved = 0f;
            foreach (Storage source in GetSourceStorages(tag))
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float sourceAmount = source.GetAmountAvailable(tag);
                if (sourceAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                float transferAmount = Mathf.Min(amount - moved, sourceAmount, Mathf.Max(0f, fabricator.inStorage.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float transferred = source.Transfer(fabricator.inStorage, tag, transferAmount, block_events: false, hide_popups: true);
                moved += transferred;
                if (transferred > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    Debug.Log(string.Format(
                        "[StorageNetworkMaterialRequester] Moved {0} of {1} from {2} to {3}.",
                        GameUtil.GetFormattedMass(transferred),
                        tag,
                        source.GetProperName(),
                        gameObject.GetProperName()));
                }
            }

            return moved;
        }

        private void StoreOutputsToNetwork()
        {
            if (!OutputStoreEnabled)
            {
                lastOutputStatus = string.Empty;
                return;
            }

            float totalMoved = StoreOutputsFromOutputStorage(out string lastBlockedItem);
            UpdateOutputStatus(totalMoved, lastBlockedItem, "等待成品进入输出栏");
        }

        private float StoreOutputsFromOutputStorage(out string lastBlockedItem)
        {
            lastBlockedItem = null;
            EnsureFabricator();
            if (fabricator.outStorage == null || fabricator.outStorage.items == null)
            {
                lastOutputStatus = "建筑没有可读取的输出栏";
                return 0f;
            }

            List<GameObject> outputs = fabricator.outStorage.items
                .Where(item => item != null)
                .ToList();
            if (outputs.Count == 0)
            {
                return 0f;
            }

            float totalMoved = 0f;
            foreach (GameObject output in outputs)
            {
                PrimaryElement primaryElement = output.GetComponent<PrimaryElement>();
                if (primaryElement == null || primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                Tag tag = GetStorageTransferTag(output);
                float remaining = primaryElement.Mass;
                Storage target = FindOutputTarget(output);
                if (target == null)
                {
                    lastBlockedItem = GetTagDisplayName(tag);
                    continue;
                }

                while (target != null && remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    float transferAmount = Mathf.Min(remaining, Mathf.Max(0f, target.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float moved = fabricator.outStorage.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                    if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    totalMoved += moved;
                    remaining -= moved;
                    target = FindOutputTarget(output);
                }

                if (remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    lastBlockedItem = GetTagDisplayName(tag);
                }
            }

            return totalMoved;
        }

        private bool TryStoreLooseOutput(GameObject output, out float moved, out string blockedItem)
        {
            moved = 0f;
            blockedItem = null;
            if (output == null)
            {
                return false;
            }

            PrimaryElement primaryElement = output.GetComponent<PrimaryElement>();
            if (primaryElement == null || primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            Tag tag = GetStorageTransferTag(output);
            Storage target = FindOutputTarget(output);
            if (target == null)
            {
                blockedItem = GetTagDisplayName(tag);
                return false;
            }

            Pickupable pickupable = output.GetComponent<Pickupable>();
            if (pickupable == null)
            {
                blockedItem = GetTagDisplayName(tag);
                return false;
            }

            while (target != null && output != null && primaryElement.Mass > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                float transferAmount = Mathf.Min(primaryElement.Mass, Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                Pickupable taken = pickupable.Take(transferAmount);
                if (taken == null)
                {
                    break;
                }

                float takenMass = GetMass(taken.gameObject);
                if (takenMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                target.Store(taken.gameObject, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                moved += takenMass;
                if (taken.gameObject == output)
                {
                    break;
                }

                target = FindOutputTarget(output);
            }

            if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                blockedItem = GetTagDisplayName(tag);
            }

            return moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private void UpdateOutputStatus(float totalMoved, string lastBlockedItem, string idleText)
        {
            if (totalMoved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastOutputStatus = string.Format("已入网 {0}", GameUtil.GetFormattedMass(totalMoved));
            }
            else if (!string.IsNullOrEmpty(lastBlockedItem))
            {
                lastOutputStatus = string.Format("无法存入 {0}：没有匹配箱子或容量不足", lastBlockedItem);
            }
            else
            {
                lastOutputStatus = idleText;
            }
        }

        private Storage FindOutputTarget(GameObject output)
        {
            Tag tag = GetStorageTransferTag(output);
            if (CurrentOutputStoreMode == OutputStoreMode.SpecificStorage)
            {
                Storage target = ResolveOutputStorage();
                return IsUsableOutputTarget(target, output) ? target : null;
            }

            return StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .Where(storage => IsUsableOutputTarget(storage, output))
                .Where(storage => IsAutoOutputMatch(storage, output))
                .OrderByDescending(storage => storage.GetAmountAvailable(tag))
                .ThenByDescending(storage => IsFilterAccepting(storage, tag))
                .ThenByDescending(storage => storage.RemainingCapacity())
                .FirstOrDefault();
        }

        private bool IsUsableOutputTarget(Storage storage, GameObject output)
        {
            return storage != null &&
                   storage != fabricator.inStorage &&
                   storage != fabricator.buildStorage &&
                   storage != fabricator.outStorage &&
                   storage.GetComponent<ComplexFabricator>() == null &&
                   storage.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static bool IsAutoOutputMatch(Storage storage, GameObject output)
        {
            Tag tag = GetStorageTransferTag(output);
            return storage.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                   IsFilterAccepting(storage, tag) ||
                   HasNoExplicitStorageFilter(storage);
        }

        private static bool IsFilterAccepting(Storage storage, Tag tag)
        {
            TreeFilterable filterable = storage != null ? storage.GetComponent<TreeFilterable>() : null;
            return filterable != null && filterable.ContainsTag(tag);
        }

        private static bool HasNoExplicitStorageFilter(Storage storage)
        {
            TreeFilterable filterable = storage != null ? storage.GetComponent<TreeFilterable>() : null;
            return filterable == null || filterable.GetTags() == null || filterable.GetTags().Count == 0;
        }

        private IEnumerable<Storage> GetSourceStorages(Tag tag)
        {
            if (CurrentMode == RequestMode.SpecificStorage)
            {
                Storage source = ResolveSourceStorage();
                if (IsUsableSource(source, tag))
                {
                    yield return source;
                }

                yield break;
            }

            foreach (Storage storage in StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .Where(storage => IsUsableSource(storage, tag))
                .OrderByDescending(storage => storage.GetAmountAvailable(tag)))
            {
                yield return storage;
            }
        }

        private bool IsUsableSource(Storage storage, Tag tag)
        {
            return storage != null &&
                   storage != fabricator.inStorage &&
                   storage != fabricator.buildStorage &&
                   storage != fabricator.outStorage &&
                   storage.GetComponent<ComplexFabricator>() == null &&
                   storage.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private float GetAmountAvailableInFabricator(Tag tag)
        {
            return GetAmountAvailable(fabricator.inStorage, tag) +
                   GetAmountAvailable(fabricator.buildStorage, tag);
        }

        private static float GetAmountAvailable(Storage storage, Tag tag)
        {
            return storage != null ? storage.GetAmountAvailable(tag) : 0f;
        }

        private void EnsureFabricator()
        {
            if (fabricator == null)
            {
                fabricator = GetComponent<ComplexFabricator>();
            }
        }

        public static Tag GetStorageTransferTag(GameObject item)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            if (primaryElement != null)
            {
                Tag elementTag = primaryElement.ElementID.CreateTag();
                if (elementTag != Tag.Invalid && item.HasTag(elementTag))
                {
                    return elementTag;
                }
            }

            KPrefabID prefabID = item != null ? item.GetComponent<KPrefabID>() : null;
            return prefabID != null ? prefabID.PrefabTag : Tag.Invalid;
        }

        public static bool MatchesStorageTag(GameObject item, Tag tag)
        {
            return item != null && tag != Tag.Invalid && item.HasTag(tag);
        }

        private static float GetMass(GameObject item)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            return primaryElement != null ? primaryElement.Mass : 0f;
        }

        public static int GetStorageInstanceId(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private static string GetTagDisplayName(Tag tag)
        {
            Element element = ElementLoader.FindElementByHash((SimHashes)tag.GetHash());
            if (element != null && !string.IsNullOrEmpty(element.name))
            {
                return element.name;
            }

            GameObject prefab = Assets.GetPrefab(tag);
            if (prefab != null)
            {
                return prefab.GetProperName();
            }

            string key = "STRINGS.MISC.TAGS." + tag.Name.ToUpperInvariant();
            if (Strings.TryGet(key, out StringEntry entry) && entry != null && !string.IsNullOrEmpty(entry.String))
            {
                return entry.String;
            }

            return tag.Name;
        }
    }
}
