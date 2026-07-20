using KMod;
using UnityEngine;

namespace LogicNetwork
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(HarmonyLib.Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[LogicNetwork] Logic Network已加载。");
        }
    }
}
