using HarmonyLib;
using KMod;
using UnityEngine;

namespace DebugUI
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            DebugUIDriver.Ensure();
            Debug.Log("[DebugUI] Loaded. Press F8 to toggle UI inspector.");
        }
    }
}
