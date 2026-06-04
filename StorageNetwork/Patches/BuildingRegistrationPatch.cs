using HarmonyLib;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using StorageNetwork.Research;

namespace StorageNetwork.Patches
{
    public static class BuildingRegistrationPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            private const string StorageNetworkSubcategory = "StorageNetwork";

            public static void Prefix()
            {
                if (!SelectModuleSideScreen.moduleButtonSortOrder.Contains(StorageNetworkRelayModuleConfig.ID))
                {
                    int insertIndex = SelectModuleSideScreen.moduleButtonSortOrder.IndexOf("ResearchClusterModule");
                    if (insertIndex < 0)
                    {
                        insertIndex = SelectModuleSideScreen.moduleButtonSortOrder.Count - 1;
                    }

                    SelectModuleSideScreen.moduleButtonSortOrder.Insert(insertIndex + 1, StorageNetworkRelayModuleConfig.ID);
                }

                foreach (string buildingId in StorageNetworkStorageBuildingSpecs.AllIds)
                {
                    ModUtil.AddBuildingToPlanScreen("Base", buildingId, StorageNetworkSubcategory);
                }

                // 火箭舱由 SelectModuleSideScreen.moduleButtonSortOrder 显示。
                // 同时加入 Rocketry 建造菜单会让 Codex 为同一舱块生成两次条目。
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
