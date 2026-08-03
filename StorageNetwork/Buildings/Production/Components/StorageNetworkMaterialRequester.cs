using System.Collections.Generic;
using System;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 生产建筑材料请求与成品入网组件。挂在 ComplexFabricator 上，按队列从储存网络调拨材料。
    /// </summary>
    public sealed partial class StorageNetworkMaterialRequester : KMonoBehaviour, ISim1000ms
    {
        private const float EmptyOutputRetrySeconds = 5f;

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
        private float outputStoreCooldown;
        private string lastStatus;
        private string lastOutputStatus;
        private readonly HashSet<Tag> lastRecipeResultTags = new HashSet<Tag>();
        private readonly HashSet<Tag> currentRecipeResultTagBuffer = new HashSet<Tag>();
        private readonly HashSet<Tag> knownRecipeResultTags = new HashSet<Tag>();
        private readonly HashSet<Storage> fabricatorStorageExclusions = new HashSet<Storage>();
        private readonly List<Storage> sourceStorageBuffer = new List<Storage>();
        private readonly List<ComplexRecipe> queuedRecipeBuffer = new List<ComplexRecipe>();
        private readonly List<GameObject> producedOutputBuffer = new List<GameObject>();
        private readonly Tag[] sourceWantedTagBuffer = new Tag[1];
        private readonly QueuedRecipeComparer queuedRecipeComparer = new QueuedRecipeComparer();
        private bool knownRecipeResultTagsBuilt;

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
                outputStoreCooldown = 0f;
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
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_NO_QUEUE);
                requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
                return;
            }

            float remainingLimit = LimitEnabled ? Mathf.Max(0f, LimitKg - RequestedKg) : float.MaxValue;
            if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED);
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
                float moved = RequestIngredient(recipe, ingredient.material, missing, remainingLimit);
                if (moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    lastStatus = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_MISSING_SOURCE), GetTagDisplayName(ingredient.material));
                    requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
                    break;
                }

                movedAny = true;
                RequestedKg += moved;
                lastStatus = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_REQUESTED), GameUtil.GetFormattedMass(moved), GetTagDisplayName(ingredient.material));
                remainingLimit -= moved;
            }

            if (!requestedAny)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED);
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

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
            return StorageSceneRegistry.TryGetReachableStorage(
                       SourceStorageInstanceId,
                       worldId,
                       out Storage storage) &&
                   IsUsableSourceForWorld(storage, worldId)
                ? storage
                : null;
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

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
            return StorageSceneRegistry.TryGetReachableStorage(
                       OutputStorageInstanceId,
                       worldId,
                       out Storage storage) &&
                   StorageSceneRegistry.HasOnlineCoreInWorld(worldId) &&
                   StorageSceneRegistry.IsLive(storage) &&
                   StorageNetworkStorageRules.IsNetworkStorageTarget(storage) &&
                   StorageTargetSelector.IsStorageReachableFromWorld(storage, worldId)
                ? storage
                : null;
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
            StorageNetworkRuntimeCatalog.RegisterMaterialRequester(fabricator, this);
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRuntimeCatalog.UnregisterMaterialRequester(fabricator, this);
            RemoveMaterialRequestStatus();
            base.OnCleanUp();
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

            List<ComplexRecipe> queuedRecipes = queuedRecipeBuffer;
            queuedRecipes.Clear();
            foreach (ComplexRecipe recipe in fabricator.GetRecipes())
            {
                if (recipe != null && fabricator.IsRecipeQueued(recipe))
                {
                    queuedRecipes.Add(recipe);
                }
            }

            queuedRecipeComparer.Fabricator = fabricator;
            queuedRecipes.Sort(queuedRecipeComparer);

            for (int i = 0; i < queuedRecipes.Count; i++)
            {
                ComplexRecipe recipe = queuedRecipes[i];
                if (NeedsAnyIngredient(recipe))
                {
                    return recipe;
                }
            }

            return null;
        }

        private bool NeedsAnyIngredient(ComplexRecipe recipe)
        {
            if (recipe.ingredients == null)
            {
                return false;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                if (GetTargetIngredientAmount(recipe, ingredient) - GetAmountAvailableInFabricator(ingredient.material) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return true;
                }
            }

            return false;
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
                count = StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(fabricator, recipe);
                if (count == ComplexFabricator.QUEUE_INFINITE)
                {
                    count = Config.Instance.InfiniteQueueRequestBatchCount;
                }
            }

            if (StorageNetworkFabricatorProgress.IsWorkingOnRecipe(fabricator, recipe) || fabricator.NextOrder == recipe)
            {
                count = Mathf.Max(count, 1);
            }

            return Mathf.Clamp(count, 1, Config.Instance.MaxRequestBatchCount);
        }

        private float RequestIngredient(ComplexRecipe recipe, Tag tag, float amountUnits, float maximumMassKg)
        {
            float massPerUnit = GetMassPerUnit(tag);
            float allowedUnits = Mathf.Min(amountUnits, maximumMassKg / massPerUnit);
            float amount = allowedUnits * massPerUnit;
            float moved = 0f;
            if (CurrentMode == RequestMode.SearchNetwork)
            {
                moved += ProductionOrderService.RequestLeasedMaterial(fabricator, recipe, tag, amount, fabricator.inStorage);
            }

            FillSourceStorages(tag, sourceStorageBuffer);
            for (int sourceIndex = 0;
                 sourceIndex < sourceStorageBuffer.Count;
                 sourceIndex++)
            {
                Storage source = sourceStorageBuffer[sourceIndex];
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                int destinationWorldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
                if (!IsUsableSourceForWorld(source, destinationWorldId))
                {
                    continue;
                }

                float sourceAmount = GetMatchingMassAvailable(source, tag);
                if (sourceAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                float transferAmount = Mathf.Min(amount - moved, sourceAmount, Mathf.Max(0f, fabricator.inStorage.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                // Source membership, power and relay state can change after the indexed
                // enumeration. Revalidate immediately before the native storage mutation.
                if (!IsUsableSourceForWorld(source, destinationWorldId))
                {
                    continue;
                }

                float requestedUnits = transferAmount / massPerUnit;
                float transferredUnits = NetworkStorageTransferService.TransferMatchingItemUnitsFromStorage(source, fabricator.inStorage, tag, requestedUnits);
                moved += transferredUnits * massPerUnit;
            }

            return moved;
        }

        private void FillSourceStorages(Tag tag, List<Storage> result)
        {
            result.Clear();
            if (CurrentMode == RequestMode.SpecificStorage)
            {
                Storage source = ResolveSourceStorage();
                if (IsUsableSource(source, tag))
                {
                    result.Add(source);
                }

                return;
            }

            int worldId = StorageTargetSelector.GetObjectWorldId(gameObject);
            sourceWantedTagBuffer[0] = tag;
            StorageNetworkSourceIndexService.FillSourceStorages(
                worldId,
                includeReachableWorlds: true,
                sourceWantedTagBuffer,
                BuildSourceExclusions(),
                specificSource: null,
                result);
            for (int i = result.Count - 1; i >= 0; i--)
            {
                if (!IsUsableSource(result[i], tag))
                {
                    result.RemoveAt(i);
                }
            }
        }

        private bool IsUsableSource(Storage storage, Tag tag)
        {
            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
            return IsUsableSourceForWorld(storage, worldId) &&
                   storage != fabricator.inStorage &&
                   storage != fabricator.buildStorage &&
                   storage != fabricator.outStorage &&
                   GetMatchingAmountAvailable(storage, tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT;
        }

        private static bool IsUsableSourceForWorld(Storage storage, int destinationWorldId)
        {
            if (destinationWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId) ||
                !StorageSceneRegistry.IsLive(storage) ||
                !StorageTargetSelector.IsStorageReachableFromWorld(storage, destinationWorldId) ||
                !StorageNetworkStorageRules.IsServerStorage(storage) ||
                !StorageNetworkStorageRules.IsConnectedNetworkStorage(storage) ||
                StorageNetworkStorageRules.IsMinionStorage(storage) ||
                StorageNetworkStorageRules.IsProductionStorage(storage))
            {
                return false;
            }

            int sourceWorldId = StorageNetworkWorldUtility.GetObjectWorldId(storage.gameObject);
            return sourceWorldId >= 0 && StorageSceneRegistry.HasOnlineCoreInWorld(sourceWorldId);
        }

        private float GetAmountAvailableInFabricator(Tag tag)
        {
            return GetMatchingAmountAvailable(fabricator.inStorage, tag) +
                   GetMatchingAmountAvailable(fabricator.buildStorage, tag);
        }

        private static float GetMatchingAmountAvailable(Storage storage, Tag tag)
        {
            if (storage?.items == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            float amount = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                    amount += primaryElement != null ? primaryElement.Units : 0f;
                }
            }

            return amount;
        }

        private static float GetMatchingMassAvailable(Storage storage, Tag tag)
        {
            if (storage?.items == null || tag == Tag.Invalid)
            {
                return 0f;
            }

            float amount = 0f;
            foreach (GameObject item in storage.items)
            {
                if (item != null && StorageItemUtility.MatchesStorageTag(item, tag))
                {
                    amount += StorageItemUtility.GetMass(item);
                }
            }

            return amount;
        }

        private static float GetMassPerUnit(Tag tag)
        {
            GameObject prefab = tag != Tag.Invalid ? Assets.GetPrefab(tag) : null;
            PrimaryElement primaryElement = prefab != null ? prefab.GetComponent<PrimaryElement>() : null;
            return primaryElement != null && primaryElement.MassPerUnit > 0f ? primaryElement.MassPerUnit : 1f;
        }

        private void EnsureFabricator()
        {
            if (fabricator == null)
            {
                fabricator = GetComponent<ComplexFabricator>();
            }
        }

        private sealed class QueuedRecipeComparer : IComparer<ComplexRecipe>
        {
            public ComplexFabricator Fabricator { get; set; }

            public int Compare(ComplexRecipe left, ComplexRecipe right)
            {
                bool leftInfinite =
                    StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(
                        Fabricator,
                        left) == ComplexFabricator.QUEUE_INFINITE;
                bool rightInfinite =
                    StorageNetworkFabricatorProgress.GetRecipeQueueCountSafe(
                        Fabricator,
                        right) == ComplexFabricator.QUEUE_INFINITE;
                int compare = rightInfinite.CompareTo(leftInfinite);
                return compare != 0
                    ? compare
                    : string.Compare(
                        left?.GetUIName(false),
                        right?.GetUIName(false),
                        StringComparison.CurrentCulture);
            }
        }

    }
}
