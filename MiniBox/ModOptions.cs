using Newtonsoft.Json;
using PeterHan.PLib.Options;


namespace MiniBox
{
    internal class ModOptions
    {
        [JsonObject(MemberSerialization.OptIn)]
        [ConfigFile("MiniBox.json", true, true)]
        [RestartRequired]
        public class Settings : SingletonOptions<Settings>
        {
            //挖掘掉落倍率
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.DIGGINGDROPRATE.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.DIGGINGDROPRATE.TOOLTIP", OptionStrings.Misc.Category, Format = "F1")]
            [Limit(0.1f, 100f)]
            [JsonProperty]
            public float DiggingDropRate { get; set; } = 0.5f;

            //复制人睡觉
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.MINIONSLEEP.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.MINIONSLEEP.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("NoWantSleepPatch")]
            public bool DisableFatigue { get; set; } = false;

            // 擦水
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.WATERING.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.WATERING.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("WateringPatch")]
            public bool UnlimitedMopping { get; set; } = false;

            //超级太空服
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.SUPERSPACESUIT.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.SUPERSPACESUIT.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("SuperSpaceSuitPatch")]
            public bool EnableSuperSpaceSuit { get; set; } = false;

            // 通用生存需求
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOFOOD.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOFOOD.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("NoFoodPatch")]
            public bool DisableHunger { get; set; } = false;
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOBLADDER.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOBLADDER.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("NoBladderPatch")]
            public bool DisableBladder { get; set; } = false;
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOSTRESS.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.NOSTRESS.TOOLTIP", OptionStrings.Misc.Category)]
            [JsonProperty("NoStressPatch")]
            public bool DisableStress { get; set; } = false;
            [Option("STRINGS.CONFIGURATIONITEM.MISCCONFIG.CARRYCAPACITY.TITLE", "STRINGS.CONFIGURATIONITEM.MISCCONFIG.CARRYCAPACITY.TOOLTIP", OptionStrings.Misc.Category, Format = "F1")]
            [Limit(0.1f, 100f)]
            [JsonProperty("CarryCapacityMultiplier")]
            public float CarryCapacityMultiplier { get; set; } = 1f;





            //轨道容量
            [Option("STRINGS.CONFIGURATIONITEM.TRANSPORTROUTECONFIG.TRANSPORTTRACK.TITLE", "STRINGS.CONFIGURATIONITEM.TRANSPORTROUTECONFIG.TRANSPORTTRACK.TOOLTIP", OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(20.0, 10000.0)]
            [JsonProperty("TransportTrackMaxCapacity")]
            public float ConveyorRailMaxPackageMassKg { get; set; } = 20f;

            //运输存放器&运输装载器
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.SOLIDCONDUITOUTBOX.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(20.0, 10000.0)]
            [JsonProperty]
            public float SolidConduitOutboxCapacityKg { get; set; } = 2000f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.SOLIDCONDUITINBOX.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(20.0, 10000.0)]
            [JsonProperty]
            public float SolidConduitInboxCapacityKg { get; set; } = 2000f;

            //储物箱
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.STORAGELOCKER.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(0, 10000000)]
            [JsonProperty]
            public float StorageLockerCapacityKg { get; set; } = 20000f;


            //气库
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.GASRESERVOIR.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(0, 100000)]
            [JsonProperty]
            public float GasReservoirCapacityKg { get; set; } = 20000f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.GASRESERVOIR.FOUNDATION", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("GasReservoirFoundation")]
            public bool GasReservoirRequiresFoundation { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.GASRESERVOIR.OVERHEATABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("GasReservoirOverheatable")]
            public bool GasReservoirCanOverheat { get; set; } = true;




            //液库
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.LIQUIDRESERVOIR.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(0, 100000)]
            [JsonProperty]
            public float LiquidReservoirCapacityKg { get; set; } = 20000f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.LIQUIDRESERVOIR.FOUNDATION", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("LiquidReservoirFoundation")]
            public bool LiquidReservoirRequiresFoundation { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.LIQUIDRESERVOIR.OVERHEATABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("LiquidReservoirOverheatable")]
            public bool LiquidReservoirCanOverheat { get; set; } = true;







            // 电线配置项
            [Option(OptionStrings.Wiring.Wire500WTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("Wire500W")]
            public float Wire500WLoad { get; set; } = 500f;
            [Option(OptionStrings.Wiring.WireLoadKwTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("Wires")]
            public float WireLoadKw { get; set; } = 1f;
            [Option(OptionStrings.Wiring.ConductiveWireLoadKwTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("Conductors")]
            public float ConductiveWireLoadKw { get; set; } = 2f;
            [Option(OptionStrings.Wiring.RubberWireLoadKwTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("RubberWires")]
            public float RubberWireLoadKw { get; set; } = 4f;
            [Option(OptionStrings.Wiring.HighLoadWireLoadKwTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("HighLoadWires")]
            public float HighLoadWireLoadKw { get; set; } = 20f;
            [Option(OptionStrings.Wiring.HighLoadConductiveWireLoadKwTitle, OptionStrings.Wiring.WiringTooltip, OptionStrings.Wiring.WiringCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty("HighLoadConductors")]
            public float HighLoadConductiveWireLoadKw { get; set; } = 50f;



            //小变压器&大变压器
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.POWERTRANSFORMERSMALL.HEATGENERATION", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("PowerTransformerSmallHeatGeneration")]
            public bool EnableSmallPowerTransformerHeatGeneration { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.POWERTRANSFORMER.HEATGENERATION", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("PowerTransformerHeatGeneration")]
            public bool EnablePowerTransformerHeatGeneration { get; set; } = true;











            //电解器制氧
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.TITLE", OptionStrings.Buildings.OxygenOutputTooltip, OptionStrings.Buildings.BuildingCategory, Format = "F1")]
            [Limit(0, 1000000)]
            [JsonProperty("MineralDeoxidizerEmissionValues")]
            public float MineralDeoxidizerOxygenOutputKgPerSecond { get; set; } = 0.5f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.OUTPUTTEMPERATURE", OptionStrings.Buildings.OutputTemperatureTooltip, OptionStrings.Buildings.BuildingCategory, Format = "F0")]
            [Limit(0, 100000000)]
            [JsonProperty]
            public float MineralDeoxidizerOutputTemperature { get; set; } = 30f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.ENERGYCONSUMPTIONWHENACTIVE", OptionStrings.Buildings.PowerConsumptionTooltip, OptionStrings.Buildings.BuildingCategory, Format = "F0")]
            [Limit(0, 500)]
            [JsonProperty]
            public float MineralDeoxidizerPowerConsumption { get; set; } = 120f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.FLOODABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("MineralDeoxidizerFloodable")]
            public bool MineralDeoxidizerCanFlood { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.OVERHEATABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("MineralDeoxidizerOverheatable")]
            public bool MineralDeoxidizerCanOverheat { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.HEATGENERATION", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("MineralDeoxidizerHeatGeneration")]
            public bool EnableMineralDeoxidizerHeatGeneration { get; set; } = true;




            //冰箱
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.REFRIGERATOR.ENERGYCONSUMPTIONWHENACTIVE", OptionStrings.Buildings.PowerConsumptionTooltip, OptionStrings.Buildings.BuildingCategory, Format = "F0")]
            [Limit(0, 500)]
            [JsonProperty("RefrigeratorEmissionValues")]
            public float RefrigeratorPowerConsumption { get; set; } = 120f;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.REFRIGERATOR.FLOODABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("RefrigeratorFloodable")]
            public bool RefrigeratorCanFlood { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.REFRIGERATOR.OVERHEATABLE", OptionStrings.Buildings.ToggleTooltip, OptionStrings.Buildings.BuildingCategory)]
            [JsonProperty("RefrigeratorOverheatable")]
            public bool RefrigeratorCanOverheat { get; set; } = true;
            [Option("STRINGS.CONFIGURATIONITEM.BUILDDINGS.REFRIGERATOR.CAPACITY", OptionStrings.Buildings.CapacityTooltip, OptionStrings.Buildings.CapacityCategory, Format = "F0")]
            [Limit(0, 1000000)]
            [JsonProperty]
            public float RefrigeratorCapacityKg { get; set; } = 100f;





            // 植物配置项


            [Option(OptionStrings.Plant.EnableCropPatch, OptionStrings.Plant.EnableCropPatchTooltip, OptionStrings.Plant.PlantCategory)]
            [JsonProperty]
            public bool EnableCropPatch { get; set; } = true;


            // === 米虱木 (BasicPlantFood) 600/1===
            [Option(OptionStrings.Plant.BasicPlantFoodTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1800)]
            [JsonProperty]
            public float BasicPlantFood_GrowTime { get; set; } = 1800f;
            [Option(OptionStrings.Plant.BasicPlantFoodTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(1, 10000)]
            [JsonProperty]
            public int BasicPlantFood_Yield { get; set; } = 1;

            // === 毛刺花 (PrickleFruit) ===
            [Option(OptionStrings.Plant.PrickleFruitTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 3600)]
            [JsonProperty]
            public float PrickleFruit_GrowTime { get; set; } = 3600f;
            [Option(OptionStrings.Plant.PrickleFruitTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(1, 10000)]
            [JsonProperty]
            public int PrickleFruit_Yield { get; set; } = 1;

            // === 沼浆笼 (SwampFruit) ===
            [Option(OptionStrings.Plant.SwampFruitTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 3960)]
            [JsonProperty]
            public float SwampFruit_GrowTime { get; set; } = 3960f;
            [Option(OptionStrings.Plant.SwampFruitTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(1, 10000)]
            [JsonProperty]
            public int SwampFruit_Yield { get; set; } = 1;

            // === 夜幕菇 (Mushroom) ===
            [Option(OptionStrings.Plant.MushroomTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 4500)]
            [JsonProperty]
            public float Mushroom_GrowTime { get; set; } = 4500f;
            [Option(OptionStrings.Plant.MushroomTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(1, 10000)]
            [JsonProperty]
            public int MushroomPlant_Yield { get; set; } = 1;

            // === 冰霜小麦 (ColdWheatSeed) ===
            [Option(OptionStrings.Plant.ColdWheatSeedTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 10800)]
            [JsonProperty]
            public float ColdWheatSeed_GrowTime { get; set; } = 10800f;
            [Option(OptionStrings.Plant.ColdWheatSeedTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(18, 10000)]
            [JsonProperty]
            public int ColdWheatSeed_Yield { get; set; } = 18;

            // === 火椒藤 (SpiceNut) ===
            [Option(OptionStrings.Plant.SpiceNutTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 4800)]
            [JsonProperty]
            public float SpiceNut_GrowTime { get; set; } = 4800f;
            [Option(OptionStrings.Plant.SpiceNutTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(4, 10000)]
            [JsonProperty]
            public int SpiceNut_Yield { get; set; } = 4;

            // === 顶针芦苇 (BasicFabric) ===
            [Option(OptionStrings.Plant.BasicFabricTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1200)]
            [JsonProperty]
            public float BasicFabric_GrowTime { get; set; } = 1200f;
            [Option(OptionStrings.Plant.BasicFabricTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(1, 10000)]
            [JsonProperty]
            public int BasicFabric_Yield { get; set; } = 1;

            // === 芳香百合 (SwampLilyFlower) ===
            [Option(OptionStrings.Plant.SwampLilyFlowerTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 7200)]
            [JsonProperty]
            public float SwampLilyFlower_GrowTime { get; set; } = 7200f;
            [Option(OptionStrings.Plant.SwampLilyFlowerTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(2, 10000)]
            [JsonProperty]
            public int SwampLilyFlower_Yield { get; set; } = 2;

            // === 释气草 (PlantFiber) ===
            [Option(OptionStrings.Plant.PlantFiberTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 2400)]
            [JsonProperty]
            public float PlantFiber_GrowTime { get; set; } = 2400f;
            [Option(OptionStrings.Plant.PlantFiberTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory)]
            [Limit(400, 10000)]
            [JsonProperty]
            public int PlantFiber_Yield { get; set; } = 400;


            // === 木材 (WoodLog) ===
            [Option(OptionStrings.Plant.WoodLogTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 2700)]
            [JsonProperty]
            public float WoodLog_GrowTime { get; set; } = 2700f;
            [Option(OptionStrings.Plant.WoodLogTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(300, 100000)]
            [JsonProperty]
            public int WoodLog_Yield { get; set; } = 300;

            // === 蜜露 (SugarWater) ===
            [Option(OptionStrings.Plant.SugarWaterTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 150)]
            [JsonProperty]
            public float SugarWater_GrowTime { get; set; } = 150f; 

            [Option(OptionStrings.Plant.SugarWaterTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(20, 100000)]
            [JsonProperty]
            public int SugarWater_Yield { get; set; } = 20;

            // === 糖心树木材 (SpaceTreeBranch) ===
            [Option(OptionStrings.Plant.SpaceTreeBranchTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 2700)]
            [JsonProperty]
            public float SpaceTreeBranch_GrowTime { get; set; } = 2700f;

            [Option(OptionStrings.Plant.SpaceTreeBranchTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int SpaceTreeBranch_Yield { get; set; } = 1;

            // === 刺壳果灌木 (HardSkinBerry) ===
            [Option(OptionStrings.Plant.HardSkinBerryTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1800)]
            [JsonProperty]
            public float HardSkinBerry_GrowTime { get; set; } = 1800f;

            [Option(OptionStrings.Plant.HardSkinBerryTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int HardSkinBerry_Yield { get; set; } = 1;

            // === 羽叶果薯 (Carrot) ===
            [Option(OptionStrings.Plant.CarrotTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 5400)]
            [JsonProperty]
            public float Carrot_GrowTime { get; set; } = 5400f;

            [Option(OptionStrings.Plant.CarrotTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(0, 100000)]
            [JsonProperty]
            public int Carrot_Yield { get; set; } = 1;


            // === 漫殖藤 (VineFruit) ===
            [Option(OptionStrings.Plant.VineFruitTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1800)]
            [JsonProperty]
            public float VineFruit_GrowTime { get; set; } = 1800f;
            [Option(OptionStrings.Plant.VineFruitTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int VineFruit_Yield { get; set; } = 1;


            // === 多孔芦荟 (OxyRock) ===
            [Option(OptionStrings.Plant.OxyRockTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1200)]
            [JsonProperty]
            public float OxyRock_GrowTime { get; set; } = 1200f;

            [Option(OptionStrings.Plant.OxyRockTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(2, 100000)]
            [JsonProperty]
            public int OxyRock_Yield { get; set; } = 36;

            // === 生菜 (Lettuce) ===
            [Option(OptionStrings.Plant.LettuceTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 7200)]
            [JsonProperty]
            public float Lettuce_GrowTime { get; set; } = 7200f;
            [Option(OptionStrings.Plant.LettuceTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(12, 100000)]
            [JsonProperty]
            public int Lettuce_Yield { get; set; } = 12;

            // === 海梳蕨 (Kelp) ===
            [Option(OptionStrings.Plant.KelpTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 3000)]
            [JsonProperty]
            public float Kelp_GrowTime { get; set; } = 3000f;
            [Option(OptionStrings.Plant.KelpTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(50, 100000)]
            [JsonProperty]
            public int Kelp_Yield { get; set; } = 50;


            // === 小吃芽 (BeanPlantSeed) ===
            [Option(OptionStrings.Plant.BeanPlantSeedTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 12600)]
            [JsonProperty]
            public float BeanPlantSeed_GrowTime { get; set; } = 12600f;
            [Option(OptionStrings.Plant.BeanPlantSeedTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(12, 100000)]
            [JsonProperty]
            public int BeanPlantSeed_Yield { get; set; } = 12;


            // === 植物肉 (PlantMeat) ===
            [Option(OptionStrings.Plant.PlantMeatTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 18000)]
            [JsonProperty]
            public float PlantMeat_GrowTime { get; set; } = 18000f;
            [Option(OptionStrings.Plant.PlantMeatTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(10, 100000)]
            [JsonProperty]
            public int PlantMeat_Yield { get; set; } = 10;


            // === 贫瘠虫果 ===
            [Option(OptionStrings.Plant.WormBasicFruitTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 2400)]
            [JsonProperty]
            public float WormBasicFruit_GrowTime { get; set; } = 2400f;
            [Option(OptionStrings.Plant.WormBasicFruitTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int WormBasicFruit_Yield { get; set; } = 1;

            // === 虫果 ===
            [Option(OptionStrings.Plant.WormSuperFruitTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 4800)]
            [JsonProperty]
            public float WormSuperFruit_GrowTime { get; set; } = 4800f;
            [Option(OptionStrings.Plant.WormSuperFruitTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(8, 100000)]
            [JsonProperty]
            public int WormSuperFruit_Yield { get; set; } = 8;



            // === 露珠藤 ===
            [Option(OptionStrings.Plant.DewDripTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1200)]
            [JsonProperty]
            public float DewDrip_GrowTime { get; set; } = 1200f;
            [Option(OptionStrings.Plant.DewDripTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int DewDrip_Yield { get; set; } = 1;

            // === 巨蕨 ===
            [Option(OptionStrings.Plant.FernFoodTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 5400)]
            [JsonProperty]
            public float FernFood_GrowTime { get; set; } = 5400f;
            [Option(OptionStrings.Plant.FernFoodTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(36, 100000)]
            [JsonProperty]
            public int FernFood_Yield { get; set; } = 36;

            // === 沙盐藤 ===
            [Option(OptionStrings.Plant.SaltTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 3600)]
            [JsonProperty]
            public float Salt_GrowTime { get; set; } = 3600f;
            [Option(OptionStrings.Plant.SaltTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(65, 100000)]
            [JsonProperty]
            public int Salt_Yield { get; set; } = 65;

            // === 仙水掌 ===
            [Option(OptionStrings.Plant.WaterTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 6000)]
            [JsonProperty]
            public float Water_GrowTime { get; set; } = 6000f;
            [Option(OptionStrings.Plant.WaterTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(350, 100000)]
            [JsonProperty]
            public int Water_Yield { get; set; } = 350;

            // === 露饵花 ===
            [Option(OptionStrings.Plant.AmberTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 7200)]
            [JsonProperty]
            public float Amber_GrowTime { get; set; } = 7200f;
            [Option(OptionStrings.Plant.AmberTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(264, 100000)]
            [JsonProperty]
            public int Amber_Yield { get; set; } = 264;

            // === 汗甜玉米 800/1===
            [Option(OptionStrings.Plant.GardenFoodPlantFoodTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 1800)]
            [JsonProperty]
            public float GardenFoodPlantFood_GrowTime { get; set; } = 1800f;
            [Option(OptionStrings.Plant.GardenFoodPlantFoodTitle, OptionStrings.Plant.NumProducedTooltip, OptionStrings.Plant.NumProducedCategory, Format = "F0")]
            [Limit(1, 100000)]
            [JsonProperty]
            public int GardenFoodPlantFood_Yield { get; set; } = 1;

            // === 拟芽 ===
            [Option(OptionStrings.Plant.ButterflyTitle, OptionStrings.Plant.CropDurationTooltip, OptionStrings.Plant.CropDurationCategory, Format = "F1")]
            [Limit(0, 3000)]
            [JsonProperty]
            public float Butterfly_GrowTime { get; set; } = 3000f;

        }

        public static class OptionStrings
        {
            public static class Buildings
            {
                public const string CapacityCategory = "STRINGS.CONFIGURATIONITEM.TRANSPORTROUTECONFIG.TRANSPORTTRACK.CATEGORY";
                public const string BuildingCategory = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.CATEGORY";
                public const string CapacityTooltip = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.CAPACITY_TOOLTIP";
                public const string ToggleTooltip = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.TOGGLE_TOOLTIP";
                public const string OxygenOutputTooltip = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.OXYGENOUTPUT_TOOLTIP";
                public const string OutputTemperatureTooltip = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.MINERALDEOXIDIZER.OUTPUTTEMPERATURE_TOOLTIP";
                public const string PowerConsumptionTooltip = "STRINGS.CONFIGURATIONITEM.BUILDDINGS.POWERCONSUMPTION_TOOLTIP";
            }
            public static class Misc
            {
                public const string Category = "STRINGS.CONFIGURATIONITEM.MISCCONFIG.CATEGORY";
            }
          

            public static class Plant
            {
                public const string EnableCropPatch = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.ENABLECROPPATCH";
                public const string EnableCropPatchTooltip = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.ENABLECROPPATCH_TOOLTIP";
                public const string PlantCategory = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.CATEGORY";
                public const string CropDurationCategory = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.CROPDURATION";
                public const string NumProducedCategory = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.NUMPRODUCED";
                public const string CropDurationTooltip = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.CROPDURATION_TOOLTIP";
                public const string NumProducedTooltip = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.NUMPRODUCED_TOOLTIP";

                public const string BasicPlantFoodTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.BASICPLANTFOOD";
                public const string PrickleFruitTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.PRICKLEFRUIT";
                public const string SwampFruitTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SWAMPFRUIT";
                public const string MushroomTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.MUSHROOM";
                public const string ColdWheatSeedTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.COLDWHEATSEED";
                public const string SpiceNutTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SPICENUT";
                public const string BasicFabricTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.BASICFABRIC";
                public const string SwampLilyFlowerTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SWAMPLILYFLOWER";
                public const string PlantFiberTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.PLANTFIBER";
                public const string WoodLogTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.WOODLOG";
                public const string SugarWaterTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SUGARWATER";
                public const string SpaceTreeBranchTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SPACETREEBRANCH";
                public const string HardSkinBerryTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.HARDSKINBERRY";
                public const string CarrotTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.CARROT";
                public const string VineFruitTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.VINEFRUIT";
                public const string OxyRockTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.OXYROCK";
                public const string LettuceTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.LETTUCE";
                public const string KelpTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.KELP";
                public const string BeanPlantSeedTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.BEANPLANTSEED";
                public const string OxyfernSeedTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.OXYFERNSEED";
                public const string PlantMeatTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.PLANTMEAT";
                public const string WormBasicFruitTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.WORMSBASICFRUIT";
                public const string WormSuperFruitTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.WORMSUPERFRUIT";
                public const string DewDripTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.DEWDRIP";
                public const string FernFoodTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.FERNFOOD";
                public const string SaltTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.SALT";
                public const string WaterTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.WATER";
                public const string AmberTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.AMBER";
                public const string GardenFoodPlantFoodTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.GARDENFOODPLANTFOOD";
                public const string ButterflyTitle = "STRINGS.CONFIGURATIONITEM.PLANTCONFIG.PLANT.BUTTERFLY";
            }
            public static class Wiring
            {

                public const string Wire500WTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.WIRE500W";
                public const string WireLoadKwTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.WIRES";
                public const string ConductiveWireLoadKwTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.CONDUCTORS";
                public const string RubberWireLoadKwTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.RUBBERWIRES";
                public const string HighLoadWireLoadKwTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.HIGHLOADWIRES";
                public const string HighLoadConductiveWireLoadKwTitle = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.HIGHLOADCONDUCTORS";
                public const string WiringTooltip = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.TOOLTIP";
                public const string WiringCategory = "STRINGS.CONFIGURATIONITEM.POWERCONFIG.WIRING.CATEGORY";

            }
        }
    }
}




