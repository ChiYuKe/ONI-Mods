using System;
using System.Collections.Generic;

namespace LogicNetwork.Runtime
{
    /// <summary>Central catalogue of runtime modules supported by the Logic Network editor.</summary>
    internal static class LogicNetworkNodeRegistry
    {
        private static readonly HashSet<string> modules = new HashSet<string>(StringComparer.Ordinal)
        {
            "Add", "Subtract", "Multiply", "Divide", "Negate", "Min", "Max", "Clamp", "Modulo",
            "GreaterThan", "Equal", "LessThan", "Range", "Constant", "TestSignal",
            "BoolTrue", "BoolFalse", "BoolAnd", "BoolNand", "BoolOr", "BoolNor", "BoolXor", "BoolNot",
            "Selector", "Sequence", "MusicSequencer", "Delay", "Latch", "EdgePulse", "Hysteresis", "Toggle", "PulseShaper", "NumberChanged",
            "MapRange", "Counter", "RandomChance", "TimerPulse", "Cycle4",
            "Output", "Split4", "Merge4", "Select", "PixelScreen", "Group"
        };

        public static bool IsKnown(string module)
        {
            return !string.IsNullOrEmpty(module) && modules.Contains(module);
        }

    }
}
