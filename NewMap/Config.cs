using System;
using System.IO;
using System.Reflection;
using ModConfig;
using Newtonsoft.Json;

namespace NewMap
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Config
    {
        [ModConfigOption("启用运行时气候", "关闭后只保留开图初始温度分区。", 0f, 1f)]
        [JsonProperty]
        public bool EnableClimateControl { get; set; }

        [ModConfigOption("一年周期长度", "四季循环的总周期数。", 20f, 400f)]
        [JsonProperty]
        public float YearLengthCycles { get; set; }

        [ModConfigOption("每次气候扫描格数", "越高温度调整越快，但性能压力越高。", 50f, 5000f, Integer = true)]
        [JsonProperty]
        public int ClimateCellsPerTick { get; set; }

        [ModConfigOption("单次最大调温 K", "每次扫描单格最多改变多少开尔文。", 0.1f, 25f)]
        [JsonProperty]
        public float ClimateMaxKelvinPerVisit { get; set; }

        [ModConfigOption("天井温度 C", "中央天井和出生口袋目标温度。", -50f, 80f)]
        [JsonProperty]
        public float SkywellTemperatureC { get; set; }

        [ModConfigOption("春季左侧 C", "春季左半图目标温度。", -150f, 100f)]
        [JsonProperty]
        public float SpringLeftC { get; set; }

        [ModConfigOption("春季右侧 C", "春季右半图目标温度。", -50f, 150f)]
        [JsonProperty]
        public float SpringRightC { get; set; }

        [ModConfigOption("夏季左侧 C", "夏季左半图目标温度。", -150f, 100f)]
        [JsonProperty]
        public float SummerLeftC { get; set; }

        [ModConfigOption("夏季右侧 C", "夏季右半图目标温度。", -50f, 150f)]
        [JsonProperty]
        public float SummerRightC { get; set; }

        [ModConfigOption("秋季左侧 C", "秋季左半图目标温度。", -150f, 100f)]
        [JsonProperty]
        public float AutumnLeftC { get; set; }

        [ModConfigOption("秋季右侧 C", "秋季右半图目标温度。", -50f, 150f)]
        [JsonProperty]
        public float AutumnRightC { get; set; }

        [ModConfigOption("冬季左侧 C", "冬季左半图目标温度。", -150f, 100f)]
        [JsonProperty]
        public float WinterLeftC { get; set; }

        [ModConfigOption("冬季右侧 C", "冬季右半图目标温度。", -50f, 150f)]
        [JsonProperty]
        public float WinterRightC { get; set; }

        [ModConfigOption("开图左侧 C", "地图生成时左半图初始温度。", -150f, 100f)]
        [JsonProperty]
        public float InitialLeftC { get; set; }

        [ModConfigOption("开图右侧 C", "地图生成时右半图初始温度。", -50f, 150f)]
        [JsonProperty]
        public float InitialRightC { get; set; }

        [ModConfigOption("天井核心半径", "中间真空/氧气主通道半径。", 2f, 12f, Integer = true)]
        [JsonProperty]
        public int SkywellCoreRadius { get; set; }

        [ModConfigOption("天井墙半径", "深渊晶体护壁外缘半径。", 3f, 18f, Integer = true)]
        [JsonProperty]
        public int SkywellWallRadius { get; set; }

        [ModConfigOption("天井晕圈半径", "矿物点缀和温和边界半径。", 4f, 28f, Integer = true)]
        [JsonProperty]
        public int SkywellHaloRadius { get; set; }

        [ModConfigOption("出生高度比例", "出生点在地图高度中的比例。", 0.2f, 0.85f)]
        [JsonProperty]
        public float SpawnHeightPercent { get; set; }

        [ModConfigOption("自然固体最低 kg", "地图创建时自然固体质量下限。", 1f, 2000f)]
        [JsonProperty]
        public float NaturalSolidMinimumMassKg { get; set; }

        [ModConfigOption("自然固体最高 kg", "地图创建时自然固体质量上限。", 1f, 5000f)]
        [JsonProperty]
        public float NaturalSolidMaximumMassKg { get; set; }

        private static ModConfigController<Config> controller;
        private static string modPath;

        public Config()
        {
            EnableClimateControl = true;
            YearLengthCycles = 80f;
            ClimateCellsPerTick = 900;
            ClimateMaxKelvinPerVisit = 5f;
            SkywellTemperatureC = 23f;
            SpringLeftC = -40f;
            SpringRightC = 45f;
            SummerLeftC = -20f;
            SummerRightC = 100f;
            AutumnLeftC = -60f;
            AutumnRightC = 60f;
            WinterLeftC = -100f;
            WinterRightC = 30f;
            InitialLeftC = -100f;
            InitialRightC = 100f;
            SkywellCoreRadius = 4;
            SkywellWallRadius = 7;
            SkywellHaloRadius = 10;
            SpawnHeightPercent = 0.55f;
            NaturalSolidMinimumMassKg = 100f;
            NaturalSolidMaximumMassKg = 400f;
        }

        public static Config Instance
        {
            get { return Controller.Instance; }
        }

        public static void SetModPath(string path)
        {
            modPath = path;
            controller = null;
        }

        public static void Load()
        {
            Controller.Load();
            Controller.Save();
        }

        public static void RegisterOptionsButton()
        {
            Controller.RegisterOptionsButton(
                "新地图：葱翠裂谷",
                "NewMapOptionsButton",
                "调整葱翠裂谷地图生成和气候参数",
                "葱翠裂谷配置",
                "保存后会写入 NewMapConfig.json。地图生成参数需要重启游戏并新开图才会完全生效。");
        }

        public static float ToKelvin(float celsius)
        {
            return celsius + 273.15f;
        }

        private void Normalize()
        {
            YearLengthCycles = Clamp(YearLengthCycles, 20f, 400f);
            ClimateCellsPerTick = ClampInt(ClimateCellsPerTick, 50, 5000);
            ClimateMaxKelvinPerVisit = Clamp(ClimateMaxKelvinPerVisit, 0.1f, 25f);
            SkywellTemperatureC = Clamp(SkywellTemperatureC, -50f, 80f);
            SpringLeftC = Clamp(SpringLeftC, -150f, 100f);
            SpringRightC = Clamp(SpringRightC, -50f, 150f);
            SummerLeftC = Clamp(SummerLeftC, -150f, 100f);
            SummerRightC = Clamp(SummerRightC, -50f, 150f);
            AutumnLeftC = Clamp(AutumnLeftC, -150f, 100f);
            AutumnRightC = Clamp(AutumnRightC, -50f, 150f);
            WinterLeftC = Clamp(WinterLeftC, -150f, 100f);
            WinterRightC = Clamp(WinterRightC, -50f, 150f);
            InitialLeftC = Clamp(InitialLeftC, -150f, 100f);
            InitialRightC = Clamp(InitialRightC, -50f, 150f);
            SkywellCoreRadius = ClampInt(SkywellCoreRadius, 2, 12);
            SkywellWallRadius = Math.Max(SkywellCoreRadius + 1, ClampInt(SkywellWallRadius, 3, 18));
            SkywellHaloRadius = Math.Max(SkywellWallRadius + 1, ClampInt(SkywellHaloRadius, 4, 28));
            SpawnHeightPercent = Clamp(SpawnHeightPercent, 0.2f, 0.85f);
            NaturalSolidMinimumMassKg = Clamp(NaturalSolidMinimumMassKg, 1f, 2000f);
            NaturalSolidMaximumMassKg = Math.Max(NaturalSolidMinimumMassKg, Clamp(NaturalSolidMaximumMassKg, 1f, 5000f));
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Max(min, Math.Min(max, value));
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static string GetConfigPath()
        {
            string root = !string.IsNullOrEmpty(modPath)
                ? modPath
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(root, "NewMapConfig.json");
        }

        private static ModConfigController<Config> Controller
        {
            get
            {
                return controller ?? (controller = new ModConfigController<Config>(GetConfigPath(), "NewMap", config => config.Normalize()));
            }
        }
    }
}
