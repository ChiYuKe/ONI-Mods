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
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {
        private void ShowProductionSettingsPanel(Storage storage)
        {
            productionSettingsStorage = storage;
            EnsureProductionSettingsPanel();
            productionSettingsRoot.SetActive(true);
            UpdateProductionSettingsPanel();
        }

        private void CloseProductionSettingsPanel()
        {
            productionSettingsStorage = null;
            if (productionSettingsRoot != null)
            {
                productionSettingsRoot.SetActive(false);
            }
        }

        private void EnsureProductionSettingsPanel()
        {
            if (productionSettingsRoot != null)
            {
                return;
            }

            productionSettingsRoot = CreateBox("ProductionSettingsPanel", transform, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            productionSettingsRoot.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(productionSettingsRoot.GetComponent<Image>());
            RectTransform panelRect = productionSettingsRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(488f, -142f);
            panelRect.sizeDelta = new Vector2(590f, 480f);

            GameObject header = CreateBox("Header", productionSettingsRoot.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            TextMeshProUGUI title = CreateText("Title", header.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            title.name = "ProductionSettingsTitle";
            title.fontStyle = FontStyles.Bold;
            title.lineSpacing = 2f;
            Stretch(title.rectTransform(), 10f, 7f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", CloseProductionSettingsPanel);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-4f, -4f);
            closeRect.sizeDelta = new Vector2(22f, 20f);

            GameObject viewport = CreateBox("Viewport", productionSettingsRoot.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 10f, 10f, 10f, 70f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            productionSettingsContent = content.AddComponent<RectTransform>();
            productionSettingsContent.anchorMin = new Vector2(0f, 1f);
            productionSettingsContent.anchorMax = new Vector2(1f, 1f);
            productionSettingsContent.pivot = new Vector2(0.5f, 1f);
            productionSettingsContent.offsetMin = Vector2.zero;
            productionSettingsContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 5f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(productionSettingsRoot.transform);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = productionSettingsContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 2f;
            viewport.AddComponent<ScrollWheelBlocker>();

            productionSettingsRoot.SetActive(false);
        }

        private void UpdateProductionSettingsPanel()
        {
            if (productionSettingsRoot == null || !productionSettingsRoot.activeSelf || productionSettingsContent == null)
            {
                return;
            }

            ClearProductionSettingsContent();
            Storage storage = productionSettingsStorage;
            if (storage == null)
            {
                CloseProductionSettingsPanel();
                return;
            }

            SetProductionSettingsTitle(storage);
            ComplexFabricator fabricator = storage.GetComponent<ComplexFabricator>();
            AddProductionSettingsInfo(storage, fabricator);
            AddMaterialRequestSettings(storage);
            AddProductionSettingsItems(storage, fabricator);

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(productionSettingsContent);
        }

        private void ClearProductionSettingsContent()
        {
            for (int i = productionSettingsContent.childCount - 1; i >= 0; i--)
            {
                Destroy(productionSettingsContent.GetChild(i).gameObject);
            }
        }

        private void SetProductionSettingsTitle(Storage storage)
        {
            TextMeshProUGUI title = productionSettingsRoot.GetComponentsInChildren<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.name == "ProductionSettingsTitle");
            if (title != null)
            {
                title.text = string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_DETAILS),
                    GameUtil.GetFormattedMass(storage.MassStored()),
                    GameUtil.GetFormattedMass(storage.Capacity()),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity())));
            }
        }

        private void AddProductionSettingsInfo(Storage storage, ComplexFabricator fabricator)
        {
            AddProductionSettingsText(storage.GetProperName(), 16, FontStyles.Bold, 34f);
            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 12, FontStyles.Bold, 24f);
            AddProductionSettingsStatus(fabricator);
        }

        private void AddMaterialRequestSettings(Storage storage)
        {
            StorageNetworkMaterialRequester requester = storage.GetComponent<StorageNetworkMaterialRequester>();
            if (requester == null)
            {
                return;
            }

            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_TITLE), 12, FontStyles.Bold, 24f);
            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_ENABLED),
                requester.RequestEnabled,
                value =>
                {
                    requester.RequestEnabled = value;
                    UpdateProductionSettingsPanel();
                });

            string modeName = requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            CreateProductionButtonRow(
                string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE), modeName),
                requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                () =>
                {
                    requester.CurrentMode = requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork
                        ? StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                        : StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
                    UpdateProductionSettingsPanel();
                });

            if (requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = requester.ResolveSourceStorage();
                string sourceName = source != null
                    ? source.GetProperName()
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE_NONE);
                CreateProductionButtonRow(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), sourceName),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE),
                    () => ShowMaterialRequestSourceSelection(storage, requester));
            }

            CreateProductionToggleRow(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED),
                requester.LimitEnabled,
                value =>
                {
                    requester.LimitEnabled = value;
                    UpdateProductionSettingsPanel();
                });

            if (requester.LimitEnabled)
            {
                CreateProductionButtonRow(
                    string.Format(
                        Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                        GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                    () => ShowMaterialRequestLimitDialog(requester));
                CreateProductionButtonRow(
                    string.Empty,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_RESET),
                    () =>
                    {
                        requester.ResetRequestedAmount();
                        UpdateProductionSettingsPanel();
                    });
            }

            if (!string.IsNullOrEmpty(requester.LastStatus))
            {
                AddProductionSettingsText(
                    ColorizeMaterialRequestStatus(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_STATUS), requester.LastStatus)),
                    11,
                    FontStyles.Normal,
                    22f);
            }

            AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CONTENT_TITLE), 12, FontStyles.Bold, 24f);
        }

        private void AddProductionSettingsStatus(ComplexFabricator fabricator)
        {
            if (fabricator == null)
            {
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 24f);
                return;
            }

            ComplexRecipe currentRecipe = fabricator.CurrentWorkingOrder;
            if (currentRecipe == null)
            {
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_IDLE), "#5f665d"), 12, FontStyles.Normal, 22f);
                AddProductionSettingsText(ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_NO_RECIPE), "#6b6b63"), 12, FontStyles.Normal, 22f);
                return;
            }

            string statusText = fabricator.WaitingForWorker
                ? ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_WAITING_WORKER), "#b5753c")
                : ColorText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_STATUS_CRAFTING), "#3f7f4a");
            AddProductionSettingsText(statusText, 12, FontStyles.Normal, 22f);
            AddProductionSettingsText(
                ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_CURRENT_RECIPE), GetRecipeDisplayName(currentRecipe)), "#38485d"),
                12,
                FontStyles.Normal,
                22f);
            AddProductionSettingsText(
                ColorText(string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_PROGRESS), Mathf.RoundToInt(Mathf.Clamp01(fabricator.OrderProgress) * 100f)), "#5a5f66"),
                12,
                FontStyles.Normal,
                22f);
        }

        private void AddProductionSettingsItems(Storage storage, ComplexFabricator fabricator)
        {
            List<GameObject> items = GetProductionStorages(storage, fabricator)
                .SelectMany(itemStorage => itemStorage.items.Where(item => item != null))
                .ToList();
            if (items.Count == 0)
            {
                AddProductionSettingsText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_STORAGE_CONTENT), 12, FontStyles.Normal, 26f);
                return;
            }

            foreach (IGrouping<string, GameObject> group in items.GroupBy(GetStoredItemKey).OrderBy(group => GetStoredItemName(group.FirstOrDefault())))
            {
                float mass = group.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
                CreateProductionSettingsItemRow(
                    GetStoredItemName(group.FirstOrDefault()),
                    GameUtil.GetFormattedMass(mass),
                    group.FirstOrDefault());
            }
        }

        private void AddProductionSettingsText(string text, int size, FontStyles style, float height)
        {
            TextMeshProUGUI label = CreateText("ProductionSettingsText", productionSettingsContent, text, size, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            label.fontStyle = style;
            label.richText = true;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static string ColorizeMaterialRequestStatus(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (text.Contains("缺少") || text.Contains("没有可用") || text.Contains("Missing") || text.Contains("no source"))
            {
                return ColorText(text, "#a64c3c");
            }

            if (text.Contains("限额") || text.Contains("limit"))
            {
                return ColorText(text, "#b5753c");
            }

            if (text.Contains("已请求") || text.Contains("已满足") || text.Contains("requested") || text.Contains("satisfied"))
            {
                return ColorText(text, "#3f7f4a");
            }

            return ColorText(text, "#5a5f66");
        }

        private static string ColorText(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        private void CreateProductionToggleRow(string label, bool value, System.Action<bool> onChanged)
        {
            CreateProductionButtonRow(
                label,
                value ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ON) : string.Empty,
                () => onChanged?.Invoke(!value),
                value ? KleiPinkStyle() : KleiBlueStyle());
        }

        private void CreateProductionButtonRow(string label, string buttonText, System.Action onClick, ColorStyleSetting buttonStyle = null)
        {
            GameObject row = CreatePlainImage("ProductionSettingRow", productionSettingsContent, new Color(0.86f, 0.85f, 0.80f, 1f));
            row.AddComponent<LayoutElement>().preferredHeight = 28f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 2, 2);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI labelText = CreateText("Label", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            labelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject button = CreateStyledButton("Button", row.transform, buttonText, onClick, buttonStyle ?? KleiBlueStyle());
            LayoutElement buttonLayout = button.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 168f;
            buttonLayout.preferredHeight = 22f;
        }

        private void ShowMaterialRequestSourceSelection(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<Storage> targets = StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .Where(storage => storage != null && storage != ownerStorage && storage.GetComponent<ComplexFabricator>() == null)
                .OrderBy(storage => storage.GetProperName())
                .ToList();

            if (targets.Count == 0)
            {
                ShowMessageDialog(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_TRANSFER_TARGET));
                return;
            }

            ShowTargetSelectionDialog(ownerStorage, targets, requester.ResolveSourceStorage(), target =>
            {
                requester.SetSourceStorage(target);
                CloseModal();
                UpdateProductionSettingsPanel();
            });
        }

        private void ShowMaterialRequestLimitDialog(StorageNetworkMaterialRequester requester)
        {
            ShowAmountDialog(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SET_LIMIT),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT_ENABLED),
                string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_LIMIT),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, requester.GetRequestedAmountForDisplay())),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, requester.LimitKg))),
                Mathf.Max(1f, requester.LimitKg <= 0f ? 1000f : requester.LimitKg * 10f),
                amount =>
                {
                    requester.LimitKg = amount;
                    UpdateProductionSettingsPanel();
                });
        }

        private void CreateProductionSettingsItemRow(string itemName, string formattedMass, GameObject representative)
        {
            GameObject row = CreatePlainImage("ProductionSettingsItemRow", productionSettingsContent, new Color(0.86f, 0.85f, 0.80f, 1f));
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
            SetStoredItemIcon(icon, representative);

            TextMeshProUGUI name = CreateText("Name", row.transform, itemName, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.18f, 0.19f, 0.19f, 1f);
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI mass = CreateText("Mass", row.transform, formattedMass, 11, TextAlignmentOptions.MidlineRight);
            mass.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            mass.textWrappingMode = TextWrappingModes.NoWrap;
            mass.gameObject.AddComponent<LayoutElement>().preferredWidth = 92f;
        }

        private static IEnumerable<Storage> GetProductionStorages(Storage storage, ComplexFabricator fabricator)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            AddProductionStorage(storages, storage);
            if (fabricator != null)
            {
                AddProductionStorage(storages, fabricator.inStorage);
                AddProductionStorage(storages, fabricator.buildStorage);
                AddProductionStorage(storages, fabricator.outStorage);
            }

            return storages;
        }

        private static void AddProductionStorage(HashSet<Storage> storages, Storage storage)
        {
            if (storage != null)
            {
                storages.Add(storage);
            }
        }

        private static string GetRecipeDisplayName(ComplexRecipe recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            return recipe.GetUIName(false);
        }
    }
}
