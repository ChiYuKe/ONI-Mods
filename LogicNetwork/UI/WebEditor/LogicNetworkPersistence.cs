using Newtonsoft.Json;
using LogicNetwork.Components;
using LogicNetwork.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LogicNetwork.UI.WebEditor
{
    internal static class LogicNetworkPersistence
    {
        private const int Version = 1;
        private const string Extension = ".LogicNetwork.json";

        public static void Save(LogicNetworkEmitter logic)
        {
            int id = GetPersistentId(logic);
            string path = GetStorePath();
            if (id == KPrefabID.InvalidInstanceID || string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                LogicNetworkStore store = LoadStore(path);
                store.version = Version;
                store.entries[id.ToString()] = new LogicNetworkEntry
                {
                    runtimeBlueprintJson = logic.RuntimeBlueprintJson ?? string.Empty,
                    runtimeLayoutJson = logic.RuntimeLayoutJson ?? string.Empty,
                    outputModeValue = logic.OutputModeValue,
                    updatedAtUtcTicks = System.DateTime.UtcNow.Ticks
                };

                LogicNetworkConfigStore.WriteAtomically(path, JsonConvert.SerializeObject(store, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LogicNetwork] Failed to save logic network config: " + ex);
            }
        }

        public static bool TryLoad(LogicNetworkEmitter logic)
        {
            int id = GetPersistentId(logic);
            string path = GetStorePath();
            if (id == KPrefabID.InvalidInstanceID || string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                LogicNetworkStore store = LoadStore(path);
                if (store.entries == null || !store.entries.TryGetValue(id.ToString(), out LogicNetworkEntry entry) || entry == null)
                {
                    return false;
                }

                logic.ApplyPersistedWebEditorState(
                    entry.runtimeBlueprintJson,
                    entry.outputModeValue,
                    entry.runtimeLayoutJson);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LogicNetwork] Failed to load logic network config: " + ex);
                return false;
            }
        }

        private static LogicNetworkStore LoadStore(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new LogicNetworkStore();
            }

            LogicNetworkStore store = JsonConvert.DeserializeObject<LogicNetworkStore>(LogicNetworkConfigStore.ReadAllText(path));
            return store ?? new LogicNetworkStore();
        }

        private static string GetStorePath()
        {
            string savePath = SaveLoader.GetActiveSaveFilePath();
            return string.IsNullOrEmpty(savePath) ? null : savePath + Extension;
        }

        private static int GetPersistentId(LogicNetworkEmitter logic)
        {
            KPrefabID prefabId = logic != null ? logic.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private sealed class LogicNetworkStore
        {
            public int version = Version;
            public Dictionary<string, LogicNetworkEntry> entries = new Dictionary<string, LogicNetworkEntry>();
        }

        private sealed class LogicNetworkEntry
        {
            public string runtimeBlueprintJson;
            public string runtimeLayoutJson;
            public int outputModeValue;
            public long updatedAtUtcTicks;
        }
    }
}
