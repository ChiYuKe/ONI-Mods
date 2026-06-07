using StorageNetwork.Buildings;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkBuildingPlanInstaller
    {
        private const string StorageNetworkSubcategory = "StorageNetwork";

        public static void Install()
        {
            InstallRocketModuleSortOrder();
            InstallStorageBuildings();

            // 火箭舱由 SelectModuleSideScreen.moduleButtonSortOrder 显示。
            // 同时加入 Rocketry 建造菜单会让 Codex 为同一舱块生成两次条目。
        }

        private static void InstallRocketModuleSortOrder()
        {
            if (SelectModuleSideScreen.moduleButtonSortOrder.Contains(StorageNetworkRelayModuleConfig.ID))
            {
                return;
            }

            int insertIndex = SelectModuleSideScreen.moduleButtonSortOrder.IndexOf("ResearchClusterModule");
            if (insertIndex < 0)
            {
                insertIndex = SelectModuleSideScreen.moduleButtonSortOrder.Count - 1;
            }

            SelectModuleSideScreen.moduleButtonSortOrder.Insert(insertIndex + 1, StorageNetworkRelayModuleConfig.ID);
        }

        private static void InstallStorageBuildings()
        {
            foreach (string buildingId in StorageNetworkStorageBuildingSpecs.AllIds)
            {
                ModUtil.AddBuildingToPlanScreen("Base", buildingId, StorageNetworkSubcategory);
            }
        }
    }
}
