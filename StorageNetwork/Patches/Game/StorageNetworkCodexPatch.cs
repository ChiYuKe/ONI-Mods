using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using Loc = StorageNetwork.STRINGS;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkCodexPatch
    {
        private static readonly string CategoryId = CodexCache.FormatLinkID("STORAGENETWORK");
        private static readonly string HomeCategoryId = CodexCache.FormatLinkID("HOME");
        private static readonly MethodInfo GenerateSingleBuildingEntryMethod = AccessTools.Method(
            typeof(CodexEntryGenerator),
            "GenerateSingleBuildingEntry",
            new[] { typeof(BuildingDef), typeof(string) });

        [HarmonyPatch(typeof(CodexCache), nameof(CodexCache.CodexCacheInit))]
        public static class CodexCacheInitPatch
        {
            public static void Postfix()
            {
                if (CodexCache.entries == null ||
                    CodexCache.entries.ContainsKey(CategoryId) ||
                    !CodexCache.entries.TryGetValue(HomeCategoryId, out CodexEntry homeEntry) ||
                    !(homeEntry is CategoryEntry homeCategory))
                {
                    return;
                }

                Dictionary<string, CodexEntry> storageNetworkEntries = new Dictionary<string, CodexEntry>();
                foreach (string buildingId in GetStorageNetworkBuildingIds())
                {
                    CodexEntry entry = FindOrCreateBuildingEntry(buildingId);
                    if (entry != null)
                    {
                        storageNetworkEntries[buildingId] = entry;
                    }
                }

                CodexEntry engravingDiskEntry = FindOrCreateEngravingDiskEntry();
                if (engravingDiskEntry != null)
                {
                    storageNetworkEntries[StorageNetworkEngravingDiskConfig.ID] = engravingDiskEntry;
                }

                if (storageNetworkEntries.Count == 0)
                {
                    return;
                }

                Sprite icon = StorageNetworkSprites.GetOverviewIcon() ?? Assets.GetSprite("codexIconBuildings");
                CategoryEntry categoryEntry = CodexEntryGenerator.GenerateCategoryEntry(
                    CategoryId,
                    STRINGS.UI.STORAGE_NETWORK.TITLE,
                    storageNetworkEntries,
                    icon,
                    true,
                    true,
                    STRINGS.UI.STORAGE_NETWORK.TITLE);
                categoryEntry.parentId = HomeCategoryId;
                categoryEntry.category = HomeCategoryId;
                categoryEntry.icon = icon;
                homeCategory.entriesInCategory.Add(categoryEntry);
            }

            private static IEnumerable<string> GetStorageNetworkBuildingIds()
            {
                return StorageNetworkStorageBuildingSpecs.UnlockIds
                    .Concat(StorageNetworkPortSpecs.AllIds)
                    .Distinct();
            }

            private static CodexEntry FindOrCreateBuildingEntry(string buildingId)
            {
                string codexId = CodexCache.FormatLinkID(buildingId);
                if (CodexCache.entries.TryGetValue(codexId, out CodexEntry existingEntry))
                {
                    return existingEntry;
                }

                BuildingDef buildingDef = Assets.GetBuildingDef(buildingId);
                if (buildingDef == null ||
                    buildingDef.DebugOnly ||
                    buildingDef.Deprecated ||
                    !Game.IsCorrectDlcActiveForCurrentSave(buildingDef) ||
                    GenerateSingleBuildingEntryMethod == null)
                {
                    return null;
                }

                CodexEntry generatedEntry = GenerateSingleBuildingEntryMethod.Invoke(
                    null,
                    new object[] { buildingDef, CategoryId }) as CodexEntry;
                if (generatedEntry != null && buildingDef.ExtendCodexEntry != null)
                {
                    generatedEntry = buildingDef.ExtendCodexEntry(generatedEntry);
                }

                return generatedEntry;
            }

            private static CodexEntry FindOrCreateEngravingDiskEntry()
            {
                string codexId = CodexCache.FormatLinkID(StorageNetworkEngravingDiskConfig.ID);
                if (CodexCache.entries.TryGetValue(codexId, out CodexEntry existingEntry))
                {
                    return existingEntry;
                }

                GameObject prefab = Assets.GetPrefab(StorageNetworkEngravingDiskConfig.ID);
                if (prefab == null)
                {
                    return null;
                }

                string name = Loc.Get(Loc.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME);
                string description = Loc.Get(Loc.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.DESC);
                string recipeDescription = Loc.Get(Loc.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC);
                Tuple<Sprite, Color> icon = Def.GetUISprite(prefab, "ui", false);
                List<ContentContainer> containers = new List<ContentContainer>
                {
                    new ContentContainer(new List<ICodexWidget>
                    {
                        new CodexText(name, CodexTextStyle.Title),
                        new CodexDividerLine()
                    }, ContentContainer.ContentLayout.Vertical),
                    new ContentContainer(new List<ICodexWidget>
                    {
                        new CodexImage(96, 96, icon),
                        new CodexText(description, CodexTextStyle.Body),
                        new CodexSpacer(),
                        new CodexText(recipeDescription, CodexTextStyle.Body)
                    }, ContentContainer.ContentLayout.Vertical)
                };

                CodexEntry entry = new CodexEntry(CategoryId, containers, name)
                {
                    parentId = CategoryId,
                    icon = icon.first,
                    iconColor = icon.second,
                    sortString = name
                };
                CodexCache.AddEntry(codexId, entry);
                return entry;
            }
        }
    }
}
