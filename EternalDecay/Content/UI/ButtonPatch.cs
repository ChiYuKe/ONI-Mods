using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using CykUtils;

public class Patches
{
    [HarmonyPatch(typeof(MeterScreen), "OnSpawn")]
    public static class MeterScreen_OnSpawn_Patch
    {
        public static void Postfix(MeterScreen __instance)
        {
            // 1. 获取注入位置 (RedAlertButton 所在的容器)
            var redAlertButton = Traverse.Create(__instance).Field("RedAlertButton").GetValue<MultiToggle>();
            if (redAlertButton == null) return;

            GameObject parentObj = redAlertButton.transform.parent.gameObject;

            // 2. 防止重复创建按钮 (存档加载或 UI 重启时)
            string buttonName = "MyInheritanceInfoButton";
            if (parentObj.transform.Find(buttonName) != null) return;

            // 3. 实例化按钮
            GameObject prefab = ABHelper.ButtonPrefab; // 确保你的 ABHelper 已经加载了该资源
            if (prefab == null) return;

            GameObject myButtonGO = Util.KInstantiateUI(prefab, parentObj, true);
            myButtonGO.name = buttonName;

            // 4. 重置 UI 变换 (必须先重置，由布局组件接管)
            Util.Reset(myButtonGO.rectTransform());

            // 5. 配置文字 (TextMeshProUGUI 方案)
            SetupButtonText(myButtonGO, __instance);

            // 6. 配置 MultiToggle (确保点击响应)
            SetupMultiToggle(myButtonGO);

            // 7. 配置 ToolTip (鼠标悬停提示)
            SetupToolTip(myButtonGO);

            // 8. 确保根对象有 Image 组件以接收点击射线
            var rootImage = myButtonGO.FindOrAddUnityComponent<Image>();
            if (rootImage.sprite == null)
            {
                // 如果根对象没图，设为全透明但开启 RaycastTarget
                rootImage.color = new Color(0, 0, 0, 0);
            }
            rootImage.raycastTarget = true;
        }

        private static void SetupButtonText(GameObject buttonGO, MeterScreen meterScreen)
        {
            // 按照你的层级：TestButton -> TextContainer -> Text
            Transform textNode = Util.FindTransformRecursive(buttonGO.transform, "Text");
            if (textNode == null) return;

            // 添加必要的渲染组件
            textNode.gameObject.FindOrAddUnityComponent<CanvasRenderer>();
            TextMeshProUGUI tmp = textNode.gameObject.FindOrAddUnityComponent<TextMeshProUGUI>();

            // 基本设置
            tmp.text = "继承";
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // 文字不遮挡鼠标点击

            // 同步游戏原生字体，防止乱码或方块
            LocText referenceText = meterScreen.GetComponentInChildren<LocText>();
            if (referenceText != null)
            {
                tmp.font = referenceText.font;
                // 注意：在某些版本中直接赋值 fontAsset 
                // tmp.fontSharedMaterial = referenceText.fontSharedMaterial;
            }

            // 确保 Text 节点的 RectTransform 有足够的大小
            RectTransform textRT = textNode.rectTransform();
            textRT.sizeDelta = new Vector2(40f, 20f);
        }

        private static void SetupMultiToggle(GameObject buttonGO)
        {
            MultiToggle myMultiToggle = buttonGO.FindOrAddUnityComponent<MultiToggle>();

            // 核心修复：必须定义状态列表，否则点击逻辑不会触发
            if (myMultiToggle.states == null || myMultiToggle.states.Length == 0)
            {
                myMultiToggle.states = new ToggleState[1]
                {
                    new ToggleState { Name = "Default" }
                };
            }

            // 绑定点击音效
            myMultiToggle.play_sound_on_click = true;
           

            // 绑定点击逻辑
            myMultiToggle.onClick += () =>
            {
                KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click"));
                Debug.Log("[MyMod] 继承信息按钮被点击！");
                // 在这里调用你的界面弹出逻辑
                // OpenMyInfoPanel();
            };
        }

        private static void SetupToolTip(GameObject buttonGO)
        {
            ToolTip toolTip = buttonGO.FindOrAddUnityComponent<ToolTip>();
            toolTip.SetSimpleTooltip("查看复制人的基因继承信息");

            // 强制刷新，确保提示能够显示
            toolTip.OnToolTip = () => "查看复制人的基因继承信息";
        }
    }
}