using System;
using System.Collections.Generic;
using System.Reflection;
using CykUtils;
using HarmonyLib;
using UnityEngine;

namespace AutomaticHarvest
{
    public class KModPatch
    {
        private static readonly HashSet<string> SuppressedAutomaticHarvestStatusIds = new HashSet<string>
        {
            "ConduitBlocked",
            "OutputPipeFull",
            "OutputTileBlocked",
            "ConduitBlockedMultiples",
            "SolidConduitBlockedMultiples",
            "SolidPipeObstructed",
        };

        private static readonly FieldInfo RangeVisualizerMaterialField =
            typeof(RangeVisualizerEffect).GetField("material", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void AddTagToPrefab(GameObject prefab, Tag tag)
        {
            KPrefabID prefabId = prefab.GetComponent<KPrefabID>();
            prefabId?.AddTag(tag, false);
        }

        private static bool ShouldSuppressStatusItem(KSelectable selectable, StatusItem statusItem)
        {
            if (selectable == null || statusItem == null)
            {
                return false;
            }

            KPrefabID prefabId = selectable.GetComponent<KPrefabID>();
            return prefabId != null &&
                   prefabId.HasTag(AutomaticHarvestTags.Building) &&
                   SuppressedAutomaticHarvestStatusIds.Contains(statusItem.Id);
        }

        [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
        public static class RegisterBuildingPatch
        {
            public static void Prefix()
            {
                ModUtil.AddBuildingToPlanScreen("Conveyance", AutomaticHarvestConfig.ID);
                KModStringUtils.Add_New_BuildStrings(
                    AutomaticHarvestConfig.ID,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.NAME,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.DESC,
                    STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.EFFECT);
            }
        }

        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class RegisterTechUnlockPatch
        {
            public static void Postfix()
            {
                Tech tech = Db.Get().Techs.Get("SmartStorage");
                if (tech != null && !tech.unlockedItemIDs.Contains(AutomaticHarvestConfig.ID))
                {
                    tech.unlockedItemIDs.Add(AutomaticHarvestConfig.ID);
                }
            }
        }

        [HarmonyPatch(typeof(Localization), "Initialize")]
        private static class LocalizationPatch
        {
            public static void Postfix()
            {
                Loc.Translate(typeof(STRINGS), false);
            }
        }

        [HarmonyPatch(typeof(RangeVisualizerEffect), "OnPostRender")]
        public static class HarvestRangeVisualizerPatch
        {
            public static void Prefix(RangeVisualizerEffect __instance)
            {
                GameObject target = GetCurrentVisualizerTarget();
                Color highlightColor = new Color(0f, 1f, 0.8f, 1f);

                if (target != null &&
                    target.TryGetComponent(out KPrefabID prefabId) &&
                    prefabId.HasTag(AutomaticHarvestTags.Building))
                {
                    highlightColor = new Color(0.1f, 1f, 0f, 1f);
                }

                __instance.highlightColor = highlightColor;
                if (RangeVisualizerMaterialField?.GetValue(__instance) is Material material)
                {
                    material.SetColor("_HighlightColor", highlightColor);
                }
            }

            private static GameObject GetCurrentVisualizerTarget()
            {
                if (SelectTool.Instance?.selected != null)
                {
                    return SelectTool.Instance.selected.gameObject;
                }

                return BuildTool.Instance?.visualizer;
            }
        }

        [HarmonyPatch(typeof(KSelectable), nameof(KSelectable.AddStatusItem), new[] { typeof(StatusItem), typeof(object) })]
        public static class SuppressOptionalConveyorAddStatusPatch
        {
            public static bool Prefix(KSelectable __instance, StatusItem status_item, ref Guid __result)
            {
                if (!ShouldSuppressStatusItem(__instance, status_item))
                {
                    return true;
                }

                __result = Guid.Empty;
                return false;
            }
        }

        [HarmonyPatch(typeof(KSelectable), nameof(KSelectable.SetStatusItem), new[] { typeof(StatusItemCategory), typeof(StatusItem), typeof(object) })]
        public static class SuppressOptionalConveyorSetStatusPatch
        {
            public static bool Prefix(KSelectable __instance, StatusItem status_item, ref Guid __result)
            {
                if (!ShouldSuppressStatusItem(__instance, status_item))
                {
                    return true;
                }

                __result = Guid.Empty;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlantFiberConfig), "CreatePrefab")]
        public static class PlantFiberTagPatch
        {
            public static void Postfix(GameObject __result)
            {
                AddTagToPrefab(__result, AutomaticHarvestTags.PlantFiber);
            }
        }

        [HarmonyPatch(typeof(KelpConfig), "CreatePrefab")]
        public static class KelpTagPatch
        {
            public static void Postfix(GameObject __result)
            {
                AddTagToPrefab(__result, AutomaticHarvestTags.Kelp);
            }
        }
    }
}
