using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Database;
using HarmonyLib;

namespace RunningOutOfTime.Content.Patches
{
    public class ChoreTypesPatch
    {
        [HarmonyPatch(typeof(ChoreTypes))]
        public static class AddNewChorePatch
        {
            public static ChoreType AcceptInheritance;
            public static ChoreType ImpulseDestruction;

            [HarmonyPostfix]
            [HarmonyPatch(MethodType.Constructor, new Type[] { typeof(ResourceSet) })]
            public static void Postfix(ChoreTypes __instance)
            {
                if (__instance == null) return;

                MethodInfo addMethod = typeof(ChoreTypes).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance);
                if (addMethod == null) return;

                AcceptInheritance = (ChoreType)addMethod.Invoke(__instance, new object[]
                {
                    "AcceptInheritance",
                    new string[0],
                    "",
                    new string[0],
                    "接受 罐中脑",
                    "顷刻炼化 罐中脑",
                    "这个复制人正在接受 罐中脑 的传承！！",
                    false,
                    -1,
                    null
                });
                AcceptInheritance.interruptPriority = 100000;

                ImpulseDestruction = (ChoreType)addMethod.Invoke(__instance, new object[]
                {
                    "ImpulseDestruction",
                    new string[0],
                    "",
                    new string[0],
                    "皮痒了",
                    "想拆东西",
                    "由于你没有看着他，于是皮痒了想拆东西",
                    false,
                    -1,
                    null
                });
                ImpulseDestruction.interruptPriority = 100000;
            }
        }
    }
}
