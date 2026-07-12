namespace StorageNetwork.ProductionOrders
{
    /// <summary>
    /// Drives production order state and keep-stock rules independently of the order UI.
    /// </summary>
    internal sealed class ProductionOrderBackgroundMaintenance : KMonoBehaviour, ISim1000ms
    {
        private const float InventoryRefreshIntervalSeconds = 10f;
        private const float RecipeCatalogRefreshIntervalSeconds = 60f;

        private readonly ProductionOrderService service = new ProductionOrderService();
        private float inventoryElapsed = InventoryRefreshIntervalSeconds;
        private float recipeCatalogElapsed = RecipeCatalogRefreshIntervalSeconds;

        public void Sim1000ms(float dt)
        {
            inventoryElapsed += dt;
            recipeCatalogElapsed += dt;
            if (inventoryElapsed < InventoryRefreshIntervalSeconds)
            {
                return;
            }

            bool rebuildRecipeCatalog = recipeCatalogElapsed >= RecipeCatalogRefreshIntervalSeconds;
            inventoryElapsed = 0f;
            if (rebuildRecipeCatalog)
            {
                recipeCatalogElapsed = 0f;
            }

            service.SetOrderCenterScope(null);
            service.RefreshBackground(rebuildRecipeCatalog);
        }
    }
}
