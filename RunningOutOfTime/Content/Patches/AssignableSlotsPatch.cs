using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Database;
using EternalDecay.Content.Config;
using HarmonyLib;
using UnityEngine;

namespace RunningOutOfTime.Content.Patches
{
    public class AssignableSlotsPatch
    {
        public static AssignableSlot KMinionBrain;

        [HarmonyPatch(typeof(AssignableSlots))]
        public class AssignableSlots_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(MethodType.Constructor)]
            public static void Postfix(AssignableSlots __instance)
            {
                // 新增自定义的 AssignableSlot
                KMinionBrain = new OwnableSlot("KMinionBrain", Config.STRINGS.MISC.TAGS.MINIONBRAIN.NAME);
                __instance.Add(KMinionBrain);

            }
        }


        [HarmonyPatch(typeof(AssignableSideScreenRow), "Refresh")]
        public static class AssignableSideScreenRow_Refresh_Patch
        {
            public static void Postfix(AssignableSideScreenRow __instance)
            {
                if (__instance == null || __instance.gameObject == null)
                    return;


                var sideScreen = Traverse.Create(__instance).Field("sideScreen").GetValue<AssignableSideScreen>();
                if (sideScreen == null || sideScreen.targetAssignable == null)
                    return;


                // 安全获取 slot
                var slot = sideScreen.targetAssignable.slot;
                if (slot == null)
                    return;

                var proxy = __instance.targetIdentity as MinionAssignablesProxy;
                if (proxy == null)
                    return;


                GameObject targetGO = proxy.GetTargetGameObject();


                // 检查 slot.Id 是否匹配
                if (slot.Id == AssignableSlotsPatch.KMinionBrain.Id)
                {
                    var prefabID = targetGO.GetComponent<KPrefabID>();
                    //  Debug.Log($"获取到的targetGO.name ：{targetGO.name}");


                    if (prefabID.HasTag(EDGameTags.IsLegacyAssigned) || prefabID.HasTag(GameTags.Corpse) || prefabID.HasTag(GameTags.Minions.Models.Bionic))
                    {
                        // Debug.Log($"获取到的过滤标签后的targetGO.name ：{targetGO.name}");
                        __instance.gameObject.SetActive(false);

                    }


                }
            }
        }

    }
}
