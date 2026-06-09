using HarmonyLib;
using KMod;
using UnityEngine;

namespace NewElementRegistration
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[NewElementRegistration] Loaded");
        }
    }
}
