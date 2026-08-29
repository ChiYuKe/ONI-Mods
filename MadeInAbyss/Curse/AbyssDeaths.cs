using HarmonyLib;
using System;
using STRINGS;

namespace MadeInAbyss
{
    /// <summary>
    /// 注册专属死亡方式：被深渊诅咒夺去生命。
    /// 死亡消息由游戏在复制人死亡时自动弹出（{Target} 会替换为复制人名字）。
    /// </summary>
    public static class AbyssDeaths
    {
        public const string CurseDeathId = "AbyssCurse";

        public static Death Curse { get; private set; }

        [HarmonyPatch(typeof(Database.Deaths), MethodType.Constructor)]
        [HarmonyPatch(new Type[] { typeof(ResourceSet) })]
        public static class Deaths_Constructor_Patch
        {
            public static void Postfix(Database.Deaths __instance)
            {
                Curse = new Death(
                    CurseDeathId,
                    __instance,
                    Strings.Get("STRINGS.DUPLICANTS.DEATHS.ABYSSCURSE.NAME"),
                    Strings.Get("STRINGS.DUPLICANTS.DEATHS.ABYSSCURSE.DESCRIPTION"),
                    "dead_on_back",
                    "dead_on_back");
            }
        }
    }
}
