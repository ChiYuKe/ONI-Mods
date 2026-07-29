using HarmonyLib;
using KMod;
using Database;
using System.Collections.Generic;
using UnityEngine;

namespace LocalCanvas
{
    public sealed class ModEntry : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            LocalCanvasConfig.Load();
            harmony.PatchAll();
            Debug.Log("[LocalCanvas] loaded");
        }

        [HarmonyPatch(typeof(KAnimGroupFile), "Load")]
        private static class KAnimGroupFileLoadPatch
        {
            private static void Prefix()
            {
                LocalCanvasKAnimRegistry.Register();
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        private static class DbInitializePatch
        {
            private static void Postfix()
            {
                LocalCanvasRegistry.RegisterStages();
            }
        }

        [HarmonyPatch(typeof(Painting), "OnSpawn")]
        private static class PaintingOnSpawnPatch
        {
            private static void Postfix(Painting __instance)
            {
                LocalCanvasDisplay.TryAttach(__instance);
            }
        }

        [HarmonyPatch(typeof(Artable), "SetStage")]
        private static class ArtableSetStagePatch
        {
            private static void Postfix(Artable __instance)
            {
                __instance.GetComponent<LocalCanvasDisplay>()?.Refresh();
            }
        }

        [HarmonyPatch(typeof(Artable), "SetDefault")]
        private static class ArtableSetDefaultPatch
        {
            private static void Postfix(Artable __instance)
            {
                __instance.GetComponent<LocalCanvasDisplay>()?.Refresh();
            }
        }

        [HarmonyPatch(typeof(ArtableStage), "GetPermitPresentationInfo")]
        private static class ArtableStagePresentationPatch
        {
            private static void Postfix(ArtableStage __instance, ref Database.PermitPresentationInfo __result)
            {
                if (LocalCanvasRegistry.TryGetImage(__instance.id, out LocalCanvasImageInfo image))
                {
                    __result.sprite = image.GetSprite();
                    __result.SetFacadeForText(image.DisplayName);
                }
            }
        }

        [HarmonyPatch(typeof(ArtableStages), "GetPrefabStages")]
        private static class ArtableStagesGetPrefabStagesPatch
        {
            private static void Postfix(ref List<ArtableStage> __result)
            {
                if (__result == null || __result.Count < 2)
                {
                    return;
                }

                HashSet<string> seen = new HashSet<string>();
                __result.RemoveAll(stage => stage == null || !seen.Add(stage.id));
            }
        }
    }
}
