using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MiniBox.TransportConfig.ConveyorRail
{
    [HarmonyPatch(typeof(SolidConduitDispenser), "ConduitUpdate")]
    public class ConveyorRailCapacityPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == 20f)
                    instruction.operand = ModSettings.Current.ConveyorRailMaxPackageMassKg;

                yield return instruction;
            }
        }
    }
}





