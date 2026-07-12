using System.Text;
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
            return $"游戏时间: 第{(GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0)}周期\n" +
                   $"复制人: {count}名\n平均压力: {stress / divisor:0.0}%\n平均生命值: {health / divisor:0.0}\n" +
                   $"人均体内卡路里: {calories / divisor:0}\n当前活动: {workers}";
        }

        private static float Value(MinionIdentity minion, Amount amount)
        {
            AmountInstance instance = amount?.Lookup(minion.gameObject);
            return instance?.value ?? 0f;
        }
    }
}
