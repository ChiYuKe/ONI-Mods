using StorageNetwork.Buildings;

namespace StorageNetwork.Gameplay
{
    internal static class StorageNetworkBuildingPlanInstaller
    {
        private const string BaseCategory = "Base";
        private const string FoodCategory = "Food";
        private const string GasCategory = "HVAC";
        private const string LiquidCategory = "Plumbing";
        private const string PowerCategory = "Power";
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
            Add(BaseCategory, StorageNetworkCoreConfig.ID);
            Add(BaseCategory, SmallSolidServerConfig.ID);
            Add(BaseCategory, MediumSolidServerConfig.ID);
            Add(BaseCategory, LargeSolidServerConfig.ID);
            Add(BaseCategory, StorageNetworkSolidInputPortConfig.ID);
            Add(BaseCategory, StorageNetworkSolidOutputPortConfig.ID);

            Add(LiquidCategory, SmallLiquidServerConfig.ID);
            Add(LiquidCategory, MediumLiquidServerConfig.ID);
            Add(LiquidCategory, LargeLiquidServerConfig.ID);
            Add(LiquidCategory, StorageNetworkLiquidInputPortConfig.ID);
            Add(LiquidCategory, StorageNetworkLiquidOutputPortConfig.ID);

            Add(GasCategory, SmallGasServerConfig.ID);
            Add(GasCategory, MediumGasServerConfig.ID);
            Add(GasCategory, LargeGasServerConfig.ID);
            Add(GasCategory, StorageNetworkGasInputPortConfig.ID);
            Add(GasCategory, StorageNetworkGasOutputPortConfig.ID);

            Add(PowerCategory, SmallBatteryServerConfig.ID);
            Add(PowerCategory, MediumBatteryServerConfig.ID);
            Add(PowerCategory, LargeBatteryServerConfig.ID);
            Add(PowerCategory, StorageNetworkPowerInputPortConfig.ID);
            Add(PowerCategory, StorageNetworkPowerOutputPortConfig.ID);

            Add(FoodCategory, SmallColdStorageServerConfig.ID);
            Add(FoodCategory, MediumColdStorageServerConfig.ID);
            Add(FoodCategory, LargeColdStorageServerConfig.ID);

        }

        private static void Add(string category, string buildingId)
        {
            ModUtil.AddBuildingToPlanScreen(category, buildingId, StorageNetworkSubcategory);
        }
    }
}
