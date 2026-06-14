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
            if (StorageNetworkStorageRules.IsProductionStorage(storage, enrollment) ||
                StorageNetworkStorageRules.IsConfigurablePort(storage) ||
                storage.GetComponent<StorageNetworkColdStorageCooling>() != null ||
                StorageNetworkStorageRules.HasSettingsButtonTag(storage))
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

        private void ShowGeyserSettingsDialog(Geyser geyser)
        {
            ShowGeyserSettingsPanel(geyser);
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

    }
}

