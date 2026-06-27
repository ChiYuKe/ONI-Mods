using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using StorageNetwork.ModConfig;
using UnityEngine;

namespace StorageNetwork
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Config
    {
        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_DEFAULT_MATERIAL_LIMIT",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_DEFAULT_MATERIAL_LIMIT_DESC",
            "材料请求默认限额 kg",
            "新接入生产建筑的默认请求限额。",
            1f,
            1000000f)]
        [JsonProperty]
        public float DefaultMaterialRequestLimitKg { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_GEYSER_WORLD_OUTPUT_FALLBACK",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_GEYSER_WORLD_OUTPUT_FALLBACK_DESC",
            "允许泉在网络不可用时排放到世界",
            "开启后，已接入网络并启用直接入网的泉会在网络无法储存产物时排放到世界；关闭后会暂停输出并等待网络恢复。",
            0f,
            1f)]
        [JsonProperty]
        public bool AllowGeyserWorldOutputFallback { get; set; }

        [JsonProperty]
        public List<int> StoragesEnabledOutputStoreToNetwork { get; set; }

        [JsonProperty]
        public Dictionary<string, StorageNetworkWindowLayout> WindowLayouts { get; set; }

        [JsonProperty]
        public int MainWorldFilterId { get; set; }

        [JsonProperty]
        public int MainWorldFilterContextWorldId { get; set; }

        [JsonProperty]
        public int EnrollableWorldFilterId { get; set; }

        [JsonProperty]
        public int EnrollableWorldFilterContextWorldId { get; set; }

        [JsonProperty]
        public int OrderWorldFilterId { get; set; }

        [JsonProperty]
        public int OrderWorldFilterContextWorldId { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_SUCCESS_COOLDOWN",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_SUCCESS_COOLDOWN_DESC",
            "请求成功冷却秒数",
            "材料已满足或达到限额后的检查间隔。",
            0.5f,
            60f)]
        [JsonProperty]
        public float MaterialRequestSuccessCooldownSeconds { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_RETRY_COOLDOWN",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_RETRY_COOLDOWN_DESC",
            "请求失败重试秒数",
            "缺料或没有可请求配方后的重试间隔。",
            0.5f,
            60f)]
        [JsonProperty]
        public float MaterialRequestRetryCooldownSeconds { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_INFINITE_QUEUE_BATCHES",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_INFINITE_QUEUE_BATCHES_DESC",
            "无限队列请求批次数",
            "生产队列为无限时，一次按多少批材料请求。",
            1f,
            99f,
            Integer = true)]
        [JsonProperty]
        public int InfiniteQueueRequestBatchCount { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_MAX_REQUEST_BATCHES",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_MAX_REQUEST_BATCHES_DESC",
            "最大请求批次数",
            "单次材料请求最多按多少批计算。",
            1f,
            99f,
            Integer = true)]
        [JsonProperty]
        public int MaxRequestBatchCount { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_PLAN_RECURSION_DEPTH",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_PLAN_RECURSION_DEPTH_DESC",
            "生产计划递归深度",
            "补产链路向下追踪的最大层数。",
            1f,
            10f,
            Integer = true)]
        [JsonProperty]
        public int ProductionPlanMaxDepth { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_ABNORMAL_TIMEOUT",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_ABNORMAL_TIMEOUT_DESC",
            "异常订单超时周期",
            "订单多长时间无进度后自动取消排产。",
            0.05f,
            10f)]
        [JsonProperty]
        public float AbnormalOrderTimeoutCycles { get; set; }

        [ModConfigOption(
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_COMPLETED_RETENTION",
            "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_COMPLETED_RETENTION_DESC",
            "完成订单保留周期",
            "完成/取消/异常订单在列表中保留多久。",
            0.05f,
            20f)]
        [JsonProperty]
        public float FinishedOrderRecordLifetimeCycles { get; set; }

        [ModConfigOption(
            "",
            "",
            "服务器容量倍率",
            "影响新建储存服务器和电池服务器的容量。已存在建筑通常需要重建或重新加载后才完全刷新。",
            0.1f,
            100f)]
        [JsonProperty]
        public float ServerCapacityMultiplier { get; set; }

        [ModConfigOption(
            "",
            "",
            "端口缓存容量倍率",
            "影响新建材料、液体、气体端口的缓存容量。",
            0.1f,
            100f)]
        [JsonProperty]
        public float PortCapacityMultiplier { get; set; }

        [ModConfigOption(
            "",
            "",
            "电力端口缓存倍率",
            "影响新建电力入网/出网端口的缓存电量。",
            0.1f,
            100f)]
        [JsonProperty]
        public float PowerPortCapacityMultiplier { get; set; }

        [ModConfigOption(
            "",
            "",
            "电池服务器漏电 J/周期",
            "所有电池服务器每周期损失的电量。",
            0f,
            100000f)]
        [JsonProperty]
        public float BatteryServerLeakJoulesPerCycle { get; set; }

        [ModConfigOption(
            "",
            "",
            "冷库最低目标温度 °C",
            "冷库温度滑条允许设置的最低目标温度。",
            -100f,
            0f)]
        [JsonProperty]
        public float ColdStorageMinTemperatureC { get; set; }

        [ModConfigOption(
            "",
            "",
            "冷库最高目标温度 °C",
            "冷库温度滑条允许设置的最高目标温度。",
            -20f,
            30f)]
        [JsonProperty]
        public float ColdStorageMaxTemperatureC { get; set; }

        [ModConfigOption(
            "",
            "",
            "冷库节能功耗 W",
            "内容物达到目标温度后，冷库维持温度时的功耗。",
            0f,
            1000f)]
        [JsonProperty]
        public float ColdStorageEnergySaverWatts { get; set; }

        [ModConfigOption(
            "",
            "",
            "冷库最大制冷功耗 W",
            "目标温度越低越接近此功耗。",
            1f,
            10000f)]
        [JsonProperty]
        public float ColdStorageMaxCoolingWatts { get; set; }

        [ModConfigOption(
            "",
            "",
            "冷库最大产热 kDTU/s",
            "目标温度越低越接近此产热。",
            0f,
            20f)]
        [JsonProperty]
        public float ColdStorageMaxCoolingHeatKW { get; set; }

        [ModConfigOption(
            "",
            "",
            "材料出网最大速率 kg/s",
            "材料出网端口设置滑条的最大输出速率。",
            1f,
            1000f)]
        [JsonProperty]
        public float SolidOutputMaxKgPerSecond { get; set; }

        [ModConfigOption(
            "",
            "",
            "液体出网最大速率 kg/s",
            "液体出网端口设置滑条的最大输出速率。",
            1f,
            1000f)]
        [JsonProperty]
        public float LiquidOutputMaxKgPerSecond { get; set; }

        [ModConfigOption(
            "",
            "",
            "气体出网最大速率 kg/s",
            "气体出网端口设置滑条的最大输出速率。",
            0.1f,
            100f)]
        [JsonProperty]
        public float GasOutputMaxKgPerSecond { get; set; }

        [ModConfigOption(
            "",
            "",
            "电力入网最大功率 W",
            "电力入网端口滑条的最大输入功率。",
            100f,
            1000000f)]
        [JsonProperty]
        public float PowerInputMaxWatts { get; set; }

        [ModConfigOption(
            "",
            "",
            "电力出网最大功率 W",
            "电力出网端口滑条的最大输出功率。",
            100f,
            1000000f)]
        [JsonProperty]
        public float PowerOutputMaxWatts { get; set; }

        private static ModConfigController<Config> controller;
        private static string modPath;

        public static Config Instance
        {
            get
            {
                return Controller.Instance;
            }
        }

        public Config()
        {
            DefaultMaterialRequestLimitKg = 1000f;
            AllowGeyserWorldOutputFallback = true;
            StoragesEnabledOutputStoreToNetwork = new List<int>();
            WindowLayouts = new Dictionary<string, StorageNetworkWindowLayout>();
            MainWorldFilterId = -2;
            MainWorldFilterContextWorldId = -2;
            EnrollableWorldFilterId = -2;
            EnrollableWorldFilterContextWorldId = -2;
            OrderWorldFilterId = -2;
            OrderWorldFilterContextWorldId = -2;
            MaterialRequestSuccessCooldownSeconds = 2f;
            MaterialRequestRetryCooldownSeconds = 5f;
            InfiniteQueueRequestBatchCount = 2;
            MaxRequestBatchCount = 99;
            ProductionPlanMaxDepth = 4;
            AbnormalOrderTimeoutCycles = 0.5f;
            FinishedOrderRecordLifetimeCycles = 1f;
            ServerCapacityMultiplier = 1f;
            PortCapacityMultiplier = 1f;
            PowerPortCapacityMultiplier = 1f;
            BatteryServerLeakJoulesPerCycle = 100f;
            ColdStorageMinTemperatureC = -20f;
            ColdStorageMaxTemperatureC = 1f;
            ColdStorageEnergySaverWatts = 20f;
            ColdStorageMaxCoolingWatts = 1000f;
            ColdStorageMaxCoolingHeatKW = 1f;
            SolidOutputMaxKgPerSecond = 100f;
            LiquidOutputMaxKgPerSecond = 20f;
            GasOutputMaxKgPerSecond = 5f;
            PowerInputMaxWatts = 10000f;
            PowerOutputMaxWatts = 100000f;
        }

        public static void SetModPath(string path)
        {
            modPath = path;
            controller = null;
        }

        public static void Load()
        {
            Controller.Load();
        }

        public static void Save()
        {
            Controller.Save();
        }

        public static void RegisterOptionsButton()
        {
            Controller.RegisterOptionsButton(
                "StorageNetwork",
                "StorageNetworkOptionsButton",
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CONFIG_TOOLTIP),
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CONFIG_TITLE),
                STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.CONFIG_HINT));
        }

        public bool IsStorageOutputStoreToNetworkEnabled(Storage storage)
        {
            int instanceId = GetStorageInstanceId(storage);
            return instanceId != KPrefabID.InvalidInstanceID &&
                   StoragesEnabledOutputStoreToNetwork != null &&
                   StoragesEnabledOutputStoreToNetwork.Contains(instanceId);
        }

        public void SetStorageOutputStoreToNetworkEnabled(Storage storage, bool enabled)
        {
            int instanceId = GetStorageInstanceId(storage);
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return;
            }

            if (StoragesEnabledOutputStoreToNetwork == null)
            {
                StoragesEnabledOutputStoreToNetwork = new List<int>();
            }

            bool currentlyEnabled = StoragesEnabledOutputStoreToNetwork.Contains(instanceId);
            if (enabled && !currentlyEnabled)
            {
                StoragesEnabledOutputStoreToNetwork.Add(instanceId);
            }
            else if (!enabled && currentlyEnabled)
            {
                StoragesEnabledOutputStoreToNetwork.Remove(instanceId);
            }
        }

        private void Normalize()
        {
            if (StoragesEnabledOutputStoreToNetwork == null)
            {
                StoragesEnabledOutputStoreToNetwork = new List<int>();
            }

            DefaultMaterialRequestLimitKg = Clamp(DefaultMaterialRequestLimitKg, 1f, 1000000f);
            MaterialRequestSuccessCooldownSeconds = Clamp(MaterialRequestSuccessCooldownSeconds, 0.5f, 60f);
            MaterialRequestRetryCooldownSeconds = Clamp(MaterialRequestRetryCooldownSeconds, 0.5f, 60f);
            InfiniteQueueRequestBatchCount = Math.Max(1, Math.Min(99, InfiniteQueueRequestBatchCount));
            MaxRequestBatchCount = Math.Max(1, Math.Min(99, MaxRequestBatchCount));
            ProductionPlanMaxDepth = Math.Max(1, Math.Min(10, ProductionPlanMaxDepth));
            AbnormalOrderTimeoutCycles = Clamp(AbnormalOrderTimeoutCycles, 0.05f, 10f);
            FinishedOrderRecordLifetimeCycles = Clamp(FinishedOrderRecordLifetimeCycles, 0.05f, 20f);
            ServerCapacityMultiplier = Clamp(ServerCapacityMultiplier, 0.1f, 100f);
            PortCapacityMultiplier = Clamp(PortCapacityMultiplier, 0.1f, 100f);
            PowerPortCapacityMultiplier = Clamp(PowerPortCapacityMultiplier, 0.1f, 100f);
            BatteryServerLeakJoulesPerCycle = Clamp(BatteryServerLeakJoulesPerCycle, 0f, 100000f);
            float requestedColdStorageMinTemperatureC = ColdStorageMinTemperatureC;
            float requestedColdStorageMaxTemperatureC = ColdStorageMaxTemperatureC;
            ColdStorageMinTemperatureC = Clamp(ColdStorageMinTemperatureC, -100f, 0f);
            ColdStorageMaxTemperatureC = Clamp(ColdStorageMaxTemperatureC, -20f, 30f);
            if (ColdStorageMaxTemperatureC < ColdStorageMinTemperatureC)
            {
                Debug.LogWarning(
                    $"[StorageNetwork] Cold storage temperature config adjusted: min={requestedColdStorageMinTemperatureC}C, max={requestedColdStorageMaxTemperatureC}C. " +
                    $"Using min={ColdStorageMinTemperatureC}C, max={ColdStorageMinTemperatureC}C after clamping.");
                ColdStorageMaxTemperatureC = ColdStorageMinTemperatureC;
            }

            ColdStorageEnergySaverWatts = Clamp(ColdStorageEnergySaverWatts, 0f, 1000f);
            ColdStorageMaxCoolingWatts = Clamp(ColdStorageMaxCoolingWatts, 1f, 10000f);
            ColdStorageMaxCoolingHeatKW = Clamp(ColdStorageMaxCoolingHeatKW, 0f, 20f);
            SolidOutputMaxKgPerSecond = Clamp(SolidOutputMaxKgPerSecond, 1f, 1000f);
            LiquidOutputMaxKgPerSecond = Clamp(LiquidOutputMaxKgPerSecond, 1f, 1000f);
            GasOutputMaxKgPerSecond = Clamp(GasOutputMaxKgPerSecond, 0.1f, 100f);
            PowerInputMaxWatts = Clamp(PowerInputMaxWatts, 100f, 1000000f);
            PowerOutputMaxWatts = Clamp(PowerOutputMaxWatts, 100f, 1000000f);
            if (MainWorldFilterId < -2)
            {
                MainWorldFilterId = -2;
            }

            if (MainWorldFilterContextWorldId < -2)
            {
                MainWorldFilterContextWorldId = -2;
            }

            if (EnrollableWorldFilterId < -2)
            {
                EnrollableWorldFilterId = -2;
            }

            if (EnrollableWorldFilterContextWorldId < -2)
            {
                EnrollableWorldFilterContextWorldId = -2;
            }

            if (OrderWorldFilterId < -2)
            {
                OrderWorldFilterId = -2;
            }

            if (OrderWorldFilterContextWorldId < -2)
            {
                OrderWorldFilterContextWorldId = -2;
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Max(min, Math.Min(max, value));
        }

        private static int GetStorageInstanceId(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private static string GetConfigPath()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documents))
            {
                return Path.Combine(documents, "Klei", "OxygenNotIncluded", "mods", "config", "StorageNetwork", "StorageNetworkConfig.json");
            }

            string root = !string.IsNullOrEmpty(modPath)
                ? modPath
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(root, "config", "StorageNetwork", "StorageNetworkConfig.json");
        }

        private static ModConfigController<Config> Controller
        {
            get
            {
                return controller ?? (controller = new ModConfigController<Config>(GetConfigPath(), "StorageNetwork", config => config.Normalize()));
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class StorageNetworkWindowLayout
    {
        [JsonProperty]
        public float X { get; set; }

        [JsonProperty]
        public float Y { get; set; }

        [JsonProperty]
        public float Width { get; set; }

        [JsonProperty]
        public float Height { get; set; }
    }
}
