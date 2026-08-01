using HarmonyLib;
using KMod;
using Database;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocalCanvas
{
    public sealed class ModEntry : UserMod2
    {
        private static readonly HashSet<int> userMenuCaptureButtonsAdded = new HashSet<int>();

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            LocalCanvasConfig.Load();
            harmony.PatchAll();
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

        [HarmonyPatch(typeof(UserMenu), "AppendToScreen")]
        private static class UserMenuAppendToScreenPatch
        {
            private static void Postfix(GameObject go, UserMenuScreen screen)
            {
                Painting painting = go?.GetComponent<Painting>();
                Artable target = painting?.GetComponent<Artable>();
                if (target == null || screen == null)
                {
                    return;
                }

                KPrefabID prefabComponent = target.GetComponent<KPrefabID>();
                string prefabId = prefabComponent == null ? null : prefabComponent.PrefabID().ToString();
                if (prefabId != "Canvas" && prefabId != "CanvasTall" && prefabId != "CanvasWide")
                {
                    return;
                }

                int buttonKey = (screen.GetInstanceID() * 397) ^ go.GetInstanceID();
                if (!userMenuCaptureButtonsAdded.Add(buttonKey))
                {
                    return;
                }

                KIconButtonMenu.ButtonInfo button = new KIconButtonMenu.ButtonInfo(
                    "action_capture",
                    "截图",
                    delegate { LocalCanvasCaptureController.Begin(target); },
                    Action.NumActions,
                    null,
                    null,
                    null,
                    "截取当前画面并保存为本地画布图片",
                    true);
                screen.AddButtons(new[] { button });
            }
        }

        [HarmonyPatch(typeof(UserMenuScreen), "Refresh")]
        private static class UserMenuScreenRefreshPatch
        {
            private static void Prefix()
            {
                userMenuCaptureButtonsAdded.Clear();
            }
        }

        [HarmonyPatch(typeof(PlayerController), "OnKeyDown")]
        private static class PlayerControllerOnKeyDownPatch
        {
            private static bool Prefix(KButtonEvent e)
            {
                return !LocalCanvasCaptureController.TryHandleInput(e);
            }
        }

        [HarmonyPatch(typeof(PlayerController), "OnKeyUp")]
        private static class PlayerControllerOnKeyUpPatch
        {
            private static bool Prefix(KButtonEvent e)
            {
                return !LocalCanvasCaptureController.TryHandleInputUp(e);
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
