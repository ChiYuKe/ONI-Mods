using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void AddTrackingProgressRow(Transform parent, ProductionOrderRecord record, Color color)
        {
            GameObject row = new GameObject("TrackingProgressRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 18f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            AddProgressBar(row.transform, Mathf.Clamp01(record.ProducedAtSubmit / Mathf.Max(record.RequestedAmount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)), color, 82f);

            TextMeshProUGUI amount = CreateOrderText("TrackingAmount", row.transform, string.Format("{0} / {1}", GameUtil.GetFormattedMass(record.ProducedAtSubmit), GameUtil.GetFormattedMass(record.RequestedAmount)), 9, TextAlignmentOptions.MidlineLeft);
            amount.color = NeutralTextColor();
            amount.fontStyle = FontStyles.Bold;
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void AddProgressBar(Transform parent, float progress, Color color, float width)
        {
            GameObject track = CreateRoundedOrderImage("ProgressTrack", parent, new Color(0.24f, 0.26f, 0.23f, 1f), "UISprite", "Background", "InputField");
            LayoutElement trackLayout = track.AddComponent<LayoutElement>();
            trackLayout.preferredWidth = width;
            trackLayout.preferredHeight = 10f;

            GameObject fillViewport = new GameObject("ProgressFillViewport", typeof(RectTransform));
            fillViewport.transform.SetParent(track.transform, false);
            RectTransform viewportRect = fillViewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = new Vector2(0f, 0f);
            viewportRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            fillViewport.AddComponent<RectMask2D>();

            GameObject fill = CreateRoundedOrderImage("ProgressFill", fillViewport.transform, color, "UISprite", "Background", "InputField");
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        private void AddTrackingStateBadge(Transform parent, string text, Color color, float minWidth, float maxWidth)
        {
            GameObject badge = CreateRoundedOrderImage("TrackingStateBadge", parent, color, "UISprite", "Background");
            LayoutElement layout = badge.AddComponent<LayoutElement>();
            layout.preferredWidth = Mathf.Clamp(EstimateTextWidth(text, 10) + 18f, minWidth, maxWidth);
            layout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateOrderText("TrackingStateText", badge.transform, text, 10, TextAlignmentOptions.Center);
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 4f, 0f);
        }

        private static float EstimateTextWidth(string text, int size)
        {
            if (string.IsNullOrEmpty(text))
            {
                return size;
            }

            float width = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                width += text[i] <= 0x7f ? size * 0.55f : size;
            }

            return width;
        }

        private static void AddTrackingSeparator(Transform parent, float width)
        {
            GameObject holder = new GameObject("TrackingSeparatorHolder");
            holder.transform.SetParent(parent, false);
            holder.AddComponent<RectTransform>();
            LayoutElement holderLayout = holder.AddComponent<LayoutElement>();
            holderLayout.preferredWidth = width;
            holderLayout.preferredHeight = 112f;

            GameObject separator = CreatePlainImage("TrackingSeparator", holder.transform, new Color(0.50f, 0.50f, 0.46f, 0.75f));
            RectTransform rect = separator.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 72f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AddTrackingDottedLine(Transform parent)
        {
            TextMeshProUGUI line = CreateOrderText("TrackingDottedLine", parent, "----------", 8, TextAlignmentOptions.Center);
            line.color = new Color(0.44f, 0.44f, 0.40f, 0.85f);
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
        }

        private static GameObject CreateRoundedOrderImage(string name, Transform parent, Color color, params string[] spriteNames)
        {
            GameObject gameObject = CreatePlainImage(name, parent, color);
            Image image = gameObject.GetComponent<Image>();
            foreach (string spriteName in spriteNames)
            {
                Sprite sprite = GetSpriteByName(spriteName);
                if (sprite == null)
                {
                    continue;
                }

                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                image.pixelsPerUnitMultiplier = 1f;
                break;
            }

            return gameObject;
        }

        private float AddTrackingLine(Transform parent, string text, int size, FontStyles style, Color color, float minHeight, int maxLines)
        {
            TextMeshProUGUI line = CreateOrderText("TrackingLine", parent, text, size, TextAlignmentOptions.TopLeft);
            line.color = color;
            line.fontStyle = style;
            line.richText = true;
            line.textWrappingMode = TextWrappingModes.Normal;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.maxVisibleLines = maxLines;

            float lineHeight = Mathf.Max(minHeight, size + 7f);
            float height = lineHeight * EstimateTextLineCount(text, maxLines, compactOrderWindow ? 18 : 24);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return height;
        }

        private static Color GetTrackingCardColor(ProductionOrderRecord record)
        {
            if (record.State == ProductionOrderState.Completed)
            {
                return new Color(0.82f, 0.84f, 0.76f, 1f);
            }

            if (record.State == ProductionOrderState.Abnormal)
            {
                return new Color(0.84f, 0.75f, 0.70f, 1f);
            }

            if (record.State == ProductionOrderState.WaitingMaterials)
            {
                return new Color(0.84f, 0.82f, 0.72f, 1f);
            }

            if (record.State == ProductionOrderState.Cancelled)
            {
                return new Color(0.76f, 0.76f, 0.70f, 1f);
            }

            return new Color(0.84f, 0.83f, 0.76f, 1f);
        }

        private static Sprite GetCancelActionSprite()
        {
            return GetSpriteByName("action_cancel") ??
                   GetSpriteByName("icon_action_cancel") ??
                   GetSpriteByName("action_cancel.png");
        }

        private static Color GetOrderStateColor(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.WaitingMaterials:
                    return WarningColor();
                case ProductionOrderState.Producing:
                    return NeutralBlue();
                case ProductionOrderState.Completed:
                    return PositiveColor();
                case ProductionOrderState.Abnormal:
                    return DangerColor();
                case ProductionOrderState.Cancelled:
                    return new Color(0.42f, 0.42f, 0.42f, 1f);
                default:
                    return NeutralTextColor();
            }
        }

        // Keeps building state colors consistent between the order tree and detail tree.
        private static Color GetBuildingStateColor(ProductionOrderQueueAssignment assignment)
        {
            switch (StorageNetworkOrderTrackingRules.GetBuildingStateKind(assignment))
            {
                case StorageNetworkOrderTrackingRules.BuildingStateKind.Running:
                    return PositiveColor();
                case StorageNetworkOrderTrackingRules.BuildingStateKind.WaitingMaterials:
                    return WarningColor();
                case StorageNetworkOrderTrackingRules.BuildingStateKind.Disabled:
                    return new Color(0.46f, 0.46f, 0.42f, 1f);
                case StorageNetworkOrderTrackingRules.BuildingStateKind.NoRecipe:
                    return new Color(0.40f, 0.44f, 0.48f, 1f);
                case StorageNetworkOrderTrackingRules.BuildingStateKind.Abnormal:
                    return DangerColor();
                default:
                    return NeutralBlue();
            }
        }

        private static string GetBuildingStateLabel(ProductionOrderQueueAssignment assignment)
        {
            return StorageNetworkOrderTrackingRules.GetBuildingStateLabel(assignment);
        }

        private static string BuildBuildingDetailLine(ProductionOrderQueueAssignment assignment, float progress)
        {
            ComplexFabricator fabricator = assignment?.Fabricator;
            return StorageNetworkOrderTrackingRules.BuildBuildingDetailLine(assignment, progress, fabricator != null ? StorageNetworkProductionSettingsText.GetProductionStateText(fabricator) : null);
        }
    }
}
