using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRecipeHeatProfile
    {
        private const float DefaultCoolantFudge = 0.8f;
        public const float MetalRefineryOutputTemperature = 313.15f; // 40 °C

        private static readonly Dictionary<string, StorageNetworkRecipeHeatProfile> ProfileCache =
            new Dictionary<string, StorageNetworkRecipeHeatProfile>();

        public float HeatKilowatts { get; private set; }
        public float TotalHeatEnergyKJ { get; private set; }
        public float CoolantHeatEnergyKJ { get; private set; }
        public float OperationalHeatEnergyKJ { get; private set; }
        public float? HeatedTemperature { get; private set; }
        public float? ForcedProductTemperature { get; private set; }

        public static void ClearCache()
        {
            ProfileCache.Clear();
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
            float recipeTime = Mathf.Max(1f, recipe.time);
            Tag primaryFabTag = Tag.Invalid;
            bool isMetalRefinery = false;

            if (recipe.fabricators != null && recipe.fabricators.Count > 0)
            {
                foreach (Tag tag in recipe.fabricators)
                {
                    if (tag.Name == "MetalRefinery")
                    {
                        isMetalRefinery = true;
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

            if (!isMetalRefinery && sourcePrefab != null && sourcePrefab.GetComponent<LiquidCooledRefinery>() != null)
            {
                isMetalRefinery = true;
            }

            // Fallback: If not explicitly flagged as MetalRefinery, check if it refines metal from metal ore
            if (!isMetalRefinery && IsMetalSmeltingRecipe(recipe))
            {
                isMetalRefinery = true;
            }

            // 1. Calculate operational machine heat rate
            float operationalHeatKW = 0f;
            if (sourceDef != null)
            {
                operationalHeatKW = Mathf.Max(0f, sourceDef.SelfHeatKilowattsWhenActive) +
                                    Mathf.Max(0f, sourceDef.ExhaustKilowattsWhenActive);
            }
            float operationalHeatKJ = operationalHeatKW * recipeTime;

            // 2. Calculate coolant heat for metal smelting recipes
            float coolantHeatKJ = 0f;
            float? forcedProductTemp = null;

            if (isMetalRefinery)
            {
                forcedProductTemp = MetalRefineryOutputTemperature;
                if (recipe.results != null)
                {
                    foreach (ComplexRecipe.RecipeElement result in recipe.results)
                    {
                        Element elem = ElementLoader.GetElement(result.material);
                        if (elem != null && elem.highTemp > MetalRefineryOutputTemperature)
                        {
                            float deltaTemp = elem.highTemp - MetalRefineryOutputTemperature;
                            float heatKJ = deltaTemp * elem.specificHeatCapacity * result.amount * DefaultCoolantFudge;
                            coolantHeatKJ += Mathf.Max(0f, heatKJ);
                        }
                    }
                }
            }

            // 3. Determine HeatedTemperature for recipes with TemperatureOperation.Heated
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

            float totalHeatKJ = operationalHeatKJ + coolantHeatKJ;
            float totalHeatKW = totalHeatKJ / recipeTime;

            return new StorageNetworkRecipeHeatProfile
            {
                HeatKilowatts = totalHeatKW,
                TotalHeatEnergyKJ = totalHeatKJ,
                OperationalHeatEnergyKJ = operationalHeatKJ,
                CoolantHeatEnergyKJ = coolantHeatKJ,
                HeatedTemperature = heatedTemp,
                ForcedProductTemperature = forcedProductTemp
            };
        }

        private static bool IsMetalSmeltingRecipe(ComplexRecipe recipe)
        {
            if (recipe?.results == null)
            {
                return false;
            }

            foreach (ComplexRecipe.RecipeElement result in recipe.results)
            {
                Element elem = ElementLoader.GetElement(result.material);
                if (elem != null && elem.IsSolid && elem.HasTag(GameTags.RefinedMetal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
