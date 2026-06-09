using HarmonyLib;
using System;
using TUNING;

namespace MiniBox.PlantConfig.Crops
{
    [HarmonyPatch(typeof(Assets), "SubstanceListHookup")]
    public class CropPatch
    {
        private static readonly CropPatchEntry[] CropPatches =
        {
            new CropPatchEntry("BasicPlantFood", cfg => cfg.BasicPlantFood_GrowTime, cfg => cfg.BasicPlantFood_Yield),
            new CropPatchEntry(PrickleFruitConfig.ID, cfg => cfg.PrickleFruit_GrowTime, cfg => cfg.PrickleFruit_Yield),
            new CropPatchEntry(SwampFruitConfig.ID, cfg => cfg.SwampFruit_GrowTime, cfg => cfg.SwampFruit_Yield),
            new CropPatchEntry(MushroomConfig.ID, cfg => cfg.Mushroom_GrowTime, cfg => cfg.MushroomPlant_Yield),
            new CropPatchEntry("ColdWheatSeed", cfg => cfg.ColdWheatSeed_GrowTime, cfg => cfg.ColdWheatSeed_Yield),
            new CropPatchEntry(SpiceNutConfig.ID, cfg => cfg.SpiceNut_GrowTime, cfg => cfg.SpiceNut_Yield),
            new CropPatchEntry(BasicFabricConfig.ID, cfg => cfg.BasicFabric_GrowTime, cfg => cfg.BasicFabric_Yield),
            new CropPatchEntry(SwampLilyFlowerConfig.ID, cfg => cfg.SwampLilyFlower_GrowTime, cfg => cfg.SwampLilyFlower_Yield),
            new CropPatchEntry("PlantFiber", cfg => cfg.PlantFiber_GrowTime, cfg => cfg.PlantFiber_Yield),
            new CropPatchEntry("WoodLog", cfg => cfg.WoodLog_GrowTime, cfg => cfg.WoodLog_Yield),
            new CropPatchEntry(SimHashes.SugarWater.ToString(), cfg => cfg.SugarWater_GrowTime, cfg => cfg.SugarWater_Yield),
            new CropPatchEntry("SpaceTreeBranch", cfg => cfg.SpaceTreeBranch_GrowTime, cfg => cfg.SpaceTreeBranch_Yield),
            new CropPatchEntry("HardSkinBerry", cfg => cfg.HardSkinBerry_GrowTime, cfg => cfg.HardSkinBerry_Yield),
            new CropPatchEntry(CarrotConfig.ID, cfg => cfg.Carrot_GrowTime, cfg => cfg.Carrot_Yield),
            new CropPatchEntry(VineFruitConfig.ID, cfg => cfg.VineFruit_GrowTime, cfg => cfg.VineFruit_Yield),
            new CropPatchEntry(SimHashes.OxyRock.ToString(), cfg => cfg.OxyRock_GrowTime, cfg => cfg.OxyRock_Yield),
            new CropPatchEntry("Lettuce", cfg => cfg.Lettuce_GrowTime, cfg => cfg.Lettuce_Yield),
            new CropPatchEntry(KelpConfig.ID, cfg => cfg.Kelp_GrowTime, cfg => cfg.Kelp_Yield),
            new CropPatchEntry("BeanPlantSeed", cfg => cfg.BeanPlantSeed_GrowTime, cfg => cfg.BeanPlantSeed_Yield),
            new CropPatchEntry("PlantMeat", cfg => cfg.PlantMeat_GrowTime, cfg => cfg.PlantMeat_Yield),
            new CropPatchEntry("WormBasicFruit", cfg => cfg.WormBasicFruit_GrowTime, cfg => cfg.WormBasicFruit_Yield),
            new CropPatchEntry("WormSuperFruit", cfg => cfg.WormSuperFruit_GrowTime, cfg => cfg.WormSuperFruit_Yield),
            new CropPatchEntry(DewDripConfig.ID, cfg => cfg.DewDrip_GrowTime, cfg => cfg.DewDrip_Yield),
            new CropPatchEntry(FernFoodConfig.ID, cfg => cfg.FernFood_GrowTime, cfg => cfg.FernFood_Yield),
            new CropPatchEntry(SimHashes.Salt.ToString(), cfg => cfg.Salt_GrowTime, cfg => cfg.Salt_Yield),
            new CropPatchEntry(SimHashes.Water.ToString(), cfg => cfg.Water_GrowTime, cfg => cfg.Water_Yield),
            new CropPatchEntry(SimHashes.Amber.ToString(), cfg => cfg.Amber_GrowTime, cfg => cfg.Amber_Yield),
            new CropPatchEntry("GardenFoodPlantFood", cfg => cfg.GardenFoodPlantFood_GrowTime, cfg => cfg.GardenFoodPlantFood_Yield),
            new CropPatchEntry("Butterfly", cfg => cfg.Butterfly_GrowTime, null),
        };

        public static void Postfix()
        {
            var cfg = ModSettings.Current;
            if (!cfg.EnableCropPatch)
                return;

            foreach (var cropPatch in CropPatches)
                PatchCrop(cfg, cropPatch);
        }

        private static void PatchCrop(ModOptions.Settings cfg, CropPatchEntry patch)
        {
            int index = CROPS.CROP_TYPES.FindIndex(crop => crop.cropId == patch.Id);
            if (index < 0)
                return;

            var current = CROPS.CROP_TYPES[index];
            float duration = patch.DurationSelector(cfg);
            int quantity = patch.QuantitySelector?.Invoke(cfg) ?? current.numProduced;

            CROPS.CROP_TYPES[index] = new Crop.CropVal(patch.Id, duration, quantity, true);
        }

        private readonly struct CropPatchEntry
        {
            public CropPatchEntry(
                string id,
                Func<ModOptions.Settings, float> durationSelector,
                Func<ModOptions.Settings, int> quantitySelector)
            {
                Id = id;
                DurationSelector = durationSelector;
                QuantitySelector = quantitySelector;
            }

            public string Id { get; }
            public Func<ModOptions.Settings, float> DurationSelector { get; }
            public Func<ModOptions.Settings, int> QuantitySelector { get; }
        }
    }
}





