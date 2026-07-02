using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace CykModUtils.Diagnostics
{
    /// <summary>
    /// 通用 Harmony 方法耗时分析工具。通过启用文件开关，运行时批量 Patch 指定程序集内的方法并输出慢调用日志。
    /// </summary>
    public static class HarmonyProfileTool
    {
        private static readonly Dictionary<MethodBase, ProfileSession> MethodSessions =
            new Dictionary<MethodBase, ProfileSession>();
        private static readonly Dictionary<string, ProfileSession> Sessions =
            new Dictionary<string, ProfileSession>();

        /// <summary>
        /// 如果启用文件存在，则扫描目标程序集并安装 profiling patch。
        /// </summary>
        public static int DumpIfEnabled(HarmonyProfileOptions options)
        {
            ProfileSession session = ProfileSession.Create(options);
            if (session == null || !IsEnabled(session.ModId, session.ModPath, session.EnableFileName))
            {
                return 0;
            }

            if (Sessions.TryGetValue(session.HarmonyId, out ProfileSession existing))
            {
                Debug.Log(existing.LogPrefix + " already enabled. Patched methods: " + existing.PatchedMethods.Count + ".");
                return existing.PatchedMethods.Count;
            }

            PatchMethods(session);
            Sessions[session.HarmonyId] = session;
            Debug.Log(session.LogPrefix + " enabled. Patched methods: " + session.PatchedMethods.Count + ".");
            return session.PatchedMethods.Count;
        }

        /// <summary>
        /// Harmony Prefix：记录方法开始时间。
        /// </summary>
        public static void Prefix(out Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }

        /// <summary>
        /// Harmony Postfix：计算耗时并按配置输出慢调用日志。
        /// </summary>
        public static void Postfix(MethodBase __originalMethod, Stopwatch __state)
        {
            if (__state == null || __originalMethod == null)
            {
                return;
            }

            __state.Stop();
            if (!MethodSessions.TryGetValue(__originalMethod, out ProfileSession session) ||
                __state.Elapsed.TotalMilliseconds <= session.MinLogMilliseconds)
            {
                return;
            }

            Debug.Log(string.Format(
                "{0} {1}: {2:F3}ms",
                session.LogPrefix,
                GetMethodName(__originalMethod),
                __state.Elapsed.TotalMilliseconds));
        }

        private static void PatchMethods(ProfileSession session)
        {
            MethodInfo prefix = AccessTools.Method(typeof(HarmonyProfileTool), nameof(Prefix));
            MethodInfo postfix = AccessTools.Method(typeof(HarmonyProfileTool), nameof(Postfix));
            HarmonyMethod prefixPatch = new HarmonyMethod(prefix);
            HarmonyMethod postfixPatch = new HarmonyMethod(postfix);

            foreach (Type type in session.TargetAssembly.GetTypes())
            {
                if (!ShouldProfileType(type, session))
                {
                    continue;
                }

                foreach (MethodBase method in GetDeclaredMethods(type))
                {
                    if (!ShouldProfileMethod(method, session))
                    {
                        continue;
                    }

                    try
                    {
                        session.Harmony.Patch(method, prefixPatch, postfixPatch);
                        session.PatchedMethods.Add(method);
                        MethodSessions[method] = session;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(session.LogPrefix + " failed to patch " +
                            GetMethodName(method) + ": " + exception.Message);
                    }
                }
            }
        }

        private static bool IsEnabled(string modId, string modPath, string enableFileName)
        {
            return File.Exists(Path.Combine(GetConfigDirectory(modId), enableFileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, enableFileName)));
        }

        private static string GetConfigDirectory(string modId)
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", string.IsNullOrEmpty(modId) ? "CykModUtils" : modId);
            }
            catch
            {
                return string.Empty;
            }
        }

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

        private static bool ShouldProfileType(Type type, ProfileSession session)
        {
            if (type == null ||
                type == typeof(HarmonyProfileTool) ||
                type.IsGenericTypeDefinition ||
                type.IsInterface)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(session.NamespacePrefix) &&
                (type.Namespace == null || !type.Namespace.StartsWith(session.NamespacePrefix, StringComparison.Ordinal)))
            {
                return false;
            }

            return session.TypeFilter == null || session.TypeFilter(type);
        }

        private static bool ShouldProfileMethod(MethodBase method, ProfileSession session)
        {
            if (method == null ||
                method.IsAbstract ||
                method.ContainsGenericParameters ||
                method.IsGenericMethodDefinition ||
                method.DeclaringType == typeof(HarmonyProfileTool) ||
                method.GetMethodBody() == null)
            {
                return false;
            }

            string name = method.Name;
            bool regularMethod =
                !name.StartsWith("get_", StringComparison.Ordinal) &&
                !name.StartsWith("set_", StringComparison.Ordinal) &&
                !name.StartsWith("add_", StringComparison.Ordinal) &&
                !name.StartsWith("remove_", StringComparison.Ordinal);
            return regularMethod && (session.MethodFilter == null || session.MethodFilter(method));
        }

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

        private sealed class ProfileSession
        {
            private ProfileSession()
            {
            }

            public string ModId { get; private set; }
            public string ModPath { get; private set; }
            public string EnableFileName { get; private set; }
            public string NamespacePrefix { get; private set; }
            public string HarmonyId { get; private set; }
            public string LogPrefix { get; private set; }
            public double MinLogMilliseconds { get; private set; }
            public Assembly TargetAssembly { get; private set; }
            public Func<Type, bool> TypeFilter { get; private set; }
            public Func<MethodBase, bool> MethodFilter { get; private set; }
            public Harmony Harmony { get; private set; }
            public HashSet<MethodBase> PatchedMethods { get; } = new HashSet<MethodBase>();

            public static ProfileSession Create(HarmonyProfileOptions options)
            {
                if (options == null)
                {
                    return null;
                }

                string modId = string.IsNullOrEmpty(options.ModId) ? "CykModUtils" : options.ModId;
                string harmonyId = string.IsNullOrEmpty(options.HarmonyId)
                    ? modId + ".HarmonyProfileTool"
                    : options.HarmonyId;
                Assembly assembly = options.TargetAssembly ??
                                    options.AnchorType?.Assembly;
                if (assembly == null)
                {
                    Debug.LogWarning("[" + modId + "][Profile] missing target assembly.");
                    return null;
                }

                return new ProfileSession
                {
                    ModId = modId,
                    ModPath = options.ModPath,
                    EnableFileName = string.IsNullOrEmpty(options.EnableFileName)
                        ? "HarmonyProfileTool.enabled"
                        : options.EnableFileName,
                    NamespacePrefix = options.NamespacePrefix,
                    HarmonyId = harmonyId,
                    LogPrefix = string.IsNullOrEmpty(options.LogPrefix)
                        ? "[" + modId + "][Profile]"
                        : options.LogPrefix,
                    MinLogMilliseconds = Math.Max(0d, options.MinLogMilliseconds),
                    TargetAssembly = assembly,
                    TypeFilter = options.TypeFilter,
                    MethodFilter = options.MethodFilter,
                    Harmony = new Harmony(harmonyId)
                };
            }
        }
    }
}
