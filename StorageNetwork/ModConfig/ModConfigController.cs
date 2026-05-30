using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace ModConfig
{
    public sealed class ModConfigController<T> where T : class, new()
    {
        private readonly string configPath;
        private readonly string logPrefix;
        private readonly Action<T> normalize;
        private T instance;

        public ModConfigController(
            string configPath,
            string logPrefix,
            Action<T> normalize = null)
        {
            this.configPath = configPath;
            this.logPrefix = logPrefix;
            this.normalize = normalize;
        }

        public T Instance
        {
            get
            {
                if (instance == null)
                {
                    Load();
                }

                return instance;
            }
        }

        public void Load()
        {
            instance = JsonConfigStore.Load(configPath, () => new T(), normalize, logPrefix);
        }

        public void Save()
        {
            JsonConfigStore.Save(configPath, Instance, normalize, logPrefix);
        }

        public void RegisterOptionsButton(string modTitlePrefix, string buttonName, string tooltip, string dialogTitle, string hint = null)
        {
            ModsScreenOptionsButton.Register(new ModsScreenOptionsButtonDefinition
            {
                ModTitlePrefix = modTitlePrefix,
                ButtonName = buttonName,
                ButtonText = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OPTIONS_BUTTON),
                Tooltip = tooltip,
                OnClick = () => ShowDialog(dialogTitle, hint)
            });
        }

        public void ShowDialog(string title, string hint = null)
        {
            T config = Instance;
            ModConfigDialogDefinition dialog = new ModConfigDialogDefinition
            {
                OverlayName = logPrefix + "ConfigOverlay",
                Title = title,
                Hint = hint,
                Save = Save
            };

            List<OptionBinding> bindings = BuildBindings(config);
            foreach (OptionBinding binding in bindings)
            {
                dialog.Fields.Add(new ModConfigField
                {
                    Label = TranslateOptionText(binding.Option.Label),
                    Description = TranslateOptionText(binding.Option.Description),
                    Value = binding.GetValue(config),
                    Min = binding.Option.Min,
                    Max = binding.Option.Max,
                    Integer = binding.Option.Integer || binding.Property.PropertyType == typeof(int),
                    Apply = value => binding.SetValue(config, value)
                });
            }

            dialog.Reset = inputs =>
            {
                T defaults = new T();
                normalize?.Invoke(defaults);
                for (int i = 0; i < inputs.Count && i < bindings.Count; i++)
                {
                    ModConfigDialog.SetInput(inputs[i], bindings[i].GetValue(defaults));
                }
            };

            ModConfigDialog.Show(dialog);
        }

        private static string TranslateOptionText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text == "场景储存箱容量 kg") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_SCENE_STORAGE_CAPACITY);
            if (text == "专用场景储存箱的容量。新建建筑生效。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_SCENE_STORAGE_CAPACITY_DESC);
            if (text == "场景扫描缓存秒数") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_SCENE_SCAN_CACHE);
            if (text == "数值越小刷新越快，但遍历储存建筑更频繁。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_SCENE_SCAN_CACHE_DESC);
            if (text == "材料请求默认限额 kg") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_DEFAULT_MATERIAL_LIMIT);
            if (text == "新接入生产建筑的默认请求限额。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_DEFAULT_MATERIAL_LIMIT_DESC);
            if (text == "请求成功冷却秒数") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_SUCCESS_COOLDOWN);
            if (text == "材料已满足或达到限额后的检查间隔。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_SUCCESS_COOLDOWN_DESC);
            if (text == "请求失败重试秒数") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_RETRY_COOLDOWN);
            if (text == "缺料或没有可请求配方后的重试间隔。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_REQUEST_RETRY_COOLDOWN_DESC);
            if (text == "无限队列请求批次数") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_INFINITE_QUEUE_BATCHES);
            if (text == "生产队列为无限时，一次按多少批材料请求。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_INFINITE_QUEUE_BATCHES_DESC);
            if (text == "最大请求批次数") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_MAX_REQUEST_BATCHES);
            if (text == "单次材料请求最多按多少批计算。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_MAX_REQUEST_BATCHES_DESC);
            if (text == "生产计划递归深度") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_PLAN_RECURSION_DEPTH);
            if (text == "补产链路向下追踪的最大层数。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_PLAN_RECURSION_DEPTH_DESC);
            if (text == "异常订单超时周期") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_ABNORMAL_TIMEOUT);
            if (text == "订单多长时间无进度后自动取消排产。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_ABNORMAL_TIMEOUT_DESC);
            if (text == "完成订单保留周期") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_COMPLETED_RETENTION);
            if (text == "完成/取消/异常订单在列表中保留多久。") return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIG_COMPLETED_RETENTION_DESC);
            return text;
        }

        private static List<OptionBinding> BuildBindings(T config)
        {
            List<OptionBinding> bindings = new List<OptionBinding>();
            foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                ModConfigOptionAttribute option = property.GetCustomAttribute<ModConfigOptionAttribute>();
                if (option == null || !property.CanRead || !property.CanWrite || !IsSupportedNumber(property.PropertyType))
                {
                    continue;
                }

                bindings.Add(new OptionBinding(property, option));
            }

            return bindings;
        }

        private static bool IsSupportedNumber(Type type)
        {
            return type == typeof(float) ||
                   type == typeof(double) ||
                   type == typeof(int);
        }

        private sealed class OptionBinding
        {
            public OptionBinding(PropertyInfo property, ModConfigOptionAttribute option)
            {
                Property = property;
                Option = option;
            }

            public PropertyInfo Property { get; }
            public ModConfigOptionAttribute Option { get; }

            public float GetValue(T config)
            {
                object value = Property.GetValue(config, null);
                if (value is int intValue)
                {
                    return intValue;
                }

                if (value is double doubleValue)
                {
                    return (float)doubleValue;
                }

                return value is float floatValue ? floatValue : 0f;
            }

            public void SetValue(T config, float value)
            {
                float clamped = Clamp(value, Option.Min, Option.Max);
                if (Option.Integer || Property.PropertyType == typeof(int))
                {
                    Property.SetValue(config, Mathf.RoundToInt(clamped), null);
                }
                else if (Property.PropertyType == typeof(double))
                {
                    Property.SetValue(config, (double)clamped, null);
                }
                else
                {
                    Property.SetValue(config, clamped, null);
                }
            }

            private static float Clamp(float value, float min, float max)
            {
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return min;
                }

                return Mathf.Clamp(value, min, max);
            }
        }
    }
}
