using System.Linq;
using StorageNetwork.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private StorageNetworkSettingsPanelBuilder CreateAddonSettingsPanelBuilder()
        {
            return new StorageNetworkSettingsPanelBuilder(
                SetAddonSettingsPanelTitle,
                (name, title, preferredHeight) => CreateProductionCard(productionSettingsContent, name, title, preferredHeight),
                CreateAddonSettingsCard,
                (name, preferredHeight, spacing) => CreateAddonSettingsHorizontalGroup(productionSettingsContent, name, preferredHeight, spacing),
                CreateAddonSettingsHorizontalGroup,
                (parent, label, value, accent) => CreateMetricTile(parent, label, value, accent),
                (parent, text, color) =>
                {
                    if (parent != null)
                    {
                        CreateStatusStrip(parent, text, color);
                    }
                },
                (parent, label, value) =>
                {
                    if (parent != null)
                    {
                        CreateProductionReadOnlyRow(parent, label, value);
                    }
                },
                CreateAddonSettingsHeading,
                (parent, text) =>
                {
                    if (parent != null)
                    {
                        CreateFinePrint(parent, text);
                    }
                },
                (parent, label, buttonText, onClick) =>
                {
                    if (parent != null)
                    {
                        CreateProductionActionRow(parent, label, buttonText, onClick);
                    }
                },
                (parent, label, buttonText, onClick, currentlyEnabled) =>
                {
                    if (parent != null)
                    {
                        CreateToggleActionRow(parent, label, buttonText, onClick, currentlyEnabled);
                    }
                });
        }

        private GameObject CreateAddonSettingsCard(Transform parent, string name, string title, float preferredHeight)
        {
            GameObject card = CreateProductionCard(parent, name, title, preferredHeight);
            if (parent != null && parent.GetComponent<HorizontalLayoutGroup>() != null)
            {
                ApplyEqualAutomationCardLayout(card);
            }

            return card;
        }

        private Transform CreateAddonSettingsHorizontalGroup(Transform parent, string name, float preferredHeight, float spacing)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            group.AddComponent<RectTransform>();

            LayoutElement layoutElement = group.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layoutElement.minHeight = preferredHeight;
                layoutElement.preferredHeight = preferredHeight;
            }
            else
            {
                layoutElement.preferredHeight = -1f;
                if (group.GetComponent<ContentSizeFitter>() == null)
                {
                    group.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
            }

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return group.transform;
        }

        private void CreateAddonSettingsHeading(Transform parent, string text)
        {
            if (parent == null)
            {
                return;
            }

            TextMeshProUGUI heading = CreateText("AddonHeading", parent, text ?? string.Empty, 16, TextAlignmentOptions.MidlineLeft);
            heading.color = StorageNetworkPanelPalette.HeadingText;
            heading.fontStyle = FontStyles.Bold;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.overflowMode = TextOverflowModes.Ellipsis;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        }

        private void SetAddonSettingsPanelTitle(string titleText)
        {
            TextMeshProUGUI title = productionSettingsRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "ProductionSettingsTitle");
            if (title != null)
            {
                title.text = titleText ?? string.Empty;
            }
        }
    }
}
