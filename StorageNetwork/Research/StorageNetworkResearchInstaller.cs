using System.Collections.Generic;
using StorageNetwork.Buildings;
using Loc = StorageNetwork.STRINGS;
using UnityEngine;

namespace StorageNetwork.Research
{
    public static class StorageNetworkResearchInstaller
    {
        private const string TreeTitleId = "_StorageNetwork";
        private const string AnchorTreeTitleId = "_Computers";
        private const float DefaultStartX = 675f;
        private const float TitleOffsetY = -860f;
        private const float NodeOffsetY = 220f;
        private const float DefaultNodeSpacingX = 350f;
        private const float TitleWidth = 250f;
        private const float TitleHeight = 72f;
        private const float NodeWidth = 250f;
        private const float NodeHeight = 110f;

        private static readonly StorageNetworkTechSpec[] TechSpecs =
        {
            new StorageNetworkTechSpec("StorageNetworkResearchCore", "STORAGENETWORKRESEARCHCORE", StorageNetworkResearchText.Core, new[] { StorageNetworkCoreConfig.ID }, 35f, 0f, 0f),
            new StorageNetworkTechSpec("StorageNetworkResearchSmallStorage", "STORAGENETWORKRESEARCHSMALLSTORAGE", StorageNetworkResearchText.SmallStorage, new[] { SmallSolidServerConfig.ID, SmallLiquidServerConfig.ID, SmallGasServerConfig.ID }, 50f, 0f, 0f),
            new StorageNetworkTechSpec("StorageNetworkResearchMediumStorage", "STORAGENETWORKRESEARCHMEDIUMSTORAGE", StorageNetworkResearchText.MediumStorage, new[] { MediumSolidServerConfig.ID, MediumLiquidServerConfig.ID, MediumGasServerConfig.ID }, 50f, 30f, 0f),
            new StorageNetworkTechSpec("StorageNetworkResearchLargeStorage", "STORAGENETWORKRESEARCHLARGESTORAGE", StorageNetworkResearchText.LargeStorage, new[] { LargeSolidServerConfig.ID, LargeLiquidServerConfig.ID, LargeGasServerConfig.ID }, 70f, 50f, 0f),
            new StorageNetworkTechSpec("StorageNetworkResearchRelay", "STORAGENETWORKRESEARCHRELAY", StorageNetworkResearchText.Relay, new[] { StorageNetworkRelayModuleConfig.ID }, 70f, 100f, 200f)
        };

        public static void Install(Database.Techs techs)
        {
            EnsureResearchStrings();
            RemoveOldStorageNetworkUnlocks(techs);

            Vector2 titleCenter = GetTitleCenter(techs);
            InstallTreeTitle(titleCenter);

            for (int i = 0; i < TechSpecs.Length; i++)
            {
                Tech tech = GetOrCreateTech(techs, TechSpecs[i]);
                LinkStorageNetworkTechs(techs, tech, i);
                PlaceNode(techs, tech, titleCenter, i);
            }

            LinkNodeEdges(techs);
        }

        public static void RefreshUnlockedItems()
        {
            RemoveOldStorageNetworkUnlocks(Db.Get().Techs);

            foreach (StorageNetworkTechSpec spec in TechSpecs)
            {
                Tech tech = Db.Get().Techs.TryGet(spec.TechId);
                if (tech == null)
                {
                    continue;
                }

                SyncUnlockIds(tech, spec);
                tech.unlockedItems.Clear();
                foreach (string buildingId in tech.unlockedItemIDs)
                {
                    TechItem techItem = Db.Get().TechItems.TryGet(buildingId);
                    if (techItem == null)
                    {
                        continue;
                    }

                    techItem.parentTechId = spec.TechId;
                    if (!tech.unlockedItems.Contains(techItem))
                    {
                        tech.unlockedItems.Add(techItem);
                    }
                }
            }
        }

        private static void RemoveOldStorageNetworkUnlocks(Database.Techs techs)
        {
            HashSet<string> storageNetworkIds = new HashSet<string>(StorageNetworkStorageBuildingSpecs.UnlockIds);
            foreach (Tech tech in techs.resources)
            {
                tech.unlockedItemIDs.RemoveAll(storageNetworkIds.Contains);
                tech.unlockedItems.RemoveAll(item => item != null && storageNetworkIds.Contains(item.Id));
            }
        }

