using System.Text;
using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace DeepSeekDanmaku
{
    internal static class ColonySnapshot
    {
        public static string Build()
        {
            int count = 0;
            float stress = 0f, health = 0f, calories = 0f;
            StringBuilder workers = new StringBuilder();
            if (global::Components.LiveMinionIdentities != null)
            {
                foreach (MinionIdentity minion in global::Components.LiveMinionIdentities.Items)
                {
                    if (minion == null) continue;
                    count++;
                    stress += Value(minion, Db.Get().Amounts.Stress);
                    health += Value(minion, Db.Get().Amounts.HitPoints);
                    calories += Value(minion, Db.Get().Amounts.Calories);
                    if (workers.Length < 500)
                    {
                        Chore chore = minion.GetComponent<ChoreDriver>()?.GetCurrentChore();
                        workers.Append(minion.GetProperName()).Append(':')
                            .Append(chore?.choreType?.Name ?? "Idle").Append("; ");
                    }
                }
            }
            float divisor = Mathf.Max(1, count);
            float averageCaloriesKcal = calories / divisor / 1000f;
            StringBuilder snapshot = new StringBuilder(1400);
            snapshot.Append($"游戏时间: 第{(GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0)}周期\n")
                .Append($"复制人: {count}名\n平均压力: {stress / divisor:0.0}%\n平均生命值: {health / divisor:0.0}\n")
                .Append($"人均体内热量: {averageCaloriesKcal:0.0} kcal（千卡）\n当前活动: {workers}\n");
            AppendDailyReport(snapshot, count);
            AppendResources(snapshot);
            return snapshot.ToString();
        }

        private static void AppendDailyReport(StringBuilder snapshot, int minionCount)
        {
            ReportManager manager = ReportManager.Instance;
            ReportManager.DailyReport report = manager?.YesterdaysReport ?? manager?.TodaysReport;
            if (report == null) return;
            float people = Mathf.Max(1, minionCount);
            snapshot.Append($"周期报告（第{report.day}周期）:\n")
                .Append($"- 氧气净产出: {Net(report, ReportManager.ReportType.OxygenCreated):0.0} kg\n")
                .Append($"- 电力净使用: {Mathf.Abs(Net(report, ReportManager.ReportType.EnergyCreated)) / 1000f:0.0} kJ\n")
                .Append($"- 电力浪费: {Mathf.Abs(Net(report, ReportManager.ReportType.EnergyWasted)) / 1000f:0.0} kJ\n")
                .Append($"- 平均工作时间: {Net(report, ReportManager.ReportType.WorkTime) / 600f / people * 100f:0.0}%\n")
                .Append($"- 平均步行时间: {Net(report, ReportManager.ReportType.TravelTime) / 600f / people * 100f:0.0}%\n")
                .Append($"- 平均个人时间: {Net(report, ReportManager.ReportType.PersonalTime) / 600f / people * 100f:0.0}%\n")
                .Append($"- 平均空闲时间: {Net(report, ReportManager.ReportType.IdleTime) / 600f / people * 100f:0.0}%\n")
                .Append($"- 新增任务/完成任务/净增: {Positive(report, ReportManager.ReportType.ChoreStatus):0}/{Mathf.Abs(Negative(report, ReportManager.ReportType.ChoreStatus)):0}/{Net(report, ReportManager.ReportType.ChoreStatus):0}\n")
                .Append($"- 驯化小动物: {Net(report, ReportManager.ReportType.DomesticatedCritters):0}，野生小动物: {Net(report, ReportManager.ReportType.WildCritters):0}\n");
        }

        private static void AppendResources(StringBuilder snapshot)
        {
            WorldInventory inventory = ClusterManager.Instance?.activeWorld?.worldInventory;
            if (inventory == null) return;
            snapshot.Append("当前可用资源:\n")
                .Append($"- 食物: {GetFoodKcal(inventory):0} kcal（千卡）\n")
                .Append($"- 可呼吸气体库存: {inventory.GetAmount(GameTags.Breathable, true):0.0} kg\n")
                .Append($"- 可耕种材料: {inventory.GetAmount(GameTags.Farmable, true):0.0} kg\n")
                .Append($"- 工业原料: {inventory.GetAmount(GameTags.IndustrialIngredient, true):0.0}\n")
                .Append($"- 工业产品: {inventory.GetAmount(GameTags.IndustrialProduct, true):0.0}\n")
                .Append($"- 医疗用品: {inventory.GetAmount(GameTags.MedicalSupplies, true):0.0}\n")
                .Append($"- 科技组件: {inventory.GetAmount(GameTags.TechComponents, true):0.0}\n");
        }

        private static float GetFoodKcal(WorldInventory inventory)
        {
            HashSet<Tag> foods;
            if (DiscoveredResources.Instance == null || !DiscoveredResources.Instance.TryGetDiscoveredResourcesFromTag(GameTags.Edible, out foods)) return 0f;
            float calories = 0f;
            foreach (Tag tag in foods)
            {
                EdiblesManager.FoodInfo info = EdiblesManager.GetFoodInfo(tag.Name);
                if (info != null) calories += inventory.GetAmount(tag, true) * info.CaloriesPerUnit;
            }
            return calories / 1000f;
        }

        private static float Net(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Net ?? 0f;
        private static float Positive(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Positive ?? 0f;
        private static float Negative(ReportManager.DailyReport report, ReportManager.ReportType type) => report.GetEntry(type)?.Negative ?? 0f;

        private static float Value(MinionIdentity minion, Amount amount)
        {
            AmountInstance instance = amount?.Lookup(minion.gameObject);
            return instance?.value ?? 0f;
        }
    }
}
