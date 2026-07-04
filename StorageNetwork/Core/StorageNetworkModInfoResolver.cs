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
        /// 首先根据建筑的 `IBuildingConfig` 程序集推断其所属模组
        /// 运行时组件可能会由不相关模组提供的兼容性补丁添加，因此它们仅作为备选方案
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

            string prefabModName = GetSourceModNameFromPrefabComponents(storage, modNames);
            if (!string.IsNullOrEmpty(prefabModName))
            {
                return prefabModName;
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

        private static string GetSourceModNameFromPrefabComponents(Storage storage, Dictionary<Assembly, string> modNames)
        {
            string prefabId = GetStoragePrefabId(storage);
            if (string.IsNullOrEmpty(prefabId))
            {
                return string.Empty;
            }

            GameObject prefab = Assets.GetPrefab(new Tag(prefabId));
            if (prefab == null)
            {
                return string.Empty;
            }

            foreach (Component component in prefab.GetComponents<Component>())
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
            if (modNames == null || modNames.Count == 0)
            {
                return modNamesByPrefabId;
            }

            BuildingConfigManager manager = BuildingConfigManager.Instance;
            if (manager != null)
            {
                FieldInfo configTableField = typeof(BuildingConfigManager).GetField("configTable", BindingFlags.Instance | BindingFlags.NonPublic);
                IDictionary configTable = configTableField != null ? configTableField.GetValue(manager) as IDictionary : null;
                if (configTable != null)
                {
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
                }
            }

            AddModNamesFromBuildingConfigIds(modNames);
            return modNamesByPrefabId;
        }

        private static void AddModNamesFromBuildingConfigIds(Dictionary<Assembly, string> modNames)
        {
            foreach (KeyValuePair<Assembly, string> entry in modNames)
            {
                Assembly assembly = entry.Key;
                if (assembly == null || assembly == StorageNetworkAssembly)
                {
                    continue;
                }

                foreach (System.Type type in GetLoadableTypes(assembly))
                {
                    if (type == null || type.IsAbstract || !typeof(IBuildingConfig).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    string prefabId = GetBuildingConfigId(type);
                    if (!string.IsNullOrEmpty(prefabId) && !modNamesByPrefabId.ContainsKey(prefabId))
                    {
                        modNamesByPrefabId[prefabId] = entry.Value;
                    }
                }
            }
        }

        private static IEnumerable<System.Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        private static string GetBuildingConfigId(System.Type type)
        {
            FieldInfo field = type.GetField("ID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null && field.FieldType == typeof(string))
            {
                return field.GetValue(null) as string ?? string.Empty;
            }

            PropertyInfo property = type.GetProperty("ID", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null && property.PropertyType == typeof(string))
            {
                try
                {
                    return property.GetValue(null, null) as string ?? string.Empty;
                }
                catch (System.Exception)
                {
                    return string.Empty;
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
