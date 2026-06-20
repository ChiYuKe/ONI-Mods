using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Research;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkEngravingDiskConfig : IEntityConfig
    {
        public const string ID = "StorageNetworkEngravingDisk";
        private const float CraftingTableMetalMass = 100f;
        private const float CraftingTablePlasticMass = 100f;
        private const float SupermaterialRefineryMetalMass = 50f;
        private const float SupermaterialRefineryPlasticMass = 50f;
        private const float RecipeTime = 20f;
        private static bool craftingTableRecipeRegistered;
        private static bool supermaterialRefineryRecipeRegistered;

        public string[] GetDlcIds()
        {
            return null;
        }

        public GameObject CreatePrefab()
        {
            GameObject go = EntityTemplates.CreateLooseEntity(
                ID,
                STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME),
                STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC),
                1f,
                true,
                Assets.GetAnim("artifact_databank_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.6f,
                0.6f,
                true,
                0,
                SimHashes.Steel,
                new List<Tag> { GameTags.IndustrialIngredient });

            go.AddOrGet<StorageNetworkEngravingDisk>();
            go.AddOrGet<InfoDescription>().description = STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC);
            go.AddOrGet<KSelectable>();
            go.AddOrGet<UserNameable>();
            return go;
        }

        public void OnPrefabInit(GameObject inst)
        {
            inst.AddOrGet<CodexEntryRedirector>().CodexID = ID;
        }

        public void OnSpawn(GameObject inst)
        {
        }

        public static void RegisterRecipe()
        {
            RegisterCraftingTableRecipe();
            RegisterSupermaterialRefineryRecipe();
        }

        public static void RegisterCraftingTableRecipe()
        {
            if (craftingTableRecipeRegistered)
            {
                return;
            }

            craftingTableRecipeRegistered = true;
            ComplexRecipe.RecipeElement[] inputs =
            {
                new ComplexRecipe.RecipeElement(SimHashes.Iron.CreateTag(), CraftingTableMetalMass),
                new ComplexRecipe.RecipeElement(SimHashes.Polypropylene.CreateTag(), CraftingTablePlasticMass)
            };
            ComplexRecipe.RecipeElement[] outputs =
            {
                new ComplexRecipe.RecipeElement(ID, 1f)
            };

            ComplexRecipe recipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID(CraftingTableConfig.ID, inputs, outputs), inputs, outputs)
            {
                time = RecipeTime,
                description = STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { CraftingTableConfig.ID },
                requiredTech = StorageNetworkResearchInstaller.OrderProductionTechId,
                sortOrder = 200
            };
        }

        public static void RegisterSupermaterialRefineryRecipe()
        {
            if (supermaterialRefineryRecipeRegistered)
            {
                return;
            }

            supermaterialRefineryRecipeRegistered = true;
            ComplexRecipe.RecipeElement[] inputs =
            {
                new ComplexRecipe.RecipeElement(SimHashes.Iron.CreateTag(), SupermaterialRefineryMetalMass),
                new ComplexRecipe.RecipeElement(SimHashes.Polypropylene.CreateTag(), SupermaterialRefineryPlasticMass)
            };
            ComplexRecipe.RecipeElement[] outputs =
            {
                new ComplexRecipe.RecipeElement(ID, 1f)
            };

            ComplexRecipe recipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID(SupermaterialRefineryConfig.ID, inputs, outputs), inputs, outputs)
            {
                time = RecipeTime,
                description = STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { SupermaterialRefineryConfig.ID },
                requiredTech = StorageNetworkResearchInstaller.OrderProductionTechId,
                sortOrder = 200
            };
        }

    }
}
