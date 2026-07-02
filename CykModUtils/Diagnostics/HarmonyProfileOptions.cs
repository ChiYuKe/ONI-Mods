using System;
using System.Reflection;

namespace CykModUtils.Diagnostics
{
    /// <summary>
    /// Harmony 方法耗时分析工具的配置。
    /// </summary>
    public sealed class HarmonyProfileOptions
    {
        /// <summary>Mod 稳定 ID，用于日志、默认 Harmony ID 和默认启用文件目录。</summary>
        public string ModId { get; set; }

        /// <summary>Mod 内容目录。工具会在此目录下查找启用文件。</summary>
        public string ModPath { get; set; }

        /// <summary>要扫描并 Patch 的程序集。为空时使用调用方传入锚点类型所在程序集。</summary>
        public Assembly TargetAssembly { get; set; }

        /// <summary>用于推断 TargetAssembly 的类型。</summary>
        public Type AnchorType { get; set; }

        /// <summary>只分析命名空间以此前缀开头的类型。为空时不过滤命名空间。</summary>
        public string NamespacePrefix { get; set; }

        /// <summary>Harmony 实例 ID。为空时使用 "{ModId}.HarmonyProfileTool"。</summary>
        public string HarmonyId { get; set; }

        /// <summary>日志前缀。为空时使用 "[{ModId}][Profile]"。</summary>
        public string LogPrefix { get; set; }

        /// <summary>启用文件名。</summary>
        public string EnableFileName { get; set; } = "HarmonyProfileTool.enabled";

        /// <summary>低于此耗时的方法不会输出日志，单位毫秒。</summary>
        public double MinLogMilliseconds { get; set; } = 1d;

        /// <summary>额外类型过滤器。返回 false 时跳过该类型。</summary>
        public Func<Type, bool> TypeFilter { get; set; }

        /// <summary>额外方法过滤器。返回 false 时跳过该方法。</summary>
        public Func<MethodBase, bool> MethodFilter { get; set; }
    }
}
