using CykUtils;
using Database;
using HarmonyLib;
using Klei.AI;
using RunningOutOfTime.Content.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static STRINGS.DUPLICANTS.DISEASES;

namespace RunningOutOfTime.Content.Patches
{ 
    public class MinionPatch
    {


        #region 注册年龄和衰老属性
        [HarmonyPatch(typeof(Database.Amounts), "Load")]
        [HarmonyPriority(Priority.First)]
        public static class AmountsPatch
        {
            public static Amount AgeAttribute;

            public static void Postfix(Database.Amounts __instance)
            {

                AgeAttribute = __instance.CreateAmount(
                    id: "AgeAttribute",
                    min: 0f,
                    max: 0f,
                    show_max: true,
                    units: Units.Flat,
                    delta_threshold: 0.1675f,
                    show_in_ui: true,
                    string_root: "STRINGS.DUPLICANTS",
                    uiSprite: "icon_display_screen_status_old",
                    thoughtSprite: "attribute_stamina",
                    uiFullColourSprite: "mod_vitality"
                );

                AgeAttribute.SetDisplayer(new StandardAmountDisplayer(GameUtil.UnitClass.SimpleInteger, GameUtil.TimeSlice.PerCycle));

                LogUtil.Log("已添加自定义AgeAttribute");
            }
        }

        [HarmonyPatch(typeof(BaseMinionConfig))]
        public static class AddMinionAmountsPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("AddMinionAmounts")]
            public static void Postfix(Modifiers modifiers)
            {
                if (modifiers?.initialAmounts != null)
                {
                    if (AmountsPatch.AgeAttribute != null)
                    {
                        modifiers.initialAmounts.Add(AmountsPatch.AgeAttribute.Id);
                    }
                }
            }


            [HarmonyPostfix]
            [HarmonyPatch("BaseMinion")]
            public static void Postfix_BaseMinion(GameObject __result)
            {
                //  __result.AddComponent<Trigger>(); // 添加DebuffContainer组件
                __result.AddComponent<AgingMonitor>();

            }


        }

        // 在复制人状态面板中显示年龄属性
        [HarmonyPatch(typeof(MinionVitalsPanel), "Init")]
        [HarmonyPriority(Priority.Low)]
        public static class MinionVitalsPanelPatch
        {
            public static void Postfix(MinionVitalsPanel __instance)
            {
                Amount AgeAttribute = Db.Get().Amounts.Get("AgeAttribute");
                if (AgeAttribute != null)
                {
                    var method = AccessTools.Method(typeof(MinionVitalsPanel), "AddAmountLine");
                    method.Invoke(__instance, new object[] { AgeAttribute, null });
                }

            }
        }

        [HarmonyPatch(typeof(MinionVitalsPanel), "Refresh")]
        public static class MinionVitalsPanel_RefreshPatch
        {
            public static void Postfix(MinionVitalsPanel __instance, GameObject selectedEntity)
            {
                if (selectedEntity == null) return;

                // 判断当前选中对象是否仿生人
                var prefabID = selectedEntity.GetComponent<KPrefabID>();
                bool isBionic = prefabID != null && prefabID.HasTag(GameTags.Minions.Models.Bionic);

                var ageAmount = Db.Get().Amounts.Get("AgeAttribute");
                if (ageAmount == null) return;

                var ageLine = __instance.amountsLines.Find(line => line.amount == ageAmount);
                if (isBionic)
                {
                    // 仿生人老不死的不需要显示年龄
                    if (ageLine.go != null)
                        ageLine.go.SetActive(false);
                }
                else
                {
                    ageLine.go.transform.SetAsFirstSibling();
                }

            }
        }

        // 给复制人添加年龄属性和衰老效果
        [HarmonyPatch(typeof(MinionConfig))]
        public static class AddMinionTraitsPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("GetTraits")]
            public static void Postfix(ref AttributeModifier[] __result)
            {
                var db = Db.Get();
                if (db == null || db.Amounts == null || AmountsPatch.AgeAttribute == null)
                {
                    return;
                }

                var newModifiers = __result.ToList();
                if (!__result.Any(m => m.AttributeId == AmountsPatch.AgeAttribute.Id))
                {
                    newModifiers.Add(new AttributeModifier(
                        AmountsPatch.AgeAttribute.maxAttribute.Id,
                        Config.TUNINGS.AGE.MINION_AGE_THRESHOLD,
                        Config.STRINGS.MISC.NOTIFICATIONS.AGEATTRIBUTE.NAME,
                        false,
                        false,
                        true
                    ));

                    newModifiers.Add(new AttributeModifier(
                        AmountsPatch.AgeAttribute.deltaAttribute.Id,
                        1 / 600f,
                        Config.STRINGS.MISC.NOTIFICATIONS.AGEATTRIBUTE.NAME,
                        false,
                        false,
                        true
                    ));
                }

                __result = newModifiers.ToArray();
            }
        }

        // 在复制人信息面板中显示年龄
        [HarmonyPatch(typeof(MinionPersonalityPanel), "RefreshBioPanel")]
        public static class MinionPersonalityPanel_RefreshBioPanel_Patch
        {
            public static void Postfix(CollapsibleDetailContentPanel targetPanel, GameObject targetEntity)
            {
                // 只处理复制人
                if (!IsDuplicant(targetEntity)) return;

                // 获取年龄信息
                if (!TryGetAgeAttribute(targetEntity, out float currentAge, out float maxAge)) return;

                // 构建显示文本
                string ageLabel = string.Format(Config.STRINGS.DUPLICANTS.AGEATTRIBUTE.NAME, currentAge.ToString("F1"), maxAge.ToString("F0"));
                string tooltip = string.Format(Config.STRINGS.DUPLICANTS.AGEATTRIBUTE.TOOLTIP, currentAge.ToString("F1"), maxAge.ToString("F0"));
                targetPanel.SetLabel("age", ageLabel, tooltip);
            }

            private static bool IsDuplicant(GameObject entity)
            {
                if (entity == null) return false;
                var prefabID = entity.GetComponent<KPrefabID>();
                return prefabID == null || !prefabID.HasTag(GameTags.Minions.Models.Bionic);
            }

            private static bool TryGetAgeAttribute(GameObject entity, out float currentAge, out float maxAge)
            {
                currentAge = 0f;
                maxAge = 0f;

                if (entity == null) return false;
                var identity = entity.GetComponent<MinionIdentity>();
                var amounts = identity?.GetComponent<MinionModifiers>()?.amounts;
                var ageInstance = amounts?.Get(AmountsPatch.AgeAttribute);

                if (ageInstance == null) return false;

                currentAge = ageInstance.value;
                maxAge = ageInstance.GetMax();
                return true;
            }
        }
        #endregion




    }
}