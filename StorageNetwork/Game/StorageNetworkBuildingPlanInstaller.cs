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

            // Rocket modules are shown by SelectModuleSideScreen.moduleButtonSortOrder.
            // Adding the relay to the Rocketry build menu as well creates duplicate Codex entries.
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

            foreach (string buildingId in StorageNetworkPortSpecs.AllIds)
            {
                ModUtil.AddBuildingToPlanScreen("Base", buildingId, StorageNetworkSubcategory);
            }
        }
    }
}
