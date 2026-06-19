using HarmonyLib;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using StorageNetwork.Gameplay;
using StorageNetwork.Research;

namespace StorageNetwork.Patches
{
    public static class BuildingRegistrationPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Prefix()
            {
                StorageNetworkBuildingPlanInstaller.Install();
            }
        }

        [HarmonyPatch(typeof(EntityConfigManager), "LoadGeneratedEntities")]
        public static class LoadGeneratedEntitiesPatch
        {
            public static void Prefix()
            {
                Strings.Add(
                    "STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME",
                    STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME));
                Strings.Add(
                    "STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC",
                    STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC));
                Strings.Add(
                    "STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC",
                    STRINGS.Get(STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC));
                StorageNetworkEngravingDiskConfig.RegisterRecipe();
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class DbInitializePatch
        {
            public static void Postfix()
            {
                StorageNetworkResearchInstaller.RefreshUnlockedItems();
            }
        }

        [HarmonyPatch(typeof(Database.Techs), "Load")]
        public static class TechsLoadPatch
        {
            public static void Postfix(Database.Techs __instance)
            {
                StorageNetworkResearchInstaller.Install(__instance);
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class LocalizationPatch
        {
            public static void Postfix()
            {
                StorageNetworkLocalization.Translate(typeof(STRINGS), false);
            }
        }
    }
}
