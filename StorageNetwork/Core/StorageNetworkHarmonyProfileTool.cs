using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Core
{
    /// <summary>
    /// Harmony 性能分析工具（运行时自动 Patch StorageNetwork 内所有方法）
    /// 用于在开发/调试阶段统计方法耗时，定位卡顿热点
    /// </summary>
    internal static class StorageNetworkHarmonyProfileTool
    {
        /// <summary>
        /// 启用标记文件名
        /// 存在该文件则启用 profiling
        /// </summary>
        private const string EnableFileName = "HarmonyProfileTool.enabled";

        /// <summary>
        /// Harmony ID（避免冲突）
        /// </summary>
        private const string HarmonyId = "StorageNetwork.HarmonyProfileTool";

        /// <summary>
        /// 日志前缀
        /// </summary>
        private const string LogPrefix = "[StorageNetwork][Profile]";

        /// <summary>
        /// 最小输出耗时阈值（低于该值不记录，避免刷屏）
        /// 单位：毫秒
        /// </summary>
        private const double MinLogMilliseconds = 1d;

        /// <summary>
        /// Harmony 实例，用于动态 Patch 方法
        /// </summary>
        private static readonly Harmony ProfilerHarmony = new Harmony(HarmonyId);

        /// <summary>
        /// 已经 Patch 的方法集合（防止重复 Patch）
        /// </summary>
        private static readonly HashSet<MethodBase> PatchedMethods = new HashSet<MethodBase>();

        /// <summary>
        /// 检查是否启用 profiling，并执行 Patch
        /// </summary>
        /// <param name="modPath">Mod 路径（可选，用于额外检测 enable 文件）</param>
        public static void DumpIfEnabled(string modPath)
        {
            if (!IsEnabled(modPath))
            {
                return;
            }

            PatchStorageNetworkMethods();
            Debug.Log(LogPrefix + " enabled. Patched methods: " + PatchedMethods.Count + ".");
        }

        /// <summary>
        /// Harmony Prefix：在目标方法执行前调用
        /// 用于开始计时
        /// </summary>
        /// <param name="__state">Stopwatch 状态对象（Harmony 传递）</param>
        public static void Prefix(out Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }

        /// <summary>
        /// Harmony Postfix：在目标方法执行后调用
        /// 用于统计耗时并输出日志
        /// </summary>
        /// <param name="__originalMethod">被 Patch 的原始方法</param>
        /// <param name="__state">Prefix 中创建的 Stopwatch</param>
        public static void Postfix(MethodBase __originalMethod, Stopwatch __state)
        {
            if (__state == null)
            {
                return;
            }

            __state.Stop();

            // 过滤低耗时调用，避免日志爆炸
            if (__state.Elapsed.TotalMilliseconds <= MinLogMilliseconds)
            {
                return;
            }

            Debug.Log(string.Format(
                "{0} {1}: {2:F3}ms",
                LogPrefix,
                GetMethodName(__originalMethod),
                __state.Elapsed.TotalMilliseconds));
        }

        /// <summary>
        /// 判断是否启用 profiler
        /// 条件：
        /// 1. mods/StorageNetwork/HarmonyProfileTool.enabled 存在
        /// 或
        /// 2. modPath/HarmonyProfileTool.enabled 存在
        /// </summary>
        private static bool IsEnabled(string modPath)
        {
            return File.Exists(Path.Combine(GetConfigDirectory(), EnableFileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, EnableFileName)));
        }

        /// <summary>
        /// 获取配置目录（ONI Mods 根目录）
        /// </summary>
        private static string GetConfigDirectory()
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", "StorageNetwork");
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 扫描 Assembly 并 Patch StorageNetwork 下所有方法
        /// 核心逻辑：
        /// - 遍历所有 Type
        /// - 过滤 StorageNetwork namespace
        /// - Patch 每个方法
        /// </summary>
        private static void PatchStorageNetworkMethods()
        {
            MethodInfo prefix = AccessTools.Method(typeof(StorageNetworkHarmonyProfileTool), nameof(Prefix));
            MethodInfo postfix = AccessTools.Method(typeof(StorageNetworkHarmonyProfileTool), nameof(Postfix));
            HarmonyMethod prefixPatch = new HarmonyMethod(prefix);
            HarmonyMethod postfixPatch = new HarmonyMethod(postfix);

            foreach (Type type in typeof(StorageNetworkHarmonyProfileTool).Assembly.GetTypes())
            {
                if (!ShouldProfileType(type))
                {
                    continue;
                }

                foreach (MethodBase method in GetDeclaredMethods(type))
                {
                    if (!ShouldProfileMethod(method))
                    {
                        continue;
                    }

                    try
                    {
                        ProfilerHarmony.Patch(method, prefixPatch, postfixPatch);
                        PatchedMethods.Add(method);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(LogPrefix + " failed to patch " +
                            GetMethodName(method) + ": " + exception.Message);
                    }
                }
            }
        }

        /// <summary>
        /// 获取类型中声明的所有方法（包含构造函数）
        /// 只获取 DeclaredOnly（不递归父类）
        /// </summary>
        private static IEnumerable<MethodBase> GetDeclaredMethods(Type type)
        {
            const BindingFlags Flags = BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.DeclaredOnly;

            foreach (ConstructorInfo constructor in type.GetConstructors(Flags))
            {
                yield return constructor;
            }

            foreach (MethodInfo method in type.GetMethods(Flags))
            {
                yield return method;
            }
        }

        /// <summary>
        /// 判断是否需要 Profile 某个类型
        /// 过滤条件：
        /// - 必须属于 StorageNetwork namespace
        /// - 排除自身
        /// - 排除泛型 / 接口
        /// </summary>
        private static bool ShouldProfileType(Type type)
        {
            return type != null &&
                   type.Namespace != null &&
                   type.Namespace.StartsWith("StorageNetwork", StringComparison.Ordinal) &&
                   type != typeof(StorageNetworkHarmonyProfileTool) &&
                   !type.IsGenericTypeDefinition &&
                   !type.IsInterface;
        }

        /// <summary>
        /// 判断是否需要 Profile 某个方法
        /// 过滤条件：
        /// - 排除 abstract / 泛型方法
        /// - 排除 getter/setter/event
        /// - 必须有 MethodBody（排除 extern / ILStub）
        /// - 排除工具自身方法
        /// </summary>
        private static bool ShouldProfileMethod(MethodBase method)
        {
            if (method == null ||
                method.IsAbstract ||
                method.ContainsGenericParameters ||
                method.IsGenericMethodDefinition ||
                method.DeclaringType == typeof(StorageNetworkHarmonyProfileTool) ||
                method.GetMethodBody() == null)
            {
                return false;
            }

            string name = method.Name;

            // 排除属性访问器 & 事件
            return !name.StartsWith("get_", StringComparison.Ordinal) &&
                   !name.StartsWith("set_", StringComparison.Ordinal) &&
                   !name.StartsWith("add_", StringComparison.Ordinal) &&
                   !name.StartsWith("remove_", StringComparison.Ordinal);
        }

        /// <summary>
        /// 格式化方法名用于日志输出
        /// </summary>
        private static string GetMethodName(MethodBase method)
        {
            if (method == null)
            {
                return "<unknown>";
            }

            Type declaringType = method.DeclaringType;
            string typeName = declaringType != null ? declaringType.FullName : "<global>";

            return typeName + "." + method.Name;
        }
    }
}