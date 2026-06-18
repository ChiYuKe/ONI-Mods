using HarmonyLib;
using KMod;
using UnityEngine;

namespace ONIVisualEnhancer
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            VisualEnhancerSettings.Load();
            Debug.Log("[ONIVisualEnhancer] Loaded");
        }
    }
}

