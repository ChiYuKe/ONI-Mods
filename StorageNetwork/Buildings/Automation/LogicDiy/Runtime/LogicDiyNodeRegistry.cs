using System;
using System.Collections.Generic;

namespace StorageNetwork.LogicDiy.Runtime
{
    /// <summary>Central catalogue of runtime modules supported by the DIY editor.</summary>
    internal static class LogicDiyNodeRegistry
    {
        private static readonly HashSet<string> materialModules = new HashSet<string>(StringComparer.Ordinal)
        {
            "MaterialCondition", "MaterialLow", "MaterialHigh", "MaterialChanged"
        };

        private static readonly HashSet<string> modules = new HashSet<string>(StringComparer.Ordinal)
        {
            "Add", "Subtract", "Multiply", "Divide", "Negate", "Min", "Max", "Clamp", "Modulo",
            "GreaterThan", "Equal", "LessThan", "Range", "Variable", "Constant", "TestSignal",
            "BoolTrue", "BoolFalse", "BoolAnd", "BoolNand", "BoolOr", "BoolNor", "BoolXor", "BoolNot",
            "Selector", "Sequence", "Delay", "Latch", "EdgePulse", "Hysteresis", "Toggle", "PulseShaper", "NumberChanged",
            "MapRange", "Counter", "RandomChance", "TimerPulse", "Cycle4", "MaterialCondition", "MaterialLow",
            "MaterialHigh", "MaterialChanged", "InventoryPercent", "InventoryStored", "InventoryRemaining",
            "InventoryCapacity", "PowerPercent", "PowerStored", "PowerCapacity", "PowerRemaining", "BuildingStatus",
            "BuildingSignal", "Output", "Split4", "Merge4", "Select", "PixelScreen", "Group"
        };

        public static bool IsKnown(string module)
        {
            return !string.IsNullOrEmpty(module) && modules.Contains(module);
        }

        public static bool UsesMaterialInput(string module)
        {
            return !string.IsNullOrEmpty(module) && materialModules.Contains(module);
        }
    }
}
