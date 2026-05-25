using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Core;
using StorageNetwork.UI;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace StorageNetwork.Patches
{
    public static class SideScreenPatch
    {
        [HarmonyPatch(typeof(ManagementMenu), "OnPrefabInit")]
        public static class ManagementMenuOnPrefabInitPatch
        {
            public static void Postfix(ManagementMenu __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    StorageNetworkManagementButton.Add(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add management menu button: " + exception);
                }
            }
        }

        private static class StorageNetworkManagementButton
        {
            private const string ButtonName = "StorageNetworkManagementButton";
            private const string ToggleText = "储存网络";
            private const string IconName = "storage_network_overlay";

            public static void Add(ManagementMenu menu)
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
                    label.SetText(ToggleText);
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
                toolTip.SetSimpleTooltip(STRINGS.UI.STORAGE_NETWORK.OVERVIEW_TOOLTIP);
                button.onClick += () =>
                {
                    button.isOn = false;
                    toggleState?.SetInactive();
                    KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));
                    StorageNetworkPanel.Show();
                };

                button.transform.SetSiblingIndex(GetInsertIndex(parent));
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
                    if (name.Contains("星图") || name.Contains("STARMAP"))
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
}
