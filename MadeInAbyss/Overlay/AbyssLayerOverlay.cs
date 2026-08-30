using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 「深渊层阶」概览层：以颜色显示地图上深渊六层的分布，
    /// 各层颜色与其诅咒名称配色一致，按各世界实际深度的百分比划分。
    /// </summary>
    public static class AbyssLayerOverlay
    {
        public const string ModeId = "AbyssLayers";

        public static readonly HashedString Mode = new HashedString(ModeId);

        /// <summary>六层层阶颜色（与各层诅咒名称配色一致）。</summary>
        public static readonly Color[] LayerColors =
        {
            new Color32(0xC7, 0xC7, 0x4A, 0xFF),   // 第1层 阿比斯之渊
            new Color32(0xF2, 0x91, 0x3D, 0xFF),   // 第2层 诱惑之森
            new Color32(0xB0, 0x4A, 0xF2, 0xFF),   // 第3层 大断层
            new Color32(0xF4, 0x4A, 0x4A, 0xFF),   // 第4层 巨人之杯
            new Color32(0x4A, 0x9B, 0xF2, 0xFF),   // 第5层 亡骸之海
            new Color32(0xF2, 0x41, 0x8A, 0xFF),   // 第6层 最终地
        };

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        /// <summary>
        /// 概览模式实例：接入 OverlayScreen 的模式生命周期
        /// （注册后 GetMode/右键退出/ESC 退出与原版概览行为一致）。
        /// </summary>
        public class ModeInstance : OverlayModes.Mode
        {
            public override HashedString ViewMode()
            {
                return Mode;
            }

            public override string GetSoundName()
            {
                return "";
            }

            public override void Enable()
            {
                AbyssLayerOverlayLabels.SetActive(true);
            }

            public override void Disable()
            {
                AbyssLayerOverlayLabels.SetActive(false);
            }

            public override void Update()
            {
                AbyssLayerOverlayLabels.Refresh();
            }
        }

        /// <summary>在概览栏追加「深渊层阶」开关。</summary>
        [HarmonyPatch(typeof(OverlayMenu), "InitializeToggles")]
        public static class OverlayMenu_InitializeToggles_Patch
        {
            public static void Postfix(OverlayMenu __instance)
            {
                List<KIconToggleMenu.ToggleInfo> list = Traverse.Create(__instance)
                    .Field<List<KIconToggleMenu.ToggleInfo>>("overlayToggleInfos").Value;
                if (list == null)
                    return;

                // OverlayToggleInfo 是私有嵌套类，通过反射构造。
                Type toggleType = AccessTools.Inner(typeof(OverlayMenu), "OverlayToggleInfo");
                object toggle = Activator.CreateInstance(
                    toggleType,
                    "深渊层阶",
                    "overlay_temperature",
                    Mode,
                    "",                          // 无科技前置
                    global::Action.NumActions,   // 不绑定快捷键
                    "以颜色显示深渊的六个层阶分布：\n黄·阿比斯之渊 / 橙·诱惑之森 / 紫·大断层 / 红·巨人之杯 / 蓝·亡骸之海 / 品红·最终地",
                    "深渊层阶");
                list.Add((KIconToggleMenu.ToggleInfo)toggle);

                // 初始化概览层的文字标注。
                AbyssLayerOverlayLabels.Initialize();
            }
        }

        /// <summary>把自定义模式注册进 OverlayScreen 的模式表。</summary>
        [HarmonyPatch(typeof(OverlayScreen), "RegisterModes")]
        public static class OverlayScreen_RegisterModes_Patch
        {
            public static void Postfix(OverlayScreen __instance)
            {
                AccessTools.Method(typeof(OverlayScreen), "RegisterMode")
                    .Invoke(__instance, new object[] { new ModeInstance() });
            }
        }

        /// <summary>按格子所属世界与营地表锚点计算深度，返回层阶颜色。</summary>
        private static Color LayerCellColour(SimDebugView view, int cell)
        {
            if (!Grid.IsValidCell(cell))
                return Clear;

            int worldId = Grid.WorldIdx[cell];
            float surfaceY = AbyssAnchors.GetSurfaceY(worldId);
            if (float.IsNaN(surfaceY))
                return Clear;

            float[] thresholds = AbyssStatics.GetLayerThresholds(worldId);
            if (thresholds == null || thresholds.Length == 0)
                return Clear;

            Grid.CellToXY(cell, out int x, out int y);
            float depth = surfaceY - y;

            if (depth < thresholds[0])
                return Clear;

            int layer = AbyssStatics.GetLayerIndex(depth, thresholds);
            if (layer < 0 || layer >= LayerColors.Length)
                return Clear;

            Color color = LayerColors[layer];
            color.a = 0.45f;
            return color;
        }
    }
}
