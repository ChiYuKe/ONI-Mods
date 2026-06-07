using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace StorageNetwork.ModConfig
{
    public static class JsonConfigStore
    {
        public static T Load<T>(string path, Func<T> createDefault, Action<T> normalize, string logPrefix) where T : class
        {
            T loaded = null;
            try
            {
                if (File.Exists(path))
                {
                    loaded = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("[{0}] Failed to load config: {1}", logPrefix, ex.Message));
            }

            T config = loaded ?? createDefault();
            normalize?.Invoke(config);
            return config;
        }

        public static void Save<T>(string path, T config, Action<T> normalize, string logPrefix) where T : class
        {
            try
            {
                normalize?.Invoke(config);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning(string.Format("[{0}] Failed to save config: {1}", logPrefix, ex.Message));
            }
        }
    }
}
