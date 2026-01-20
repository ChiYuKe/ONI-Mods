using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CykUtils;
using Database;
using FuzzySharp;
using HarmonyLib;
using Klei.AI;
using RunningOutOfTime.Content.Components;
using STRINGS;
using UnityEngine;
using UnityEngine.UI;

namespace RunningOutOfTime.Content.Patches
{
    public class DetailTabHeaderPatch
    {
        [HarmonyPatch(typeof(DetailTabHeader), "Init")]
        public static class DetailTabHeader_Init_Patch
        {
            // Postfix 方法会在原始方法执行后运行
            public static void Postfix(DetailTabHeader __instance)
            {
                LogUtil.Log("DetailTabHeader_Init_Patch: Postfix called");

                var makeTabMethod = typeof(DetailTabHeader).GetMethod("MakeTab", BindingFlags.NonPublic | BindingFlags.Instance);

                if (makeTabMethod != null)
                {
                   //  LogUtil.Log("MakeTab method found");

                    // 创建一个新的 GameObject
                    var minionAgeTabObject = new GameObject("MinionAgeTabObject");

                    // 将 MySimpleInfoScreen 组件附加到 GameObject 上
                    var mySimpleInfoScreen = minionAgeTabObject.AddComponent<MinionBrainCoreInfoScreen>();

                    // 调用 MakeTab 方法
                    makeTabMethod.Invoke(__instance, new object[] { "MINION_AGE_TAB", "知识遗产", Assets.GetSprite("icon_display_screen_properties"), "查看当前的知识遗产", minionAgeTabObject });
                    // 找到新生成的 tab
                    var tabTransform = __instance.transform.Find("tab: MINION_AGE_TAB");
                    if (tabTransform != null)
                    {
                        // 移动到第一个
                        tabTransform.SetSiblingIndex(0);
                        // LogUtil.Log("Moved MINION_AGE_TAB to first position");
                    }

                }
                else
                {
                    LogUtil.LogError("MakeTab method not found!");
                }
            }
        }



    }


    public class MinionBrainCoreInfoScreen : DetailScreenTab
    {
        public static bool IsDefault = false;

        public override bool IsValidForTarget(GameObject target)
        {
            if (target == null)
                return false;

            // 判断是否已经有 KMinionBrain 组件
            if (target.GetComponent<Items.MinionBrain>() != null)
            {
                return true;
            }

            return false;
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            // 所有UI对象必须包含
            gameObject.AddComponent<RectTransform>();

            // 需要自动布局时添加
            gameObject.AddOrGet<ContentSizeFitter>();
            var layout = gameObject.AddOrGet<VerticalLayoutGroup>();
            layout.spacing = 10f;          // 子对象间距10像素
            layout.padding = new RectOffset(5, 5, 5, 5); // 上下左右边距5像素
            layout.childAlignment = TextAnchor.UpperCenter; // 子对象顶部居中


            gameObject.AddOrGet<LayoutElement>();
            gameObject.AddOrGet<CanvasRenderer>();
            gameObject.AddOrGet<Image>();



            // 创建栏目
            newTraitsPanel = CreateCollapsableSection(Config.STRINGS.UI.INFOSCREEN.TRAITS.NAME);// "个性特征"
            newResumePanel = CreateCollapsableSection(Config.STRINGS.UI.INFOSCREEN.RESUME.NAME); // "简历"
            newAttributesPanel = CreateCollapsableSection(Config.STRINGS.UI.INFOSCREEN.ATTRIBUTES.NAME); // "属性"
            D = CreateCollapsableSection("测试栏目");

        }







        protected override void OnSelectTarget(GameObject target)
        {



        }

        public override void OnDeselectTarget(GameObject target) { }

        private bool customButtonAdded = false;
        protected override void Refresh(bool force = false)
        {
            MinionBrainCoreInfoScreen.NewTraitsRefreshInfoPanel(this.newTraitsPanel, this.selectedTarget);
            MinionBrainCoreInfoScreen.NewRefreshResumePanel(this.newResumePanel, this.selectedTarget);
            MinionBrainCoreInfoScreen.NewAttributesPanel(this.newAttributesPanel, this.selectedTarget);
            // MinionBrainCoreInfoScreen.RefreshD(this.D, this.selectedTarget);

           
        }


