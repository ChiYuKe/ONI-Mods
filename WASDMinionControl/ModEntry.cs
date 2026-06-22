using HarmonyLib;
using KMod;
using UnityEngine;

namespace WASDMinionControl
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            Debug.Log("[WASDMinionControl] Loaded");
        }
    }
}
