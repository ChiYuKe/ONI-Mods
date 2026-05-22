using HarmonyLib;
using StorageNetwork.Buildings;

namespace StorageNetwork.Patches
{
    public static class BuildingRegistrationPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Automation", StorageNetworkCableConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Automation", StorageNetworkHubConfig.ID);
                Db.Get().Techs.Get("SmartStorage").unlockedItemIDs.Add(StorageNetworkCableConfig.ID);
                Db.Get().Techs.Get("SmartStorage").unlockedItemIDs.Add(StorageNetworkHubConfig.ID);
            }
        }
    }
}
