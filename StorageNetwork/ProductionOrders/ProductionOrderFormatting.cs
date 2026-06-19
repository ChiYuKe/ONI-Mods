using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderFormatting
    {
        public static string FormatFabricatorGroupName(List<ComplexFabricator> fabricators)
        {
            if (fabricators == null || fabricators.Count == 0)
            {
                return "?";
            }

            string firstName = fabricators[0].gameObject.GetProperName();
            return fabricators.Count == 1 ? firstName : string.Format("{0} x{1}", firstName, fabricators.Count);
        }

        public static string FormatRecipeDetails(ComplexRecipe recipe)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_DETAILS),
                FormatRecipeElements(recipe.ingredients),
                FormatRecipeElements(recipe.results));
        }

        public static string FormatRecipeElements(IEnumerable<ComplexRecipe.RecipeElement> elements)
        {
            return elements == null ? string.Empty : string.Join("  +  ", elements.Select(FormatRecipeElement));
        }

        public static string FormatRecipeElement(ComplexRecipe.RecipeElement element)
        {
            if (element == null)
            {
                return string.Empty;
            }

            return string.Format("{0} {1}", GameUtil.GetFormattedMass(element.amount), GetRecipeElementName(element));
        }

        public static string GetRecipeElementName(ComplexRecipe.RecipeElement element)
        {
            if (element.material != Tag.Invalid)
            {
                return GetTagDisplayName(element.material);
            }

            return element.possibleMaterials != null && element.possibleMaterials.Length > 0
                ? string.Join("/", element.possibleMaterials.Select(GetTagDisplayName).ToArray())
                : "?";
        }

        public static string GetTagDisplayName(Tag tag)
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

        public static string FormatCycle(float cycle)
        {
            return cycle.ToString("0.0");
        }

        public static string FormatCycleStamp(float cycle)
        {
            return (cycle + 1f).ToString("0.0");
        }
    }
}
