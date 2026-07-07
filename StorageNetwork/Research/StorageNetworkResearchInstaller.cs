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
        private const float BranchOffsetY = 260f;
        private const float NodeOffsetColumns = -2f;

        private const string CoreTechId = "StorageNetworkResearchCore";
        private const string PortsTechId = "StorageNetworkResearchPorts";
        private const string SignalTechId = "StorageNetworkResearchSignal";
        private const string SmallStorageTechId = "StorageNetworkResearchSmallStorage";
        private const string MediumStorageTechId = "StorageNetworkResearchMediumStorage";
        public const string OrderProductionTechId = "StorageNetworkResearchOrderProduction";
        private const string LargeStorageTechId = "StorageNetworkResearchLargeStorage";
        private const string RelayTechId = "StorageNetworkResearchRelay";

        private static readonly StorageNetworkTechSpec[] TechSpecs =
        {
            new StorageNetworkTechSpec(CoreTechId, "STORAGENETWORKRESEARCHCORE", new string[0], 0, 0, new[] { StorageNetworkCoreConfig.ID }, 35f, 0f, 0f),
            new StorageNetworkTechSpec(PortsTechId, "STORAGENETWORKRESEARCHPORTS", CoreTechId, 1, 1, new[]
            {
                StorageNetworkSolidInputPortConfig.ID,
                StorageNetworkSolidOutputPortConfig.ID,
                StorageNetworkLiquidInputPortConfig.ID,
                StorageNetworkLiquidOutputPortConfig.ID,
                StorageNetworkGasInputPortConfig.ID,
                StorageNetworkGasOutputPortConfig.ID,
                StorageNetworkPowerInputPortConfig.ID,
                StorageNetworkPowerOutputPortConfig.ID,
                StorageNetworkParticleInputPortConfig.ID,
                StorageNetworkParticleOutputPortConfig.ID
            }, 50f, 0f, 0f),
            new StorageNetworkTechSpec(SmallStorageTechId, "STORAGENETWORKRESEARCHSMALLSTORAGE", CoreTechId, 1, 0, new[] { SmallSolidServerConfig.ID, SmallLiquidServerConfig.ID, SmallGasServerConfig.ID, SmallParticleServerConfig.ID, SmallBatteryServerConfig.ID, SmallColdStorageServerConfig.ID }, 50f, 0f, 0f),
            new StorageNetworkTechSpec(MediumStorageTechId, "STORAGENETWORKRESEARCHMEDIUMSTORAGE", SmallStorageTechId, 2, 0, new[] { MediumSolidServerConfig.ID, MediumLiquidServerConfig.ID, MediumGasServerConfig.ID, MediumParticleServerConfig.ID, MediumBatteryServerConfig.ID, MediumColdStorageServerConfig.ID }, 50f, 30f, 0f),
            new StorageNetworkTechSpec(SignalTechId, "STORAGENETWORKRESEARCHSIGNAL", PortsTechId, 2, 1, new[] { StorageNetworkLogicDiyConfig.ID, StorageNetworkEnergySensorConfig.ID }, 50f, 30f, 0f),
            new StorageNetworkTechSpec(OrderProductionTechId, "STORAGENETWORKRESEARCHORDERPRODUCTION", new[] { MediumStorageTechId, SignalTechId }, 3, 1, new[] { StorageNetworkOrderProductionCenterConfig.ID, StorageNetworkEngravingDiskConfig.ID }, 50f, 30f, 50f),
            new StorageNetworkTechSpec(LargeStorageTechId, "STORAGENETWORKRESEARCHLARGESTORAGE", MediumStorageTechId, 3, 0, new[] { LargeSolidServerConfig.ID, LargeLiquidServerConfig.ID, LargeGasServerConfig.ID, LargeParticleServerConfig.ID, LargeBatteryServerConfig.ID, LargeColdStorageServerConfig.ID }, 70f, 50f, 50f),
            new StorageNetworkTechSpec(RelayTechId, "STORAGENETWORKRESEARCHRELAY", LargeStorageTechId, 4, 0, new[] { StorageNetworkRelayModuleConfig.ID }, 70f, 100f, 200f)
        };

        public static void Install(Database.Techs techs)
        {
            EnsureResearchStrings();
            EnsureEngravingDiskTechItem();
            RemoveOldStorageNetworkUnlocks(techs);

            Vector2 titleCenter = GetTitleCenter(techs);
            InstallTreeTitle(titleCenter);

            for (int i = 0; i < TechSpecs.Length; i++)
            {
                Tech tech = GetOrCreateTech(techs, TechSpecs[i]);
                LinkStorageNetworkTechs(techs, tech, TechSpecs[i]);
                PlaceNode(techs, tech, titleCenter, TechSpecs[i]);
            }

            LinkNodeEdges(techs);
        }

        public static void RefreshUnlockedItems()
        {
            EnsureEngravingDiskTechItem();
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
            HashSet<string> storageNetworkIds = new HashSet<string>();
            foreach (StorageNetworkTechSpec spec in TechSpecs)
            {
                foreach (string id in spec.BuildingIds)
                {
                    storageNetworkIds.Add(id);
                }
            }

            foreach (Tech tech in techs.resources)
            {
                tech.unlockedItemIDs.RemoveAll(storageNetworkIds.Contains);
                tech.unlockedItems.RemoveAll(item => item != null && storageNetworkIds.Contains(item.Id));
            }
        }

        private static void EnsureEngravingDiskTechItem()
        {
            Database.TechItems techItems = Db.Get().TechItems;
            if (techItems == null || techItems.TryGet(StorageNetworkEngravingDiskConfig.ID) != null)
            {
                return;
            }

            techItems.AddTechItem(
                StorageNetworkEngravingDiskConfig.ID,
                STRINGS.Get(Loc.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.NAME),
                STRINGS.Get(Loc.ITEMS.INDUSTRIAL_PRODUCTS.STORAGE_NETWORK_ENGRAVING_DISK.RECIPEDESC),
                GetEngravingDiskTechSprite,
                null,
                null,
                false);
        }

        private static Sprite GetEngravingDiskTechSprite(string name, bool active)
        {
            GameObject prefab = Assets.GetPrefab(StorageNetworkEngravingDiskConfig.ID);
            if (prefab != null)
            {
                Tuple<Sprite, Color> uiSprite = Def.GetUISprite(prefab, "ui", false);
                if (uiSprite != null && uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            Sprite sprite = Assets.GetSprite(StorageNetworkEngravingDiskConfig.ID);
            return sprite != null ? sprite : Assets.GetSprite("unknown");
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

        private static void LinkStorageNetworkTechs(Database.Techs techs, Tech tech, StorageNetworkTechSpec spec)
        {
            tech.requiredTech.Clear();
            foreach (Tech candidate in techs.resources)
            {
                candidate.unlockedTech.Remove(tech);
            }

            foreach (string parentTechId in spec.ParentTechIds)
            {
                Tech parent = techs.TryGet(parentTechId);
                if (parent != null)
                {
                    tech.requiredTech.Add(parent);
                    parent.unlockedTech.Add(tech);
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

        private static void PlaceNode(Database.Techs techs, Tech tech, Vector2 titleCenter, StorageNetworkTechSpec spec)
        {
            if (tech.FoundNode)
            {
                return;
            }

            Vector2 center = GetNodeCenter(techs, titleCenter, spec);
            ResourceTreeNode node = CreateNode(tech.Id, center);

            tech.SetNode(node, TreeTitleId);
        }

        private static void LinkNodeEdges(Database.Techs techs)
        {
            foreach (StorageNetworkTechSpec spec in TechSpecs)
            {
                Tech current = techs.TryGet(spec.TechId);
                if (current == null || !current.FoundNode)
                {
                    continue;
                }

                current.edges.Clear();

                foreach (StorageNetworkTechSpec childSpec in TechSpecs)
                {
                    if (!childSpec.HasParentTechId(spec.TechId))
                    {
                        continue;
                    }

                    Tech child = techs.TryGet(childSpec.TechId);
                    if (child == null || !child.FoundNode)
                    {
                        continue;
                    }

                    current.edges.Add(new ResourceTreeNode.Edge(
                        CreateEdgePoint(current.Id, current.center),
                        CreateEdgePoint(child.Id, child.center),
                        ResourceTreeNode.Edge.EdgeType.PolyLineEdge));
                }
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

        private static Vector2 GetNodeCenter(Database.Techs techs, Vector2 titleCenter, StorageNetworkTechSpec spec)
        {
            float x = GetAutomationStartX(techs);
            float spacing = GetAutomationColumnSpacing(techs);
            return new Vector2(x + (spec.Column + NodeOffsetColumns) * spacing, titleCenter.y - NodeOffsetY - spec.Row * BranchOffsetY);
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
            public StorageNetworkTechSpec(string techId, string stringId, string parentTechId, int column, int row, string[] buildingIds, float basicCost, float advancedCost, float orbitalCost)
                : this(
                    techId,
                    stringId,
                    string.IsNullOrEmpty(parentTechId) ? new string[0] : new[] { parentTechId },
                    column,
                    row,
                    buildingIds,
                    basicCost,
                    advancedCost,
                    orbitalCost)
            {
            }

            public StorageNetworkTechSpec(string techId, string stringId, string[] parentTechIds, int column, int row, string[] buildingIds, float basicCost, float advancedCost, float orbitalCost)
            {
                TechId = techId;
                StringId = stringId;
                ParentTechIds = parentTechIds ?? new string[0];
                Column = column;
                Row = row;
                BuildingIds = buildingIds;
                BasicCost = basicCost;
                AdvancedCost = advancedCost;
                OrbitalCost = orbitalCost;
            }

            public string TechId { get; }
            public string StringId { get; }
            public string[] ParentTechIds { get; }
            public int Column { get; }
            public int Row { get; }
            public string[] BuildingIds { get; }
            public float BasicCost { get; }
            public float AdvancedCost { get; }
            public float OrbitalCost { get; }

            public bool HasParentTechId(string techId)
            {
                foreach (string parentTechId in ParentTechIds)
                {
                    if (parentTechId == techId)
                    {
                        return true;
                    }
                }

                return false;
            }

            public LocString Name
            {
                get
                {
                    switch (StringId)
                    {
                        case "STORAGENETWORKRESEARCHCORE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKCORE.NAME;
                        case "STORAGENETWORKRESEARCHPORTS":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKPORTS.NAME;
                        case "STORAGENETWORKRESEARCHSMALLSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.NAME;
                        case "STORAGENETWORKRESEARCHMEDIUMSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.NAME;
                        case "STORAGENETWORKRESEARCHSIGNAL":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSIGNAL.NAME;
                        case "STORAGENETWORKRESEARCHORDERPRODUCTION":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.NAME;
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
                        case "STORAGENETWORKRESEARCHPORTS":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKPORTS.DESC;
                        case "STORAGENETWORKRESEARCHSMALLSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSMALLSTORAGE.DESC;
                        case "STORAGENETWORKRESEARCHMEDIUMSTORAGE":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKMEDIUMSTORAGE.DESC;
                        case "STORAGENETWORKRESEARCHSIGNAL":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKSIGNAL.DESC;
                        case "STORAGENETWORKRESEARCHORDERPRODUCTION":
                            return Loc.RESEARCH.TECHS.STORAGENETWORKORDERPRODUCTION.DESC;
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

    }
}
