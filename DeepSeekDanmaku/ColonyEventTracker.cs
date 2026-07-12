using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepSeekDanmaku
{
    internal sealed class ColonyEventTracker : MonoBehaviour
    {
        private static ColonyEventTracker current;
        private readonly List<string> pending = new List<string>();
        private int lastCycle = -1, lastDuplicants = -1, lastRockets = -1;
        private float nextScan;
        private bool foodAlert, stressAlert, healthAlert, oxygenAlert, wasteAlert;

        public static ColonyEventTracker Attach(GameObject host)
        {
            current = host.GetComponent<ColonyEventTracker>() ?? host.AddComponent<ColonyEventTracker>();
            return current;
        }

        public static bool HasPending => current != null && current.pending.Count > 0;
        public static List<string> SnapshotEvents() => current != null ? new List<string>(current.pending) : new List<string>();
        public static void ConfirmSent() { if (current != null) current.pending.Clear(); }
        public static void ResetCurrent() => current = null;

        private void OnEnable()
        {
            if (Game.Instance != null)
            {
                Game.Instance.Subscribe((int)GameHashes.ResearchComplete, OnResearchComplete);
                Game.Instance.Subscribe((int)GameHashes.RocketLaunched, OnRocketLaunched);
                Game.Instance.Subscribe((int)GameHashes.RocketLanded, OnRocketLanded);
            }
        }

        private void OnDisable()
        {
            if (Game.Instance != null)
            {
                Game.Instance.Unsubscribe((int)GameHashes.ResearchComplete, OnResearchComplete);
                Game.Instance.Unsubscribe((int)GameHashes.RocketLaunched, OnRocketLaunched);
                Game.Instance.Unsubscribe((int)GameHashes.RocketLanded, OnRocketLanded);
            }
        }

        private void OnResearchComplete(object data) => Add("完成了一项研究");
        private void OnRocketLaunched(object data) => Add("有火箭发射");
        private void OnRocketLanded(object data) => Add("有火箭返回");

        private void Update()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 5f;
            Scan();
        }

        private void Scan()
        {
            int cycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0;
            int dupes = global::Components.LiveMinionIdentities?.Count ?? 0;
            int rockets = GetRocketsInFlight();
            if (lastCycle >= 0 && cycle != lastCycle) Add($"进入第{cycle}周期");
            if (lastDuplicants >= 0 && dupes != lastDuplicants) Add(dupes > lastDuplicants ? $"复制人增加到{dupes}名" : $"复制人减少到{dupes}名，可能有人死亡或离开");
            if (lastRockets >= 0 && rockets != lastRockets) Add(rockets > lastRockets ? "有火箭发射" : "有火箭返回或结束任务");
            lastCycle = cycle; lastDuplicants = dupes; lastRockets = rockets;

            ColonySnapshotData data = ColonySnapshot.BuildData(false);
            SetAlert(ref foodAlert, ModConfig.Instance.includeResources && data.totals.resources.foodKcal < Mathf.Max(2000f, dupes * 2000f), $"食物库存偏低：{data.totals.resources.foodKcal:0}千卡");
            SetAlert(ref stressAlert, data.totals.averageStressPercent >= 60f, $"平均压力达到{data.totals.averageStressPercent:0}%");
            SetAlert(ref healthAlert, data.totals.averageHealth > 0f && data.totals.averageHealth < 50f, $"复制人平均生命值偏低：{data.totals.averageHealth:0}");
            SetAlert(ref oxygenAlert, data.report != null && data.report.oxygenNetKg < 0f, $"氧气净产出为负：{data.report?.oxygenNetKg:0.0}千克");
            SetAlert(ref wasteAlert, data.report != null && data.report.energyWastedKJ > 10000f, $"电力浪费较高：{data.report?.energyWastedKJ:0}千焦");
        }

        private void SetAlert(ref bool state, bool active, string message)
        {
            if (active && !state) Add(message);
            state = active;
        }

        private void Add(string message)
        {
            if (!ModConfig.Instance.includeEvents || string.IsNullOrWhiteSpace(message)) return;
            if (pending.Count >= 20) pending.RemoveAt(0);
            pending.Add(message);
            Debug.Log("[DeepSeekDanmaku][Event] " + message);
        }

        private static int GetRocketsInFlight()
        {
            ReportManager.DailyReport report = ReportManager.Instance?.TodaysReport;
            return report != null ? Mathf.RoundToInt(report.GetEntry(ReportManager.ReportType.RocketsInFlight)?.Net ?? 0f) : 0;
        }
    }
}
