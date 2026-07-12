using System;
using System.Collections.Generic;
using System.Linq;
using Klei.AI;
using Newtonsoft.Json;
using UnityEngine;

namespace DeepSeekDanmaku
{
    internal static class ColonySnapshot
    {
        private sealed class HistoryPoint { public int cycle; public float food; public int duplicants; }
        private static readonly List<HistoryPoint> history = new List<HistoryPoint>();

        public static string Build() => JsonConvert.SerializeObject(BuildData(true), Formatting.None);

        public static ColonySnapshotData BuildData(bool recordHistory)
        {
            ModConfig config = ModConfig.Instance;
            ColonySnapshotData data = new ColonySnapshotData();
            ClusterManager cluster = ClusterManager.Instance;
            WorldContainer active = cluster?.activeWorld;
            data.basic.cycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0;
            data.basic.activeWorld = WorldName(active);
            data.basic.scope = config.dataScope == DataScope.AllWorlds ? "全部星球" : "当前星球及关联世界";
            data.basic.gameSpeed = Time.timeScale;

            List<WorldContainer> worlds = SelectWorlds(cluster, active, config.dataScope);
            Dictionary<int, WorldSnapshot> byId = new Dictionary<int, WorldSnapshot>();
            foreach (WorldContainer world in worlds)
            {
                if (world == null || world.worldInventory == null) continue;
                WorldSnapshot item = new WorldSnapshot { id = world.id, name = WorldName(world) };
                if (config.includeResources) item.resources = BuildResources(world.worldInventory, false);
                data.worlds.Add(item);
                byId[world.id] = item;
                AddResources(data.totals.resources, item.resources);
            }

            float stress = 0f, health = 0f, calories = 0f;
            if (global::Components.LiveMinionIdentities != null)
            {
                foreach (MinionIdentity minion in global::Components.LiveMinionIdentities.Items)
                {
                    if (minion == null || !byId.TryGetValue(minion.gameObject.GetMyWorldId(), out WorldSnapshot world)) continue;
                    float s = Value(minion, Db.Get().Amounts.Stress);
                    float h = Value(minion, Db.Get().Amounts.HitPoints);
                    float kcal = Value(minion, Db.Get().Amounts.Calories) / 1000f;
                    Chore chore = minion.GetComponent<ChoreDriver>()?.GetCurrentChore();
                    string choreName = chore?.choreType?.Name ?? "空闲";
                    world.duplicants++;
                    data.totals.duplicants++;
                    stress += s; health += h; calories += kcal;
                    if (config.includeDuplicants)
                    {
                        world.people.Add(new DuplicantSnapshot
                        {
                            name = config.includeDuplicantNames ? minion.GetProperName() : $"复制人{world.people.Count + 1}",
                            stressPercent = Round(s), health = Round(h), caloriesKcal = Round(kcal), chore = choreName,
                            idle = chore == null || string.Equals(choreName, "Idle", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }
            float divisor = Mathf.Max(1, data.totals.duplicants);
            data.totals.averageStressPercent = Round(stress / divisor);
            data.totals.averageHealth = Round(health / divisor);
            data.totals.averageCaloriesKcal = Round(calories / divisor);
            data.basic.duplicants = data.totals.duplicants;
            if (config.includeDailyReport) data.report = BuildReport(data.totals.duplicants);
            if (config.includeTrends) data.trends = BuildTrends(config.trendCycles, data);
            BuildRisks(data);
            if (config.includeEvents) data.events = ColonyEventTracker.SnapshotEvents();
            if (recordHistory) RecordHistory(data);
            return data;
        }

        private static List<WorldContainer> SelectWorlds(ClusterManager cluster, WorldContainer active, DataScope scope)
        {
            if (cluster == null) return new List<WorldContainer>();
            if (scope == DataScope.AllWorlds) return cluster.WorldContainers.Where(w => w != null && !w.IsModuleInterior).ToList();
            if (active == null) return new List<WorldContainer>();
            int parentId = active.ParentWorldId;
            return cluster.WorldContainers.Where(w => w != null && w.ParentWorldId == parentId).ToList();
        }

        private static ResourceSnapshot BuildResources(WorldInventory inventory, bool includeRelated) => new ResourceSnapshot
        {
            foodKcal = Round(GetFoodKcal(inventory, includeRelated)),
            breathableKg = Round(inventory.GetAmount(GameTags.Breathable, includeRelated)),
            farmableKg = Round(inventory.GetAmount(GameTags.Farmable, includeRelated)),
            industrialIngredients = Round(inventory.GetAmount(GameTags.IndustrialIngredient, includeRelated)),
            industrialProducts = Round(inventory.GetAmount(GameTags.IndustrialProduct, includeRelated)),
            medicalSupplies = Round(inventory.GetAmount(GameTags.MedicalSupplies, includeRelated)),
            techComponents = Round(inventory.GetAmount(GameTags.TechComponents, includeRelated))
        };

        private static DailyReportSnapshot BuildReport(int minions)
        {
            ReportManager manager = ReportManager.Instance;
            ReportManager.DailyReport report = manager?.YesterdaysReport ?? manager?.TodaysReport;
            if (report == null) return null;
            float people = Mathf.Max(1, minions);
            return new DailyReportSnapshot
            {
                cycle = report.day,
                oxygenNetKg = Round(Net(report, ReportManager.ReportType.OxygenCreated)),
                energyUsedKJ = Round(Mathf.Abs(Net(report, ReportManager.ReportType.EnergyCreated)) / 1000f),
                energyWastedKJ = Round(Mathf.Abs(Net(report, ReportManager.ReportType.EnergyWasted)) / 1000f),
                workPercent = Round(Net(report, ReportManager.ReportType.WorkTime) / 600f / people * 100f),
                travelPercent = Round(Net(report, ReportManager.ReportType.TravelTime) / 600f / people * 100f),
                personalPercent = Round(Net(report, ReportManager.ReportType.PersonalTime) / 600f / people * 100f),
                idlePercent = Round(Net(report, ReportManager.ReportType.IdleTime) / 600f / people * 100f),
                choresAdded = Mathf.RoundToInt(Positive(report, ReportManager.ReportType.ChoreStatus)),
                choresCompleted = Mathf.RoundToInt(Mathf.Abs(Negative(report, ReportManager.ReportType.ChoreStatus))),
                choresNet = Mathf.RoundToInt(Net(report, ReportManager.ReportType.ChoreStatus)),
                domesticatedCritters = Mathf.RoundToInt(Net(report, ReportManager.ReportType.DomesticatedCritters)),
                wildCritters = Mathf.RoundToInt(Net(report, ReportManager.ReportType.WildCritters)),
                rocketsInFlight = Mathf.RoundToInt(Net(report, ReportManager.ReportType.RocketsInFlight))
            };
        }

        private static TrendSnapshot BuildTrends(int count, ColonySnapshotData current)
        {
            TrendSnapshot t = new TrendSnapshot { cycles = count };
            List<ReportManager.DailyReport> reports = ReportManager.Instance?.reports?.TakeLast(Mathf.Max(2, count)).ToList();
            if (reports == null || reports.Count < 2)
            {
                t.oxygen = t.energyUse = t.energyWaste = t.work = t.travel = t.idle = t.critters = "数据不足";
            }
            else
            {
                ReportManager.DailyReport first = reports.First(), last = reports.Last();
                t.oxygen = Direction(Net(first, ReportManager.ReportType.OxygenCreated), Net(last, ReportManager.ReportType.OxygenCreated));
                t.energyUse = Direction(Mathf.Abs(Net(first, ReportManager.ReportType.EnergyCreated)), Mathf.Abs(Net(last, ReportManager.ReportType.EnergyCreated)));
                t.energyWaste = Direction(Mathf.Abs(Net(first, ReportManager.ReportType.EnergyWasted)), Mathf.Abs(Net(last, ReportManager.ReportType.EnergyWasted)));
                t.work = Direction(Net(first, ReportManager.ReportType.WorkTime), Net(last, ReportManager.ReportType.WorkTime));
                t.travel = Direction(Net(first, ReportManager.ReportType.TravelTime), Net(last, ReportManager.ReportType.TravelTime));
                t.idle = Direction(Net(first, ReportManager.ReportType.IdleTime), Net(last, ReportManager.ReportType.IdleTime));
                t.critters = Direction(Net(first, ReportManager.ReportType.DomesticatedCritters) + Net(first, ReportManager.ReportType.WildCritters), Net(last, ReportManager.ReportType.DomesticatedCritters) + Net(last, ReportManager.ReportType.WildCritters));
            }
            List<HistoryPoint> points = history.TakeLast(Mathf.Max(2, count)).ToList();
            t.food = points.Count >= 2 ? Direction(points.First().food, current.totals.resources.foodKcal) : "数据不足";
            t.duplicants = points.Count >= 2 ? Direction(points.First().duplicants, current.totals.duplicants) : "数据不足";
            return t;
        }

        private static void BuildRisks(ColonySnapshotData data)
        {
            int people = data.totals.duplicants;
            if (ModConfig.Instance.includeResources && data.totals.resources.foodKcal < Mathf.Max(2000f, people * 2000f)) data.risks.Add("食物不足");
            if (data.totals.averageStressPercent >= 60f) data.risks.Add("平均压力过高");
            if (data.totals.averageHealth > 0 && data.totals.averageHealth < 50f) data.risks.Add("平均生命值偏低");
            if (HasConsecutiveNegativeOxygen()) data.risks.Add("氧气连续多个周期净产出为负");
            else if (data.report?.oxygenNetKg < 0f) data.risks.Add("氧气净产出为负");
            if (data.report?.energyWastedKJ > 10000f) data.risks.Add("电力浪费严重");
            if (data.worlds.SelectMany(w => w.people).Any(p => p.caloriesKcal < 1000f)) data.risks.Add("有复制人接近饥饿");
        }

        private static void RecordHistory(ColonySnapshotData data)
        {
            history.Add(new HistoryPoint { cycle = data.basic.cycle, food = data.totals.resources.foodKcal, duplicants = data.totals.duplicants });
            if (history.Count > 40) history.RemoveAt(0);
        }

        private static bool HasConsecutiveNegativeOxygen()
        {
            List<ReportManager.DailyReport> reports = ReportManager.Instance?.reports;
            return reports != null && reports.Count >= 2 &&
                   Net(reports[reports.Count - 1], ReportManager.ReportType.OxygenCreated) < 0f &&
                   Net(reports[reports.Count - 2], ReportManager.ReportType.OxygenCreated) < 0f;
        }

        private static void AddResources(ResourceSnapshot target, ResourceSnapshot source)
        {
            if (source == null) return;
            target.foodKcal += source.foodKcal; target.breathableKg += source.breathableKg; target.farmableKg += source.farmableKg;
            target.industrialIngredients += source.industrialIngredients; target.industrialProducts += source.industrialProducts;
            target.medicalSupplies += source.medicalSupplies; target.techComponents += source.techComponents;
        }

        private static float GetFoodKcal(WorldInventory inventory, bool includeRelated)
        {
            if (DiscoveredResources.Instance == null || !DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(GameTags.Edible, out HashSet<Tag> foods)) return 0f;
            float calories = 0f;
            foreach (Tag tag in foods) { EdiblesManager.FoodInfo info = EdiblesManager.GetFoodInfo(tag.Name); if (info != null) calories += inventory.GetAmount(tag, includeRelated) * info.CaloriesPerUnit; }
            return calories / 1000f;
        }

        private static string Direction(float first, float last)
        {
            float threshold = Mathf.Max(0.01f, Mathf.Abs(first) * 0.05f);
            return last - first > threshold ? "上升" : first - last > threshold ? "下降" : "稳定";
        }
        private static string WorldName(WorldContainer world) => world != null ? (!string.IsNullOrWhiteSpace(world.overrideName) ? world.overrideName : world.GetProperName()) : "未知";
        private static float Value(MinionIdentity minion, Amount amount) => amount?.Lookup(minion.gameObject)?.value ?? 0f;
        private static float Net(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Net ?? 0f;
        private static float Positive(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Positive ?? 0f;
        private static float Negative(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Negative ?? 0f;
        private static float Round(float value) => (float)Math.Round(value, 1);
    }
}
