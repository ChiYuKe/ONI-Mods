using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ModConfig
{
    public sealed class ModsScreenOptionsButtonDefinition
    {
        public string ModTitlePrefix { get; set; }
        public string ButtonName { get; set; }
        public string ButtonText { get; set; } = "选项";
        public string Tooltip { get; set; }
        public Vector2 ButtonOffset { get; set; } = new Vector2(-72f, 0f);
        public System.Action OnClick { get; set; }
    }

    public static class ModsScreenOptionsButton
    {
        private static readonly List<ModsScreenOptionsButtonDefinition> Definitions = new List<ModsScreenOptionsButtonDefinition>();

        public static void Register(ModsScreenOptionsButtonDefinition definition)
        {
            if (definition == null ||
                string.IsNullOrEmpty(definition.ModTitlePrefix) ||
                string.IsNullOrEmpty(definition.ButtonName) ||
                definition.OnClick == null)
            {
                return;
            }

            Definitions.RemoveAll(existing => existing.ButtonName == definition.ButtonName);
            Definitions.Add(definition);
        }

        [HarmonyPatch(typeof(ModsScreen), "BuildDisplay")]
        private static class BuildDisplayPatch
        {
            public static void Postfix(ModsScreen __instance)
            {
                Transform entryParent = AccessTools.Field(typeof(ModsScreen), "entryParent")?.GetValue(__instance) as Transform;
                if (entryParent == null)
                {
                    return;
                }

                foreach (Transform entry in entryParent)
                {
                    foreach (ModsScreenOptionsButtonDefinition definition in Definitions)
                    {
                        if (!MatchesEntry(entry, definition) || entry.Find(definition.ButtonName) != null)
                        {
                            continue;
                        }

                        AddOptionsButton(entry, definition);
                    }
                }
            }
        }

        private static bool MatchesEntry(Transform entry, ModsScreenOptionsButtonDefinition definition)
        {
            if (entry == null || definition == null)
            {
                return false;
            }

            if (entry.name == definition.ModTitlePrefix)
            {
                return true;
            }

            HierarchyReferences references = entry.GetComponent<HierarchyReferences>();
            LocText title = references != null ? references.GetReference<LocText>("Title") : null;
            return title != null &&
                   title.text != null &&
                   title.text.StartsWith(definition.ModTitlePrefix);
        }

        private static void AddOptionsButton(Transform entry, ModsScreenOptionsButtonDefinition definition)
        {
            HierarchyReferences references = entry.GetComponent<HierarchyReferences>();
            KButton manageButton = references != null ? references.GetReference<KButton>("ManageButton") : null;
            if (manageButton == null)
            {
                return;
            }

            GameObject buttonObject = Util.KInstantiateUI(manageButton.gameObject, manageButton.transform.parent.gameObject, false);
            buttonObject.name = definition.ButtonName;

            KButton button = buttonObject.GetComponent<KButton>();
            button.ClearOnClick();
            button.ClearOnPointerEvents();
            button.isInteractable = true;
            button.onClick += definition.OnClick;

            LocText label = buttonObject.GetComponentInChildren<LocText>();
            if (label != null)
            {
                label.text = definition.ButtonText;
            }

            ToolTip tooltip = buttonObject.GetComponent<ToolTip>() ?? buttonObject.AddComponent<ToolTip>();
            tooltip.toolTip = definition.Tooltip;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            RectTransform sourceRect = manageButton.GetComponent<RectTransform>();
            if (rect != null && sourceRect != null)
            {
                rect.anchorMin = sourceRect.anchorMin;
                rect.anchorMax = sourceRect.anchorMax;
                rect.pivot = sourceRect.pivot;
                rect.sizeDelta = new Vector2(64f, sourceRect.sizeDelta.y);
                rect.anchoredPosition = sourceRect.anchoredPosition + definition.ButtonOffset;
            }

            buttonObject.SetActive(true);
        }
    }
}
