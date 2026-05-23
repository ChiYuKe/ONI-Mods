using KSerialization;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 储存网络制造器设置组件。
    /// 挂在 ComplexFabricator 建筑上，用于控制制造器是否从储存网络请求配方材料，
    /// 以及是否将生产完成的产物存入储存网络。
    /// 同时会定时检测当前配方缺少的网络材料，并在建筑状态栏显示缺料提示。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkFabricatorSettings : KMonoBehaviour, ISim1000ms
    {
        private const int StableTickThreshold = 2;

        private static StatusItem missingNetworkMaterialStatus;

        private ComplexFabricator fabricator;

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

        public bool RequestIngredientsFromNetwork
        {
            get => requestIngredientsFromNetwork;
            set
            {
                requestIngredientsFromNetwork = value;
                RefreshFabricatorQueue();
                if (!requestIngredientsFromNetwork)
                {
                    ClearMissingMaterialStatus();
                }
            }
        }

        public bool StoreProductsToNetwork
        {
            get => storeProductsToNetwork;
            set => storeProductsToNetwork = value;
        }

        public bool SupportsNetworkRecipeSettings =>
            GetFabricator() != null &&
            GetFabricator().GetRecipes() != null &&
            GetFabricator().GetRecipes().Length > 0;

        public string MissingNetworkMaterialName => GetMaterialName(missingNetworkMaterial);

        protected override void OnSpawn()
        {
            base.OnSpawn();
            fabricator = GetComponent<ComplexFabricator>();
            selectable = GetComponent<KSelectable>();
            ClearMissingMaterialStatus();
        }

        protected override void OnCleanUp()
        {
            ClearMissingMaterialStatus();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshMissingMaterialStatus();
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

        private void RefreshMissingMaterialStatus()
        {
            if (!requestIngredientsFromNetwork ||
                !StorageNetworkTransferService.TryGetMissingNetworkRecipeIngredient(GetFabricator(), out Tag missingTag))
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
