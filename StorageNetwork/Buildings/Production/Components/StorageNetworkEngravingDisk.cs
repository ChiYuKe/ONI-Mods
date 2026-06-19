using System.Collections.Generic;
using System.Linq;
using KSerialization;
using StorageNetwork.ProductionOrders;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkEngravingDisk : KMonoBehaviour
    {
        [Serialize]
        private List<string> engravedRecipeIds = new List<string>();

        [MyCmpGet]
        private InfoDescription infoDescription = null;

        public IReadOnlyList<string> EngravedRecipeIds => engravedRecipeIds;

        public bool IsBlank => engravedRecipeIds == null || engravedRecipeIds.Count == 0;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            gameObject.AddOrGet<UserNameable>();
            RefreshInfoDescription();
        }

        public void SetRecipeIds(IEnumerable<string> recipeIds)
        {
            engravedRecipeIds = (recipeIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();
            RefreshInfoDescription();
        }

        public bool RemoveRecipe(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId) || engravedRecipeIds == null)
            {
                return false;
            }

            bool removed = engravedRecipeIds.RemoveAll(id => id == recipeId) > 0;
            if (removed)
            {
                RefreshInfoDescription();
            }

            return removed;
        }

        public string GetRecipeSummary(int maxNames = 2)
        {
            return GetRecipeSummary(engravedRecipeIds, maxNames);
        }

        public string GetRecipeDetails()
        {
            return GetRecipeDetails(engravedRecipeIds);
        }

        public static string GetRecipeSummary(IEnumerable<string> recipeIds, int maxNames = 2)
        {
            List<string> names = GetRecipeNames(recipeIds);
            if (names.Count == 0)
            {
                return StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK);
            }

            string summary = string.Join("、", names.Take(maxNames));
            if (names.Count > maxNames)
            {
                summary += string.Format(" 等 {0} 个配方", names.Count);
            }

            return summary;
        }

        public static string GetRecipeDetails(IEnumerable<string> recipeIds)
        {
            List<ComplexRecipe> recipes = GetRecipes(recipeIds);
            return recipes.Count == 0
                ? StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK)
                : string.Join("\n\n", recipes.Select(FormatRecipeDetail));
        }

        private void RefreshInfoDescription()
        {
            if (infoDescription == null)
            {
                infoDescription = gameObject.AddOrGet<InfoDescription>();
            }

            if (infoDescription == null)
            {
                return;
            }

            string baseDescription = StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC);
            if (IsBlank)
            {
                infoDescription.description = baseDescription;
                return;
            }

            infoDescription.description = string.Format(
                "{0}\n\n<b>已刻录配方</b>\n共 {1} 个配方\n\n{2}",
                baseDescription,
                GetRecipes(engravedRecipeIds).Count,
                GetRecipeDetails());
        }

        private static List<string> GetRecipeNames(IEnumerable<string> recipeIds)
        {
            return (recipeIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Select(GetRecipeName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
        }

        private static string GetRecipeName(string recipeId)
        {
            ComplexRecipe recipe = ComplexRecipeManager.Get()?.GetRecipe(recipeId);
            return recipe != null ? recipe.GetUIName(false) : recipeId;
        }

        private static List<ComplexRecipe> GetRecipes(IEnumerable<string> recipeIds)
        {
            return (recipeIds ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .Select(id => ComplexRecipeManager.Get()?.GetRecipe(id))
                .Where(recipe => recipe != null)
                .OrderBy(recipe => recipe.GetUIName(false))
                .ToList();
        }

        private static string FormatRecipeDetail(ComplexRecipe recipe)
        {
            return string.Format(
                "<b>{0}</b>\n  材料：{1}\n  产出：{2}",
                recipe.GetUIName(false),
                ProductionOrderFormatting.FormatRecipeElements(recipe.ingredients),
                ProductionOrderFormatting.FormatRecipeElements(recipe.results));
        }
    }
}
