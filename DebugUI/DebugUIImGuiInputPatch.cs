using System;
using System.Reflection;
using HarmonyLib;
using ImGuiNET;
using UnityEngine;

namespace DebugUI
{
    [HarmonyPatch]
    internal static class DebugUIImGuiInputPatch
    {
        private static int lastInputFrame = -1;
        private static string lastInputString;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method("ImGuiRenderer:UpdateInput");
        }

        private static void Postfix()
        {
            ImGuiIOPtr io = ImGui.GetIO();
            Input.imeCompositionMode = io.WantTextInput ? IMECompositionMode.On : IMECompositionMode.Auto;

            string input = Input.inputString;
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            if (lastInputFrame == Time.frameCount && lastInputString == input)
            {
                return;
            }

            lastInputFrame = Time.frameCount;
            lastInputString = input;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!char.IsControl(c) && c > 127)
                {
                    io.AddInputCharacter(c);
                }
            }
        }
    }
}
