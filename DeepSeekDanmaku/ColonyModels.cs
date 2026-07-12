using System;
using System.Collections.Generic;

namespace DeepSeekDanmaku
{
    [Serializable]
    internal sealed class ColonySnapshotData
    {
        public SnapshotBasic basic = new SnapshotBasic();
        public List<WorldSnapshot> worlds = new List<WorldSnapshot>();
        public SnapshotTotals totals = new SnapshotTotals();
        public DailyReportSnapshot report;
        public TrendSnapshot trends;
        public List<string> risks = new List<string>();
        public List<string> events = new List<string>();
    }

    [Serializable] internal sealed class SnapshotBasic { public int cycle; public string activeWorld; public string scope; public float gameSpeed; public int duplicants; }
    [Serializable] internal sealed class WorldSnapshot { public int id; public string name; public int duplicants; public List<DuplicantSnapshot> people = new List<DuplicantSnapshot>(); public ResourceSnapshot resources = new ResourceSnapshot(); }
    [Serializable] internal sealed class DuplicantSnapshot { public string name; public float stressPercent; public float health; public float caloriesKcal; public string chore; public bool idle; }
    [Serializable] internal sealed class ResourceSnapshot { public float foodKcal; public float breathableKg; public float farmableKg; public float industrialIngredients; public float industrialProducts; public float medicalSupplies; public float techComponents; }
    [Serializable] internal sealed class SnapshotTotals { public int duplicants; public float averageStressPercent; public float averageHealth; public float averageCaloriesKcal; public ResourceSnapshot resources = new ResourceSnapshot(); }
    [Serializable] internal sealed class DailyReportSnapshot { public int cycle; public float oxygenNetKg; public float energyUsedKJ; public float energyWastedKJ; public float workPercent; public float travelPercent; public float personalPercent; public float idlePercent; public int choresAdded; public int choresCompleted; public int choresNet; public int domesticatedCritters; public int wildCritters; public int rocketsInFlight; }
    [Serializable] internal sealed class TrendSnapshot { public int cycles; public string food; public string oxygen; public string energyUse; public string energyWaste; public string work; public string travel; public string idle; public string duplicants; public string critters; }

    internal enum DanmakuSeverity { Normal, Notice, Warning }
    internal sealed class AiDanmakuResult { public List<string> comments = new List<string>(); public DanmakuSeverity severity; public string topic = "综合"; }
}
