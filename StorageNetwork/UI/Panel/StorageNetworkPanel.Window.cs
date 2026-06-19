using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void BuildWindow(Transform parent)
        {
            GameObject window = CreateBox("Window", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(window.GetComponent<Image>());
            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(960f, 850f); 
            StorageNetworkWindowDrag.TryApplyLayout("mainWindow", windowRect, new Vector2(760f, 520f), new Vector2(1400f, 1100f));

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(windowRect, "mainWindow");

            TextMeshProUGUI title = CreateText("Title", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TITLE), 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;
            Stretch(title.rectTransform(), 12f, 0f);
            title.rectTransform().offsetMax = new Vector2(-210f, 0f);

            GameObject enrollableButton = CreateGameButton("EnrollableButton", header.transform, string.Empty, ShowEnrollableBuildingsDialog);
            RectTransform enrollableRect = enrollableButton.GetComponent<RectTransform>();
            enrollableRect.anchorMin = new Vector2(0f, 0.5f);
            enrollableRect.anchorMax = new Vector2(0f, 0.5f);
            enrollableRect.pivot = new Vector2(0f, 0.5f);
            enrollableRect.anchoredPosition = new Vector2(92f, 0f);
            enrollableRect.sizeDelta = new Vector2(72f, 22f);
            AddButtonIconLabel(enrollableButton.transform, "storage_network_overlay", "+", Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_SHORT));
            ToolTip enrollableTooltip = enrollableButton.AddComponent<ToolTip>();
            enrollableTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_BUTTON_TOOLTIP);

            GameObject orderButton = CreateGameButton("OrderCenterButton", header.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_BUTTON), ToggleHeaderWindow);
            RectTransform orderRect = orderButton.GetComponent<RectTransform>();
            orderRect.anchorMin = new Vector2(0f, 0.5f);
            orderRect.anchorMax = new Vector2(0f, 0.5f);
            orderRect.pivot = new Vector2(0f, 0.5f);
            orderRect.anchoredPosition = new Vector2(170f, 0f);
            orderRect.sizeDelta = new Vector2(72f, 22f);
            ToolTip orderTooltip = orderButton.AddComponent<ToolTip>();
            orderTooltip.toolTip = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_TOOLTIP);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, Close);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-4f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            GameObject content = CreateBox("Content", window.transform, new Color(0.88f, 0.89f, 0.91f, 0.98f));
            SetStretch(content.GetComponent<RectTransform>(), 8f, 8f, 8f, 42f);

            GameObject summary = CreateBox("Summary", content.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(summary.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            summaryText = CreateText("SummaryText", summary.transform, string.Empty, 14, TextAlignmentOptions.TopLeft);
            summaryText.lineSpacing = 4f;
            summaryText.rectTransform().anchorMin = Vector2.zero;
            summaryText.rectTransform().anchorMax = Vector2.one;
            summaryText.rectTransform().offsetMin = new Vector2(12f, 7f);
            summaryText.rectTransform().offsetMax = new Vector2(-256f, -7f);
            CreateMainWorldFilter(summary.transform);
            CreateCategorySummaryButton(summary.transform);

            GameObject healthBar = CreateBox("HealthBar", content.transform, new Color(0.74f, 0.75f, 0.69f, 1f));
            SetTopStretch(healthBar.GetComponent<RectTransform>(), 8f, 8f, 64f, 36f);
            HorizontalLayoutGroup healthLayout = healthBar.AddComponent<HorizontalLayoutGroup>();
            healthLayout.padding = new RectOffset(8, 8, 4, 4);
            healthLayout.spacing = 6f;
            healthLayout.childAlignment = TextAnchor.MiddleLeft;
            healthLayout.childControlWidth = true;
            healthLayout.childControlHeight = true;
            healthLayout.childForceExpandWidth = true;
            healthLayout.childForceExpandHeight = true;
            healthContent = healthBar.GetComponent<RectTransform>();

            GameObject list = CreateBox("List", content.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(list.GetComponent<RectTransform>(), 8f, 8f, 8f, 106f);

            HorizontalLayoutGroup listColumns = list.AddComponent<HorizontalLayoutGroup>();
            listColumns.padding = new RectOffset(8, 8, 8, 8);
            listColumns.spacing = 8f;
            listColumns.childAlignment = TextAnchor.UpperLeft;
            listColumns.childControlWidth = true;
            listColumns.childControlHeight = true;
            listColumns.childForceExpandWidth = false;
            listColumns.childForceExpandHeight = true;

            GameObject categories = CreateBox("Categories", list.transform, new Color(0.74f, 0.73f, 0.68f, 1f));
            LayoutElement categoryLayout = categories.AddComponent<LayoutElement>();
            categoryLayout.minWidth = 130f;
            categoryLayout.preferredWidth = 130f;
            categoryLayout.flexibleWidth = 0f;
            AddVerticalContainer(categories, 4f, 4, 4, 4, 4);

            GameObject categoryContentObject = new GameObject("CategoryContent");
            categoryContentObject.transform.SetParent(categories.transform, false);
            categoryContent = categoryContentObject.AddComponent<RectTransform>();
            categoryContent.anchorMin = new Vector2(0f, 1f);
            categoryContent.anchorMax = new Vector2(1f, 1f);
            categoryContent.pivot = new Vector2(0.5f, 1f);
            categoryContent.offsetMin = Vector2.zero;
            categoryContent.offsetMax = Vector2.zero;
            AddVerticalContainer(categoryContentObject, 5f, 0, 0, 0, 0);
            categoryContentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject rightList = CreateBox("RightList", list.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            rightList.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(rightList.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetStretch(viewportRect, 8f, 22f, 8f, 8f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            listContent = contentObject.AddComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero;
            listContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup listLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 5f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(rightList.transform);

            ScrollRect scrollRect = rightList.AddComponent<ScrollRect>();
            listScrollRect = scrollRect;
            scrollRect.viewport = viewportRect;
            scrollRect.content = listContent;
            ConfigureSmoothVerticalScroll(scrollRect, 30f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 4f;

            rightList.AddComponent<ScrollWheelBlocker>();
        }

    }
}
