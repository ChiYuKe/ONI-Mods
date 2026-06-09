using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static STRINGS.DUPLICANTS.MODIFIERS;
using UnityEngine.UI;
using UnityEngine;

namespace RunningOutOfTime.Content
{
    public class HoverInfoDrawer
    {

        private static bool isHoverInfoVisible;
        private static int hoverCell;
        private static TextStyleSetting hoverTextStyle;
        private static Sprite hoverIcon;

        [HarmonyPatch(typeof(SelectToolHoverTextCard), "UpdateHoverElements")]
        public class HoverInfoFetcher
        {
            public static bool Prefix(ref SelectToolHoverTextCard __instance)
            {
                // 如果 hoverTextStyle 和 hoverIcon 还没有被初始化，就进行初始化
                if (hoverTextStyle == null)
                {
                    hoverTextStyle = __instance.Styles_BodyText.Standard;
                    hoverIcon = __instance.iconDash;
                }

                // 获取当前鼠标位置对应的格子
                hoverCell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.GetMousePos()));

                // 判断当前格子是否可见且属于当前活跃世界
                isHoverInfoVisible = Grid.IsVisible(hoverCell) && (int)Grid.WorldIdx[hoverCell] == ClusterManager.Instance.activeWorldId;

                return true; 
            }
        }

        [HarmonyPatch(typeof(HoverTextDrawer), "EndDrawing")]
        public class HoverInfoDrawer_AddText
        {
            public static void Prefix(ref HoverTextDrawer __instance)
            {
                bool flag = !isHoverInfoVisible;  
                if (!flag)
                {
                    isHoverInfoVisible = false; 
                    __instance.BeginShadowBar(false);
                    __instance.DrawIcon(HoverInfoDrawer.hoverIcon, 18);
                    __instance.DrawText(string.Format("Cell:{0}, X = {1}  Y = {2}", hoverCell, hoverCell % Grid.WidthInCells, hoverCell / Grid.WidthInCells), hoverTextStyle);

                    bool isValidCell = Grid.IsValidCell(hoverCell);  
                    if (false)
                    {
                        __instance.NewLine(18);
                        __instance.DrawIcon(hoverIcon, 18);  
                        float num = 5;
                        bool isLowResistance = num < 10f;  // 变量名改为更具描述性
                        if (isLowResistance)
                        {
                            __instance.DrawText(string.Format("S_Text.HOVER_BAR.ELEMENT", 5, num), hoverTextStyle);  // hoverTextStyle
                        }
                        else
                        {
                            __instance.DrawText(string.Format("S_Text.HOVER_BAR.ELEMENT", 5, "S_Text.POWER_RESISTANCE.RESIST_DESC0.text"), hoverTextStyle);  // hoverTextStyle
                        }

                        bool isInited = false;
                        if (isInited)
                        {
                            __instance.NewLine(18);
                            __instance.DrawIcon(HoverInfoDrawer.hoverIcon, 18);  // hoverIcon
                            __instance.DrawText(string.Format("S_Text.HOVER_BAR.VISUAL", 5), hoverTextStyle);  // hoverTextStyle
                        }

                        string text = "";
                        int surfaceStatus = 1;  // 变量名改为更具描述性
                        bool isSurface = surfaceStatus == 1;
                        if (isSurface)
                        {
                            text = text + "S_Text.HOVER_BAR.SURFACE" + " ";
                        }
                        else
                        {
                            bool isVapor = surfaceStatus == 2;
                            if (isVapor)
                            {
                                text = text + "S_Text.HOVER_BAR.VAPOR" + " ";
                            }
                        }

                        bool isCrash = false;
                        if (isCrash)
                        {
                            text = text + "S_Text.HOVER_BAR.CRASH" + " ";
                        }

                        bool isChaos = false;
                        if (isChaos)
                        {
                            text = text + "S_Text.HOVER_BAR.CHAOS" + " ";
                        }

                        bool hasAdditionalInfo = text != "";
                        if (hasAdditionalInfo)
                        {
                            __instance.NewLine(18);
                            __instance.DrawIcon(hoverIcon, 18);  // hoverIcon
                            __instance.DrawText(text, hoverTextStyle);  // hoverTextStyle
                        }

                        
                    }
                    __instance.EndShadowBar();
                }
            }
        }

    }

}
