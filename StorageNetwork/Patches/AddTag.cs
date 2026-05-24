using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using StorageNetwork.Buildings;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class AddTagPatches
    {
        // Solid storage
        [HarmonyPatch(typeof(StorageLockerConfig), nameof(StorageLockerConfig.ConfigureBuildingTemplate))]
        public static class StorageLockerConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Storage,true);
            }
        }

        // Liquid reservoir
        [HarmonyPatch(typeof(LiquidReservoirConfig), nameof(LiquidReservoirConfig.ConfigureBuildingTemplate))]
        public static class LiquidReservoirConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Liquid, true);
            }
        }

        // Gas reservoir
        [HarmonyPatch(typeof(GasReservoirConfig), nameof(GasReservoirConfig.ConfigureBuildingTemplate))]
        public static class GasReservoirConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Gas, true);
            }
        }

        [HarmonyPatch(typeof(HydroponicFarmConfig), nameof(HydroponicFarmConfig.ConfigureBuildingTemplate))]
        public static class HydroponicFarmConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Planting, true);
                go.AddOrGet<StorageNetworkFabricatorSettings>();
            }
        }

        [HarmonyPatch(typeof(PlanterBoxConfig), nameof(PlanterBoxConfig.ConfigureBuildingTemplate))]
        public static class PlanterBoxConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Planting, true);
                go.AddOrGet<StorageNetworkFabricatorSettings>();
            }
        }

        [HarmonyPatch(typeof(FarmTileConfig), nameof(FarmTileConfig.ConfigureBuildingTemplate))]
        public static class FarmTileConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.Planting, true);
                go.AddOrGet<StorageNetworkFabricatorSettings>();
            }
        }

        // Oxygen-category gas production buildings
        //[HarmonyPatch(typeof(MineralDeoxidizerConfig), nameof(MineralDeoxidizerConfig.ConfigureBuildingTemplate))]
        //public static class MineralDeoxidizerConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(SublimationStationConfig), nameof(SublimationStationConfig.ConfigureBuildingTemplate))]
        //public static class SublimationStationConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(OxysconceConfig), nameof(OxysconceConfig.ConfigureBuildingTemplate))]
        //public static class OxysconceConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(ElectrolyzerConfig), nameof(ElectrolyzerConfig.ConfigureBuildingTemplate))]
        //public static class ElectrolyzerConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(RustDeoxidizerConfig), nameof(RustDeoxidizerConfig.ConfigureBuildingTemplate))]
        //public static class RustDeoxidizerConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(AlgaeHabitatConfig), nameof(AlgaeHabitatConfig.ConfigureBuildingTemplate))]
        //public static class AlgaeHabitatConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(AirFilterConfig), nameof(AirFilterConfig.ConfigureBuildingTemplate))]
        //public static class AirFilterConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        //[HarmonyPatch(typeof(CO2ScrubberConfig), nameof(CO2ScrubberConfig.ConfigureBuildingTemplate))]
        //public static class CO2ScrubberConfigPatch
        //{
        //    public static void Postfix(GameObject go)
        //    {
        //        AddOxygenBuildingInterface(go);
        //    }
        //}

        // DropAllWorkable usually means the building has a user-facing storage.
        [HarmonyPatch(typeof(DropAllWorkable), "OnPrefabInit")]
        public static class DropAllWorkablePatch
        {
            public static void Postfix(DropAllWorkable __instance)
            {
                if (__instance == null || __instance.gameObject == null)
                {
                    return;
                }

                KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
                if (prefabId == null)
                {
                    return;
                }

                StorageNetworkTags.MarkStorageNetworkBuilding(prefabId, Tag.Invalid);
            }
        }

        [HarmonyPatch(typeof(ComplexFabricator), "OnPrefabInit")]
        public static class ComplexFabricatorPatch
        {
            public static void Postfix(ComplexFabricator __instance)
            {
                if (__instance == null || __instance.gameObject == null)
                {
                    return;
                }

                KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
                if (prefabId != null)
                {
                    StorageNetworkTags.MarkStorageNetworkBuilding(prefabId, StorageNetworkTags.IndustrialMachinery);
                }

                __instance.gameObject.AddOrGet<StorageNetworkFabricatorSettings>();
            }
        }

        [HarmonyPatch(typeof(ElementConverter), "OnPrefabInit")]
        public static class ElementConverterPatch
        {
            public static void Postfix(ElementConverter __instance)
            {
                if (__instance?.gameObject == null)
                {
                    return;
                }

                KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
                StorageNetworkTags.MarkStorageNetworkBuilding(prefabId, Tag.Invalid);
                __instance.gameObject.AddOrGet<StorageNetworkFabricatorSettings>();
            }
        }

        private static void AddOxygenBuildingInterface(GameObject go)
        {
            StorageNetworkTags.MarkStorageNetworkBuilding(go, StorageNetworkTags.GasProduction,false);
        }
    }
}
