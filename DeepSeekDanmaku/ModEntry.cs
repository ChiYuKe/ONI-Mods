using HarmonyLib;
using KMod;
using UnityEngine;
using PeterHan.PLib.Options;

namespace DeepSeekDanmaku
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            new POptions().RegisterOptions(this, typeof(ModConfig));
            ModConfig.Load();
            Debug.Log($"[DeepSeekDanmaku] 已加载，提供商={ModConfig.Instance.selectedProvider}，模型={ModConfig.Instance.EffectiveModel}，配置文件: {ModConfig.ConfigPath}");
        }
    }

    [HarmonyPatch(typeof(Game), "OnSpawn")]
    internal static class GameSpawnPatch
    {
        private static void Postfix() => DeepSeekController.EnsureCreated();
    }

    [HarmonyPatch(typeof(Game), "OnCleanUp")]
    internal static class GameCleanUpPatch
    {
        private static void Postfix() => DeepSeekController.DestroyCurrent();
    }
}