        private static Tech GetOrCreateTech(Database.Techs techs, StorageNetworkTechSpec spec)
        {
            Tech tech = techs.TryGet(spec.TechId);
            if (tech == null)
            {
                tech = new Tech(spec.TechId, new List<string>(), techs, spec.CreateCostOverride());
                tech.AddSearchTerms(global::STRINGS.SEARCH_TERMS.STORAGE);
            }

            SyncUnlockIds(tech, spec);
            return tech;
        }

        private static void SyncUnlockIds(Tech tech, StorageNetworkTechSpec spec)
        {
            tech.unlockedItemIDs.Clear();
            tech.unlockedItemIDs.AddRange(spec.BuildingIds);
        }

        private static void LinkStorageNetworkTechs(Database.Techs techs, Tech tech, int index)
        {
            tech.requiredTech.Clear();
            foreach (Tech candidate in techs.resources)
            {
                candidate.unlockedTech.Remove(tech);
            }

            if (index > 0)
            {
                Tech previous = techs.TryGet(TechSpecs[index - 1].TechId);
                if (previous != null)
                {
                    tech.requiredTech.Add(previous);
                    previous.unlockedTech.Add(tech);
                }
            }

            tech.tier = Database.Techs.GetTier(tech);
        }

        private static void InstallTreeTitle(Vector2 titleCenter)
        {
            Database.TechTreeTitles titles = Db.Get().TechTreeTitles;
            if (titles.TryGet(TreeTitleId) != null)
            {
                return;
            }

            new TechTreeTitle(
                TreeTitleId,
                titles,
                STRINGS.Get(STRINGS.RESEARCH.TREES.TITLE_STORAGENETWORK),
                CreateTitleNode(titleCenter));
        }

        private static void PlaceNode(Database.Techs techs, Tech tech, Vector2 titleCenter, int index)
        {
            if (tech.FoundNode)
            {
                return;
            }

            Vector2 center = GetNodeCenter(techs, titleCenter, index);
            ResourceTreeNode node = CreateNode(tech.Id, center);

            tech.SetNode(node, TreeTitleId);
        }

        private static void LinkNodeEdges(Database.Techs techs)
        {
            for (int i = 0; i < TechSpecs.Length - 1; i++)
            {
                Tech current = techs.TryGet(TechSpecs[i].TechId);
                Tech next = techs.TryGet(TechSpecs[i + 1].TechId);
                if (current == null || next == null || !current.FoundNode || !next.FoundNode)
                {
                    continue;
                }

                current.edges.Clear();
                current.edges.Add(new ResourceTreeNode.Edge(
                    CreateEdgePoint(current.Id, current.center),
                    CreateEdgePoint(next.Id, next.center),
                    ResourceTreeNode.Edge.EdgeType.PolyLineEdge));
            }
        }

        private static Vector2 GetTitleCenter(Database.Techs techs)
        {
            TechTreeTitle anchorTitle = Db.Get().TechTreeTitles.TryGet(AnchorTreeTitleId);
            if (anchorTitle != null)
            {
                return new Vector2(anchorTitle.center.x, anchorTitle.center.y + TitleOffsetY);
            }

            return new Vector2(-25f, 7000f);
        }

        private static Vector2 GetNodeCenter(Database.Techs techs, Vector2 titleCenter, int index)
        {
            float x = GetAutomationStartX(techs);
            float spacing = GetAutomationColumnSpacing(techs);
            return new Vector2(x + index * spacing, titleCenter.y - NodeOffsetY);
        }

        // 对齐自动化行最左侧卡片，避免不同 DLC 研究树里固定节点不在同一列。
        private static float GetAutomationStartX(Database.Techs techs)
        {
            float startX = float.MaxValue;
            foreach (Tech tech in techs.resources)
            {
                if (tech == null || !tech.FoundNode || tech.category != AnchorTreeTitleId)
                {
                    continue;
                }

                startX = Mathf.Min(startX, tech.center.x);
            }

            return startX < float.MaxValue ? startX : DefaultStartX;
        }

