using HarmonyLib;
using KMod;
using UnityEngine;

namespace FunnyComponents
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll();
            Debug.Log("[FunnyComponents] Loaded. Duplicants are getting a tiny box of odd habits.");
        }
    }
}
