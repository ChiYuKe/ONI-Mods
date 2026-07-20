using LogicNetwork.Buildings;

namespace LogicNetwork
{
    internal static class LogicNetworkBuildingPlanInstaller
    {
        private const string AutomationCategory = "Automation";
        private const string LogicGatesCategory = "LogicGates";

        public static void Install()
        {
            ModUtil.AddBuildingToPlanScreen(
                AutomationCategory,
                LogicNetworkEmitterConfig.ID,
                LogicGatesCategory,
                "LogicGateNOT",
                ModUtil.BuildingOrdering.After);
            ModUtil.AddBuildingToHotkeyBuildMenu(LogicGatesCategory, LogicNetworkEmitterConfig.ID, global::Action.NumActions);
        }
    }
}
