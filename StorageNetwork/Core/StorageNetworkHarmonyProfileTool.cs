using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkHarmonyProfileTool
    {
        private const string ModId = "StorageNetwork";
        private const string EnableFileName = "HarmonyProfileTool.enabled";
        private const string LogPrefix = "[StorageNetwork][Profile]";
        private static readonly Dictionary<MethodBase, bool> PatchedMethods =
            new Dictionary<MethodBase, bool>();
        private static readonly Harmony Harmony = new Harmony("StorageNetwork.HarmonyProfileTool");
        private static string modPath;
        private static bool installed;
        private static bool verboseLogging;

        [ThreadStatic]
        private static int profileDepth;

        public static void SetModPath(string path)
        {
            modPath = path;
        }

        public static void DumpIfEnabled()
        {
            bool frameProfileEnabled = IsMarkerEnabled("FrameProfileTool.enabled");
            bool harmonyProfileEnabled = IsMarkerEnabled(EnableFileName);
            if (installed)
            {
                verboseLogging |= harmonyProfileEnabled;
                return;
            }

            if (!frameProfileEnabled && !harmonyProfileEnabled)
            {
                return;
            }

            verboseLogging = harmonyProfileEnabled;
            PatchMethods();
            installed = true;
            Debug.Log(LogPrefix + " enabled. Patched methods: " + PatchedMethods.Count + ".");
        }

        public static void Prefix(out ProfileState __state)
        {
            bool isRoot = profileDepth++ == 0;
            __state = new ProfileState(
                isRoot,
                isRoot ? Stopwatch.GetTimestamp() : 0L,
                isRoot ? GetAllocatedBytesForCurrentThread() : 0L);
        }

        public static void Postfix(MethodBase __originalMethod, ProfileState __state)
        {
            profileDepth = Math.Max(0, profileDepth - 1);
            if (!__state.IsRoot || __originalMethod == null)
            {
                return;
            }

            long elapsedTicks = Stopwatch.GetTimestamp() - __state.StartedTicks;
            long allocatedBytes = Math.Max(
                0L,
                GetAllocatedBytesForCurrentThread() - __state.StartedAllocatedBytes);
            StorageNetworkFrameProfileTool.RecordWork(elapsedTicks, allocatedBytes);
            if (!verboseLogging)
            {
                return;
            }

            double elapsedMilliseconds = elapsedTicks * 1000d / Stopwatch.Frequency;
            if (elapsedMilliseconds <= 1d)
            {
                return;
            }

            Debug.Log(string.Format(
                "{0} {1}: {2:F3}ms",
                LogPrefix,
                GetMethodName(__originalMethod),
                elapsedMilliseconds));
        }

        public static Exception Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                profileDepth = Math.Max(0, profileDepth - 1);
            }

            return __exception;
        }

        public static void ResetCurrentThreadDepth()
        {
            profileDepth = 0;
        }

        private static long GetAllocatedBytesForCurrentThread()
        {
            try
            {
                return GC.GetAllocatedBytesForCurrentThread();
            }
            catch (MissingMethodException)
            {
                return 0L;
            }
            catch (NotSupportedException)
            {
                return 0L;
            }
        }

        private static void PatchMethods()
        {
            MethodInfo prefix = AccessTools.Method(typeof(StorageNetworkHarmonyProfileTool), nameof(Prefix));
            MethodInfo postfix = AccessTools.Method(typeof(StorageNetworkHarmonyProfileTool), nameof(Postfix));
            MethodInfo finalizer = AccessTools.Method(typeof(StorageNetworkHarmonyProfileTool), nameof(Finalizer));
            HarmonyMethod prefixPatch = new HarmonyMethod(prefix);
            HarmonyMethod postfixPatch = new HarmonyMethod(postfix);
            HarmonyMethod finalizerPatch = verboseLogging ? new HarmonyMethod(finalizer) : null;

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
                        Harmony.Patch(method, prefixPatch, postfixPatch, finalizer: finalizerPatch);
                        PatchedMethods[method] = true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning(LogPrefix + " failed to patch " +
                            GetMethodName(method) + ": " + exception.Message);
                    }
                }
            }
        }

        private static bool IsMarkerEnabled(string fileName)
        {
            return File.Exists(Path.Combine(GetConfigDirectory(), fileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, fileName)));
        }

        private static string GetConfigDirectory()
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", ModId);
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

        private static bool ShouldProfileType(Type type)
        {
            return type != null &&
                   !IsTypeOrNestedUnder(type, typeof(StorageNetworkHarmonyProfileTool)) &&
                   !IsTypeOrNestedUnder(type, typeof(StorageNetworkFrameProfileTool)) &&
                   !type.IsGenericTypeDefinition &&
                   !type.IsInterface &&
                   type.Namespace != null &&
                   type.Namespace.StartsWith("StorageNetwork", StringComparison.Ordinal);
        }

        private static bool ShouldProfileMethod(MethodBase method)
        {
            if (method == null ||
                method.IsAbstract ||
                method.ContainsGenericParameters ||
                method.IsGenericMethodDefinition ||
                IsTypeOrNestedUnder(method.DeclaringType, typeof(StorageNetworkHarmonyProfileTool)) ||
                IsTypeOrNestedUnder(method.DeclaringType, typeof(StorageNetworkFrameProfileTool)) ||
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

        private static bool IsTypeOrNestedUnder(Type type, Type owner)
        {
            for (Type current = type; current != null; current = current.DeclaringType)
            {
                if (current == owner)
                {
                    return true;
                }
            }

            return false;
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

        public readonly struct ProfileState
        {
            public ProfileState(bool isRoot, long startedTicks, long startedAllocatedBytes)
            {
                IsRoot = isRoot;
                StartedTicks = startedTicks;
                StartedAllocatedBytes = startedAllocatedBytes;
            }

            public bool IsRoot { get; }
            public long StartedTicks { get; }
            public long StartedAllocatedBytes { get; }
        }
    }
}
