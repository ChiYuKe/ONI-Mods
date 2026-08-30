using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

            /// <summary>图例面板：颜色 → 层阶名称（叠加层激活时右上角显示）。</summary>
            public override List<LegendEntry> GetCustomLegendData()
            {
                var entries = new List<LegendEntry>(AbyssStatics.Layers.Length);
                for (int i = 0; i < LayerColors.Length && i < AbyssStatics.Layers.Length; i++)
                {
                    string name = Strings.Get($"STRINGS.OVERLAYS.ABYSS_LAYERS.LAYER{i + 1}.NAME");
                    string tooltip = $"第 {i + 1} 层 · {AbyssStatics.Layers[i].Name}";
                    // sprite 传 null → 使用 Assets.instance.LegendColourBox 色块；
                    // displaySprite=true 显示该颜色的色块，desc 作为悬停提示。
                    entries.Add(new LegendEntry(name, tooltip, LayerColors[i]));
                }
                return entries;
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
                KIconToggleMenu.ToggleInfo toggleInfo = (KIconToggleMenu.ToggleInfo)toggle;
                // 使用 mod 自带图标：getSpriteCB 优先于 icon 名查表，
                // 加载失败时回退到占位图标（overlay_temperature）。
                toggleInfo.getSpriteCB = LoadOverlayIcon;
                list.Add(toggleInfo);

                // 初始化概览层的文字标注。
                AbyssLayerOverlayLabels.Initialize();
            }
        }

        /// <summary>
        /// 加载 mod 自带的概览层图标（Assets/Sprite/madeInabyss_icon.png）。
        /// 失败时回退到原版占位图标，避免概览栏出现空白。
        /// </summary>
        private static Sprite LoadOverlayIcon()
        {
            try
            {
                string modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string iconPath = Path.Combine(modDir, "Assets", "Sprite", "madeInabyss_icon.png");
                if (!File.Exists(iconPath))
                {
                    Debug.LogWarning($"[MadeInAbyss] 未找到概览图标：{iconPath}，使用占位图标");
                    return Assets.GetSprite("overlay_temperature");
                }

                byte[] bytes = File.ReadAllBytes(iconPath);
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(bytes))
                {
                    Debug.LogWarning("[MadeInAbyss] 概览图标解析失败，使用占位图标");
                    return Assets.GetSprite("overlay_temperature");
                }

                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MadeInAbyss] 加载概览图标异常，使用占位图标：{ex.Message}");
                return Assets.GetSprite("overlay_temperature");
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

        /// <summary>注册逐格取色函数（仅在本概览层激活时被调用）。</summary>
        /// <remarks>
        /// 色块渲染与模式表注册是两条独立链路：
        /// OverlayScreen 负责模式切换/退出，SimDebugView 则每帧用当前 mode
        /// 在 getColourFuncs 里查取色函数，查不到就整幅图渲染成黑色。
        /// 若不注册，开启「深渊层阶」后只有标签、没有色块。
        /// </remarks>
        [HarmonyPatch(typeof(SimDebugView), "OnPrefabInit")]
        public static class SimDebugView_OnPrefabInit_Patch
        {
            public static void Postfix(SimDebugView __instance)
            {
                Dictionary<HashedString, Func<SimDebugView, int, Color>> funcs = Traverse.Create(__instance)
                    .Field<Dictionary<HashedString, Func<SimDebugView, int, Color>>>("getColourFuncs").Value;
                if (funcs == null)
                    return;
                funcs[Mode] = LayerCellColour;
            }
        }

        /// <summary>
        /// 把「深渊层阶」的图例条目注册进 OverlayLegend。
        /// SetLegend 按 mode 在 overlayInfoList 里查找 OverlayInfo，
        /// 查不到就会 ClearLegend（面板空白）；isProgrammaticallyPopulated=true
        /// 会让面板改由 Mode.GetCustomLegendData() 动态生成。
        /// </summary>
        [HarmonyPatch(typeof(OverlayLegend), "OnSpawn")]
        public static class OverlayLegend_OnSpawn_Patch
        {
            public static void Postfix(OverlayLegend __instance)
            {
                List<OverlayLegend.OverlayInfo> list = Traverse.Create(__instance)
                    .Field<List<OverlayLegend.OverlayInfo>>("overlayInfoList").Value;
                if (list == null)
                    return;
                if (list.Exists(info => info != null && info.mode == Mode))
                    return;

                list.Add(new OverlayLegend.OverlayInfo
                {
                    name = "深渊层阶",
                    mode = Mode,
                    infoUnits = null,
                    diagrams = null,
                    isProgrammaticallyPopulated = true,
                });
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
