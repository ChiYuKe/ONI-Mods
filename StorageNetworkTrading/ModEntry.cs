using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using KMod;
using StorageNetwork.API;
using StorageNetwork.Trading;
using StorageNetworkTrading.UI;

namespace StorageNetworkTrading
{
    public sealed class ModEntry : UserMod2
    {
        private string contentPath;
        public override void OnLoad(Harmony harmony)
        {
            contentPath = mod.ContentPath;
            TradingAiFeatureGate.Configure(contentPath);
        }

        public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<KMod.Mod> mods)
        {
            // Dev and Workshop installations have different default static IDs.
            // The loaded prerequisite assembly and its API are the stable contract.
            var dependency = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "StorageNetwork");
            var api = dependency?.GetType("StorageNetwork.API.StorageNetworkApi", false);
            if (!(api?.GetProperty("ApiVersion")?.GetValue(null) is int version) || version < 1 ||
                dependency.GetType("StorageNetwork.API.StorageNetworkUiAccess", false)?.GetMethod("RegisterRightClickClose") == null)
            {
                UnityEngine.Debug.LogWarning("[StorageNetworkTrading] Required mod StorageNetwork is missing or too old. Enable the matching StorageNetwork version.");
                return;
            }

            Initialize(harmony);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Initialize(Harmony harmony)
        {
            TradingLocalization.Path = contentPath;
            TradingLocalization.Load();
            StorageNetworkPanelHeaderButtonRegistry.RegisterFactory(GetHeaderButtons);
            StorageNetworkUiAccess.RegisterTextInputFocus("StorageNetworkTrading", TradingPanel.IsTextInputFocused);
            StorageNetworkUiAccess.RegisterRightClickClose("StorageNetworkTrading", TradingPanel.IsOpen, TradingPanel.CloseTopWindow);
            harmony.PatchAll(assembly);
        }

        private static IEnumerable<StorageNetworkPanelHeaderButton> GetHeaderButtons()
        {
            return new[]
            {
                new StorageNetworkPanelHeaderButton("StorageNetworkTrading", TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.TEXT_919DDBBD91C0")), (System.Action)TradingPanel.Show, TradingText.Render(TradingText.Ref("STRINGS.UI.STORAGENETWORK_TRADING.OPEN_DESCRIPTION")), width: 72, order: 100)
            };
        }
    }

    internal static class TradingLocalization
    {
        internal static string Path;
        internal static void Load()
        {
            foreach (var entry in TradingTextCatalog.Entries)
                Strings.Add(entry.Key, entry.Source);
            string locale = Localization.GetLocale()?.Code ?? "en";
            string file = System.IO.Path.Combine(Path, "translations", locale + ".po");
            if (!File.Exists(file) && !locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                file = System.IO.Path.Combine(Path, "translations", "en.po");
            if (File.Exists(file))
                Localization.OverloadStrings(Localization.LoadStringsFile(file, false));
            TradingText.Configure(locale, key => Strings.Get(key));
        }
    }

    [HarmonyPatch(typeof(Localization), "Initialize")]
    internal static class LocalizationPatch
    {
        private static void Postfix()
        {
            TradingLocalization.Load();
        }
    }
}
