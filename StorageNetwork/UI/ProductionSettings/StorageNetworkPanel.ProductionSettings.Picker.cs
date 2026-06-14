using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void ShowProductionPicker(string title, List<ProductionPickerOption> options)
        {
            CloseProductionPicker();
            GameObject pickerParent = productionSettingsRoot != null && productionSettingsRoot.activeSelf
                ? productionSettingsRoot
                : null;
            if (pickerParent == null || options == null || options.Count == 0)
            {
                return;
            }

            productionPickerRoot = CreatePlainImage("ProductionPicker", pickerParent.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            productionPickerRoot.AddComponent<ScrollWheelBlocker>();
            RectTransform pickerRect = productionPickerRoot.GetComponent<RectTransform>();
            SetStretch(pickerRect, 10f, 10f, 8f, 78f);

            GameObject header = CreatePlainImage("PickerHeader", productionPickerRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            SetTopStretch(headerRect, 8f, 8f, 8f, 34f);
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("PickerTitle", header.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("PickerClose", header.transform, CloseProductionPicker);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject viewport = CreatePlainImage("PickerViewport", productionPickerRoot.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("PickerContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 24f);
            viewport.AddComponent<ScrollWheelBlocker>();

            foreach (ProductionPickerOption option in options)
            {
                CreateStorageOptionRow(content.transform, option.Title, option.Details, option.Selected, option.OnClick, option.IconTag);
            }

            CreateProductionPickerFooter(content.transform, options.Count);
        }

        private static void ShowStandaloneOutputFilterPicker(string title, List<ProductionPickerOption> options)
        {
            CloseStandaloneOutputFilterPicker();
            if (options == null || options.Count == 0)
            {
                return;
            }

            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkOutputFilterPicker");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            standaloneOutputFilterPickerRoot = root;
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.04f);
            blocker.raycastTarget = false;

            GameObject picker = CreatePlainImage("ProductionPicker", root.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            standaloneOutputFilterPickerWindow = picker;
            picker.AddComponent<ScrollWheelBlocker>();
            picker.AddComponent<StandaloneRightClickCloseHandler>();
            RectTransform pickerRect = picker.GetComponent<RectTransform>();
            pickerRect.anchorMin = new Vector2(0.5f, 0.5f);
            pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0.5f, 0.5f);
            pickerRect.sizeDelta = new Vector2(430f, 520f);
            pickerRect.anchoredPosition = Vector2.zero;
            StorageNetworkWindowDrag.TryApplyLayout("standaloneOutputFilterPicker", pickerRect, new Vector2(360f, 360f), new Vector2(820f, 900f));

            GameObject header = CreatePlainImage("PickerHeader", picker.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            RectTransform headerRect = header.GetComponent<RectTransform>();
            SetTopStretch(headerRect, 8f, 8f, 8f, 34f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(pickerRect, "standaloneOutputFilterPicker");
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI headerText = CreateText("PickerTitle", header.transform, title, 12, TextAlignmentOptions.MidlineLeft);
            headerText.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            headerText.fontStyle = FontStyles.Bold;
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.overflowMode = TextOverflowModes.Ellipsis;
            headerText.raycastTarget = false;
            headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("PickerClose", header.transform, CloseStandaloneOutputFilterPickerAction);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject viewport = CreatePlainImage("PickerViewport", picker.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("PickerContent");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 4f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 24f);
            viewport.AddComponent<ScrollWheelBlocker>();

            foreach (ProductionPickerOption option in options)
            {
                CreateStorageOptionRow(content.transform, option.Title, option.Details, option.Selected, option.OnClick, option.IconTag);
            }

            CreateProductionPickerFooter(content.transform, options.Count);
        }

        private static bool CloseStandaloneOutputFilterPicker()
        {
            if (standaloneOutputFilterPickerRoot != null)
            {
                Destroy(standaloneOutputFilterPickerRoot);
                standaloneOutputFilterPickerRoot = null;
                standaloneOutputFilterPickerWindow = null;
                standaloneRightClickCloseCandidate = false;
                return true;
            }

            return false;
        }

        private static void CloseStandaloneOutputFilterPickerAction()
        {
            CloseStandaloneOutputFilterPicker();
        }

        private void CloseProductionPicker()
        {
            if (productionPickerRoot != null)
            {
                Destroy(productionPickerRoot);
                productionPickerRoot = null;
            }
        }

        private static void CreateStorageOptionRow(Transform parent, string title, string details, bool selected, System.Action onClick, Tag? iconTag = null)
        {
            GameObject row = new GameObject("StorageOptionRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            KImage background = row.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = selected
                ? KleiPinkStyle()
                : KleiBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;
            row.AddComponent<LayoutElement>().preferredHeight = 42f;
            KButton button = row.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            if (iconTag.HasValue)
            {
                GameObject iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(row.transform, false);
                iconObject.AddComponent<RectTransform>();
                LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 22f;
                iconLayout.minWidth = 22f;
                Image icon = iconObject.AddComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                var uiSprite = Def.GetUISprite(iconTag.Value, "ui", false);
                icon.sprite = uiSprite.first;
                icon.color = uiSprite.first != null ? uiSprite.second : Color.clear;
            }

            TextMeshProUGUI titleText = CreateText("Title", row.transform, title, 11, TextAlignmentOptions.MidlineLeft);
            titleText.color = selected ? new Color(0.98f, 0.96f, 0.90f, 1f) : new Color(0.90f, 0.92f, 0.95f, 1f);
            titleText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI detailText = CreateText("Details", row.transform, details, 9, TextAlignmentOptions.MidlineLeft);
            detailText.color = selected ? new Color(0.88f, 0.84f, 0.78f, 1f) : new Color(0.70f, 0.73f, 0.78f, 1f);
            detailText.textWrappingMode = TextWrappingModes.NoWrap;
            detailText.overflowMode = TextOverflowModes.Ellipsis;
            detailText.gameObject.AddComponent<LayoutElement>().preferredWidth = 170f;
        }

        private static void CreateProductionPickerFooter(Transform parent, int optionCount)
        {
            GameObject footer = CreatePlainImage("PickerFooter", parent, new Color(0.68f, 0.68f, 0.61f, 1f));
            LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
            footerLayout.minHeight = 82f;
            footerLayout.preferredHeight = 82f;

            VerticalLayoutGroup layout = footer.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI countText = CreateText(
                "PickerFooterCount",
                footer.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PICKER_OPTION_COUNT), Mathf.Max(0, optionCount - 1)),
                11,
                TextAlignmentOptions.MidlineLeft);
            countText.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            countText.fontStyle = FontStyles.Bold;
            countText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI hintText = CreateText(
                "PickerFooterHint",
                footer.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PICKER_POLICY_HINT),
                10,
                TextAlignmentOptions.TopLeft);
            hintText.color = new Color(0.30f, 0.31f, 0.30f, 1f);
            hintText.textWrappingMode = TextWrappingModes.Normal;
            hintText.overflowMode = TextOverflowModes.Ellipsis;
            hintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        }

        private sealed class StandaloneRightClickCloseHandler : MonoBehaviour
        {
            private bool closeCandidate;
            private Vector2 startPosition;

            private void Update()
            {
                Vector2 mousePosition = KInputManager.GetMousePos();
                if (Input.GetMouseButtonDown(1))
                {
                    closeCandidate = true;
                    startPosition = mousePosition;
                    return;
                }

                if (!Input.GetMouseButtonUp(1))
                {
                    return;
                }

                bool shouldClose = closeCandidate &&
                    (mousePosition - startPosition).sqrMagnitude <= RightClickDragThresholdPixels * RightClickDragThresholdPixels;
                closeCandidate = false;
                if (shouldClose)
                {
                    CloseStandaloneOutputFilterPicker();
                }
            }
        }

    }
}
