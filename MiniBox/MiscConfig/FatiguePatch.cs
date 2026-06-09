using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MiniBox.MiscConfig
{
    [HarmonyPatch(typeof(ElementSplitterComponents), "CanFirstAbsorbSecond")]
    public class FatiguePatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == -0.11666667f)
                    instruction.operand = ModSettings.Current.DisableFatigue ? 0f : -0.11666667f;

                yield return instruction;
            }
        }
    }
}





