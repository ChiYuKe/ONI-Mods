using HarmonyLib;
using KMod;
using UnityEngine;

namespace VignetteBegone
{
    public sealed class Mod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[VignetteBegone] MOD加载成功".Color("lightblue"));
        }
    }
}
