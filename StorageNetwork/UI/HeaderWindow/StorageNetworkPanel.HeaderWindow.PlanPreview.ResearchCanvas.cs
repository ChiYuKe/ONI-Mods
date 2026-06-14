using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private static void ApplyResearchNodeRect(GameObject node, Vector2 position, Vector2 size)
        {
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private void AddResearchIconSlot(Transform parent, Sprite sprite, float size)
        {
            GameObject slot = CreatePlainImage("ResearchIconSlot", parent, new Color(0.93f, 0.92f, 0.87f, 1f));
            LayoutElement layout = slot.AddComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            layout.minWidth = size;
            layout.minHeight = size;
            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2f, 2f);
            iconRect.offsetMax = new Vector2(-2f, -2f);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void AddResearchIconSlot(Transform parent, Sprite sprite, Vector2 topLeft, float size)
        {
            GameObject slot = CreatePlainImage("ResearchIconSlot", parent, new Color(0.93f, 0.92f, 0.87f, 1f));
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            slotRect.sizeDelta = new Vector2(size, size);

            GameObject icon = new GameObject("Icon");
            icon.transform.SetParent(slot.transform, false);
            RectTransform iconRect = icon.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(2f, 2f);
            iconRect.offsetMax = new Vector2(-2f, -2f);
            Image image = icon.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        private void AddResearchProgressLine(Transform parent, float y, string text, Color color)
        {
            GameObject bar = CreatePlainImage("ResearchProgress", parent, new Color(0.61f, 0.61f, 0.57f, 1f));
            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(9f, -y - 13f);
            rect.offsetMax = new Vector2(-9f, -y);

            TextMeshProUGUI label = CreateOrderText("ProgressText", bar.transform, text, 7, TextAlignmentOptions.Center);
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(label.rectTransform(), 2f, 0f);
        }

        private static void AddResearchConnector(Transform parent, float x1, float y1, float x2, float y2, Color color)
        {
            if (Mathf.Abs(x1 - x2) > 0.5f)
            {
                AddResearchLine(parent, new Vector2(Mathf.Min(x1, x2), y1), new Vector2(Mathf.Abs(x2 - x1), 2f), color);
            }

            if (Mathf.Abs(y1 - y2) > 0.5f)
            {
                AddResearchLine(parent, new Vector2(x2, Mathf.Min(y1, y2)), new Vector2(2f, Mathf.Abs(y2 - y1)), color);
            }
        }

        private static void AddResearchVerticalBus(Transform parent, float x, List<float> yCenters, Color color)
        {
            if (yCenters == null || yCenters.Count <= 1)
            {
                return;
            }

            float minY = yCenters.Min();
            float maxY = yCenters.Max();
            AddResearchLine(parent, new Vector2(x, minY), new Vector2(2f, Mathf.Max(2f, maxY - minY)), color);
        }

        private static void AddResearchLine(Transform parent, Vector2 topLeft, Vector2 size, Color color)
        {
            GameObject line = CreatePlainImage("ResearchConnector", parent, color);
            line.transform.SetAsFirstSibling();
            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rect.sizeDelta = size;
        }

        private static Sprite GetFabricatorSprite(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.gameObject == null)
            {
                return Assets.GetSprite("unknown");
            }

            KPrefabID prefabId = fabricator.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                var uiSprite = Def.GetUISprite(prefabId.PrefabID(), "ui", false);
                if (uiSprite.first != null)
                {
                    return uiSprite.first;
                }
            }

            return Assets.GetSprite("unknown");
        }
    }
}
