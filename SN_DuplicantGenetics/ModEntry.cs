using KMod;
using StorageNetwork.API;
using UnityEngine;

namespace SN_DuplicantGenetics
{
    public sealed class ModEntry : UserMod2
    {
        private static readonly ExampleHeaderButtonProvider HeaderButtons = new ExampleHeaderButtonProvider();

        internal static string ContentPath { get; private set; }

        public override void OnLoad(HarmonyLib.Harmony harmony)
        {
            base.OnLoad(harmony);
            ContentPath = mod.ContentPath;
            StorageNetworkPanelHeaderButtonRegistry.Register(HeaderButtons);
            Debug.Log("[SN_DuplicantGenetics] SN_DuplicantGenetics已加载。");
        }
    }
}


