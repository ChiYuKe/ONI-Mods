using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KMod;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkModInfoResolver
    {
        private static readonly Assembly StorageNetworkAssembly = typeof(StorageNetworkModInfoResolver).Assembly;
        private static Dictionary<Assembly, string> modNamesByAssembly;

        public static void ResetRuntimeState()
        {
            modNamesByAssembly = null;
        }

        /// <summary>
        /// Infers a building's source mod from component assemblies, skipping StorageNetwork itself.
        /// </summary>
        public static string GetSourceModName(Storage storage)
        {
            if (storage == null)
            {
                return string.Empty;
            }

            Dictionary<Assembly, string> modNames = GetModNamesByAssembly();
            if (modNames.Count == 0)
            {
                return string.Empty;
            }

            foreach (Component component in storage.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                Assembly assembly = component.GetType().Assembly;
                if (assembly == StorageNetworkAssembly)
                {
                    continue;
                }

                if (assembly != null && modNames.TryGetValue(assembly, out string modName))
                {
                    return modName;
                }
            }

            return string.Empty;
        }

        private static Dictionary<Assembly, string> GetModNamesByAssembly()
        {
            if (modNamesByAssembly != null)
            {
                return modNamesByAssembly;
            }

            modNamesByAssembly = new Dictionary<Assembly, string>();
            Manager manager = Global.Instance != null ? Global.Instance.modManager : null;
            if (manager == null || manager.mods == null)
            {
                return modNamesByAssembly;
            }

            foreach (Mod mod in manager.mods.Where(mod => mod != null && mod.loaded_mod_data != null && mod.loaded_mod_data.dlls != null))
            {
                foreach (Assembly assembly in mod.loaded_mod_data.dlls.Where(assembly => assembly != null))
                {
                    if (!modNamesByAssembly.ContainsKey(assembly))
                    {
                        modNamesByAssembly[assembly] = mod.title;
                    }
                }
            }

            return modNamesByAssembly;
        }
    }
}
