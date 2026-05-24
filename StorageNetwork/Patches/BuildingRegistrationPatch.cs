using HarmonyLib;
using System.Collections.Generic;
using StorageNetwork.Buildings;

namespace StorageNetwork.Patches
{
    public static class BuildingRegistrationPatch
    {
        private const string StorageNetworkTechId = "StorageNetwork";
        private const string StorageNetworkTreeId = "_StorageNetwork";
        private const float StorageNetworkResearchNodeY = -7600f;

        [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
        public static class LoadGeneratedBuildingsPatch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Base", StorageNetworkCableConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Base", StorageNetworkHubConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Base", StorageNetworkCableBridgeConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Database.Techs), nameof(Database.Techs.Init))]
        public static class TechsInitPatch
        {
            public static void Postfix(Database.Techs __instance)
            {
                if (__instance.TryGet(StorageNetworkTechId) == null)
                {
                    new Tech(
                        StorageNetworkTechId,
                        new List<string> { StorageNetworkHubConfig.ID, StorageNetworkCableConfig.ID, StorageNetworkCableBridgeConfig.ID },
                        __instance,
                        null).AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
                }
            }
        }

        [HarmonyPatch(typeof(Database.TechTreeTitles), nameof(Database.TechTreeTitles.Load))]
        public static class TechTreeTitlesLoadPatch
        {
            public static void Postfix(Database.TechTreeTitles __instance)
            {
                if (__instance.TryGet(StorageNetworkTreeId) == null)
                {
                    new TechTreeTitle(
                        StorageNetworkTreeId,
                        __instance,
                        STRINGS.RESEARCH.TREES.TITLE_STORAGENETWORK,
                        CreateStorageNetworkTitleNode(__instance));
                }
            }
        }

        [HarmonyPatch(typeof(Database.Techs), nameof(Database.Techs.Load))]
        public static class TechsLoadPatch
        {
            public static void Prefix(Database.Techs __instance)
            {
                Tech storageNetworkTech = __instance.TryGet(StorageNetworkTechId);
                if (storageNetworkTech != null)
                {
                    SetStorageNetworkNode(storageNetworkTech);
                }
            }
        }

        [HarmonyPatch(typeof(Db), nameof(Db.PostProcess))]
        public static class DbPostProcessPatch
        {
            public static void Prefix(Db __instance)
            {
                Database.Techs techs = __instance.Techs;
                if (techs == null)
                {
                    return;
                }

                Tech storageNetworkTech = techs.TryGet(StorageNetworkTechId);
                if (storageNetworkTech == null)
                {
                    return;
                }

                BuildingRegistrationPatch.SetStorageNetworkNode(storageNetworkTech);
                storageNetworkTech.tier = 4;
                storageNetworkTech.costsByResearchTypeID.Clear();
                storageNetworkTech.costsByResearchTypeID["basic"] = 35f;

                SetUnlockOrder(storageNetworkTech);
            }

            private static void SetUnlockOrder(Tech tech)
            {
                tech.unlockedItemIDs.Remove(StorageNetworkHubConfig.ID);
                tech.unlockedItemIDs.Remove(StorageNetworkCableConfig.ID);
                tech.unlockedItemIDs.Remove(StorageNetworkCableBridgeConfig.ID);
                tech.unlockedItemIDs.Insert(0, StorageNetworkCableBridgeConfig.ID);
                tech.unlockedItemIDs.Insert(0, StorageNetworkCableConfig.ID);
                tech.unlockedItemIDs.Insert(0, StorageNetworkHubConfig.ID);

                if (tech.unlockedItems != null)
                {
                    tech.unlockedItems.Clear();
                    foreach (string itemId in tech.unlockedItemIDs)
                    {
                        TechItem item = Db.Get().TechItems.TryGet(itemId);
                        if (item != null)
                        {
                            tech.unlockedItems.Add(item);
                        }
                    }
                }
            }

        }

        private static void SetStorageNetworkNode(Tech tech)
        {
            ResourceTreeNode node = new ResourceTreeNode
            {
                Id = StorageNetworkTechId,
                Name = StorageNetworkTechId,
                nodeX = 200f,
                nodeY = StorageNetworkResearchNodeY,
                width = 250f,
                height = 100f
            };
            tech.SetNode(node, StorageNetworkTreeId);
        }

        private static ResourceTreeNode CreateStorageNetworkTitleNode(Database.TechTreeTitles titles)
        {
            return new ResourceTreeNode
            {
                Id = StorageNetworkTreeId,
                Name = StorageNetworkTreeId,
                nodeX = -150f,
                nodeY = StorageNetworkResearchNodeY,
                width = 250f,
                height = 100f
            };
        }

    }
}
