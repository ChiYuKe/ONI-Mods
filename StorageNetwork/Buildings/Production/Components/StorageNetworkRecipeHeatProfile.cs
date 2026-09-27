using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkRecipeHeatProfile
    {
        public const float MetalRefineryOutputTemperature = 313.15f; // 40 °C

        private static readonly Dictionary<string, StorageNetworkRecipeHeatProfile> ProfileCache =
            new Dictionary<string, StorageNetworkRecipeHeatProfile>();
        private static bool? cachedConserveHeatMode;

        public float SelfHeatKilowatts { get; private set; }
        public float ExhaustKilowatts { get; private set; }
        public float TimeMultiplier { get; private set; }
        public float? HeatedTemperature { get; private set; }
        public float? ForcedProductTemperature { get; private set; }

        public static void ClearCache()
        {
            ProfileCache.Clear();
            cachedConserveHeatMode = null;
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

            bool currentMode = Config.Instance?.OrderProductionCenterConserveSmeltingHeat ?? false;
            if (cachedConserveHeatMode != currentMode)
            {
                ProfileCache.Clear();
                cachedConserveHeatMode = currentMode;
            }

            if (ProfileCache.TryGetValue(recipe.id, out StorageNetworkRecipeHeatProfile cached))
            {
                return cached;
            }

            StorageNetworkRecipeHeatProfile profile = ComputeProfile(recipe, currentMode);
            ProfileCache[recipe.id] = profile;
            return profile;
        }

        private static StorageNetworkRecipeHeatProfile ComputeProfile(ComplexRecipe recipe, bool conserveSmeltingHeat)
        {
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

            if (!isMetalRefinery && IsMetalSmeltingRecipe(recipe))
            {
                isMetalRefinery = true;
            }

            // 1. Calculate operational machine heat rates
            float selfHeatKW = 0f;
            float exhaustKW = 0f;
            if (sourceDef != null)
            {
                selfHeatKW = Mathf.Max(0f, sourceDef.SelfHeatKilowattsWhenActive);
                exhaustKW = Mathf.Max(0f, sourceDef.ExhaustKilowattsWhenActive);
            }

            // 2. Option 3: Conserve 100% phase-change heat over 3x fabrication time for metal smelting recipes
            float timeMultiplier = 1f;
            float? forcedProductTemp = null;

            if (conserveSmeltingHeat && isMetalRefinery)
            {
                timeMultiplier = 3f;
                forcedProductTemp = MetalRefineryOutputTemperature;

                float coolantHeatKJ = 0f;
                if (recipe.results != null)
                {
                    foreach (ComplexRecipe.RecipeElement result in recipe.results)
                    {
                        Element elem = ElementLoader.GetElement(result.material);
                        if (elem != null && elem.highTemp > MetalRefineryOutputTemperature)
                        {
                            float deltaTemp = elem.highTemp - MetalRefineryOutputTemperature;
                            float heatKJ = deltaTemp * elem.specificHeatCapacity * result.amount;
                            coolantHeatKJ += Mathf.Max(0f, heatKJ);
                        }
                    }
                }

                float recipeDuration = Mathf.Max(1f, recipe.time * timeMultiplier);
                float smeltingHeatKW = coolantHeatKJ / recipeDuration;
                selfHeatKW += smeltingHeatKW;
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

            return new StorageNetworkRecipeHeatProfile
            {
                SelfHeatKilowatts = selfHeatKW,
                ExhaustKilowatts = exhaustKW,
                TimeMultiplier = timeMultiplier,
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
