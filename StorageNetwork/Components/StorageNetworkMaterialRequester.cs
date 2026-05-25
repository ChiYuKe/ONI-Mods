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

        [MyCmpGet]
        private ComplexFabricator fabricator;

        private static StatusItem materialRequestStatusItem;
        private Guid materialRequestStatusHandle = Guid.Empty;
        private float requestCooldown;
        private string lastStatus;

        public string LastStatus => lastStatus;

        public RequestMode CurrentMode
        {
            get => (RequestMode)Mathf.Clamp(Mode, 0, 1);
            set => Mode = (int)value;
        }

        public void Sim1000ms(float dt)
        {
            if (!RequestEnabled || fabricator == null || fabricator.inStorage == null)
            {
                lastStatus = string.Empty;
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

        public void SetSourceStorage(Storage storage)
        {
            SourceStorageInstanceId = GetStorageInstanceId(storage);
        }

        public float GetRequestedAmountForDisplay()
        {
            return RequestedKg;
        }

        public void ResetRequestedAmount()
        {
            RequestedKg = 0f;
        }

        protected override void OnCleanUp()
        {
            RemoveMaterialRequestStatus();
            base.OnCleanUp();
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
