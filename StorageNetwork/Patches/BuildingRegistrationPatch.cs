using HarmonyLib;
using StorageNetwork.Buildings;

namespace StorageNetwork.Patches
{
    public static class BuildingRegistrationPatch
    {
        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Base", SceneStorageBoxConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class DbInitializePatch
        {
            public static void Postfix()
            {
                Tech tech = Db.Get().Techs.Get("SmartStorage");
                if (tech != null && !tech.unlockedItemIDs.Contains(SceneStorageBoxConfig.ID))
                {
                    tech.unlockedItemIDs.Add(SceneStorageBoxConfig.ID);
                }
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        public static class LocalizationPatch
        {
            public static void Postfix()
            {
                LocString.CreateLocStringKeys(typeof(STRINGS), null);
            }
        }
    }
}
