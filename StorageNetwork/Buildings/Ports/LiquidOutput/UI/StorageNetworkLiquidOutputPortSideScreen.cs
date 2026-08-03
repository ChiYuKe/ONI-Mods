using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkLiquidOutputPortSideScreen : SideScreenContent
    {
        private const float LiveRefreshSeconds = 0.5f;
        private readonly Dictionary<SimHashes, LiquidAmountView> optionAmountViews =
            new Dictionary<SimHashes, LiquidAmountView>();
        private readonly Dictionary<SimHashes, float> availableLiquidAmounts =
            new Dictionary<SimHashes, float>();
        private readonly List<SimHashes> sortedElements = new List<SimHashes>();
        private readonly List<LiquidOption> optionWorkspace = new List<LiquidOption>();
        private readonly List<SimHashes?> lastStructureElements = new List<SimHashes?>();
        private static ColorStyleSetting blueStyle;
        private static ColorStyleSetting pinkStyle;
        private GameObject targetObject;
        private StorageNetworkLiquidOutputPortEgress targetEgress;
        private Storage targetStorage;
        private Transform optionRoot;
        private StorageNetworkKeyedRowCache optionRowCache;
        private GameObject contentRoot;
        private TextMeshProUGUI statusText;
        private SimHashes? lastStructureSelection;
        private bool hasStructureSignature;
        private SimHashes? lastStatusSelection;
        private bool hasStatusSelection;
        private float refreshTimer;

        public StorageNetworkLiquidOutputPortSideScreen()
        {
            titleKey = string.Empty;
        }

        public override string GetTitle()
        {
            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LIQUID_OUTPUT_SIDE_SCREEN_TITLE);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            BuildContent();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkLiquidOutputPortEgress>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            targetObject = target;
            ResolveTargetComponents();
            InvalidateStructure();
            Refresh(true);
        }

        public override void ClearTarget()
        {
            targetObject = null;
            targetEgress = null;
            targetStorage = null;
            InvalidateStructure();
            ClearOptions();
            base.ClearTarget();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            ResolveTargetComponents();
            if (targetEgress == null)
            {
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = LiveRefreshSeconds;
            Refresh(false);
        }

        private void BuildContent()
        {
            if (contentRoot != null)
            {
                return;
            }

            EnsureRootLayout();
            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;
            ClearContainer(parent);

            contentRoot = new GameObject("LiquidOutputQuickFilter");
            contentRoot.transform.SetParent(parent, false);
            RectTransform contentRect = contentRoot.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            LayoutElement rootLayout = contentRoot.AddComponent<LayoutElement>();
            rootLayout.minHeight = 168f;
            rootLayout.preferredHeight = 220f;

            VerticalLayoutGroup layout = contentRoot.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            statusText = CreateText(contentRoot.transform, string.Empty, 12f, FontStyles.Bold, new Color(0.22f, 0.24f, 0.25f, 1f));
            statusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

            GameObject optionObject = new GameObject("Options");
            optionObject.transform.SetParent(contentRoot.transform, false);
            optionRoot = optionObject.transform;
            optionObject.AddComponent<RectTransform>();
            optionRowCache = new StorageNetworkKeyedRowCache(optionRoot, 32, 120);
            optionObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            VerticalLayoutGroup optionLayout = optionObject.AddComponent<VerticalLayoutGroup>();
            optionLayout.spacing = 4f;
            optionLayout.childControlWidth = true;
            optionLayout.childControlHeight = true;
            optionLayout.childForceExpandWidth = true;
            optionLayout.childForceExpandHeight = false;
            optionObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Refresh(true);
        }

        private static void ClearContainer(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void EnsureRootLayout()
        {
            if (GetComponent<RectTransform>() == null)
            {
                gameObject.AddComponent<RectTransform>();
            }

            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 176f;
            layoutElement.preferredHeight = 232f;
            layoutElement.flexibleWidth = 1f;

            if (GetComponent<VerticalLayoutGroup>() == null)
            {
                VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.spacing = 0f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
        }

        private void Refresh(bool force)
        {
            ResolveTargetComponents();
            if (targetEgress == null || targetStorage == null || optionRoot == null)
            {
                if (force)
                {
                    Debug.LogWarning("[StorageNetwork] Liquid output side screen refresh skipped. target=" +
                        (targetObject != null ? targetObject.name : "<null>") +
                        ", egress=" + (targetEgress != null ? "ok" : "<null>") +
                        ", storage=" + (targetStorage != null ? "ok" : "<null>") +
                        ", optionRoot=" + (optionRoot != null ? "ok" : "<null>") + ".");
                }

                return;
            }

            using var performanceScope =
                StorageNetworkFrameProfileTool.BeginWork(StorageNetworkPerformanceArea.LiquidSideScreen);
            List<LiquidOption> options = BuildOptions();
            bool structureChanged = force || HasStructureChanged(options);
            SetStatusText(options);
            if (!structureChanged)
            {
                UpdateOptionAmounts(options);
                return;
            }

            RememberStructure(options);
            ReconcileOptions(options);
        }

        private void ResolveTargetComponents()
        {
            if (targetObject == null)
            {
                return;
            }

            if (targetEgress == null)
            {
                targetEgress = targetObject.GetComponent<StorageNetworkLiquidOutputPortEgress>();
            }

            if (targetStorage == null)
            {
                targetStorage = targetEgress != null ? targetEgress.PortStorage : null;
            }

            if (targetStorage == null)
            {
                targetStorage = targetObject.GetComponent<Storage>();
            }

            if (targetStorage == null)
            {
                targetStorage = targetObject.GetComponentInChildren<Storage>();
            }
        }

        private List<LiquidOption> BuildOptions()
        {
            Storage specificSource = targetEgress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? targetEgress.ResolveSourceStorage()
                : null;
            SimHashes? selected = targetEgress.GetSelectedOutputElement();
            List<LiquidOption> options = optionWorkspace;
            options.Clear();
            options.Add(new LiquidOption(
                null,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY),
                !selected.HasValue,
                0f));

            Dictionary<SimHashes, float> amounts = BuildAvailableLiquidAmounts(specificSource);
            RefreshSortedElements(amounts);

            foreach (SimHashes elementHash in sortedElements)
            {
                Element element = ElementLoader.FindElementByHash(elementHash);
                string name = element != null ? element.name : elementHash.ToString();
                float amount = amounts[elementHash];
                options.Add(new LiquidOption(
                    elementHash,
                    name,
                    selected == elementHash,
                    amount));
            }

            return options;
        }

        private Dictionary<SimHashes, float> BuildAvailableLiquidAmounts(Storage specificSource)
        {
            Dictionary<SimHashes, float> amounts = availableLiquidAmounts;
            amounts.Clear();
            if (specificSource == null)
            {
                StorageNetworkContentIndexService.FillWorldLiquidMasses(
                    StorageTargetSelector.GetObjectWorldId(targetStorage?.gameObject),
                    amounts);
                return amounts;
            }

            if (StorageNetworkContentIndexService.TryFillStorageLiquidMasses(
                    specificSource,
                    amounts))
            {
                return amounts;
            }

            // Compatibility fallback for an explicitly selected third-party
            // storage that is not enrolled in the runtime catalog. This scans only
            // that storage, never the entire world.
            if (specificSource.items == null)
            {
                return amounts;
            }

            foreach (GameObject item in specificSource.items)
            {
                PrimaryElement primaryElement = item != null
                    ? item.GetComponent<PrimaryElement>()
                    : null;
                if (primaryElement == null ||
                    primaryElement.Mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                Element element = ElementLoader.FindElementByHash(primaryElement.ElementID);
                if (element == null || !element.IsLiquid)
                {
                    continue;
                }

                amounts[primaryElement.ElementID] =
                    amounts.TryGetValue(primaryElement.ElementID, out float current)
                        ? current + primaryElement.Mass
                        : primaryElement.Mass;
            }

            return amounts;
        }

        private void RefreshSortedElements(Dictionary<SimHashes, float> amounts)
        {
            bool changed = sortedElements.Count != amounts.Count;
            if (!changed)
            {
                for (int index = 0; index < sortedElements.Count; index++)
                {
                    if (!amounts.ContainsKey(sortedElements[index]))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            sortedElements.Clear();
            foreach (SimHashes element in amounts.Keys)
            {
                sortedElements.Add(element);
            }

            sortedElements.Sort(CompareElementsByName);
        }

        private static int CompareElementsByName(SimHashes left, SimHashes right)
        {
            Element leftElement = ElementLoader.FindElementByHash(left);
            Element rightElement = ElementLoader.FindElementByHash(right);
            return string.Compare(
                leftElement != null ? leftElement.name : left.ToString(),
                rightElement != null ? rightElement.name : right.ToString(),
                System.StringComparison.CurrentCulture);
        }

        private void SetStatusText(List<LiquidOption> options)
        {
            if (statusText == null)
            {
                return;
            }

            SimHashes? selection = null;
            string selectedName = Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY);
            for (int index = 0; index < options.Count; index++)
            {
                if (options[index].Selected)
                {
                    selection = options[index].Element;
                    selectedName = options[index].Name;
                    break;
                }
            }

            if (hasStatusSelection && lastStatusSelection == selection)
            {
                return;
            }

            hasStatusSelection = true;
            lastStatusSelection = selection;
            string text = string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LIQUID_OUTPUT_SIDE_SCREEN_CURRENT),
                selectedName);
            if (statusText.text != text)
            {
                statusText.text = text;
            }
        }

        private void ReconcileOptions(List<LiquidOption> options)
        {
            optionRowCache ??= new StorageNetworkKeyedRowCache(optionRoot, 32, 120);
            optionRowCache.Begin();
            optionAmountViews.Clear();
            for (int index = 0; index < options.Count; index++)
            {
                LiquidOption option = options[index];
                string key = GetOptionRowKey(option.Element);
                GameObject row = optionRowCache.Use(
                    key,
                    () => CreateOptionRow(option.Element));
                if (!optionRowCache.TryGetMetadata(
                        key,
                        out LiquidOptionRowView view))
                {
                    view = row.GetComponent<LiquidOptionRowBinding>()?.View;
                    optionRowCache.SetMetadata(key, view);
                }

                UpdateOptionRow(view, option);
            }

            if (options.Count <= 1)
            {
                optionRowCache.Use("\0empty", CreateEmptyHint);
            }

            optionRowCache.Commit();
        }

        private static string GetOptionRowKey(SimHashes? element)
        {
            return element.HasValue
                ? "element:" + ((int)element.Value).ToString()
                : "any";
        }

        private GameObject CreateOptionRow(SimHashes? element)
        {
            GameObject row = new GameObject("LiquidFilterOption");
            row.transform.SetParent(optionRoot, false);
            row.AddComponent<RectTransform>();

            KImage background = row.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            background.colorStyleSetting = CreateBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = row.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () =>
            {
                targetEgress?.SetOutputElementAndRefresh(element);
                InvalidateStructure();
                Refresh(true);
            };

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 32f;
            rowLayout.minHeight = 32f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 3, 3);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            AddIcon(row.transform, element);
            TextMeshProUGUI name = CreateText(row.transform, string.Empty, 10f, FontStyles.Normal, new Color(0.94f, 0.96f, 0.98f, 1f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText(
                row.transform,
                string.Empty,
                8f,
                FontStyles.Normal,
                new Color(0.78f, 0.80f, 0.83f, 1f));
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
            LiquidOptionRowView view = new LiquidOptionRowView(
                background,
                name,
                amount);
            row.AddComponent<LiquidOptionRowBinding>().View = view;
            return row;
        }

        private void UpdateOptionRow(LiquidOptionRowView view, LiquidOption option)
        {
            if (view == null)
            {
                return;
            }

            ColorStyleSetting style = option.Selected ? CreatePinkStyle() : CreateBlueStyle();
            if (view.Background.colorStyleSetting != style)
            {
                view.Background.colorStyleSetting = style;
                view.Background.ApplyColorStyleSetting();
            }

            FontStyles fontStyle = option.Selected ? FontStyles.Bold : FontStyles.Normal;
            if (view.Name.fontStyle != fontStyle)
            {
                view.Name.fontStyle = fontStyle;
            }

            if (view.Name.text != option.Name)
            {
                view.Name.text = option.Name;
            }

            if (option.Element.HasValue)
            {
                int fingerprint = option.AmountKg.GetHashCode();
                if (view.Amount.LastAmountFingerprint != fingerprint)
                {
                    view.Amount.LastAmountFingerprint = fingerprint;
                    string details = GameUtil.GetFormattedMass(option.AmountKg);
                    if (view.Amount.Text.text != details)
                    {
                        view.Amount.Text.text = details;
                    }
                }

                optionAmountViews[option.Element.Value] = view.Amount;
            }
        }

        private void UpdateOptionAmounts(IEnumerable<LiquidOption> options)
        {
            foreach (LiquidOption option in options)
            {
                if (!option.Element.HasValue ||
                    !optionAmountViews.TryGetValue(option.Element.Value, out LiquidAmountView view) ||
                    view?.Text == null)
                {
                    continue;
                }

                int fingerprint = option.AmountKg.GetHashCode();
                if (view.LastAmountFingerprint == fingerprint)
                {
                    continue;
                }

                view.LastAmountFingerprint = fingerprint;
                string details = GameUtil.GetFormattedMass(option.AmountKg);
                if (view.Text.text != details)
                {
                    view.Text.text = details;
                }
            }
        }

        private GameObject CreateEmptyHint()
        {
            GameObject hint = new GameObject("EmptyHint");
            hint.transform.SetParent(optionRoot, false);
            hint.AddComponent<RectTransform>();

            LayoutElement layout = hint.AddComponent<LayoutElement>();
            layout.preferredHeight = 32f;
            layout.minHeight = 32f;

            TextMeshProUGUI text = CreateText(
                hint.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_EMPTY),
                9f,
                FontStyles.Normal,
                new Color(0.30f, 0.31f, 0.30f, 1f));
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(text.rectTransform(), 4f, 4f, 2f, 2f);
            return hint;
        }

        private static void AddIcon(Transform parent, SimHashes? elementHash)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 22f;
            iconLayout.minWidth = 22f;

            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            if (elementHash.HasValue)
            {
                var uiSprite = Def.GetUISprite(elementHash.Value.CreateTag(), "ui", false);
                icon.sprite = uiSprite.first;
                icon.color = uiSprite.first != null ? uiSprite.second : Color.clear;
            }
            else
            {
                icon.sprite = Assets.GetSprite("icon_filter");
                icon.color = icon.sprite != null ? Color.white : Color.clear;
            }
        }

        private void ClearOptions()
        {
            if (optionRowCache != null)
            {
                optionRowCache.Begin();
                optionRowCache.Commit();
            }

            optionAmountViews.Clear();
        }

        private bool HasStructureChanged(List<LiquidOption> options)
        {
            if (!hasStructureSignature ||
                lastStructureElements.Count != options.Count)
            {
                return true;
            }

            SimHashes? selected = null;
            for (int index = 0; index < options.Count; index++)
            {
                LiquidOption option = options[index];
                if (lastStructureElements[index] != option.Element)
                {
                    return true;
                }

                if (option.Selected)
                {
                    selected = option.Element;
                }
            }

            return lastStructureSelection != selected;
        }

        private void RememberStructure(List<LiquidOption> options)
        {
            lastStructureElements.Clear();
            lastStructureSelection = null;
            for (int index = 0; index < options.Count; index++)
            {
                LiquidOption option = options[index];
                lastStructureElements.Add(option.Element);
                if (option.Selected)
                {
                    lastStructureSelection = option.Element;
                }
            }

            hasStructureSignature = true;
        }

        private void InvalidateStructure()
        {
            hasStructureSignature = false;
            hasStatusSelection = false;
        }

        private static GameObject CreatePanel(Transform parent, Color color)
        {
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string textValue, float fontSize, FontStyles style, Color color)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = textValue ?? string.Empty;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static ColorStyleSetting CreateBlueStyle()
        {
            if (blueStyle == null)
            {
                blueStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                blueStyle.inactiveColor = new Color(0.17f, 0.19f, 0.25f, 1f);
                blueStyle.hoverColor = new Color(0.25f, 0.28f, 0.35f, 1f);
                blueStyle.activeColor = new Color(0.11f, 0.12f, 0.16f, 1f);
                blueStyle.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
                blueStyle.disabledActiveColor = blueStyle.disabledColor;
                blueStyle.disabledhoverColor = blueStyle.disabledColor;
            }

            return blueStyle;
        }

        private static ColorStyleSetting CreatePinkStyle()
        {
            if (pinkStyle == null)
            {
                pinkStyle = ScriptableObject.CreateInstance<ColorStyleSetting>();
                pinkStyle.inactiveColor = new Color(0.53f, 0.27f, 0.40f, 1f);
                pinkStyle.hoverColor = new Color(0.62f, 0.33f, 0.47f, 1f);
                pinkStyle.activeColor = new Color(0.79f, 0.45f, 0.62f, 1f);
                pinkStyle.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
                pinkStyle.disabledActiveColor = pinkStyle.disabledColor;
                pinkStyle.disabledhoverColor = pinkStyle.disabledColor;
            }

            return pinkStyle;
        }

        internal static void ResetRuntimeStyles()
        {
            if (blueStyle != null)
            {
                Destroy(blueStyle);
                blueStyle = null;
            }

            if (pinkStyle != null)
            {
                Destroy(pinkStyle);
                pinkStyle = null;
            }
        }

        private readonly struct LiquidOption
        {
            public LiquidOption(
                SimHashes? element,
                string name,
                bool selected,
                float amountKg)
            {
                Element = element;
                Name = name;
                Selected = selected;
                AmountKg = amountKg;
            }

            public SimHashes? Element { get; }
            public string Name { get; }
            public bool Selected { get; }
            public float AmountKg { get; }
        }

        private sealed class LiquidAmountView
        {
            public LiquidAmountView(TextMeshProUGUI text, int lastAmountFingerprint)
            {
                Text = text;
                LastAmountFingerprint = lastAmountFingerprint;
            }

            public TextMeshProUGUI Text { get; }
            public int LastAmountFingerprint { get; set; }
        }

        private sealed class LiquidOptionRowView
        {
            public LiquidOptionRowView(
                KImage background,
                TextMeshProUGUI name,
                TextMeshProUGUI amount)
            {
                Background = background;
                Name = name;
                Amount = new LiquidAmountView(amount, int.MinValue);
            }

            public KImage Background { get; }
            public TextMeshProUGUI Name { get; }
            public LiquidAmountView Amount { get; }
        }

        private sealed class LiquidOptionRowBinding : MonoBehaviour
        {
            public LiquidOptionRowView View { get; set; }
        }
    }
}
