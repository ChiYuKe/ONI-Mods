using System.Collections.Generic;
using System.Linq;
using System;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 生产建筑材料请求与成品入网组件。挂在 ComplexFabricator 上，按队列从储存网络调拨材料。
    /// </summary>
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
        public float LimitKg = Config.Instance.DefaultMaterialRequestLimitKg;

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

        /// <summary>
        /// 当前材料请求模式，封装序列化 int，避免 UI 直接处理魔法数字。
        /// </summary>
        public RequestMode CurrentMode
        {
            get => (RequestMode)Mathf.Clamp(Mode, 0, 1);
            set => Mode = (int)value;
        }

        /// <summary>
        /// 当前成品入网模式，封装序列化 int，避免 UI 直接处理魔法数字。
        /// </summary>
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
                requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
                return;
            }

            float remainingLimit = LimitEnabled ? Mathf.Max(0f, LimitKg - RequestedKg) : float.MaxValue;
            if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = "已达到请求限额";
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
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
                    requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
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
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
            }
            else if (!movedAny && requestCooldown <= 0f)
            {
                requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
            }
        }

        /// <summary>
        /// 解析当前指定的材料来源箱子。
        /// </summary>
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

        /// <summary>
        /// 解析当前指定的成品入网目标箱子。
        /// </summary>
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

        /// <summary>
        /// 设置固定材料来源箱子。
        /// </summary>
        public void SetSourceStorage(Storage storage)
        {
            SourceStorageInstanceId = GetStorageInstanceId(storage);
            CurrentMode = RequestMode.SpecificStorage;
        }

        /// <summary>
        /// 设置固定成品入网目标箱子。
        /// </summary>
        public void SetOutputStorage(Storage storage)
        {
            OutputStorageInstanceId = GetStorageInstanceId(storage);
            CurrentOutputStoreMode = OutputStoreMode.SpecificStorage;
        }

        /// <summary>
        /// 切换为自动寻找材料来源。
        /// </summary>
        public void UseAutomaticMaterialSource()
        {
            CurrentMode = RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        /// <summary>
        /// 切换为自动寻找成品入网目标。
        /// </summary>
        public void UseAutomaticOutputStorage()
        {
            CurrentOutputStoreMode = OutputStoreMode.AutoNetwork;
            OutputStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        /// <summary>
        /// 获取当前已请求材料量，供 UI 展示。
        /// </summary>
        public float GetRequestedAmountForDisplay()
        {
            return RequestedKg;
        }

        /// <summary>
        /// 重置已请求材料计数。
        /// </summary>
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
                    StorageTransferResult result = NetworkStorageTransferService.TransferLooseItemToNetwork(
                        output,
                        GetFabricatorStorages(),
                        GetSpecificOutputTarget());
                    totalMoved += result.MovedKg;
                    if (!string.IsNullOrEmpty(result.BlockedItem))
                    {
                        lastBlockedItem = result.BlockedItem;
                    }
                }
            }

            StorageTransferResult outputStorageResult = StoreOutputsFromOutputStorage();
            totalMoved += outputStorageResult.MovedKg;
            if (!string.IsNullOrEmpty(outputStorageResult.BlockedItem))
            {
                lastBlockedItem = outputStorageResult.BlockedItem;
            }

            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(
                new StorageTransferResult(totalMoved, lastBlockedItem),
                "等待下一批成品");
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
                    count = Config.Instance.InfiniteQueueRequestBatchCount;
                }
            }

            if (fabricator.CurrentWorkingOrder == recipe || fabricator.NextOrder == recipe)
            {
                count = Mathf.Max(count, 1);
            }

            return Mathf.Clamp(count, 1, Config.Instance.MaxRequestBatchCount);
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

            StorageTransferResult result = StoreOutputsFromOutputStorage();
            lastOutputStatus = NetworkStorageTransferService.FormatOutputStatus(result, "等待成品进入输出栏");
        }

        private StorageTransferResult StoreOutputsFromOutputStorage()
        {
            EnsureFabricator();
            if (fabricator.outStorage == null || fabricator.outStorage.items == null)
            {
                lastOutputStatus = "建筑没有可读取的输出栏";
                return StorageTransferResult.Idle;
            }

            return NetworkStorageTransferService.TransferStoredItemsToNetwork(
                fabricator.outStorage,
                GetFabricatorStorages(),
                GetSpecificOutputTarget());
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

        internal static Tag GetStorageTransferTag(GameObject item)
        {
            return StorageItemUtility.GetStorageTransferTag(item);
        }

        internal static bool MatchesStorageTag(GameObject item, Tag tag)
        {
            return StorageItemUtility.MatchesStorageTag(item, tag);
        }

        private IEnumerable<Storage> GetFabricatorStorages()
        {
            if (fabricator == null)
            {
                yield break;
            }

            yield return fabricator.inStorage;
            yield return fabricator.buildStorage;
            yield return fabricator.outStorage;
        }

        private Storage GetSpecificOutputTarget()
        {
            return CurrentOutputStoreMode == OutputStoreMode.SpecificStorage
                ? ResolveOutputStorage()
                : null;
        }

        internal static int GetStorageInstanceId(Storage storage)
        {
            return StorageItemUtility.GetStorageInstanceId(storage);
        }

        private static string GetTagDisplayName(Tag tag)
        {
            return StorageItemUtility.GetTagDisplayName(tag);
        }
    }
}
