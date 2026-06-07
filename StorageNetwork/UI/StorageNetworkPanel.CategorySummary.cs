using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private readonly Dictionary<string, Queue<MassSample>> categorySummarySamples = new Dictionary<string, Queue<MassSample>>();

        private void CreateCategorySummaryButton(Transform parent)
        {
            GameObject button = CreateGameButton("CategorySummaryButton", parent, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_BUTTON), ToggleCategorySummaryPanel);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -14f);
            rect.sizeDelta = new Vector2(56f, 26f);

            ToolTip tooltip = button.AddComponent<ToolTip>();
            tooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TOOLTIP);
        }

        private void ToggleCategorySummaryPanel()
        {
            if (categorySummaryRoot != null && categorySummaryRoot.activeSelf)
            {
                CloseCategorySummaryPanel();
                return;
            }

            ShowCategorySummaryPanel();
        }

        private void ShowCategorySummaryPanel()
        {
            EnsureCategorySummaryPanel();
            categorySummarySignature = null;
            categorySummaryRoot.SetActive(true);
            categorySummaryRoot.transform.SetAsLastSibling();
            UpdateCategorySummaryPanel();
        }

        private void CloseCategorySummaryPanel()
        {
            if (categorySummaryRoot != null)
            {
                categorySummaryRoot.SetActive(false);
            }

            categorySummarySignature = null;
        }

        private void EnsureCategorySummaryPanel()
        {
            if (categorySummaryRoot != null)
            {
                return;
            }

            categorySummaryRoot = CreateBox("CategorySummaryPanel", windowRect, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(categorySummaryRoot.GetComponent<Image>());
            RectTransform panelRect = categorySummaryRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(488f, 0f);
            panelRect.sizeDelta = new Vector2(320f, 720f);

            GameObject header = CreateBox("Header", categorySummaryRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            TextMeshProUGUI title = CreateText("Title", header.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            title.name = "CategorySummaryTitle";
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = 2f;
            Stretch(title.rectTransform(), 10f, 7f);
            categorySummaryTitle = title;

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseCategorySummaryPanel);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4f, -4f);
            closeRect.sizeDelta = new Vector2(22f, 20f);

            GameObject viewport = CreateBox("Viewport", categorySummaryRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 24f, 10f, 70f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            categorySummaryContent = content.AddComponent<RectTransform>();
            categorySummaryRows = new StorageNetworkKeyedRowCache(categorySummaryContent);
            categorySummaryContent.anchorMin = new Vector2(0f, 1f);
            categorySummaryContent.anchorMax = new Vector2(1f, 1f);
            categorySummaryContent.pivot = new Vector2(0.5f, 1f);
            categorySummaryContent.offsetMin = Vector2.zero;
            categorySummaryContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(categorySummaryRoot.transform, 70f, 10f);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = categorySummaryContent;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            viewport.AddComponent<ScrollWheelBlocker>();

            categorySummaryRoot.SetActive(false);
        }

        private void UpdateCategorySummaryPanel()
        {
            if (categorySummaryRoot == null || !categorySummaryRoot.activeSelf || categorySummaryContent == null)
            {
                return;
            }

            List<Storage> storages = new List<Storage>();
            if (currentSnapshot?.Storages != null)
            {
                foreach (StorageInfo info in currentSnapshot.Storages)
                {
                    Storage storage = info?.Storage;
                    if (storage != null && GetStorageCategoryKey(info) == selectedCategoryKey)
                    {
                        storages.Add(storage);
                    }
                }
            }

            string categoryName = StorageCategories.GetName(selectedCategoryKey);
            float storedKg = 0f;
            Dictionary<string, ItemTotalAccumulator> totalsByKey = new Dictionary<string, ItemTotalAccumulator>();
            foreach (Storage storage in storages)
            {
                if (storage == null)
                {
                    continue;
                }

                storedKg += storage.MassStored();
                if (storage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string key = StorageItemUtility.GetStoredItemKey(item);
                    float mass = GetStoredItemMass(item);
                    if (totalsByKey.TryGetValue(key, out ItemTotalAccumulator accumulator))
                    {
                        accumulator.MassKg += mass;
                        totalsByKey[key] = accumulator;
                    }
                    else
                    {
                        totalsByKey.Add(key, new ItemTotalAccumulator(key, GetStoredItemName(item), mass, item));
                    }
                }
            }

            SetCategorySummaryTitle(categoryName, storages.Count, storedKg);

            List<StorageNetworkCategorySummaryItemTotal> totals = new List<StorageNetworkCategorySummaryItemTotal>(totalsByKey.Count);
            foreach (ItemTotalAccumulator total in totalsByKey.Values)
            {
                totals.Add(new StorageNetworkCategorySummaryItemTotal(total.Key, total.Name, total.MassKg, total.Representative));
            }

            totals.Sort((left, right) =>
            {
                int compare = right.MassKg.CompareTo(left.MassKg);
                return compare != 0 ? compare : string.Compare(left.Name, right.Name, System.StringComparison.CurrentCulture);
            });

            UpdateCategorySummarySamples(selectedCategoryKey, totals);
            string signature = StorageNetworkCategorySummarySignature.Build(selectedCategoryKey, storages, totals);
            if (signature == categorySummarySignature)
            {
                return;
            }

            categorySummarySignature = signature;
            categorySummaryRows ??= new StorageNetworkKeyedRowCache(categorySummaryContent);
            categorySummaryRows.Begin();

            if (totals.Count == 0)
            {
                UpdateSummaryText("empty", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_EMPTY), 12, FontStyles.Normal, 26f);
                categorySummaryRows.Commit();
                ForceCategorySummaryLayout();
                return;
            }

            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                UpdateCategorySummaryItemRow(total, GetMassTrendPerCycle(selectedCategoryKey, total.Key));
            }

            categorySummaryRows.Commit();
            ForceCategorySummaryLayout();
        }

        private void ClearCategorySummaryContent()
        {
            categorySummaryRows?.ClearDestroy();
            categorySummaryRows = categorySummaryContent != null ? new StorageNetworkKeyedRowCache(categorySummaryContent) : null;
        }

        private void SetCategorySummaryTitle(string categoryName, int storageCount, float storedKg)
        {
            if (categorySummaryTitle != null)
            {
                categorySummaryTitle.text = string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SUMMARY_TITLE_LINE),
                    categoryName,
                    storageCount,
                    GameUtil.GetFormattedMass(storedKg));
            }
        }

        private void AddSummaryText(string text, int size, FontStyles style, float height)
        {
            TextMeshProUGUI label = CreateText("SummaryText", categorySummaryContent, text, size, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = style;
            label.richText = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private void UpdateSummaryText(string key, string text, int size, FontStyles style, float height)
        {
            GameObject row = categorySummaryRows.Use("summary:" + key, () =>
            {
                TextMeshProUGUI created = CreateText("SummaryText", categorySummaryContent, text, size, TextAlignmentOptions.MidlineLeft);
                created.color = new Color(0.18f, 0.19f, 0.19f, 1f);
                created.richText = true;
                created.gameObject.AddComponent<LayoutElement>();
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
                label.fontSize = size;
                label.fontStyle = style;
            }

            LayoutElement layout = row.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = height;
            }
        }

        private void UpdateCategorySummaryItemRow(StorageNetworkCategorySummaryItemTotal total, float? trendKgPerCycle)
        {
            GameObject row = categorySummaryRows.Use("item:" + total.Key, () => CreateCategorySummaryItemRow());
            CategorySummaryRowView view = row.GetComponent<CategorySummaryRowView>();
            if (view == null)
            {
                return;
            }

            SetStoredItemIcon(view.Icon, total.Representative);
            view.Name.text = total.Name;
            view.Mass.text = GameUtil.GetFormattedMass(total.MassKg);
            view.Trend.text = StorageNetworkCategorySummaryTrend.Format(trendKgPerCycle);
            view.Trend.color = StorageNetworkCategorySummaryTrend.GetColor(trendKgPerCycle);
        }

        private GameObject CreateCategorySummaryItemRow()
        {
            GameObject row = CreatePlainImage("SummaryItemRow", categorySummaryContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 1, 1);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(row.transform, false);
            iconObject.AddComponent<RectTransform>();
            iconObject.AddComponent<LayoutElement>().preferredWidth = 20f;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            TextMeshProUGUI name = CreateText("Name", row.transform, string.Empty, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Mass", row.transform, string.Empty, 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;

            TextMeshProUGUI trend = CreateText("Trend", row.transform, string.Empty, 10, TextAlignmentOptions.MidlineRight);
            trend.textWrappingMode = TextWrappingModes.NoWrap;
            trend.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;

            row.AddComponent<CategorySummaryRowView>().Configure(icon, name, mass, trend);
            return row;
        }

        private void ForceCategorySummaryLayout()
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(categorySummaryContent);
        }

        private void UpdateCategorySummarySamples(string categoryKey, IEnumerable<StorageNetworkCategorySummaryItemTotal> totals)
        {
            float currentCycle = GetCurrentCycleTime();
            foreach (StorageNetworkCategorySummaryItemTotal total in totals)
            {
                string key = BuildSampleKey(categoryKey, total.Key);
                if (!categorySummarySamples.TryGetValue(key, out Queue<MassSample> samples))
                {
                    samples = new Queue<MassSample>();
                    categorySummarySamples.Add(key, samples);
                }

                samples.Enqueue(new MassSample(currentCycle, total.MassKg));
                while (samples.Count > 1 && currentCycle - samples.Peek().CycleTime > 1f)
                {
                    samples.Dequeue();
                }
            }
        }

        private float? GetMassTrendPerCycle(string categoryKey, string itemKey)
        {
            if (!categorySummarySamples.TryGetValue(BuildSampleKey(categoryKey, itemKey), out Queue<MassSample> samples) ||
                samples.Count < 2)
            {
                return null;
            }

            MassSample first = samples.Peek();
            MassSample last = samples.Last();
            float elapsedCycles = last.CycleTime - first.CycleTime;
            if (elapsedCycles < 0.01f)
            {
                return null;
            }

            return (last.MassKg - first.MassKg) / elapsedCycles;
        }

        private static string BuildSampleKey(string categoryKey, string itemKey)
        {
            return (categoryKey ?? string.Empty) + "|" + (itemKey ?? string.Empty);
        }

        private static float GetCurrentCycleTime()
        {
            return GameClock.Instance != null ? GameClock.Instance.GetTimeInCycles() : Time.time / 600f;
        }

        private readonly struct MassSample
        {
            public MassSample(float cycleTime, float massKg)
            {
                CycleTime = cycleTime;
                MassKg = massKg;
            }

            public float CycleTime { get; }

            public float MassKg { get; }
        }

        private struct ItemTotalAccumulator
        {
            public ItemTotalAccumulator(string key, string name, float massKg, GameObject representative)
            {
                Key = key;
                Name = name;
                MassKg = massKg;
                Representative = representative;
            }

            public string Key { get; }

            public string Name { get; }

            public float MassKg { get; set; }

            public GameObject Representative { get; }
        }

        private sealed class CategorySummaryRowView : MonoBehaviour
        {
            public Image Icon { get; private set; }

            public TextMeshProUGUI Name { get; private set; }

            public TextMeshProUGUI Mass { get; private set; }

            public TextMeshProUGUI Trend { get; private set; }

            public void Configure(Image icon, TextMeshProUGUI name, TextMeshProUGUI mass, TextMeshProUGUI trend)
            {
                Icon = icon;
                Name = name;
                Mass = mass;
                Trend = trend;
            }
        }
    }
}
