namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionKeepRule
    {
        public ProductionKeepRule(Tag productTag, string productName, string recipeKey, float targetAmount)
        {
            ProductTag = productTag;
            ProductName = productName;
            RecipeKey = recipeKey;
            TargetAmount = targetAmount;
        }

        public Tag ProductTag { get; }

        public string ProductName { get; }

        public string RecipeKey { get; }

        public float TargetAmount { get; }
    }
}
