using System.Collections;
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
        private static Dictionary<string, string> modNamesByPrefabId;

        public static void ResetRuntimeState()
        {
            modNamesByAssembly = null;
            modNamesByPrefabId = null;
        }

        /// <summary>
        /// Infers a building's source mod from its IBuildingConfig assembly first.
        /// Runtime components may be added by compatibility patches from unrelated mods, so they are only a fallback.
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

            string configModName = GetSourceModNameFromBuildingConfig(storage, modNames);
            if (!string.IsNullOrEmpty(configModName))
            {
                return configModName;
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

        private static string GetSourceModNameFromBuildingConfig(Storage storage, Dictionary<Assembly, string> modNames)
        {
            string prefabId = GetStoragePrefabId(storage);
            if (string.IsNullOrEmpty(prefabId))
            {
                return string.Empty;
            }

            Dictionary<string, string> prefabModNames = GetModNamesByPrefabId(modNames);
            return prefabModNames.TryGetValue(prefabId, out string modName) ? modName : string.Empty;
        }

        private static string GetStoragePrefabId(Storage storage)
        {
            BuildingComplete building = storage != null ? storage.GetComponent<BuildingComplete>() : null;
            if (building != null && building.Def != null && !string.IsNullOrEmpty(building.Def.PrefabID))
            {
                return building.Def.PrefabID;
            }

            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.PrefabTag.Name : string.Empty;
        }

        private static Dictionary<string, string> GetModNamesByPrefabId(Dictionary<Assembly, string> modNames)
        {
            if (modNamesByPrefabId != null)
            {
                return modNamesByPrefabId;
            }

            modNamesByPrefabId = new Dictionary<string, string>();
            BuildingConfigManager manager = BuildingConfigManager.Instance;
            if (manager == null || modNames == null || modNames.Count == 0)
            {
                return modNamesByPrefabId;
            }

            FieldInfo configTableField = typeof(BuildingConfigManager).GetField("configTable", BindingFlags.Instance | BindingFlags.NonPublic);
            IDictionary configTable = configTableField != null ? configTableField.GetValue(manager) as IDictionary : null;
            if (configTable == null)
            {
                return modNamesByPrefabId;
            }

            foreach (DictionaryEntry entry in configTable)
            {
                object config = entry.Key;
                BuildingDef buildingDef = entry.Value as BuildingDef;
                if (config == null || buildingDef == null || string.IsNullOrEmpty(buildingDef.PrefabID))
                {
                    continue;
                }

                Assembly assembly = config.GetType().Assembly;
                if (assembly == StorageNetworkAssembly)
                {
                    continue;
                }

                if (assembly != null &&
                    modNames.TryGetValue(assembly, out string modName) &&
                    !modNamesByPrefabId.ContainsKey(buildingDef.PrefabID))
                {
                    modNamesByPrefabId[buildingDef.PrefabID] = modName;
                }
            }

            return modNamesByPrefabId;
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
