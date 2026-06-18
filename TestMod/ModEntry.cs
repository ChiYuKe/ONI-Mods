using HarmonyLib;
using KMod;
using UnityEngine;

namespace TestMod
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            UiPrefabAssetBundleLoader.SetModPath(mod.ContentPath);
            UiPrefabAssetBundleLoader.Preload(new[] { "cyk_ab" });
            harmony.PatchAll();
            Debug.Log("[TestMod] Loaded AB UI toolkit.");
        }
    }
}
