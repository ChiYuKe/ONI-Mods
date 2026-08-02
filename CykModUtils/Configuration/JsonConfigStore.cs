using System;
using System.IO;
using CykModUtils.Core;
using CykModUtils.IO;
using Newtonsoft.Json;

namespace CykModUtils.Configuration
{
    /// <summary>
    /// JSON 配置的加载、默认值、规范化和原子保存工具。
    /// </summary>
    public static class JsonConfigStore
    {
        /// <summary>
        /// 加载配置；文件不存在或内容无效时创建默认对象。
        /// </summary>
        public static T LoadOrCreate<T>(
            string path,
            Func<T> createDefault,
            Action<T> normalize = null,
            bool saveWhenMissing = true,
            JsonSerializerSettings settings = null,
            ModLogger logger = null)
            where T : class
        {
            if (createDefault == null)
            {
                throw new ArgumentNullException(nameof(createDefault));
            }

            bool exists = !string.IsNullOrWhiteSpace(path) && File.Exists(path);
            T config = null;
            if (exists)
            {
                try
                {
                    string json = File.ReadAllText(path);
                    config = JsonConvert.DeserializeObject<T>(json, settings);
                }
                catch (Exception ex)
                {
                    logger?.Exception(ex, "Failed to load JSON config: " + path);
                }
            }

            config = config ?? createDefault();
            normalize?.Invoke(config);

            if (!exists && saveWhenMissing)
            {
                Save(path, config, normalize: null, settings: settings, logger: logger);
            }

            return config;
        }

        /// <summary>
        /// 使用无参数构造函数创建默认配置。
        /// </summary>
        public static T LoadOrCreate<T>(
            string path,
            Action<T> normalize = null,
            bool saveWhenMissing = true,
            JsonSerializerSettings settings = null,
            ModLogger logger = null)
            where T : class, new()
        {
            return LoadOrCreate(
                path,
                () => new T(),
                normalize,
                saveWhenMissing,
                settings,
                logger);
        }

        /// <summary>
        /// 规范化后以缩进格式原子保存配置。
        /// </summary>
        /// <returns>保存成功时返回 true。</returns>
        public static bool Save<T>(
            string path,
            T config,
            Action<T> normalize = null,
            Formatting formatting = Formatting.Indented,
            JsonSerializerSettings settings = null,
            ModLogger logger = null)
            where T : class
        {
            if (config == null)
            {
                logger?.Warning("Cannot save a null JSON config: " + path);
                return false;
            }

            try
            {
                normalize?.Invoke(config);
                string json = JsonConvert.SerializeObject(config, formatting, settings);
                AtomicFileUtility.WriteAllTextAtomically(path, json);
                return true;
            }
            catch (Exception ex)
            {
                logger?.Exception(ex, "Failed to save JSON config: " + path);
                return false;
            }
        }
    }
}
