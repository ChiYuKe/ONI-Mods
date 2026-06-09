using HarmonyLib;
using Database;
using Klei.AI;
using KMod;
using ProcGen;
using ProcGenGame;
using STRINGS;
using System.Collections.Generic;
using UnityEngine;

namespace NewMap
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Config.SetModPath(mod.ContentPath);
            Config.Load();
            harmony.PatchAll();
            AddWorldStrings();
            NewMapOptions.Register();
        }

        private static void AddWorldStrings()
        {
            Strings.Add(new[]
            {
                "STRINGS.WORLDS.VERDANT_RIFT.NAME",
                UI.FormatAsLink("葱翠裂谷", "VerdantRift")
            });
            Strings.Add(new[]
            {
                "STRINGS.WORLDS.VERDANT_RIFT.DESCRIPTION",
                "一颗被冰火两极撕开的富饶星球。复制人从温和绿洲起步，向上会撞见冰原、海洋和太空遗迹，向下则是油井、金属矿脉和活跃火山带。"
            });
            Strings.Add(new[]
            {
                "STRINGS.WORLDS.VERDANT_RIFT_SPACED.NAME",
                UI.FormatAsLink("葱翠裂谷（眼冒金星）", "VerdantRiftSpacedOut")
            });
            Strings.Add(new[]
            {
                "STRINGS.WORLDS.VERDANT_RIFT_SPACED.DESCRIPTION",
                "葱翠裂谷的眼冒金星版本。自定义主星拥有冰火裂谷主题，星图中加入火箭降落点、传送星和多颗外层小行星。"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_VACUUM.NAME",
                "真空裂谷"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_VACUUM.DESCRIPTION",
                "被金属矿脉包围的纵向真空断层，偶尔封存着极寒洞穴。"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_VACUUM.UTILITY",
                "真空隔离、金属矿脉、低温资源"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_MAGMA.NAME",
                "岩浆心脏"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_MAGMA.DESCRIPTION",
                "裂谷深处的高温岩浆腔，包裹着活跃火山。"
            });
            Strings.Add(new[]
            {
                "STRINGS.SUBWORLDS.VERDANT_RIFT_MAGMA.UTILITY",
                "岩浆、黑曜石、火山"
            });
        }

        [HarmonyPatch(typeof(GameplaySeasons), MethodType.Constructor, typeof(ResourceSet))]
        private static class GameplaySeasonsPatch
        {
            private const string Spring = "VerdantRiftSpring";
            private const string Summer = "VerdantRiftSummer";
            private const string Autumn = "VerdantRiftAutumn";
            private const string Winter = "VerdantRiftWinter";

            private static void Postfix(GameplaySeasons __instance)
            {
                if (__instance.Exists(Spring))
                {
                    return;
                }

                var events = Db.Get().GameplayEvents;
                __instance.Add(new MeteorShowerSeason(Spring, GameplaySeason.Type.World, 80f, true, 0f, true, -1, 0f, float.PositiveInfinity, 1, true, 6000f, DlcManager.EXPANSION1, null)
                    .AddEvent(events.ClusterBiologicalShower)
                    .AddEvent(events.ClusterOxyliteShower));
                __instance.Add(new MeteorShowerSeason(Summer, GameplaySeason.Type.World, 80f, true, 0f, true, -1, 20f, float.PositiveInfinity, 1, true, 6000f, DlcManager.EXPANSION1, null)
                    .AddEvent(events.ClusterCopperShower)
                    .AddEvent(events.ClusterBleachStoneShower));
                __instance.Add(new MeteorShowerSeason(Autumn, GameplaySeason.Type.World, 80f, true, 0f, true, -1, 40f, float.PositiveInfinity, 1, true, 6000f, DlcManager.EXPANSION1, null)
                    .AddEvent(events.ClusterGoldShower)
                    .AddEvent(events.ClusterIronShower));
                __instance.Add(new MeteorShowerSeason(Winter, GameplaySeason.Type.World, 80f, true, 0f, true, -1, 60f, float.PositiveInfinity, 1, true, 6000f, DlcManager.EXPANSION1, null)
                    .AddEvent(events.ClusterIceShower)
                    .AddEvent(events.ClusterSnowShower));
            }
        }

        [HarmonyPatch(typeof(Game), "OnSpawn")]
        private static class GameOnSpawnPatch
        {
            private static void Postfix(Game __instance)
            {
                __instance.gameObject.AddOrGet<VerdantRiftClimateController>();
            }
        }
    }

    public sealed class VerdantRiftClimateController : KMonoBehaviour, ISim4000ms
    {
        private const string Spring = "VerdantRiftSpring";
        private const float MinimumMass = 0.001f;

        private WorldContainer targetWorld;
        private int scanX;
        private int scanY;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            RefreshTargetWorld();
        }

        public void Sim4000ms(float dt)
        {
            if (!HasTargetWorld())
            {
                return;
            }

            if (!Config.Instance.EnableClimateControl)
            {
                return;
            }

            ApplyClimateTarget();
        }

        private bool HasTargetWorld()
        {
            if (targetWorld == null || !IsVerdantRiftWorld(targetWorld))
            {
                RefreshTargetWorld();
            }

            return targetWorld != null;
        }

        private void RefreshTargetWorld()
        {
            targetWorld = null;
            if (ClusterManager.Instance == null)
            {
                return;
            }

            foreach (WorldContainer world in ClusterManager.Instance.WorldContainers)
            {
                if (IsVerdantRiftWorld(world))
                {
                    targetWorld = world;
                    scanX = targetWorld.WorldOffset.x;
                    scanY = targetWorld.WorldOffset.y;
                    return;
                }
            }
        }

        private static bool IsVerdantRiftWorld(WorldContainer world)
        {
            return world != null && world.IsStartWorld && world.GetSeasonIds() != null && world.GetSeasonIds().Contains(Spring);
        }

        private void ApplyClimateTarget()
        {
            int processed = 0;
            while (processed < Config.Instance.ClimateCellsPerTick)
            {
                int cell = Grid.XYToCell(scanX, scanY);
                TryModifyCell(cell);
                AdvanceScan(1);
                processed++;
            }
        }

        private void AdvanceScan(int cells)
        {
            if (targetWorld == null)
            {
                return;
            }

            int minX = targetWorld.WorldOffset.x;
            int minY = targetWorld.WorldOffset.y;
            int maxX = minX + targetWorld.Width - 1;
            int maxY = minY + targetWorld.Height - targetWorld.HiddenYOffset - 1;

            for (int i = 0; i < cells; i++)
            {
                scanX++;
                if (scanX > maxX)
                {
                    scanX = minX;
                    scanY++;
                    if (scanY > maxY)
                    {
                        scanY = minY;
                    }
                }
            }
        }

        private void TryModifyCell(int cell)
        {
            if (!Grid.IsValidCell(cell) || (int)Grid.WorldIdx[cell] != targetWorld.id)
            {
                return;
            }

            float mass = Grid.Mass[cell];
            float temperature = Grid.Temperature[cell];
            if (mass <= MinimumMass || temperature < 1f || temperature > 10000f)
            {
                return;
            }

            float targetTemperature = GetRegionalTargetTemperature(Grid.CellToXY(cell));
            float deltaKelvin = Mathf.Clamp(targetTemperature - temperature, -Config.Instance.ClimateMaxKelvinPerVisit, Config.Instance.ClimateMaxKelvinPerVisit);
            if (Mathf.Abs(deltaKelvin) < 0.01f)
            {
                return;
            }

            SimMessages.ReplaceElement(
                cell,
                Grid.Element[cell].id,
                CellEventLogger.Instance.SandBoxTool,
                mass,
                Mathf.Clamp(temperature + deltaKelvin, GetAbsoluteMinimumTemperature(), GetAbsoluteMaximumTemperature()),
                Grid.DiseaseIdx[cell],
                Grid.DiseaseCount[cell],
                -1);
        }

        private float GetRegionalTargetTemperature(Vector2I worldCell)
        {
            int localX = worldCell.x - targetWorld.WorldOffset.x;
            int centerX = targetWorld.Width / 2;
            int dx = Mathf.Abs(localX - centerX);
            if (dx <= Config.Instance.SkywellHaloRadius + 4)
            {
                return Config.ToKelvin(Config.Instance.SkywellTemperatureC);
            }

            GetSeasonalPoleTargets(GameUtil.GetCurrentTimeInCycles(), out float leftTemperature, out float rightTemperature);
            return localX < centerX ? leftTemperature : rightTemperature;
        }

        private static void GetSeasonalPoleTargets(float cycle, out float leftTemperature, out float rightTemperature)
        {
            Config config = Config.Instance;
            float phase = Mathf.Repeat(cycle, config.YearLengthCycles) / config.YearLengthCycles;
            if (phase < 0.25f)
            {
                leftTemperature = Config.ToKelvin(config.SpringLeftC);
                rightTemperature = Config.ToKelvin(config.SpringRightC);
            }
            else if (phase < 0.5f)
            {
                leftTemperature = Config.ToKelvin(config.SummerLeftC);
                rightTemperature = Config.ToKelvin(config.SummerRightC);
            }
            else if (phase < 0.75f)
            {
                leftTemperature = Config.ToKelvin(config.AutumnLeftC);
                rightTemperature = Config.ToKelvin(config.AutumnRightC);
            }
            else
            {
                leftTemperature = Config.ToKelvin(config.WinterLeftC);
                rightTemperature = Config.ToKelvin(config.WinterRightC);
            }
        }

        private static float GetAbsoluteMinimumTemperature()
        {
            Config config = Config.Instance;
            return Config.ToKelvin(Mathf.Min(config.SpringLeftC, config.SummerLeftC, config.AutumnLeftC, config.WinterLeftC, config.InitialLeftC, config.SkywellTemperatureC));
        }

        private static float GetAbsoluteMaximumTemperature()
        {
            Config config = Config.Instance;
            return Config.ToKelvin(Mathf.Max(config.SpringRightC, config.SummerRightC, config.AutumnRightC, config.WinterRightC, config.InitialRightC, config.SkywellTemperatureC));
        }
    }

    [HarmonyPatch(typeof(TemplateSpawning), "DetermineTemplatesForWorld")]
    public static class VerdantRiftStartLocationPatch
    {
        private const string VerdantRiftWorld = "STRINGS.WORLDS.VERDANT_RIFT.NAME";

        private static void Prefix(WorldGenSettings settings, List<TerrainCell> terrainCells)
        {
            if (settings?.world == null || settings.world.name != VerdantRiftWorld || terrainCells == null || terrainCells.Count == 0)
            {
                return;
            }

            int centerX = settings.world.worldsize.x / 2;
            int spawnY = GetSkywellSpawnY(settings.world.worldsize.y);
            TerrainCell skywellCell = null;
            float bestDistance = float.MaxValue;

            foreach (TerrainCell terrainCell in terrainCells)
            {
                terrainCell.node.tags.Remove(WorldGenTags.StartLocation);
                Vector2 centroid = terrainCell.poly.Centroid();
                float distance = (centroid.x - centerX) * (centroid.x - centerX) + (centroid.y - spawnY) * (centroid.y - spawnY);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    skywellCell = terrainCell;
                }
            }

            if (skywellCell != null)
            {
                skywellCell.node.tags.Add(WorldGenTags.StartLocation);
                skywellCell.node.tags.Add(WorldGenTags.NearStartLocation);
            }
        }

        private static int GetSkywellSpawnY(int height)
        {
            return Mathf.Clamp(Mathf.RoundToInt(height * Config.Instance.SpawnHeightPercent), 36, height - 36);
        }
    }

    [HarmonyPatch(typeof(WorldGen), "RenderToMap")]
    public static class WorldGenPostProcessPatch
    {
        private const string VerdantRiftWorld = "STRINGS.WORLDS.VERDANT_RIFT.NAME";

        private static void Postfix(WorldGen __instance, bool __result, ref Sim.Cell[] cells)
        {
            if (!__result || __instance?.Settings?.world == null || __instance.Settings.world.name != VerdantRiftWorld || cells == null)
            {
                return;
            }

            ApplyRegionalTemperature(__instance, cells);
            CarveSkywell(__instance, cells);
            BuildSkywellStartPocket(__instance, cells);
            __instance.data.gameSpawnData.baseStartPos = GetSkywellSpawnPosition(__instance.Settings.world.worldsize);
            NormalizeNaturalSolidMass(cells);
        }

        private static void NormalizeNaturalSolidMass(Sim.Cell[] cells)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                Element element = ElementLoader.elements[(int)cells[i].elementIdx];
                if (element == null || !element.IsSolid || element.HasTag(GameTags.Special) || cells[i].mass <= 0f)
                {
                    continue;
                }

                cells[i].mass = Mathf.Clamp(cells[i].mass, Config.Instance.NaturalSolidMinimumMassKg, Config.Instance.NaturalSolidMaximumMassKg);
            }
        }

        private static void CarveSkywell(WorldGen worldGen, Sim.Cell[] cells)
        {
            int width = worldGen.Settings.world.worldsize.x;
            int height = worldGen.Settings.world.worldsize.y;
            int centerX = width / 2;
            int bottomY = 18;
            int topY = height - 10;
            int coreRadius = Config.Instance.SkywellCoreRadius;
            int wallRadius = Config.Instance.SkywellWallRadius;
            int haloRadius = Config.Instance.SkywellHaloRadius;
            float skywellTemperature = Config.ToKelvin(Config.Instance.SkywellTemperatureC);

            Element vacuum = ElementLoader.FindElementByHash(SimHashes.Vacuum);
            Element abyssalite = ElementLoader.FindElementByHash(SimHashes.Katairite);
            Element obsidian = ElementLoader.FindElementByHash(SimHashes.Obsidian);
            Element diamond = ElementLoader.FindElementByHash(SimHashes.Diamond);
            Element ironOre = ElementLoader.FindElementByHash(SimHashes.IronOre);
            Element gold = ElementLoader.FindElementByHash(SimHashes.GoldAmalgam);
            Element magma = ElementLoader.FindElementByHash(SimHashes.Magma);
            Element granite = ElementLoader.FindElementByHash(SimHashes.Granite);

            for (int y = bottomY; y <= topY; y++)
            {
                float wave = Mathf.Sin(y * 0.09f) * 2.2f + Mathf.Sin(y * 0.031f) * 1.4f;
                int cx = centerX + Mathf.RoundToInt(wave);
                for (int x = cx - haloRadius; x <= cx + haloRadius; x++)
                {
                    if (x < 2 || x >= width - 2)
                    {
                        continue;
                    }

                    int dx = Mathf.Abs(x - cx);
                    int index = x + width * y;
                    if (index < 0 || index >= cells.Length || IsSpecialCell(cells[index]))
                    {
                        continue;
                    }

                    if (dx <= coreRadius)
                    {
                        SetCell(cells, index, vacuum, 0f, skywellTemperature);
                    }
                    else if (dx <= wallRadius)
                    {
                        Element wallElement = (y % 17 == 0 && dx == wallRadius) ? diamond : abyssalite;
                        SetCell(cells, index, wallElement, 400f, skywellTemperature);
                    }
                    else if (dx <= haloRadius && (y % 23 == 0 || (x + y) % 37 == 0))
                    {
                        Element oreElement = (y % 46 == 0) ? gold : ironOre;
                        SetCell(cells, index, oreElement, 300f, skywellTemperature);
                    }
                    else if (dx == haloRadius)
                    {
                        SetCell(cells, index, granite, 250f, skywellTemperature);
                    }
                }
            }

            int crownTop = Mathf.Min(bottomY + 26, height - 1);
            for (int y = 2; y <= crownTop; y++)
            {
                float t = 1f - Mathf.Clamp01((float)(y - 2) / Mathf.Max(1, crownTop - 2));
                int halfWidth = Mathf.RoundToInt(Mathf.Lerp(7f, 20f, t));
                for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
                {
                    if (x < 2 || x >= width - 2)
                    {
                        continue;
                    }

                    int index = x + width * y;
                    if (index < 0 || index >= cells.Length || IsSpecialCell(cells[index]))
                    {
                        continue;
                    }

                    int dx = Mathf.Abs(x - centerX);
                    if (dx <= halfWidth - 4)
                    {
                        SetCell(cells, index, magma, 1000f, 1823f);
                    }
                    else
                    {
                        SetCell(cells, index, obsidian, 400f, 900f);
                    }
                }
            }
        }

        private static void ApplyRegionalTemperature(WorldGen worldGen, Sim.Cell[] cells)
        {
            int width = worldGen.Settings.world.worldsize.x;
            int height = worldGen.Settings.world.worldsize.y;
            int centerX = width / 2;
            float skywellTemperature = Config.ToKelvin(Config.Instance.SkywellTemperatureC);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + width * y;
                    if (index < 0 || index >= cells.Length || IsSpecialCell(cells[index]))
                    {
                        continue;
                    }

                    cells[index].temperature = GetRegionalTemperature(x, centerX, skywellTemperature);
                }
            }
        }

        private static void BuildSkywellStartPocket(WorldGen worldGen, Sim.Cell[] cells)
        {
            int width = worldGen.Settings.world.worldsize.x;
            int height = worldGen.Settings.world.worldsize.y;
            Vector2I spawn = GetSkywellSpawnPosition(worldGen.Settings.world.worldsize);
            Element oxygen = ElementLoader.FindElementByHash(SimHashes.Oxygen);
            Element granite = ElementLoader.FindElementByHash(SimHashes.Granite);
            Element algae = ElementLoader.FindElementByHash(SimHashes.Algae);
            Element water = ElementLoader.FindElementByHash(SimHashes.Water);
            float skywellTemperature = Config.ToKelvin(Config.Instance.SkywellTemperatureC);

            for (int y = spawn.y - 4; y <= spawn.y + 5; y++)
            {
                for (int x = spawn.x - 7; x <= spawn.x + 7; x++)
                {
                    if (x < 2 || x >= width - 2 || y < 2 || y >= height - 2)
                    {
                        continue;
                    }

                    int index = x + width * y;
                    if (index < 0 || index >= cells.Length || IsSpecialCell(cells[index]))
                    {
                        continue;
                    }

                    bool floor = y == spawn.y - 3;
                    bool sideWall = Mathf.Abs(x - spawn.x) == 7 && y <= spawn.y + 2;
                    bool ceilingLip = y == spawn.y + 3 && Mathf.Abs(x - spawn.x) >= 5;
                    if (floor || sideWall || ceilingLip)
                    {
                        SetCell(cells, index, granite, 350f, skywellTemperature);
                    }
                    else
                    {
                        SetCell(cells, index, oxygen, 1.8f, skywellTemperature);
                    }
                }
            }

            PlacePatch(cells, width, spawn.x - 5, spawn.y - 2, algae, 300f, skywellTemperature);
            PlacePatch(cells, width, spawn.x + 5, spawn.y - 2, water, 800f, skywellTemperature);
        }

        private static void PlacePatch(Sim.Cell[] cells, int width, int x, int y, Element element, float mass, float temperature)
        {
            int index = x + width * y;
            if (index < 0 || index >= cells.Length || IsSpecialCell(cells[index]))
            {
                return;
            }

            SetCell(cells, index, element, mass, temperature);
        }

        private static Vector2I GetSkywellSpawnPosition(Vector2I worldSize)
        {
            return new Vector2I(worldSize.x / 2, Mathf.Clamp(Mathf.RoundToInt(worldSize.y * Config.Instance.SpawnHeightPercent), 36, worldSize.y - 36));
        }

        private static float GetRegionalTemperature(int x, int centerX, float skywellTemperature)
        {
            int dx = Mathf.Abs(x - centerX);
            if (dx <= Config.Instance.SkywellHaloRadius + 4)
            {
                return skywellTemperature;
            }

            return x < centerX ? Config.ToKelvin(Config.Instance.InitialLeftC) : Config.ToKelvin(Config.Instance.InitialRightC);
        }

        private static bool IsSpecialCell(Sim.Cell cell)
        {
            Element element = ElementLoader.elements[(int)cell.elementIdx];
            return element != null && element.HasTag(GameTags.Special);
        }

        private static void SetCell(Sim.Cell[] cells, int index, Element element, float mass, float temperature)
        {
            if (element == null)
            {
                return;
            }

            cells[index].SetValues(element.idx, temperature, mass);
        }
    }
}
