using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private static void ApplyAutomationCardLayout(GameObject card, float minHeight)
        {
            LayoutElement layout = card.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = card.AddComponent<LayoutElement>();
            }

            layout.minWidth = 0f;
            layout.preferredWidth = 0f;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
            layout.minHeight = minHeight;
            layout.preferredHeight = -1f;

            if (card.GetComponent<ContentSizeFitter>() == null)
            {
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static void ApplyEqualAutomationCardLayout(GameObject card)
        {
            ApplyAutomationCardLayout(card, 0f);
        }

        private static void MakeProductionCardAutoHeight(GameObject card, float minHeight)
        {
            LayoutElement layout = card.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = card.AddComponent<LayoutElement>();
            }

            layout.minHeight = minHeight;
            layout.preferredHeight = -1f;

            if (card.GetComponent<ContentSizeFitter>() == null)
            {
                card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private static void SetFinePrintPreferredHeight(TextMeshProUGUI text, float height)
        {
            LayoutElement layout = text != null ? text.GetComponent<LayoutElement>() : null;
            if (layout == null)
            {
                return;
            }

            layout.minHeight = height;
            layout.preferredHeight = height;
        }

        private GameObject CreateProductionCard(string name, string title, float preferredHeight)
        {
            return CreateProductionCard(productionSettingsContent, name, title, preferredHeight);
        }

        private GameObject CreateProductionCard(Transform parent, string name, string title, float preferredHeight)
        {
            GameObject card = CreatePlainImage(name, parent, new Color(0.82f, 0.81f, 0.75f, 1f));
            LayoutElement layoutElement = card.AddComponent<LayoutElement>();
            if (preferredHeight > 0f)
            {
                layoutElement.preferredHeight = preferredHeight;
            }

            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI heading = CreateText("CardTitle", card.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            heading.color = new Color(0.18f, 0.19f, 0.18f, 1f);
            heading.fontStyle = FontStyles.Bold;
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            LayoutElement headingLayout = heading.gameObject.AddComponent<LayoutElement>();
            headingLayout.minHeight = 20f;
            headingLayout.preferredHeight = 20f;
            return card;
        }

        private TextMeshProUGUI CreateMetricTile(Transform parent, string label, string value, Color accent)
        {
            GameObject tile = CreatePlainImage("MetricTile", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            tile.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup layout = tile.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 4, 4);
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", tile.transform, label, 9, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.30f, 0.32f, 0.31f, 1f);
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            TextMeshProUGUI valueText = CreateText("Value", tile.transform, value, 11, TextAlignmentOptions.MidlineLeft);
            valueText.color = accent;
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
            return valueText;
        }

        private void CreateStatusStrip(Transform parent, string text, Color color)
        {
            GameObject strip = CreatePlainImage("StatusStrip", parent, color);
            LayoutElement stripLayout = strip.AddComponent<LayoutElement>();
            stripLayout.minHeight = 24f;
            stripLayout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateText("Status", strip.transform, text, 11, TextAlignmentOptions.Center);
            label.color = new Color(0.96f, 0.96f, 0.90f, 1f);
            label.fontStyle = FontStyles.Bold;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        private void CreateEnabledStatusStrip(Transform parent, bool enabled)
        {
            CreateStatusStrip(
                parent,
                enabled ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_ENABLED) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STATUS_DISABLED),
                StorageNetworkProductionSettingsStyle.GetEnabledStatusColor(enabled));
        }

        private void CreateToggleActionRow(Transform parent, string label, string value, System.Action onClick, bool currentlyEnabled)
        {
            CreateProductionActionRow(parent, label, value, onClick, currentlyEnabled ? KleiPinkStyle() : KleiBlueStyle());
        }

        private void CreateProductionActionRow(Transform parent, string label, string value, System.Action onClick, ColorStyleSetting buttonStyle = null)
        {
            GameObject row = CreatePlainImage("ActionRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 30f;
            rowLayout.preferredHeight = 30f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 5, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 24f;

            GameObject button = CreateStyledButton("Action", row.transform, value, onClick, buttonStyle ?? KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.minWidth = 126f;
            buttonLayout.flexibleWidth = 0f;
            buttonLayout.minHeight = 24f;
            buttonLayout.preferredHeight = 24f;
            buttonLayout.flexibleHeight = 0f;
        }

        private void CreateProductionReadOnlyRow(Transform parent, string label, string value)
        {
            GameObject row = CreatePlainImage("ReadOnlyRow", parent, new Color(0.76f, 0.76f, 0.70f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.20f, 0.21f, 0.20f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 24f;

            TextMeshProUGUI valueText = CreateText("Value", row.transform, value, 10, TextAlignmentOptions.MidlineRight);
            valueText.color = new Color(0.26f, 0.28f, 0.27f, 1f);
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
        }

        private TextMeshProUGUI CreateFinePrint(Transform parent, string text)
        {
            TextMeshProUGUI label = CreateText("FinePrint", parent, text, 10, TextAlignmentOptions.TopLeft);
            label.color = new Color(0.34f, 0.35f, 0.33f, 1f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = 18f;
            layout.preferredHeight = -1f;
            label.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return label;
        }
    }
}
