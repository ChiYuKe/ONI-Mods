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
        public const string NarehateDeathId = "AbyssNarehate";

        public static Death Curse { get; private set; }

        public static Death Narehate { get; private set; }

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

                Narehate = new Death(
                    NarehateDeathId,
                    __instance,
                    Strings.Get("STRINGS.DUPLICANTS.DEATHS.ABYSSNAREHATE.NAME"),
                    Strings.Get("STRINGS.DUPLICANTS.DEATHS.ABYSSNAREHATE.DESCRIPTION"),
                    "dead_on_back",
                    "dead_on_back");
            }
        }

        /// <summary>
        /// 死亡状态项的悬浮提示按死因定制：深渊相关的死亡不再显示原版的“打盹”笑话。
        /// </summary>
        [HarmonyPatch(typeof(Database.DuplicantStatusItems), MethodType.Constructor)]
        public static class DuplicantStatusItems_Constructor_Patch
        {
            public static void Postfix(Database.DuplicantStatusItems __instance)
            {
                StatusItem dead = __instance.Dead;
                if (dead == null)
                    return;

                dead.resolveTooltipCallback = (string text, object data) =>
                {
                    Death death = data as Death;
                    if (death != null)
                    {
                        if (death.Id == CurseDeathId)
                            return Strings.Get("STRINGS.MISC.STATUSITEM_TOOLTIPS.ABYSS_CURSE_DEAD");
                        if (death.Id == NarehateDeathId)
                            return Strings.Get("STRINGS.MISC.STATUSITEM_TOOLTIPS.ABYSS_NAREHATE_DEAD");
                    }
                    return text;
                };
            }
        }
    }
}
