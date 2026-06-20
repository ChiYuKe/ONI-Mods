using HarmonyLib;
using KMod;
using StorageNetwork.Core;

namespace StorageNetwork
{
    public class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            StorageNetworkSprites.SetModPath(mod.ContentPath);
            StorageNetworkAssetBundles.SetModPath(mod.ContentPath);
            StorageNetworkLocalization.SetModPath(mod.ContentPath);
            StorageNetworkFrameProfileTool.SetModPath(mod.ContentPath);
            // StorageNetworkHarmonyProfileTool.DumpIfEnabled(mod.ContentPath);
            Config.SetModPath(mod.ContentPath);
            Config.Load();
            StorageNetworkOptions.Register();
        }
    }
}
