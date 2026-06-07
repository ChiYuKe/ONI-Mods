using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StorageNetwork.ModConfig
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
                ButtonText = "选项",
                Tooltip = StableText(tooltip, "调整模组数值"),
                OnClick = () => ShowDialog(
                    StableText(dialogTitle, "模组配置"),
                    StableText(hint, "保存后会写入配置文件。部分数值需要重进存档或重启游戏才会完全体现。"))
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
                ModConfigField field = new ModConfigField
                {
                    Label = binding.Option.Label,
                    Description = binding.Option.Description,
                    IsBoolean = binding.IsBoolean
                };

                if (binding.IsBoolean)
                {
                    field.BoolValue = binding.GetBoolValue(config);
                    field.ApplyBool = value => binding.SetBoolValue(config, value);
                }
                else
                {
                    field.Value = binding.GetNumberValue(config);
                    field.Min = binding.Option.Min;
                    field.Max = binding.Option.Max;
                    field.Integer = binding.Option.Integer || binding.Property.PropertyType == typeof(int);
                    field.Apply = value => binding.SetNumberValue(config, value);
                }

                dialog.Fields.Add(field);
            }

            dialog.Reset = inputs =>
            {
                T defaults = new T();
                normalize?.Invoke(defaults);
                for (int i = 0; i < inputs.Count && i < bindings.Count; i++)
                {
                    if (bindings[i].IsBoolean)
                    {
                        inputs[i].SetBool(bindings[i].GetBoolValue(defaults));
                    }
                    else
                    {
                        inputs[i].SetNumber(bindings[i].GetNumberValue(defaults));
                    }
                }
            };

            ModConfigDialog.Show(dialog);
        }

        private static string StableText(string value, string fallback)
        {
            return string.IsNullOrEmpty(value) || value.StartsWith("MISSING", StringComparison.OrdinalIgnoreCase)
                ? fallback
                : value;
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
                   type == typeof(int) ||
                   type == typeof(bool);
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
            public bool IsBoolean
            {
                get { return Property.PropertyType == typeof(bool); }
            }

            public bool GetBoolValue(T config)
            {
                object value = Property.GetValue(config, null);
                return value is bool boolValue && boolValue;
            }

            public void SetBoolValue(T config, bool value)
            {
                Property.SetValue(config, value, null);
            }

            public float GetNumberValue(T config)
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

            public void SetNumberValue(T config, float value)
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
