using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using StorageNetwork.ModConfig;

namespace StorageNetwork
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Config
    {
        [ModConfigOption("场景扫描缓存秒数", "数值越小刷新越快，但遍历储存建筑更频繁。", 0.05f, 5f)]
        [JsonProperty]
        public float SceneScanCacheSeconds { get; set; }

        [ModConfigOption("材料请求默认限额 kg", "新接入生产建筑的默认请求限额。", 1f, 1000000f)]
        [JsonProperty]
        public float DefaultMaterialRequestLimitKg { get; set; }

        [JsonProperty]
        public List<int> MinionsAllowedRequestMaterialsFromNetwork { get; set; }

        [JsonProperty]
        public Dictionary<string, StorageNetworkWindowLayout> WindowLayouts { get; set; }

        [ModConfigOption("请求成功冷却秒数", "材料已满足或达到限额后的检查间隔。", 0.5f, 60f)]
        [JsonProperty]
        public float MaterialRequestSuccessCooldownSeconds { get; set; }

        [ModConfigOption("请求失败重试秒数", "缺料或没有可请求配方后的重试间隔。", 0.5f, 60f)]
        [JsonProperty]
        public float MaterialRequestRetryCooldownSeconds { get; set; }

        [ModConfigOption("无限队列请求批次数", "生产队列为无限时，一次按多少批材料请求。", 1f, 99f, Integer = true)]
        [JsonProperty]
        public int InfiniteQueueRequestBatchCount { get; set; }

        [ModConfigOption("最大请求批次数", "单次材料请求最多按多少批计算。", 1f, 99f, Integer = true)]
        [JsonProperty]
        public int MaxRequestBatchCount { get; set; }

        [ModConfigOption("生产计划递归深度", "补产链路向下追踪的最大层数。", 1f, 10f, Integer = true)]
        [JsonProperty]
        public int ProductionPlanMaxDepth { get; set; }

        [ModConfigOption("异常订单超时周期", "订单多长时间无进度后自动取消排产。", 0.05f, 10f)]
        [JsonProperty]
        public float AbnormalOrderTimeoutCycles { get; set; }

        [ModConfigOption("完成订单保留周期", "完成/取消/异常订单在列表中保留多久。", 0.05f, 20f)]
        [JsonProperty]
        public float FinishedOrderRecordLifetimeCycles { get; set; }

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
            SceneScanCacheSeconds = 0.25f;
            DefaultMaterialRequestLimitKg = 1000f;
            MinionsAllowedRequestMaterialsFromNetwork = new List<int>();
            WindowLayouts = new Dictionary<string, StorageNetworkWindowLayout>();
            MaterialRequestSuccessCooldownSeconds = 2f;
            MaterialRequestRetryCooldownSeconds = 5f;
            InfiniteQueueRequestBatchCount = 2;
            MaxRequestBatchCount = 99;
            ProductionPlanMaxDepth = 4;
            AbnormalOrderTimeoutCycles = 0.5f;
            FinishedOrderRecordLifetimeCycles = 1f;
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
                "调整 StorageNetwork 模组数值",
                "StorageNetwork 选项",
                "保存后会写入 StorageNetworkConfig.json。建筑容量等部分数值需要重进存档或重建建筑才会完全体现。");
        }

        public bool IsMinionAllowedRequestMaterialsFromNetwork(MinionIdentity minion)
        {
            int instanceId = GetMinionInstanceId(minion);
            return instanceId != KPrefabID.InvalidInstanceID &&
                   MinionsAllowedRequestMaterialsFromNetwork != null &&
                   MinionsAllowedRequestMaterialsFromNetwork.Contains(instanceId);
        }

        public void SetMinionAllowedRequestMaterialsFromNetwork(MinionIdentity minion, bool allowed)
        {
            int instanceId = GetMinionInstanceId(minion);
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return;
            }

            if (MinionsAllowedRequestMaterialsFromNetwork == null)
            {
                MinionsAllowedRequestMaterialsFromNetwork = new List<int>();
            }

            if (WindowLayouts == null)
            {
                WindowLayouts = new Dictionary<string, StorageNetworkWindowLayout>();
            }

            bool currentlyAllowed = MinionsAllowedRequestMaterialsFromNetwork.Contains(instanceId);
            if (allowed && !currentlyAllowed)
            {
                MinionsAllowedRequestMaterialsFromNetwork.Add(instanceId);
            }
            else if (!allowed && currentlyAllowed)
            {
                MinionsAllowedRequestMaterialsFromNetwork.Remove(instanceId);
            }
        }

        public bool HasAnyMinionAllowedRequestMaterialsFromNetwork()
        {
            return MinionsAllowedRequestMaterialsFromNetwork != null &&
                   MinionsAllowedRequestMaterialsFromNetwork.Count > 0;
        }

        private void Normalize()
        {
            if (MinionsAllowedRequestMaterialsFromNetwork == null)
            {
                MinionsAllowedRequestMaterialsFromNetwork = new List<int>();
            }

            SceneScanCacheSeconds = Clamp(SceneScanCacheSeconds, 0.05f, 5f);
            DefaultMaterialRequestLimitKg = Clamp(DefaultMaterialRequestLimitKg, 1f, 1000000f);
            MaterialRequestSuccessCooldownSeconds = Clamp(MaterialRequestSuccessCooldownSeconds, 0.5f, 60f);
            MaterialRequestRetryCooldownSeconds = Clamp(MaterialRequestRetryCooldownSeconds, 0.5f, 60f);
            InfiniteQueueRequestBatchCount = Math.Max(1, Math.Min(99, InfiniteQueueRequestBatchCount));
            MaxRequestBatchCount = Math.Max(1, Math.Min(99, MaxRequestBatchCount));
            ProductionPlanMaxDepth = Math.Max(1, Math.Min(10, ProductionPlanMaxDepth));
            AbnormalOrderTimeoutCycles = Clamp(AbnormalOrderTimeoutCycles, 0.05f, 10f);
            FinishedOrderRecordLifetimeCycles = Clamp(FinishedOrderRecordLifetimeCycles, 0.05f, 20f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return min;
            }

            return Math.Max(min, Math.Min(max, value));
        }

        private static int GetMinionInstanceId(MinionIdentity minion)
        {
            KPrefabID prefabId = minion != null ? minion.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private static string GetConfigPath()
        {
            string root = !string.IsNullOrEmpty(modPath)
                ? modPath
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(root, "StorageNetworkConfig.json");
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
