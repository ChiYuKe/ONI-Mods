using HarmonyLib;
using TUNING;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(Db), "Initialize")]
    internal class GeneralNeedsPatch
    {
        private const float DefaultCaloriesBurnedPerCycle = -1000000f;
        private const float DefaultBladderIncreasePerSecond = 0.16666667f;
        private const float DefaultCarryCapacity = 200f;

        private static void Prefix()
        {
            var settings = ModSettings.Current;
            var baseStats = DUPLICANTSTATS.STANDARD.BaseStats;
            baseStats.CALORIES_BURNED_PER_CYCLE = settings.DisableHunger ? 0f : DefaultCaloriesBurnedPerCycle;
            baseStats.BLADDER_INCREASE_PER_SECOND = settings.DisableBladder ? 0f : DefaultBladderIncreasePerSecond;
            baseStats.CARRY_CAPACITY = DefaultCarryCapacity * settings.CarryCapacityMultiplier;
        }
    }
}
