using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private static void AddIndentGuide(Transform parent, Color color)
        {
            GameObject guide = CreatePlainImage("IndentGuide", parent, color);
            LayoutElement layout = guide.AddComponent<LayoutElement>();
            layout.preferredWidth = 3f;
            layout.preferredHeight = 22f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        private static void AddFixedSpacer(Transform parent, float width)
        {
            GameObject spacer = new GameObject("FixedSpacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().preferredWidth = width;
        }

        private void AddLedgerValue(Transform parent, string value, Color color, float width)
        {
            TextMeshProUGUI text = CreateOrderText("LedgerValue", parent, value, 8, TextAlignmentOptions.MidlineLeft);
            text.color = color;
            text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.flexibleWidth = 0f;
        }

        private void AddCompactStat(Transform parent, string label, string value, Color valueColor, float width)
        {
            GameObject stat = new GameObject("CompactStat");
            stat.transform.SetParent(parent, false);
            stat.AddComponent<RectTransform>();
            LayoutElement statLayout = stat.AddComponent<LayoutElement>();
            statLayout.preferredWidth = width;
            statLayout.flexibleWidth = 0f;
            AddVerticalContainer(stat, 0f, 0, 0, 0, 0);

            TextMeshProUGUI labelText = CreateOrderText("StatLabel", stat.transform, label, 7, TextAlignmentOptions.MidlineLeft);
            labelText.color = MutedTextColor();
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

            TextMeshProUGUI valueText = CreateOrderText("StatValue", stat.transform, value, 8, TextAlignmentOptions.MidlineLeft);
            valueText.color = valueColor;
            valueText.fontStyle = FontStyles.Bold;
            valueText.textWrappingMode = TextWrappingModes.NoWrap;
            valueText.overflowMode = TextOverflowModes.Ellipsis;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;
        }

        private void AddStatusBadge(Transform parent, string text, Color color, float width)
        {
            GameObject badge = CreatePlainImage("StatusBadge", parent, new Color(0.69f, 0.70f, 0.64f, 1f));
            LayoutElement badgeLayout = badge.AddComponent<LayoutElement>();
            badgeLayout.preferredWidth = width;
            badgeLayout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateOrderText("StatusBadgeText", badge.transform, text, 8, TextAlignmentOptions.Center);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 4f, 0f);
        }
    }
}
