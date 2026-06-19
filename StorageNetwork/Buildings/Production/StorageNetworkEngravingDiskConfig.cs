using System.Collections.Generic;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Buildings
{
    public sealed class StorageNetworkEngravingDiskConfig : IEntityConfig
    {
        public const string ID = "StorageNetworkEngravingDisk";
        private const string CraftingStationId = "CraftingTable";
        private const float RecipeMetalMass = 50f;
        private const float RecipeTime = 20f;
        private static bool recipeRegistered;

        public string[] GetDlcIds()
        {
            return null;
        }

        public GameObject CreatePrefab()
        {
            RegisterRecipe();
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
                new List<Tag>());

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
            if (recipeRegistered)
            {
                return;
            }

            recipeRegistered = true;
            ComplexRecipe.RecipeElement[] inputs =
            {
                new ComplexRecipe.RecipeElement(GameTags.RefinedMetal, RecipeMetalMass)
            };
            ComplexRecipe.RecipeElement[] outputs =
            {
                new ComplexRecipe.RecipeElement(ID, 1f)
            };

            ComplexRecipe recipe = new ComplexRecipe(ComplexRecipeManager.MakeRecipeID(CraftingStationId, inputs, outputs), inputs, outputs)
            {
                time = RecipeTime,
                description = STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC),
                nameDisplay = ComplexRecipe.RecipeNameDisplay.Result,
                fabricators = new List<Tag> { CraftingStationId },
                sortOrder = 200
            };
        }
    }
}