        private static void NewTraitsRefreshInfoPanel(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            if (targetEntity == null)
            {
                return;
            }
            if (targetPanel == null)
            {
                return;
            }

            KPrefabID kprefabid = targetEntity.GetComponent<KPrefabID>();
            if (kprefabid == null)
            {
                targetPanel.SetActive(false);
                return;
            }


            targetPanel.SetActive(true);
            foreach (Trait trait in targetEntity.GetComponent<Traits>().TraitList)
            {
                if (!string.IsNullOrEmpty(trait.Name))
                {
                    targetPanel.SetLabel(trait.Id, trait.Name, trait.GetTooltip());
                }
            }
            targetPanel.Commit();
        }

        private static void NewRefreshResumePanel(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            if (targetEntity == null)
            {
                return;
            }
            if (targetPanel == null)
            {
                return;
            }

            KPrefabID kprefabid = targetEntity.GetComponent<KPrefabID>();
            if (kprefabid == null)
            {
                targetPanel.SetActive(false);
                return;
            }


            targetPanel.SetActive(true);
            MinionBrainResume component = targetEntity.GetComponent<MinionBrainResume>();
            List<Skill> list = new List<Skill>();
            foreach (KeyValuePair<string, bool> keyValuePair in component.MasteryBySkillID)
            {
                if (keyValuePair.Value)
                {
                    Skill skill = Db.Get().Skills.Get(keyValuePair.Key);
                    list.Add(skill);
                }
            }
            targetPanel.SetLabel("mastered_skills_header", UI.DETAILTABS.PERSONALITY.RESUME.MASTERED_SKILLS, UI.DETAILTABS.PERSONALITY.RESUME.MASTERED_SKILLS_TOOLTIP);
            if (list.Count == 0)
            {
                targetPanel.SetLabel("no_skills", "  • " + UI.DETAILTABS.PERSONALITY.RESUME.NO_MASTERED_SKILLS.NAME, string.Format(UI.DETAILTABS.PERSONALITY.RESUME.NO_MASTERED_SKILLS.TOOLTIP, targetEntity.name));
            }
            else
            {
                foreach (Skill skill2 in list)
                {
                    string text = "";
                    foreach (SkillPerk skillPerk in skill2.perks)
                    {
                        text = text + "  • " + skillPerk.Name + "\n";
                    }
                    targetPanel.SetLabel(skill2.Id, "  • " + skill2.Name, skill2.description + "\n" + text);
                }
            }
            targetPanel.Commit();
        }

        private static void NewAttributesPanel(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
        {
            if (targetEntity == null)
            {
                return;
            }
            if (targetPanel == null)
            {
                return;
            }

            KPrefabID kprefabid = targetEntity.GetComponent<KPrefabID>();
            if (kprefabid == null)
            {
                targetPanel.SetActive(false);
                return;
            }


            targetPanel.SetActive(true);

            List<AttributeInstance> list = new List<AttributeInstance>(targetEntity.GetAttributes().AttributeTable).FindAll((AttributeInstance a) => a.Attribute.ShowInUI == Klei.AI.Attribute.Display.Skill);

            if (list.Count > 0)
            {

                foreach (AttributeInstance attributeInstance in list)
                {
                    targetPanel.SetLabel(attributeInstance.Attribute.Id, string.Format("{0}: {1}", attributeInstance.Name, attributeInstance.GetFormattedValue()), attributeInstance.GetAttributeValueTooltip());

                }
            }

            string targetName = targetEntity.GetComponent<KSelectable>()?.GetName() ?? " ";



            targetPanel.Commit();
        }




        private CollapsibleDetailContentPanel newTraitsPanel;
        private CollapsibleDetailContentPanel newResumePanel;
        private CollapsibleDetailContentPanel newAttributesPanel;
        private CollapsibleDetailContentPanel D;
    }

}