        // 使用自动化行现有列距，让新增卡片和上方研究卡竖向对齐。
        private static float GetAutomationColumnSpacing(Database.Techs techs)
        {
            float firstX = float.MaxValue;
            float secondX = float.MaxValue;
            foreach (Tech tech in techs.resources)
            {
                if (tech == null || !tech.FoundNode || tech.category != AnchorTreeTitleId)
                {
                    continue;
                }

                float x = tech.center.x;
                if (x < firstX)
                {
                    secondX = firstX;
                    firstX = x;
                }
                else if (x > firstX && x < secondX)
                {
                    secondX = x;
                }
            }

            return secondX < float.MaxValue ? secondX - firstX : DefaultNodeSpacingX;
        }

        private static ResourceTreeNode CreateTitleNode(Vector2 center)
        {
            ResourceTreeNode node = CreateBareNode(TreeTitleId, TitleWidth, TitleHeight);
            node.nodeX = center.x - node.width / 2f;
            node.nodeY = center.y + node.height / 2f;
            return node;
        }

        private static ResourceTreeNode CreateNode(string id, Vector2 center)
        {
            ResourceTreeNode node = CreateBareNode(id, NodeWidth, NodeHeight);
            node.nodeX = center.x - node.width / 2f;
            node.nodeY = center.y + node.height / 2f;
            return node;
        }

        private static ResourceTreeNode CreateEdgePoint(string id, Vector2 center)
        {
            ResourceTreeNode node = CreateBareNode(id, NodeWidth, NodeHeight);
            node.nodeX = center.x - node.width / 2f;
            node.nodeY = center.y + node.height / 2f;
            return node;
        }

        private static void EnsureResearchStrings()
        {
            foreach (StorageNetworkTechSpec spec in TechSpecs)
            {
                Strings.Add("STRINGS.RESEARCH.TECHS." + spec.StringId + ".NAME", Loc.Get(spec.Name));
                Strings.Add("STRINGS.RESEARCH.TECHS." + spec.StringId + ".DESC", Loc.Get(spec.Desc));
            }

            Strings.Add("STRINGS.RESEARCH.TREES.TITLE_STORAGENETWORK", Loc.Get(Loc.RESEARCH.TREES.TITLE_STORAGENETWORK));
        }

        private static ResourceTreeNode CreateBareNode(string id, float width, float height)
        {
            return new ResourceTreeNode
            {
                Id = id,
                IdHash = new HashedString(id),
                Name = id,
                width = width,
                height = height
            };
        }

        private sealed class StorageNetworkTechSpec
        {
            public StorageNetworkTechSpec(string techId, string stringId, StorageNetworkResearchText text, string[] buildingIds, float basicCost, float advancedCost, float orbitalCost)
            {
                TechId = techId;
                StringId = stringId;
                Text = text;
                BuildingIds = buildingIds;
                BasicCost = basicCost;
                AdvancedCost = advancedCost;
                OrbitalCost = orbitalCost;
            }

            public string TechId { get; }
            public string StringId { get; }
            public StorageNetworkResearchText Text { get; }
            public string[] BuildingIds { get; }
            public float BasicCost { get; }
            public float AdvancedCost { get; }
            public float OrbitalCost { get; }

            public LocString Name
            {
                get
                {
                    switch (StringId)
                    {
                        case "STORAGENETWORKRESEARCHCORE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKCORE.NAME;
                        case "STORAGENETWORKRESEARCHSMALLSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.NAME;
                        case "STORAGENETWORKRESEARCHMEDIUMSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.NAME;
                        case "STORAGENETWORKRESEARCHLARGESTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.NAME;
                        default:
                            return Loc.RESEARCH.TECHS.STORAGENETWORKRELAY.NAME;
                    }
                }
            }

            public LocString Desc
            {
                get
                {
                    switch (StringId)
                    {
                        case "STORAGENETWORKRESEARCHCORE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKCORE.DESC;
                        case "STORAGENETWORKRESEARCHSMALLSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.DESC;
                        case "STORAGENETWORKRESEARCHMEDIUMSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.DESC;
                        case "STORAGENETWORKRESEARCHLARGESTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKLARGESTORAGE.DESC;
                        default:
                            return Loc.RESEARCH.TECHS.STORAGENETWORKRELAY.DESC;
                    }
                }
            }

            public Dictionary<string, float> CreateCostOverride()
            {
                return new Dictionary<string, float>
                {
                    { "basic", BasicCost },
                    { "advanced", AdvancedCost },
                    { "orbital", OrbitalCost }
                };
            }
        }

        private enum StorageNetworkResearchText
        {
            Core,
            SmallStorage,
            MediumStorage,
            LargeStorage,
            Relay
        }
    }
}
