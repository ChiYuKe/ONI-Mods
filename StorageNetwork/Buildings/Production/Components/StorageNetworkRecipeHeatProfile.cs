using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRecipeHeatProfile
    {
        private static readonly Dictionary<string, StorageNetworkRecipeHeatProfile> ProfileCache =
            new Dictionary<string, StorageNetworkRecipeHeatProfile>();

        public float SelfHeatKilowatts { get; private set; }
        public float ExhaustKilowatts { get; private set; }
        public float? HeatedTemperature { get; private set; }

        public static void ClearCache()
        {
            ProfileCache.Clear();
        }

        public static void ResetRuntimeState()
        {
            ClearCache();
        }

        public static StorageNetworkRecipeHeatProfile GetProfile(ComplexRecipe recipe)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.id))
            {
                return null;
            }

            if (ProfileCache.TryGetValue(recipe.id, out StorageNetworkRecipeHeatProfile cached))
            {
                return cached;
            }

            StorageNetworkRecipeHeatProfile profile = ComputeProfile(recipe);
            ProfileCache[recipe.id] = profile;
            return profile;
        }

        private static StorageNetworkRecipeHeatProfile ComputeProfile(ComplexRecipe recipe)
        {
            Tag primaryFabTag = Tag.Invalid;

            if (recipe.fabricators != null && recipe.fabricators.Count > 0)
            {
                foreach (Tag tag in recipe.fabricators)
                {
                    if (tag.Name == "MetalRefinery")
                    {
                        primaryFabTag = tag;
                        break;
                    }
                }

                if (!primaryFabTag.IsValid)
                {
                    primaryFabTag = recipe.fabricators[0];
                }
            }

            BuildingDef sourceDef = primaryFabTag.IsValid ? Assets.GetBuildingDef(primaryFabTag.Name) : null;
            GameObject sourcePrefab = primaryFabTag.IsValid ? Assets.GetPrefab(primaryFabTag) : null;

            // 1. Calculate operational machine heat rates
            float selfHeatKW = 0f;
            float exhaustKW = 0f;
            if (sourceDef != null)
            {
                selfHeatKW = Mathf.Max(0f, sourceDef.SelfHeatKilowattsWhenActive);
                exhaustKW = Mathf.Max(0f, sourceDef.ExhaustKilowattsWhenActive);
            }

            // 2. Determine HeatedTemperature for recipes with TemperatureOperation.Heated
            float? heatedTemp = null;
            bool hasHeatedResult = recipe.results != null &&
                                  recipe.results.Any(r => r.temperatureOperation == ComplexRecipe.RecipeElement.TemperatureOperation.Heated);

            if (hasHeatedResult)
            {
                if (sourcePrefab != null)
                {
                    ComplexFabricator srcFab = sourcePrefab.GetComponent<ComplexFabricator>();
                    if (srcFab != null && srcFab.heatedTemperature > 0.1f)
                    {
                        heatedTemp = srcFab.heatedTemperature;
                    }
                }

                if (heatedTemp == null)
                {
                    if (primaryFabTag.Name == "Kiln")
                    {
                        heatedTemp = 353.15f; // 80 °C
                    }
                    else if (primaryFabTag.Name == "CookingStation" || primaryFabTag.Name == "GourmetCookingStation")
                    {
                        heatedTemp = 368.15f; // 95 °C
                    }
                    else
                    {
                        heatedTemp = 353.15f;
                    }
                }
            }

            return new StorageNetworkRecipeHeatProfile
            {
                SelfHeatKilowatts = selfHeatKW,
                ExhaustKilowatts = exhaustKW,
                HeatedTemperature = heatedTemp
            };
        }
    }
}
