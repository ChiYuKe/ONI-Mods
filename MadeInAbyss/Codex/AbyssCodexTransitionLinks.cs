using HarmonyLib;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 元素数据库页相变行的点击跳转：
    /// 原版相变面板按钮只跳到“来源元素”（即当前页面自身，等于原地打转），
    /// 这里为涉及祈愿培养基/祈愿雾气的“加热相变”行补挂第二个处理器，
    /// 点击后跳转到相变产物（祈愿雾气）对应的数据库页面。
    /// </summary>
    public static class AbyssCodexTransitionLinks
    {
        [HarmonyPatch(typeof(CodexTemperatureTransitionPanel), "ConfigureSource")]
        public static class CodexTemperatureTransitionPanel_ConfigureSource_Patch
        {
            public static void Postfix(CodexTemperatureTransitionPanel __instance)
            {
                Element source = Traverse.Create(__instance).Field<Element>("sourceElement").Value;
                if (source == null)
                    return;

                // 仅处理涉及本 mod 元素的加热相变行。
                if (source.id != AbyssElements.TalismanCondensate && source.id != AbyssElements.WishingVapor)
                    return;

                CodexTemperatureTransitionPanel.TransitionType type = Traverse.Create(__instance)
                    .Field<CodexTemperatureTransitionPanel.TransitionType>("transitionType").Value;
                if (type != CodexTemperatureTransitionPanel.TransitionType.HEAT)
                    return;

                Element target = source.highTempTransition;
                if (target == null)
                    return;

                GameObject sourceContainer = Traverse.Create(__instance).Field<GameObject>("sourceContainer").Value;
                if (sourceContainer == null || sourceContainer.transform.childCount == 0)
                    return;

                Transform row = sourceContainer.transform.GetChild(sourceContainer.transform.childCount - 1);
                HierarchyReferences references = row.GetComponent<HierarchyReferences>();
                KButton button = references != null ? references.GetReference<KButton>("Button") : null;
                if (button == null)
                    return;

                string targetId = target.id.ToString();
                button.onClick += delegate
                {
                    ManagementMenu.Instance.codexScreen.ChangeArticle(targetId, false, default(Vector3), CodexScreen.HistoryDirection.NewArticle);
                };
            }
        }
    }
}
