using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(WorldDamage), "OnDigComplete")]
    public class DiggingDropRatePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == 0.5f)
                    instruction.operand = ModSettings.Current.DiggingDropRate;

                yield return instruction;
            }
        }
    }
}





