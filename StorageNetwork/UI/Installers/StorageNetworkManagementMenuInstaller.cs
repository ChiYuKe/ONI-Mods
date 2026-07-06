using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StorageNetwork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI.Installers
{
    internal static class StorageNetworkManagementMenuInstaller
    {
        private const string ButtonName = "StorageNetworkManagementButton";
        private const string IconName = "storage_network_overlay";

        public static void Install(ManagementMenu menu)
        {
            if (menu == null)
            {
                return;
            }

            StorageNetworkWorldTextPanel.EnsureInstalled(menu.gameObject);
            AddManagementButton(menu);
        }

        private static void AddManagementButton(ManagementMenu menu)
        {
            Transform parent = ResolveToggleParent(menu);
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find(ButtonName);
            if (existing != null)
            {
                return;
            }

            KToggle template = ResolveTemplate(menu);
            if (template == null)
            {
                return;
            }

            GameObject buttonObject = Object.Instantiate(template.gameObject, parent, false);
            buttonObject.name = ButtonName;
            buttonObject.SetActive(true);

            KToggle button = buttonObject.GetComponent<KToggle>();
            button.ClearOnClick();
            button.group = null;
            button.isOn = false;
            button.interactable = true;

            ImageToggleState toggleState = buttonObject.GetComponent<ImageToggleState>();
            toggleState?.SetInactive();

            LocText label = buttonObject.GetComponentInChildren<LocText>(true);
            if (label != null)
            {
                SetButtonLabel(label);
            }

            if (button.fgImage != null)
            {
                Sprite sprite = StorageNetworkSpriteLoader.GetSprite(IconName);
                if (sprite != null)
                {
                    button.fgImage.sprite = sprite;
                    button.fgImage.color = Color.white;
                }
            }

            HierarchyReferences references = buttonObject.GetComponent<HierarchyReferences>();
            if (references != null)
            {
                DisableIfPresent(references, "ResearchIcon");
                DisableIfPresent(references, "AlertImage");
                DisableIfPresent(references, "GlowImage");
                DisableIfPresent(references, "CheckMark");
                DisableIfPresent(references, "Checkmark");
                DisableIfPresent(references, "Notification");
                DisableIfPresent(references, "TopRightIcon");
            }

            ToolTip toolTip = button.GetComponent<ToolTip>() ?? button.gameObject.AddComponent<ToolTip>();
            toolTip.SetSimpleTooltip(BuildTooltipText());
            button.onClick += () =>
            {
                button.isOn = false;
                toggleState?.SetInactive();
                KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));
                StorageNetworkPanel.Show();
            };

            button.transform.SetSiblingIndex(GetInsertIndex(parent));
        }

        private static string BuildTooltipText()
        {
            string tooltip = STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.OVERVIEW_TOOLTIP);
            string hotkey = GameUtil.GetHotkeyString(StorageNetwork.Patches.StorageNetworkHotkeyPatch.Action);
            return string.IsNullOrEmpty(hotkey) ? tooltip : string.Format("{0} {1}", tooltip, hotkey);
        }

        private static void SetButtonLabel(LocText label)
        {
            string title = STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.TITLE);
            label.key = string.Empty;
            label.enableAutoSizing = false;
            label.fontSize = Mathf.Min(label.fontSize, 15f);
            label.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            label.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            label.SetText(title);
            label.text = title;

            RectTransform labelRect = label.rectTransform();
            if (labelRect != null)
            {
                labelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 96f);
            }

            RectTransform buttonRect = label.GetComponentInParent<KToggle>()?.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(buttonRect.rect.width, 112f));
            }
        }

        private static Transform ResolveToggleParent(ManagementMenu menu)
        {
            Traverse traverse = Traverse.Create(menu);
            Transform toggleParent = traverse.Field("toggleParent").GetValue<Transform>();
            return toggleParent != null ? toggleParent : menu.transform;
        }

        private static KToggle ResolveTemplate(ManagementMenu menu)
        {
            Traverse traverse = Traverse.Create(menu);
            List<KToggle> toggles = traverse.Field("toggles").GetValue<List<KToggle>>();
            KToggle liveTemplate = toggles != null
                ? toggles.FirstOrDefault(toggle =>
                    toggle != null &&
                    toggle.gameObject != null &&
                    toggle.gameObject.activeSelf &&
                    IsPrimaryManagementButton(toggle))
                : null;
            if (liveTemplate != null)
            {
                return liveTemplate;
            }

            KToggle template = traverse.Field("researchButtonPrefab").GetValue<KToggle>();
            if (template != null)
            {
                return template;
            }

            template = traverse.Field("smallPrefab").GetValue<KToggle>();
            if (template != null)
            {
                return template;
            }

            return toggles != null ? toggles.FirstOrDefault(toggle => toggle != null) : null;
        }

        private static int GetInsertIndex(Transform parent)
        {
            int starMapIndex = parent.childCount;
            for (int i = 0; i < parent.childCount; i++)
            {
                string name = parent.GetChild(i).name;
                if (name.Contains(STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.STARMAP_NAME_HINT)) || name.Contains("STARMAP"))
                {
                    starMapIndex = i;
                    break;
                }
            }

            return Mathf.Clamp(starMapIndex, 0, parent.childCount);
        }

        private static void DisableIfPresent(HierarchyReferences references, string key)
        {
            Component component = references.GetReference(key);
            if (component != null)
            {
                component.gameObject.SetActive(false);
            }
        }

        private static bool IsPrimaryManagementButton(KToggle toggle)
        {
            LocText label = toggle.GetComponentInChildren<LocText>(true);
            if (label == null)
            {
                return false;
            }

            string text = label.text;
            return text == global::STRINGS.UI.VITALS ||
                   text == global::STRINGS.UI.CONSUMABLES ||
                   text == global::STRINGS.UI.JOBS ||
                   text == global::STRINGS.UI.SCHEDULE ||
                   text == global::STRINGS.UI.SKILLS;
        }
    }
}
