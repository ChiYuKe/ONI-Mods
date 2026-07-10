using Newtonsoft.Json;
using StorageNetwork.Components;
using StorageNetwork.LogicDiy.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StorageNetwork.UI.WebEditor
{
    internal static class StorageNetworkLogicDiyPersistence
    {
        private const int Version = 1;
        private const string Extension = ".StorageNetworkLogicDiy.json";

        public static void Save(StorageNetworkLogicDiy logic)
        {
            int id = GetPersistentId(logic);
            string path = GetStorePath();
            if (id == KPrefabID.InvalidInstanceID || string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                LogicDiyStore store = LoadStore(path);
                store.version = Version;
                store.entries[id.ToString()] = new LogicDiyEntry
                {
                    runtimeBlueprintJson = logic.RuntimeBlueprintJson ?? string.Empty,
                    runtimeLayoutJson = logic.RuntimeLayoutJson ?? string.Empty,
                    outputModeValue = logic.OutputModeValue,
                    sourceModeValue = logic.SourceModeValue,
                    conditionThresholdKg = logic.ConditionThresholdKg,
                    conditionItemKey = logic.ConditionItemKey ?? string.Empty,
                    updatedAtUtcTicks = System.DateTime.UtcNow.Ticks
                };

                LogicDiyConfigStore.WriteAtomically(path, JsonConvert.SerializeObject(store, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to save logic DIY config: " + ex);
            }
        }

        public static bool TryLoad(StorageNetworkLogicDiy logic)
        {
            int id = GetPersistentId(logic);
            string path = GetStorePath();
            if (id == KPrefabID.InvalidInstanceID || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                LogicDiyStore store = LoadStore(path);
                if (store.entries == null || !store.entries.TryGetValue(id.ToString(), out LogicDiyEntry entry) || entry == null)
                {
                    return false;
                }

                logic.ApplyPersistedWebEditorState(
                    entry.runtimeBlueprintJson,
                    entry.outputModeValue,
                    entry.sourceModeValue,
                    entry.conditionThresholdKg,
                    entry.conditionItemKey,
                    entry.runtimeLayoutJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load logic DIY config: " + ex);
                return false;
            }
        }

        private static LogicDiyStore LoadStore(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new LogicDiyStore();
            }

            LogicDiyStore store = JsonConvert.DeserializeObject<LogicDiyStore>(LogicDiyConfigStore.ReadAllText(path));
            return store ?? new LogicDiyStore();
        }

        private static string GetStorePath()
        {
            string savePath = SaveLoader.GetActiveSaveFilePath();
            return string.IsNullOrEmpty(savePath) ? null : savePath + Extension;
        }

        private static int GetPersistentId(StorageNetworkLogicDiy logic)
        {
            KPrefabID prefabId = logic != null ? logic.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private sealed class LogicDiyStore
        {
            public int version = Version;
            public Dictionary<string, LogicDiyEntry> entries = new Dictionary<string, LogicDiyEntry>();
        }

        private sealed class LogicDiyEntry
        {
            public string runtimeBlueprintJson;
            public string runtimeLayoutJson;
            public int outputModeValue;
            public int sourceModeValue;
            public float conditionThresholdKg;
            public string conditionItemKey;
            public long updatedAtUtcTicks;
        }
    }
}
