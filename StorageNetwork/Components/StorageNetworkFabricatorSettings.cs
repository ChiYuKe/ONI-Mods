using KSerialization;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkFabricatorSettings : KMonoBehaviour, ISim1000ms, ISim200ms
    {
        private const int StableTickThreshold = 2;

        private static StatusItem missingNetworkMaterialStatus;
        private static StatusItem requestIngredientsEnabledStatus;
        private static StatusItem requestIngredientsDisabledStatus;
        private static StatusItem storeProductsEnabledStatus;
        private static StatusItem storeProductsDisabledStatus;

        private ComplexFabricator fabricator;
        private ElementConverter elementConverter;
        private PlantablePlot plantablePlot;
        private SingleEntityReceptacle receptacle;
        private Storage storage;

        [Serialize]
        private bool requestIngredientsFromNetwork;

        [Serialize]
        private bool storeProductsToNetwork;

        private KSelectable selectable;
        private Tag missingNetworkMaterial = Tag.Invalid;
        private Tag observedMissingNetworkMaterial = Tag.Invalid;
        private int observedMissingTicks;
        private int observedClearTicks;
        private bool missingStatusVisible;
        private bool refreshQueued;

        public bool RequestIngredientsFromNetwork
        {
            get => requestIngredientsFromNetwork;
            set
            {
                requestIngredientsFromNetwork = value;
                RefreshRecipeSettingsStatus();
                RefreshFabricatorQueue();
                QueueNetworkRefresh();
                if (!requestIngredientsFromNetwork)
                {
                    ClearMissingMaterialStatus();
                }
            }
        }

        public bool StoreProductsToNetwork
        {
            get => storeProductsToNetwork;
            set
            {
                storeProductsToNetwork = value;
                RefreshRecipeSettingsStatus();
            }
        }

        public bool SupportsNetworkRecipeSettings => HasFabricatorRecipes() || HasElementConverterRecipe() || HasPlantingRequests();

        public bool SupportsStoreProductsToNetwork => HasFabricatorRecipes() || HasElementConverterRecipe();

        public string MissingNetworkMaterialName => GetMaterialName(missingNetworkMaterial);

        public void QueueNetworkRefresh()
        {
            refreshQueued = true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            fabricator = GetComponent<ComplexFabricator>();
            elementConverter = GetComponent<ElementConverter>();
            plantablePlot = GetComponent<PlantablePlot>();
            receptacle = GetComponent<SingleEntityReceptacle>();
            storage = GetComponent<Storage>();
            selectable = GetComponent<KSelectable>();
            ClearMissingMaterialStatus();
            RefreshRecipeSettingsStatus();
        }

        protected override void OnCleanUp()
        {
            ClearMissingMaterialStatus();
            ClearRecipeSettingsStatus();
            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            if (!refreshQueued)
            {
                return;
            }

            refreshQueued = false;
            RefreshNetworkTransfers();
        }

        public void Sim1000ms(float dt)
        {
            if (!requestIngredientsFromNetwork && !storeProductsToNetwork)
            {
                return;
            }

            RefreshNetworkTransfers();
            if (requestIngredientsFromNetwork)
            {
                RefreshMissingMaterialStatus();
            }
        }

        private void RefreshNetworkTransfers()
        {
            if (!requestIngredientsFromNetwork && !storeProductsToNetwork)
            {
                return;
            }

            RefreshFabricatorTransfers();
            RefreshElementConverterTransfers();
            RefreshPlantingTransfers();
        }

        private void RefreshFabricatorQueue()
        {
            if (fabricator != null)
            {
                fabricator.SetQueueDirty();
            }
        }

        private ComplexFabricator GetFabricator()
        {
            if (fabricator == null)
            {
                fabricator = GetComponent<ComplexFabricator>();
            }

            return fabricator;
        }

        private ElementConverter GetElementConverter()
        {
            if (elementConverter == null)
            {
                elementConverter = GetComponent<ElementConverter>();
            }

            return elementConverter;
        }

        private PlantablePlot GetPlantablePlot()
        {
            if (plantablePlot == null)
            {
                plantablePlot = GetComponent<PlantablePlot>();
            }

            return plantablePlot;
        }

        private SingleEntityReceptacle GetReceptacle()
        {
            if (receptacle == null)
            {
                receptacle = GetComponent<SingleEntityReceptacle>();
            }

            return receptacle;
        }

        private Storage GetStorage()
        {
            if (storage == null)
            {
                storage = GetComponent<Storage>();
            }

            return storage;
        }

        private bool HasFabricatorRecipes()
        {
            ComplexFabricator currentFabricator = GetFabricator();
            return currentFabricator != null &&
                currentFabricator.GetRecipes() != null &&
                currentFabricator.GetRecipes().Length > 0;
        }

        private bool HasElementConverterRecipe()
        {
            ElementConverter converter = GetElementConverter();
            return converter != null &&
                ((converter.consumedElements != null && converter.consumedElements.Length > 0) ||
                 (converter.outputElements != null && converter.outputElements.Length > 0));
        }

        private bool HasPlantingRequests()
        {
            return GetPlantablePlot() != null || GetReceptacle() != null;
        }

        private void RefreshElementConverterTransfers()
        {
            ElementConverter converter = GetElementConverter();
            Storage currentStorage = GetStorage();
            if (converter == null || currentStorage == null)
            {
                return;
            }

            if (requestIngredientsFromNetwork)
            {
                StorageNetworkTransferService.TryPullElementConverterInputs(currentStorage, converter);
            }

            if (storeProductsToNetwork)
            {
                StorageNetworkTransferService.TryStoreElementConverterOutputs(currentStorage, converter);
            }
        }

        private void RefreshFabricatorTransfers()
        {
            ComplexFabricator currentFabricator = GetFabricator();
            if (currentFabricator == null)
            {
                return;
            }

            if (requestIngredientsFromNetwork &&
                StorageNetworkTransferService.TryPullFabricatorQueuedIngredients(currentFabricator))
            {
                currentFabricator.SetQueueDirty();
                currentFabricator.Trigger((int)GameHashes.OnStorageChange, currentFabricator.inStorage);
            }

            if (storeProductsToNetwork)
            {
                StorageNetworkTransferService.TryStoreRecipeResults(currentFabricator, currentFabricator.CurrentWorkingOrder);
            }
        }

        private void RefreshPlantingTransfers()
        {
            if (!requestIngredientsFromNetwork)
            {
                return;
            }

            Storage currentStorage = GetStorage();
            if (currentStorage == null || !HasPlantingRequests())
            {
                return;
            }

            StorageNetworkTransferService.TryPullPlantingMaterials(currentStorage, GetReceptacle());
        }

        private void RefreshMissingMaterialStatus()
        {
            if (!requestIngredientsFromNetwork || !TryGetMissingNetworkMaterial(out Tag missingTag))
            {
                observedMissingNetworkMaterial = Tag.Invalid;
                observedMissingTicks = 0;
                observedClearTicks++;
                if (observedClearTicks >= StableTickThreshold)
                {
                    SetMissingMaterialStatus(Tag.Invalid);
                }

                return;
            }

            observedClearTicks = 0;
            if (observedMissingNetworkMaterial == missingTag)
            {
                observedMissingTicks++;
            }
            else
            {
                observedMissingNetworkMaterial = missingTag;
                observedMissingTicks = 1;
            }

            if (observedMissingTicks >= StableTickThreshold)
            {
                SetMissingMaterialStatus(missingTag);
            }
        }

        private void RefreshRecipeSettingsStatus()
        {
            if (selectable == null)
            {
                selectable = GetComponent<KSelectable>();
            }

            bool show = SupportsNetworkRecipeSettings;
            selectable?.ToggleStatusItem(GetRequestIngredientsEnabledStatus(), show && requestIngredientsFromNetwork, this);
            selectable?.ToggleStatusItem(GetRequestIngredientsDisabledStatus(), show && !requestIngredientsFromNetwork, this);
            bool showStoreProducts = show && SupportsStoreProductsToNetwork;
            selectable?.ToggleStatusItem(GetStoreProductsEnabledStatus(), showStoreProducts && storeProductsToNetwork, this);
            selectable?.ToggleStatusItem(GetStoreProductsDisabledStatus(), showStoreProducts && !storeProductsToNetwork, this);
        }

        private void ClearRecipeSettingsStatus()
        {
            selectable?.ToggleStatusItem(GetRequestIngredientsEnabledStatus(), false, this);
            selectable?.ToggleStatusItem(GetRequestIngredientsDisabledStatus(), false, this);
            selectable?.ToggleStatusItem(GetStoreProductsEnabledStatus(), false, this);
            selectable?.ToggleStatusItem(GetStoreProductsDisabledStatus(), false, this);
        }

        private bool TryGetMissingNetworkMaterial(out Tag missingTag)
        {
            if (StorageNetworkTransferService.TryGetMissingNetworkRecipeIngredient(GetFabricator(), out missingTag))
            {
                return true;
            }

            if (StorageNetworkTransferService.TryGetMissingNetworkElementConverterInput(GetStorage(), GetElementConverter(), out missingTag))
            {
                return true;
            }

            // Planting requests are transient delivery requests, so do not show recipe-missing status for them.
            return false;
        }

        private void ClearMissingMaterialStatus()
        {
            observedMissingNetworkMaterial = Tag.Invalid;
            observedMissingTicks = 0;
            observedClearTicks = StableTickThreshold;
            SetMissingMaterialStatus(Tag.Invalid);
        }

        private void SetMissingMaterialStatus(Tag missingTag)
        {
            if (selectable == null)
            {
                selectable = GetComponent<KSelectable>();
            }

            bool show = requestIngredientsFromNetwork && missingTag.IsValid;
            missingNetworkMaterial = show ? missingTag : Tag.Invalid;
            if (missingStatusVisible == show)
            {
                return;
            }

            missingStatusVisible = show;
            selectable?.ToggleStatusItem(GetMissingNetworkMaterialStatus(), show, this);
        }

        private static StatusItem GetMissingNetworkMaterialStatus()
        {
            if (missingNetworkMaterialStatus == null)
            {
                missingNetworkMaterialStatus = new StatusItem(
                    "StorageNetworkMissingRecipeMaterial",
                    STRINGS.UI.STORAGE_NETWORK.MISSING_RECIPE_MATERIAL_STATUS,
                    STRINGS.UI.STORAGE_NETWORK.MISSING_RECIPE_MATERIAL_TOOLTIP,
                    "status_item_resource_unavailable",
                    StatusItem.IconType.Custom,
                    NotificationType.BadMinor,
                    false,
                    OverlayModes.None.ID,
                    129022,
                    true,
                    ResolveMissingNetworkMaterialString);
                missingNetworkMaterialStatus.resolveTooltipCallback = ResolveMissingNetworkMaterialString;
            }

            return missingNetworkMaterialStatus;
        }

        private static StatusItem GetRequestIngredientsEnabledStatus()
        {
            if (requestIngredientsEnabledStatus == null)
            {
                requestIngredientsEnabledStatus = CreateRecipeSettingStatus(
                    "StorageNetworkRequestRecipeMaterialsEnabled",
                    STRINGS.UI.STORAGE_NETWORK.REQUEST_RECIPE_MATERIALS_ENABLED_STATUS,
                    STRINGS.UI.STORAGE_NETWORK.REQUEST_RECIPE_MATERIALS_ENABLED_TOOLTIP);
            }

            return requestIngredientsEnabledStatus;
        }

        private static StatusItem GetRequestIngredientsDisabledStatus()
        {
            if (requestIngredientsDisabledStatus == null)
            {
                requestIngredientsDisabledStatus = CreateRecipeSettingStatus(
                    "StorageNetworkRequestRecipeMaterialsDisabled",
                    STRINGS.UI.STORAGE_NETWORK.REQUEST_RECIPE_MATERIALS_DISABLED_STATUS,
                    STRINGS.UI.STORAGE_NETWORK.REQUEST_RECIPE_MATERIALS_DISABLED_TOOLTIP);
            }

            return requestIngredientsDisabledStatus;
        }

        private static StatusItem GetStoreProductsEnabledStatus()
        {
            if (storeProductsEnabledStatus == null)
            {
                storeProductsEnabledStatus = CreateRecipeSettingStatus(
                    "StorageNetworkStoreRecipeProductsEnabled",
                    STRINGS.UI.STORAGE_NETWORK.STORE_RECIPE_PRODUCTS_ENABLED_STATUS,
                    STRINGS.UI.STORAGE_NETWORK.STORE_RECIPE_PRODUCTS_ENABLED_TOOLTIP);
            }

            return storeProductsEnabledStatus;
        }

        private static StatusItem GetStoreProductsDisabledStatus()
        {
            if (storeProductsDisabledStatus == null)
            {
                storeProductsDisabledStatus = CreateRecipeSettingStatus(
                    "StorageNetworkStoreRecipeProductsDisabled",
                    STRINGS.UI.STORAGE_NETWORK.STORE_RECIPE_PRODUCTS_DISABLED_STATUS,
                    STRINGS.UI.STORAGE_NETWORK.STORE_RECIPE_PRODUCTS_DISABLED_TOOLTIP);
            }

            return storeProductsDisabledStatus;
        }

        private static StatusItem CreateRecipeSettingStatus(string id, string name, string tooltip)
        {
            return new StatusItem(
                id,
                name,
                tooltip,
                string.Empty,
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.None.ID,
                129022,
                true,
                null);
        }

        private static string ResolveMissingNetworkMaterialString(string text, object data)
        {
            StorageNetworkFabricatorSettings settings = data as StorageNetworkFabricatorSettings;
            string materialName = settings != null
                ? settings.MissingNetworkMaterialName
                : STRINGS.UI.STORAGE_NETWORK.MISSING_RECIPE_MATERIAL_FALLBACK;

            return text.Replace("{Material}", materialName);
        }

        private static string GetMaterialName(Tag tag)
        {
            if (!tag.IsValid)
            {
                return STRINGS.UI.STORAGE_NETWORK.MISSING_RECIPE_MATERIAL_FALLBACK;
            }

            GameObject prefab = Assets.TryGetPrefab(tag);
            if (prefab != null)
            {
                return prefab.GetProperName();
            }

            Element element = ElementLoader.GetElement(tag);
            return element != null ? element.name : tag.ProperName();
        }
    }
}
