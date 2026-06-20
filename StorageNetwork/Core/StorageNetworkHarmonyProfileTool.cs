using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkHarmonyProfileTool
    {
        private const string EnableFileName = "HarmonyProfileTool.enabled";
        private const string HarmonyId = "StorageNetwork.HarmonyProfileTool";
        private const string LogPrefix = "[StorageNetwork][Profile]";
        private const double MinLogMilliseconds = 1d;

        private static readonly Harmony ProfilerHarmony = new Harmony(HarmonyId);
        private static readonly HashSet<MethodBase> PatchedMethods = new HashSet<MethodBase>();

        public static void DumpIfEnabled(string modPath)
        {
            if (!IsEnabled(modPath))
            {
                return;
            }

            PatchStorageNetworkMethods();
            Debug.Log(LogPrefix + " enabled. Patched methods: " + PatchedMethods.Count + ".");
        }

        public static void Prefix(out Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }

        public static void Postfix(MethodBase __originalMethod, Stopwatch __state)
        {
            if (__state == null)
            {
                return;
            }

            __state.Stop();
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

        private static bool IsEnabled(string modPath)
        {
            return File.Exists(Path.Combine(GetConfigDirectory(), EnableFileName)) ||
                (!string.IsNullOrEmpty(modPath) && File.Exists(Path.Combine(modPath, EnableFileName)));
        }

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
                        Debug.LogWarning(LogPrefix + " failed to patch " + GetMethodName(method) + ": " + exception.Message);
                    }
                }
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

        private static bool ShouldProfileType(Type type)
        {
            return type != null &&
                type.Namespace != null &&
                type.Namespace.StartsWith("StorageNetwork", StringComparison.Ordinal) &&
                type != typeof(StorageNetworkHarmonyProfileTool) &&
                !type.IsGenericTypeDefinition &&
                !type.IsInterface;
        }

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
            return !name.StartsWith("get_", StringComparison.Ordinal) &&
                !name.StartsWith("set_", StringComparison.Ordinal) &&
                !name.StartsWith("add_", StringComparison.Ordinal) &&
                !name.StartsWith("remove_", StringComparison.Ordinal);
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
    }
}
