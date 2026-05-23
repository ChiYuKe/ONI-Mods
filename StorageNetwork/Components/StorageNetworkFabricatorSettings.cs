using KSerialization;
using StorageNetwork.Core;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkFabricatorSettings : KMonoBehaviour, ISim1000ms
    {
        private static StatusItem missingNetworkMaterialStatus;

        private ComplexFabricator fabricator;

        [Serialize]
        private bool requestIngredientsFromNetwork;

        [Serialize]
        private bool storeProductsToNetwork;

        private KSelectable selectable;

        public bool RequestIngredientsFromNetwork
        {
            get => requestIngredientsFromNetwork;
            set
            {
                requestIngredientsFromNetwork = value;
                RefreshFabricatorQueue();
                UpdateMissingMaterialStatus(false);
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

        protected override void OnSpawn()
        {
            base.OnSpawn();
            fabricator = GetComponent<ComplexFabricator>();
            selectable = GetComponent<KSelectable>();
            UpdateMissingMaterialStatus(false);
        }

        protected override void OnCleanUp()
        {
            UpdateMissingMaterialStatus(false);
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            UpdateMissingMaterialStatus(StorageNetworkTransferService.HasMissingNetworkRecipeIngredients(GetFabricator()));
        }

        public void OnNetworkMaterialFallback()
        {
            UpdateMissingMaterialStatus(true);
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

        private void UpdateMissingMaterialStatus(bool show)
        {
            if (selectable == null)
            {
                selectable = GetComponent<KSelectable>();
            }

            selectable?.ToggleStatusItem(GetMissingNetworkMaterialStatus(), requestIngredientsFromNetwork && show, this);
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
                    null);
            }

            return missingNetworkMaterialStatus;
        }
    }
}
