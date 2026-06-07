using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {


        private void ShowDropDialog(Storage storage, string itemKey)
        {
            List<GameObject> items = FindStoredItems(storage, itemKey);
            if (storage == null || items.Count == 0)
            {
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                return;
            }

            string itemName = StorageNetworkStorageDisplay.GetStoredItemName(items[0]);
            float availableMass = GetStoredItemsMass(items);
            ShowAmountDialog(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.DROP_AMOUNT_TITLE),
                itemName,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.DROP_AVAILABLE), GameUtil.GetFormattedMass(availableMass)),
                availableMass,
                amount => DropSelectedItem(storage, itemKey, amount));
        }

        private void ShowTransferDialog(Storage source, string itemKey)
        {
            List<GameObject> items = FindStoredItems(source, itemKey);
            if (source == null || items.Count == 0)
            {
                RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                return;
            }

            List<Storage> targets = StorageNetworkStorageRules.GetNetworkStorageTargets(source)
                .Where(storage => storage.RemainingCapacity() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .OrderBy(storage => storage.GetProperName())
                .ToList();

            if (targets.Count == 0)
            {
                ShowMessageDialog(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_ITEM_TITLE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_TRANSFER_TARGET));
                return;
            }

            string itemName = StorageNetworkStorageDisplay.GetStoredItemName(items[0]);
            float sourceMass = GetStoredItemsMass(items);

            void ShowTransferAmountDialog(Storage destination)
            {
                float remainingCapacity = Mathf.Max(0f, destination.RemainingCapacity());
                float maxTransfer = Mathf.Min(sourceMass, remainingCapacity);
                string details = string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_CAPACITY_DETAILS),
                    GameUtil.GetFormattedMass(destination.MassStored()),
                    GameUtil.GetFormattedMass(destination.Capacity()),
                    GameUtil.GetFormattedMass(maxTransfer));

                ShowAmountDialog(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_AMOUNT_TITLE),
                    itemName,
                    details,
                    maxTransfer,
                    amount => TransferSelectedItem(source, itemKey, destination, amount),
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_PREFIX), destination.GetProperName()),
                    () => ShowTargetSelectionDialog(source, targets, destination, ShowTransferAmountDialog));
            }

            ShowTransferAmountDialog(targets[0]);
        }

        private void ShowTargetSelectionDialog(Storage source, List<Storage> targets, Storage selectedTarget, System.Action<Storage> onSelected)
        {
            CloseModal();
            modalRoot = CreateModalFrame(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_SELECTION_TITLE), 620f, 430f, out GameObject body);
            AddModalText(body.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_SELECTION_HEADER), 14, FontStyles.Bold);

            RectTransform targetContent = CreateModalScrollList(body.transform, 300f);

            foreach (Storage target in targets)
            {
                CreateTargetSelectionRow(targetContent, source, target, target == selectedTarget, () => onSelected?.Invoke(target));
            }

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), 90f, () => onSelected?.Invoke(selectedTarget));
        }

        private void ShowStorageSettingsDialog(Storage storage)
        {
            if (storage == null)
            {
                return;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (StorageNetworkStorageRules.IsProductionStorage(storage, enrollment) || StorageNetworkStorageRules.HasSettingsButtonTag(storage))
            {
                CloseModal();
                ShowProductionSettingsPanel(storage);
                return;
            }

            CloseModal();
            modalRoot = CreateModalFrame(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.BUILDING_SETTINGS_TITLE), 420f, 210f, out GameObject body);
            AddModalText(body.transform, storage.GetProperName(), 15, FontStyles.Bold);
            AddModalText(
                body.transform,
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_DETAILS),
                    GameUtil.GetFormattedMass(storage.MassStored()),
                    GameUtil.GetFormattedMass(storage.Capacity()),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity()))),
                12,
                FontStyles.Normal);

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CLOSE), 90f, CloseModal);
        }

        private void ShowAllMinionSettingsDialog(IEnumerable<StorageInfo> storages)
        {
            List<MinionIdentity> minions = storages?
                .Select(info => info?.Minion)
                .Where(minion => minion != null)
                .Distinct()
                .OrderBy(minion => minion.GetProperName())
                .ToList() ?? new List<MinionIdentity>();

            if (minions.Count == 0)
            {
                return;
            }

            int enabledCount = minions.Count(minion => Config.Instance.IsMinionAllowedRequestMaterialsFromNetwork(minion));
            CloseModal();
            modalRoot = CreateModalFrame(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL_MINION_SETTINGS_TITLE), 460f, 230f, out GameObject body);
            AddModalText(body.transform, string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL_MINION_SETTINGS_SUMMARY), enabledCount, minions.Count), 13, FontStyles.Bold);
            AddModalText(body.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL_MINION_SETTINGS_DESC), 12, FontStyles.Normal);

            GameObject actionRow = AddHorizontalRow(body.transform, 8f);
            AddFooterSpacer(actionRow.transform);
            AddModalButton(actionRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL_MINION_ENABLE), 120f, () => SetAllMinionMaterialRequests(minions, true));
            AddModalButton(actionRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL_MINION_DISABLE), 120f, () => SetAllMinionMaterialRequests(minions, false));

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), 90f, CloseModal);
        }

        private void SetAllMinionMaterialRequests(IEnumerable<MinionIdentity> minions, bool allowed)
        {
            foreach (MinionIdentity minion in minions)
            {
                Config.Instance.SetMinionAllowedRequestMaterialsFromNetwork(minion, allowed);
            }

            Config.Save();
            CloseModal();
            productionSettingsSignature = null;
            lastListSignature = null;
            UpdateProductionSettingsPanel(true);
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
        }

        private void ShowGeyserSettingsDialog(Geyser geyser)
        {
            ShowGeyserSettingsPanel(geyser);
        }

        private void ShowAmountDialog(
            string title,
            string itemName,
            string details,
            float maxAmount,
            System.Action<float> onConfirm,
            string targetButtonText = null,
            System.Action targetButtonAction = null)
        {
            CloseModal();
            maxAmount = Mathf.Max(0f, maxAmount);
            modalRoot = CreateModalFrame(title, 430f, 300f, out GameObject body);

            AddModalText(body.transform, itemName, 15, FontStyles.Bold);
            TextMeshProUGUI detailsText = AddModalText(body.transform, details, 12, FontStyles.Normal);
            detailsText.color = new Color(0.82f, 0.85f, 0.88f, 1f);

            if (!string.IsNullOrEmpty(targetButtonText) && targetButtonAction != null)
            {
                GameObject targetRow = AddHorizontalRow(body.transform, 6f);
                AddModalButton(targetRow.transform, targetButtonText, 240f, targetButtonAction);
                AddFooterSpacer(targetRow.transform);
            }

            float currentAmount = maxAmount;
            bool updating = false;
            TextMeshProUGUI valueLabel = AddModalText(body.transform, string.Empty, 13, FontStyles.Bold);
            KSlider slider = CreateAmountSlider(body.transform, maxAmount);
            KInputTextField input = CreateAmountInputRow(body.transform);
            StorageNetworkNumberInputField numberInput = input.GetComponent<StorageNetworkNumberInputField>();
            numberInput?.Configure(input, 0f, maxAmount, false);

            System.Action<float> setAmount = value =>
            {
                currentAmount = Mathf.Clamp(value, 0f, maxAmount);
                valueLabel.text = string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.AMOUNT_LABEL), GameUtil.GetFormattedMass(currentAmount));
                if (!updating)
                {
                    updating = true;
                    slider.value = currentAmount;
                    if (numberInput != null)
                    {
                        numberInput.SetAmount(currentAmount);
                    }
                    else
                    {
                        input.text = FormatAmount(currentAmount);
                    }

                    updating = false;
                }
            };

            slider.onValueChanged.AddListener(value =>
            {
                if (!updating)
                {
                    setAmount(value);
                }
            });

            if (numberInput != null)
            {
                numberInput.onEndEdit += () =>
                {
                    setAmount(numberInput.currentValue);
                };
            }

            setAmount(currentAmount);

            GameObject shortcutRow = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(shortcutRow.transform);
            AddModalButton(shortcutRow.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ALL), 80f, () => setAmount(maxAmount));

            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CANCEL), 80f, CloseModal);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), 90f, () =>
            {
                float finalAmount = Mathf.Clamp(currentAmount, 0f, maxAmount);
                CloseModal();
                if (finalAmount > 0f)
                {
                    onConfirm?.Invoke(finalAmount);
                }
            });
        }

        private void ShowMessageDialog(string title, string message)
        {
            CloseModal();
            modalRoot = CreateModalFrame(title, 360f, 170f, out GameObject body);
            AddModalText(body.transform, message, 13, FontStyles.Normal);
            GameObject footer = AddHorizontalRow(body.transform, 6f);
            AddFooterSpacer(footer.transform);
            AddModalButton(footer.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CONFIRM), 90f, CloseModal);
        }

        private GameObject CreateModalFrame(string title, float width, float height, out GameObject body)
        {
            GameObject overlay = new GameObject("ModalOverlay");
            overlay.transform.SetParent(transform, false);
            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            Stretch(overlayRect, 0f, 0f);
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.34f);

            GameObject dialog = CreateBox("Dialog", overlay.transform, new Color(0.22f, 0.24f, 0.28f, 0.98f));
            ApplyThinBoxSprite(dialog.GetComponent<Image>());
            RectTransform dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.anchoredPosition = Vector2.zero;
            dialogRect.sizeDelta = new Vector2(width, height);

            GameObject header = CreateBox("ModalHeader", dialog.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 0f, 0f, 0f, 30f);
            TextMeshProUGUI titleText = CreateText("Title", header.transform, title, 14, TextAlignmentOptions.MidlineLeft);
            titleText.fontStyle = FontStyles.Bold;
            Stretch(titleText.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateCloseIconButton("CloseButton", header.transform, CloseModal);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-4f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            body = CreateBox("ModalBody", dialog.transform, new Color(0.34f, 0.38f, 0.40f, 1f));
            SetStretch(body.GetComponent<RectTransform>(), 8f, 8f, 8f, 38f);
            VerticalLayoutGroup layout = body.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 7f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return overlay;
        }

        private static TextMeshProUGUI AddModalText(Transform parent, string text, int size, FontStyles style)
        {
            TextMeshProUGUI label = CreateText("ModalText", parent, text, size, TextAlignmentOptions.MidlineLeft);
            label.fontStyle = style;
            label.color = Color.white;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = text.Contains("\n") ? 48f : 24f;
            return label;
        }

        private static GameObject AddHorizontalRow(Transform parent, float spacing)
        {
            GameObject row = new GameObject("ModalRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 28f;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private RectTransform CreateModalScrollList(Transform parent, float height)
        {
            GameObject list = CreatePlainImage("TargetList", parent, new Color(0.22f, 0.25f, 0.27f, 1f));
            list.AddComponent<LayoutElement>().preferredHeight = height;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(list.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetStretch(viewportRect, 6f, 46f, 6f, 6f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(list.transform);

            ScrollRect scrollRect = list.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            ConfigureSmoothVerticalScroll(scrollRect, 26f);
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            list.AddComponent<ScrollWheelBlocker>();
            return content;
        }

        private static void AddFooterSpacer(Transform parent)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static GameObject AddModalButton(Transform parent, string text, float width, System.Action onClick)
        {
            GameObject button = CreateGameButton("ModalButton", parent, text, onClick);
            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = 24f;
            return button;
        }

        private void AddSettingToggleRow(Transform parent, string label, bool initialValue, System.Action<bool> onChanged)
        {
            bool current = initialValue;
            GameObject stateButton = null;
            System.Action refreshLabel = () =>
            {
                TextMeshProUGUI text = stateButton?.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = current ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON) : string.Empty;
                }

                KImage image = stateButton?.GetComponent<KImage>();
                if (image != null)
                {
                    Color baseColor = current
                        ? new Color(0.43f, 0.58f, 0.49f, 1f)
                        : new Color(0.23f, 0.27f, 0.31f, 1f);
                    image.colorStyleSetting = CreateColorStyle(baseColor, Lighten(baseColor, 0.05f), Darken(baseColor, 0.05f));
                }
            };

            GameObject row = CreatePlainImage("SettingRow", parent, new Color(0.26f, 0.30f, 0.32f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 28f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 0, 0);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 12, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.93f, 0.95f, 0.95f, 1f);
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            stateButton = CreateGameButton("SettingToggle", row.transform, string.Empty, () =>
            {
                current = !current;
                onChanged?.Invoke(current);
                refreshLabel();
                lastListSignature = null;
            });
            LayoutElement buttonLayout = stateButton.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 42f;
            buttonLayout.preferredHeight = 18f;
            refreshLabel();
        }

        private static KSlider CreateAmountSlider(Transform parent, float maxAmount)
        {
            GameObject sliderObject = new GameObject("AmountSlider");
            sliderObject.SetActive(false);
            sliderObject.transform.SetParent(parent, false);
            sliderObject.AddComponent<RectTransform>();
            sliderObject.AddComponent<LayoutElement>().preferredHeight = 32f;

            GameObject background = CreatePlainImage("Background", sliderObject.transform, Color.white);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            ApplyOniSliderFrame(background.GetComponent<Image>());

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = Vector2.zero;
            fillAreaRect.sizeDelta = Vector2.zero;

            GameObject fillStart = CreatePlainImage("Fill Start", fillArea.transform, Color.white);
            RectTransform fillStartRect = fillStart.GetComponent<RectTransform>();
            fillStartRect.anchorMin = Vector2.zero;
            fillStartRect.anchorMax = new Vector2(0f, 1f);
            fillStartRect.anchoredPosition = Vector2.zero;
            fillStartRect.sizeDelta = new Vector2(12f, 0f);
            ApplyOniSliderFillCap(fillStart.GetComponent<Image>());

            GameObject fill = CreatePlainImage("Fill", fillArea.transform, Color.white);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            ApplyOniSliderFill(fill.GetComponent<Image>());

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.anchoredPosition = Vector2.zero;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handle = CreatePlainImage("Handle", handleArea.transform, Color.white);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = new Vector2(0.9f, 0f);
            handleRect.sizeDelta = new Vector2(22.7f, -5.8f);
            ApplyOniSliderHandle(handle.GetComponent<Image>());

            KSlider slider = sliderObject.AddComponent<KSlider>();
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(0.001f, maxAmount);
            slider.value = maxAmount;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = null;
            slider.direction = Slider.Direction.LeftToRight;
            sliderObject.SetActive(true);
            return slider;
        }

        private static KInputTextField CreateAmountInputRow(Transform parent)
        {
            GameObject row = AddHorizontalRow(parent, 8f);
            TextMeshProUGUI label = CreateText("AmountInputLabel", row.transform, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.AMOUNT_INPUT), 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.92f, 0.94f, 0.95f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            return CreateAmountInput(row.transform);
        }

        private static KInputTextField CreateAmountInput(Transform parent)
        {
            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(
                parent,
                "AmountInput",
                string.Empty,
                150f,
                24f,
                13,
                TextAlignmentOptions.MidlineLeft,
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                new Vector2(8f, 2f),
                true);
            input.gameObject.AddComponent<StorageNetworkNumberInputField>().Configure(input, 0f, float.MaxValue, false);
            return input;
        }

        private static string FormatAmount(float amount)
        {
            return amount.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseAmount(string value, out float amount)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out amount) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }
    }
}

