using UnityEngine;

namespace CykModUtils.Diagnostics
{
    /// <summary>
    /// 帧耗时统计工具的配置。
    /// </summary>
    public sealed class FrameProfileOptions
    {
        /// <summary>Mod 稳定 ID，用于日志和默认启用文件目录。</summary>
        public string ModId { get; set; }

        /// <summary>Mod 内容目录。工具会在此目录下查找启用文件。</summary>
        public string ModPath { get; set; }

        /// <summary>承载统计组件的对象，通常传 Game.Instance.gameObject。</summary>
        public GameObject Owner { get; set; }

        /// <summary>启用文件名。</summary>
        public string EnableFileName { get; set; } = "FrameProfileTool.enabled";

        /// <summary>日志前缀。为空时使用 "[{ModId}][FrameProfile]"。</summary>
        public string LogPrefix { get; set; }

        /// <summary>报告间隔，单位秒。</summary>
        public float ReportIntervalSeconds { get; set; } = 60f;
    }
}
