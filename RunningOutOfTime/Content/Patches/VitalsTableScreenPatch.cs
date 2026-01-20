using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using RunningOutOfTime.Content.Components;


namespace RunningOutOfTime.Content.Patches
{
    internal class VitalsTableScreenPatch
    {
        public static float MaxMinionAge = Config.TUNINGS.AGE.MINION_AGE_THRESHOLD;
        public static float Age80PercentThreshold = MaxMinionAge * Config.TUNINGS.AGE.AGE_80PERCENT_THRESHOLD;

        [HarmonyPatch(typeof(VitalsTableScreen), "OnActivate")]
        public static class OnActivatePatch
        {
            private static MethodInfo _getWidgetRow;
            private static MethodInfo _getWidgetColumn;
            private static bool _initialized = false;

            private static void EnsureReflectionInitialized(VitalsTableScreen instance)
            {
                if (_initialized) return;

                _getWidgetRow = typeof(TableScreen).GetMethod("GetWidgetRow",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException("未找到 GetWidgetRow 方法");

                _getWidgetColumn = typeof(TableScreen).GetMethod("GetWidgetColumn",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingMethodException("未找到 GetWidgetColumn 方法");

                _initialized = true;
            }

            private static float SecondsToCycles(float seconds) => seconds / 600f;

            private static string FormatAge(float seconds) => $"{SecondsToCycles(seconds):F1}/{MaxMinionAge:F0}";

            public static void Postfix(VitalsTableScreen __instance)
            {
                try
                {
                    EnsureReflectionInitialized(__instance);

                    var addLabelColumn = typeof(TableScreen).GetMethod(
                        "AddLabelColumn",
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null,
                        new Type[]
                        {
                            typeof(string),
                            typeof(Action<IAssignableIdentity, GameObject>),
                            typeof(Func<IAssignableIdentity, GameObject, string>),
                            typeof(Comparison<IAssignableIdentity>),
                            typeof(Action<IAssignableIdentity, GameObject, ToolTip>),
                            typeof(Action<IAssignableIdentity, GameObject, ToolTip>),
                            typeof(int),
                            typeof(bool)
                        },
                        null) ?? throw new MissingMethodException("未找到 AddLabelColumn 方法");

                    addLabelColumn.Invoke(__instance, new object[]
                    {
                        "年龄",
                        new Action<IAssignableIdentity, GameObject>((identity, go) => LoadAge(__instance, identity, go)),
                        new Func<IAssignableIdentity, GameObject, string>(GetAgeLabel),
                        new Comparison<IAssignableIdentity>(CompareByAge),
                        new Action<IAssignableIdentity, GameObject, ToolTip>(ShowTooltipAge),
                        new Action<IAssignableIdentity, GameObject, ToolTip>(ShowTooltipSortAge),
                        70,
                        true
                    });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"初始化年龄列失败: {ex}");
                }
            }

            private static void LoadAge(VitalsTableScreen screen, IAssignableIdentity identity, GameObject widget)
            {
                try
                {
                    var row = _getWidgetRow.Invoke(screen, new object[] { widget }) as TableRow;
                    var label = widget.GetComponentInChildren<LocText>(true);

                    if (identity is MinionIdentity minion)
                    {
                        var column = _getWidgetColumn.Invoke(screen, new object[] { widget }) as LabelTableColumn;
                        label.text = column?.get_value_action?.Invoke(minion, widget) ?? "年龄";
                    }
                    else
                    {
                        label.text = row?.isDefault == true ? "" : "年龄";
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载年龄失败: {ex}");
                }
            }

            private static string GetAgeLabel(IAssignableIdentity identity, GameObject widget)
            {
                if (identity is MinionIdentity minion)
                {
                    // 如果是仿生复制人，直接返回 "不适用"
                    var prefabID = minion.GetComponent<KPrefabID>();
                    if (prefabID.HasTag(GameTags.Minions.Models.Bionic))
                        return "不适用";

                    float ageSeconds = AgingMonitor.GetCurrentAge(minion.gameObject,AgingMonitor.AgeUnit.Seconds);
                    return ageSeconds >= 0 ? FormatAge(ageSeconds) : "不适用";
                }
                return "--";
            }

            private static int CompareByAge(IAssignableIdentity a, IAssignableIdentity b)
            {
                return GetAgeValue(a).CompareTo(GetAgeValue(b));
            }

            private static float GetAgeValue(IAssignableIdentity identity)
            {
                if (identity is MinionIdentity minion)
                {
                    float ageSeconds = AgingMonitor.GetCurrentAge(minion.gameObject, AgingMonitor.AgeUnit.Seconds);
                    if (ageSeconds >= 0) return SecondsToCycles(ageSeconds);
                }
                return -1f;
            }

            private static void ShowTooltipAge(IAssignableIdentity identity, GameObject widget, ToolTip tooltip)
            {
                if (identity is MinionIdentity minion)
                {
                    var prefabID = minion.GetComponent<KPrefabID>();
                    // 如果是仿生复制人，不显示 Tooltip
                    if (prefabID.HasTag(GameTags.Minions.Models.Bionic))
                        return;

                    float ageSeconds = AgingMonitor.GetCurrentAge(minion.gameObject, AgingMonitor.AgeUnit.Seconds);
                    if (ageSeconds >= 0)
                    {
                        tooltip.SetSimpleTooltip(
                            $"当前年龄: {SecondsToCycles(ageSeconds):F1} 周期\n" +
                            $"最大年龄: {MaxMinionAge:F0} 周期\n" +
                            $"衰老年龄{Age80PercentThreshold}"
                        );
                        return;
                    }
                }
                tooltip.SetSimpleTooltip("年龄数据不可用");
            }

            private static void ShowTooltipSortAge(IAssignableIdentity identity, GameObject widget, ToolTip tooltip)
            {
                tooltip.SetSimpleTooltip("按当前年龄排序（周期单位）");
            }
        }
    }
}
