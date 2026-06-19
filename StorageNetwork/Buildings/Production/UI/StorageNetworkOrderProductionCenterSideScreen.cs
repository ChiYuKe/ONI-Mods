using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkOrderProductionCenterSideScreen : SideScreenContent
    {
        private const float BodyWidth = 280f;
        private const float FallbackBodyHeight = 230f;

        private StorageNetworkOrderProductionCenter center;
        private Transform root;
        private KButton engraveButton;
        private KButton orderButton;
        private readonly TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[3];
        private readonly KButton[] slotButtons = new KButton[3];
        private readonly TextMeshProUGUI[] slotButtonLabels = new TextMeshProUGUI[3];
        private readonly GameObject[] prefabProgressRows = new GameObject[3];
        private readonly KImage[] prefabProgressFills = new KImage[3];
        private readonly KImage[] prefabProgressIcons = new KImage[3];
        private float refreshTimer;

        public StorageNetworkOrderProductionCenterSideScreen()
        {
            titleKey = "STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCTION_CENTER_TITLE";
            CheckShouldShowTopTitle = () => false;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureBuilt();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkOrderProductionCenter>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            center = target != null ? target.GetComponent<StorageNetworkOrderProductionCenter>() : null;
            EnsureBuilt();
            Refresh();
        }

        public override void ClearTarget()
        {
            center = null;
            base.ClearTarget();
        }

        public void Update()
        {
            if (center == null)
            {
                return;
            }

            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer < 0.5f)
            {
                return;
            }

            refreshTimer = 0f;
            RefreshPrefabProgressBars();
        }

        public override int GetSideScreenSortOrder()
        {
            return 25;
        }

        private void Build()
        {
            RectTransform screenRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0f, 1f);
            screenRect.anchorMax = new Vector2(1f, 1f);
            screenRect.pivot = new Vector2(0.5f, 1f);
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            LayoutElement screenLayout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            screenLayout.minHeight = 0f;
            screenLayout.preferredHeight = -1f;
            screenLayout.flexibleWidth = 1f;

            VerticalLayoutGroup screenGroup = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            screenGroup.childControlWidth = true;
            screenGroup.childControlHeight = true;
            screenGroup.childForceExpandWidth = true;
            screenGroup.childForceExpandHeight = false;

            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;
            GameObject container = new GameObject("OrderProductionCenterContent");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            root = container.transform;

            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.minHeight = 0f;
            containerLayout.preferredHeight = -1f;
            containerLayout.flexibleWidth = 1f;

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void EnsureBuilt()
        {
            if (root == null)
            {
                Build();
            }
        }

        private void Refresh()
        {
            EnsureBuilt();
            if (root == null || center == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }

            CreateAssetBundlePanel();
        }

        private void CreateAssetBundlePanel()
        {
            GameObject prefab = StorageNetworkAssetBundles.GetEngravingOrderPanelPrefab();
            if (prefab == null)
            {
                Debug.LogError("[StorageNetwork][AB] Order center side screen could not load embedded prefab.");
                return;
            }

            GameObject holder = CreateEmbeddedPrefabHolder();
            GameObject panel = Instantiate(prefab, holder.transform, false);
            ConfigureEmbeddedPrefabLayout(holder, panel);
            PreparePrefabText(panel);
            SetStaticPrefabText(panel);

            for (int i = 0; i < slotLabels.Length; i++)
            {
                int slotIndex = i;
                slotLabels[i] = FindChildComponent<TextMeshProUGUI>(panel, "Slot" + i + "Label");
                slotButtons[i] = FindChildComponent<KButton>(panel, "Slot" + i + "Button");
                slotButtonLabels[i] = slotButtons[i] != null ? FindChildComponent<TextMeshProUGUI>(slotButtons[i].gameObject, "Label") : null;
                if (slotLabels[i] != null && slotLabels[i].gameObject.GetComponent<ToolTip>() == null)
                {
                    slotLabels[i].gameObject.AddComponent<ToolTip>();
                }

                if (slotButtons[i] != null)
                {
                    slotButtons[i].ClearOnClick();
                    slotButtons[i].onClick += () => OnPrefabSlotButtonPressed(slotIndex);
                    ConfigureButtonVisualFeedback(slotButtons[i]);
                }

                prefabProgressRows[i] = FindPrefabProgressRow(panel, i);
                prefabProgressFills[i] = FindPrefabProgressFill(prefabProgressRows[i]);
                prefabProgressIcons[i] = FindPrefabProgressIcon(prefabProgressRows[i]);
            }

            engraveButton = FindChildComponent<KButton>(panel, "EngraveButton");
            orderButton = FindChildComponent<KButton>(panel, "OrderButton");
            if (engraveButton == null || orderButton == null || slotLabels.Any(label => label == null) || slotButtons.Any(button => button == null) || slotButtonLabels.Any(label => label == null))
            {
                Debug.LogError("[StorageNetwork][AB] Order center side screen embedded prefab structure validation failed. Missing: " + GetMissingPrefabParts());
                Destroy(holder);
                return;
            }

            engraveButton.ClearOnClick();
            ConfigureButtonVisualFeedback(engraveButton);
            SetButtonTooltip(engraveButton, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_TOOLTIP));
            engraveButton.onClick += () =>
            {
                center?.BeginEngraving();
                Refresh();
            };
            SetButtonInteractable(engraveButton, center != null && center.HasBlankDiskSlot);

            orderButton.ClearOnClick();
            ConfigureButtonVisualFeedback(orderButton);
            SetButtonTooltip(orderButton, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_TOOLTIP));
            orderButton.onClick += () =>
            {
                if (center != null)
                {
                    StorageNetworkPanel.ShowOrderProductionCenter(center);
                }
            };
            SetButtonInteractable(orderButton, true);

            RefreshPrefabPanel();
        }

        private static void SetButtonTooltip(KButton button, string text)
        {
            if (button == null)
            {
                return;
            }

            ToolTip tooltip = button.GetComponent<ToolTip>() ?? button.gameObject.AddComponent<ToolTip>();
            tooltip.SetSimpleTooltip(text ?? string.Empty);
        }

        private static void PreparePrefabText(GameObject panel)
        {
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (text == null)
                {
                    continue;
                }

                if (defaultFont != null)
                {
                    text.font = defaultFont;
                    text.fontSharedMaterial = defaultFont.material;
                }

                if (text.transform.parent != null && text.transform.parent.GetComponent<KButton>() != null)
                {
                    StretchTextToParent(text);
                }

                text.raycastTarget = false;
                text.ForceMeshUpdate(true);
            }
        }

        private static void SetStaticPrefabText(GameObject panel)
        {
            SetChildLabel(panel, "EngraveHeader", Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_SECTION_TITLE));
            SetChildLabel(panel, "DiskHeader", Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE));
            SetChildLabel(panel, "OrderHeader", Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ORDER_SECTION_TITLE));
            SetChildLabel(panel, "EngraveButton", Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_BUTTON));
            SetChildLabel(panel, "OrderButton", Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_BUTTON));
        }

        private GameObject CreateEmbeddedPrefabHolder()
        {
            GameObject holder = new GameObject("OrderProductionCenterEmbeddedAssetBundleHolder");
            holder.transform.SetParent(root, false);
            RectTransform rect = holder.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            holder.AddComponent<RectMask2D>();

            LayoutElement layout = holder.AddComponent<LayoutElement>();
            layout.minWidth = BodyWidth;
            layout.preferredWidth = BodyWidth;
            layout.minHeight = FallbackBodyHeight;
            layout.preferredHeight = FallbackBodyHeight;
            layout.flexibleWidth = 1f;
            layout.flexibleHeight = 0f;
            return holder;
        }

        private void ConfigureEmbeddedPrefabLayout(GameObject holder, GameObject panel)
        {
            foreach (CanvasScaler scaler in panel.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.enabled = false;
            }

            foreach (GraphicRaycaster raycaster in panel.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                raycaster.enabled = false;
            }

            foreach (Canvas canvas in panel.GetComponentsInChildren<Canvas>(true))
            {
                canvas.overrideSorting = false;
                canvas.enabled = false;
            }

            RectTransform holderRect = holder.GetComponent<RectTransform>();
            float prefabHeight = GetEmbeddedPrefabHeight(panel);
            ApplyEmbeddedPrefabSize(holder, prefabHeight);
            holderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, BodyWidth);
            holderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, prefabHeight);

            RectTransform rect = panel.GetComponent<RectTransform>() ?? panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            Debug.Log("[StorageNetwork][AB] Embedded prefab stretched to side screen holder. Size: " + BodyWidth.ToString("0.##") + "x" + prefabHeight.ToString("0.##"));
        }

        private void ApplyEmbeddedPrefabSize(GameObject holder, float height)
        {
            ApplyLayoutSize(holder, height);

            if (root != null)
            {
                ApplyLayoutSize(root.gameObject, height);
            }

            ApplyLayoutSize(gameObject, height);
        }

        private static float GetEmbeddedPrefabHeight(GameObject panel)
        {
            RectTransform rect = panel != null ? panel.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                return FallbackBodyHeight;
            }

            float rootHeight = rect.rect.height;
            if (rootHeight <= 0.01f)
            {
                rootHeight = Mathf.Abs(rect.sizeDelta.y);
            }

            return rootHeight > 0.01f ? rootHeight : FallbackBodyHeight;
        }

        private static void ApplyLayoutSize(GameObject target, float height)
        {
            if (target == null)
            {
                return;
            }

            LayoutElement layout = target.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }

        private void RefreshPrefabPanel()
        {
            if (center == null)
            {
                return;
            }

            SetButtonInteractable(engraveButton, center.HasBlankDiskSlot);
            SetButtonInteractable(orderButton, true);
            for (int i = 0; i < slotLabels.Length; i++)
            {
                StorageNetworkOrderProductionCenter.EngravingDiskSlot slot = i < center.DiskSlots.Count ? center.DiskSlots[i] : null;
                slotLabels[i].text = string.Format("{0}. {1}", i + 1, GetSlotLabel(slot));
                slotLabels[i].ForceMeshUpdate(true);
                slotLabels[i].gameObject.GetComponent<ToolTip>()?.SetSimpleTooltip(GetSlotTooltip(slot));
                slotButtonLabels[i].text = slot != null && slot.HasDisk
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_EJECT)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_INSERT);
                slotButtonLabels[i].ForceMeshUpdate(true);
                SetButtonInteractable(slotButtons[i], true);
            }

            RefreshPrefabProgressBars();
        }

        private void RefreshPrefabProgressBars()
        {
            StorageNetworkOrderProductionCenterFabricator fabricator = center != null
                ? center.GetComponent<StorageNetworkOrderProductionCenterFabricator>()
                : null;

            for (int i = 0; i < prefabProgressFills.Length; i++)
            {
                StorageNetworkOrderProductionCenterFabricator.CoreState core = fabricator != null && i < fabricator.Cores.Count
                    ? fabricator.Cores[i]
                    : null;
                ComplexRecipe recipe = GetCoreRecipe(core);
                bool hasRecipe = recipe != null;
                if (prefabProgressRows[i] != null)
                {
                    prefabProgressRows[i].SetActive(hasRecipe);
                }

                KImage fill = prefabProgressFills[i];
                float progress = hasRecipe ? Mathf.Clamp01(core.Progress) : 0f;
                if (fill != null)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = 0;
                    fill.fillAmount = progress;
                }

                KImage icon = prefabProgressIcons[i];
                if (icon != null && hasRecipe)
                {
                    ApplyRecipeIcon(icon, recipe);
                    icon.raycastTarget = false;
                }
            }
        }

        private static void ApplyRecipeIcon(KImage icon, ComplexRecipe recipe)
        {
            if (icon == null)
            {
                return;
            }

            icon.type = Image.Type.Simple;
            icon.fillAmount = 1f;
            icon.preserveAspect = true;
            icon.sprite = recipe != null ? recipe.GetUIIcon() : Assets.GetSprite("unknown");
            icon.color = Color.white;
            icon.ColorState = KImage.ColorSelector.Inactive;
        }

        private static ComplexRecipe GetCoreRecipe(StorageNetworkOrderProductionCenterFabricator.CoreState core)
        {
            return core != null && core.IsWorking && !string.IsNullOrEmpty(core.RecipeId)
                ? ComplexRecipeManager.Get().GetRecipe(core.RecipeId)
                : null;
        }

        private static void SetButtonInteractable(KButton button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.isInteractable = interactable;
            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.ColorState = interactable ? KImage.ColorSelector.Inactive : KImage.ColorSelector.Disabled;
            }
        }

        private static void ConfigureButtonVisualFeedback(KButton button)
        {
            if (button == null)
            {
                return;
            }

            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = true;
            image.colorStyleSetting = CreateButtonStyle();
            image.ColorState = button.isInteractable ? KImage.ColorSelector.Inactive : KImage.ColorSelector.Disabled;
            button.bgImage = image;
            button.additionalKImages = new KImage[0];

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            AddButtonTrigger(trigger, EventTriggerType.PointerEnter, () => SetButtonState(button, KImage.ColorSelector.Hover));
            AddButtonTrigger(trigger, EventTriggerType.PointerExit, () => SetButtonState(button, KImage.ColorSelector.Inactive));
            AddButtonTrigger(trigger, EventTriggerType.PointerDown, () => SetButtonState(button, KImage.ColorSelector.Active));
            AddButtonTrigger(trigger, EventTriggerType.PointerUp, () => SetButtonState(button, KImage.ColorSelector.Hover));
        }

        private static void AddButtonTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static void SetButtonState(KButton button, KImage.ColorSelector state)
        {
            if (button == null || !button.isInteractable)
            {
                return;
            }

            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image != null)
            {
                image.ColorState = state;
            }
        }

        private static ColorStyleSetting CreateButtonStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = new Color(0.22f, 0.25f, 0.34f, 1f);
            style.hoverColor = new Color(0.36f, 0.41f, 0.55f, 1f);
            style.activeColor = new Color(0.16f, 0.19f, 0.27f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private string GetMissingPrefabParts()
        {
            System.Collections.Generic.List<string> missing = new System.Collections.Generic.List<string>();
            if (engraveButton == null)
            {
                missing.Add("EngraveButton(KButton)");
            }

            if (orderButton == null)
            {
                missing.Add("OrderButton(KButton)");
            }

            for (int i = 0; i < slotLabels.Length; i++)
            {
                if (slotLabels[i] == null)
                {
                    missing.Add("Slot" + i + "Label(TextMeshProUGUI)");
                }

                if (slotButtons[i] == null)
                {
                    missing.Add("Slot" + i + "Button(KButton)");
                }

                if (slotButtonLabels[i] == null)
                {
                    missing.Add("Slot" + i + "Button/Label(TextMeshProUGUI)");
                }
            }

            return missing.Count == 0 ? "<none>" : string.Join(", ", missing);
        }

        private void OnPrefabSlotButtonPressed(int slotIndex)
        {
            if (center == null || slotIndex < 0 || slotIndex >= center.DiskSlots.Count)
            {
                return;
            }

            StorageNetworkOrderProductionCenter.EngravingDiskSlot slot = center.DiskSlots[slotIndex];
            if (slot != null && slot.HasDisk)
            {
                if (center.EjectDisk(slotIndex))
                {
                    StorageNetworkNotifications.ShowInfo(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_EJECTED));
                }
            }
            else
            {
                StorageNetworkPanel.ShowOrderCenterDiskPicker(center, slotIndex);
            }

            Refresh();
        }

        private static string GetSlotLabel(StorageNetworkOrderProductionCenter.EngravingDiskSlot slot)
        {
            if (slot == null || !slot.HasDisk)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_EMPTY);
            }

            int count = slot.RecipeIds?.Count(id => !string.IsNullOrEmpty(id)) ?? 0;
            return count == 0
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK)
                : StorageNetworkEngravingDisk.GetRecipeSummary(slot.RecipeIds);
        }

        private static string GetSlotTooltip(StorageNetworkOrderProductionCenter.EngravingDiskSlot slot)
        {
            if (slot == null || !slot.HasDisk)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_EMPTY);
            }

            return StorageNetworkEngravingDisk.GetRecipeDetails(slot.RecipeIds);
        }

        private static T FindChildComponent<T>(GameObject panel, string childName) where T : Component
        {
            foreach (T component in panel.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.name == childName)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindPrefabProgressRow(GameObject panel, int slotIndex)
        {
            return FindChildObject(panel, "Slot" + slotIndex + "RowProgressBar");
        }

        private static KImage FindPrefabProgressFill(GameObject row)
        {
            return row != null ? FindChildComponent<KImage>(row, "Fill") : null;
        }

        private static KImage FindPrefabProgressIcon(GameObject row)
        {
            if (row == null)
            {
                return null;
            }

            foreach (KImage image in row.GetComponentsInChildren<KImage>(true))
            {
                if (image != null && image.gameObject.name.Contains("ProgressBarIcon"))
                {
                    return image;
                }
            }

            return FindChildComponent<KImage>(row, "Slot0Slot0ProgressBarIcon");
        }

        private static void SetChildLabel(GameObject panel, string parentName, string text)
        {
            GameObject parent = FindChildObject(panel, parentName);
            TextMeshProUGUI label = parent != null ? FindChildComponent<TextMeshProUGUI>(parent, "TitleLabel") : null;
            if (label == null)
            {
                return;
            }

            label.text = text ?? string.Empty;
            if (label.transform.parent != null && label.transform.parent.GetComponent<KButton>() != null)
            {
                StretchTextToParent(label);
            }

            label.ForceMeshUpdate(true);
        }

        private static void StretchTextToParent(TextMeshProUGUI text)
        {
            RectTransform rect = text.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static GameObject FindChildObject(GameObject parent, string childName)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.gameObject.name == childName)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
