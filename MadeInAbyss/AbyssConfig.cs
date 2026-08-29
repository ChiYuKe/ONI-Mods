using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 来自深渊 mod 配置。
    /// 配置文件与当前存档同目录（存档目录/MadeInAbyssConfig.json），
    /// 首次进入游戏时若不存在则写入默认配置，玩家可手动修改后重读存档生效。
    /// </summary>
    public class AbyssConfig
    {
        /// <summary>是否启用上升负荷（深渊诅咒）。</summary>
        public bool EnableCurse = true;

        /// <summary>是否启用笛级晋升。</summary>
        public bool EnableWhistle = true;

        /// <summary>是否启用深渊遗物掉落。</summary>
        public bool EnableRelics = true;

        /// <summary>是否启用生骸化（从最终地诅咒中幸存后免疫诅咒）。</summary>
        public bool EnableNarehate = true;

        /// <summary>六层深渊的触发深度（米，相对探窟营地/打印舱所在高度，1 格 = 1 米）。</summary>
        public float[] LayerDepthM = { 30f, 50f, 70f, 90f, 110f, 130f };

        /// <summary>判定“正在上升”所需的下降回退量（米）：当前深度比本次最深浅这么多米时视为上升。</summary>
        public float AscentTriggerM = 3f;

        /// <summary>回到营地附近多少米以内时重置本次下潜记录。</summary>
        public float SurfaceResetM = 5f;

        /// <summary>遗物掉落的最小深度（米）。</summary>
        public float RelicMinDepthM = 40f;

        /// <summary>每次挖透一格时的遗物掉落概率（百分比）。</summary>
        public float RelicChancePercent = 2.5f;

        /// <summary>诅咒效果持续时间（秒）。</summary>
        public float CurseDurationS = 600f;

        private static AbyssConfig instance;

        public static AbyssConfig Instance
        {
            get
            {
                if (instance == null)
                    EnsureLoaded();
                return instance;
            }
        }

        public static void EnsureLoaded()
        {
            if (instance != null)
                return;

            instance = new AbyssConfig();
            try
            {
                string path = GetConfigPath();
                if (string.IsNullOrEmpty(path))
                    return;

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    AbyssConfig loaded = JsonConvert.DeserializeObject<AbyssConfig>(json);
                    if (loaded != null)
                        instance = loaded;
                }
                else
                {
                    File.WriteAllText(path, JsonConvert.SerializeObject(instance, Formatting.Indented));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MadeInAbyss] 读取配置失败，使用默认配置: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新加载配置。存档切换时（SaveLoader.SetActiveSaveFilePath）调用，
        /// 以便每个存档都拥有独立的配置文件。
        /// </summary>
        public static void Reload()
        {
            instance = null;
            EnsureLoaded();
        }

        private static string GetConfigPath()
        {
            try
            {
                string savePath = SaveLoader.GetActiveSaveFilePath();
                if (string.IsNullOrEmpty(savePath))
                    return null;
                return Path.Combine(Path.GetDirectoryName(savePath), "MadeInAbyssConfig.json");
            }
            catch
            {
                return null;
            }
        }
    }
}
