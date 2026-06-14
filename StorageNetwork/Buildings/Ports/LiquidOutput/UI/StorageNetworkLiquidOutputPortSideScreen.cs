using System.Collections.Generic;
using System.Linq;
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
        private readonly List<GameObject> optionRows = new List<GameObject>();
        private GameObject targetObject;
        private StorageNetworkLiquidOutputPortEgress targetEgress;
        private Storage targetStorage;
        private Transform optionRoot;
        private GameObject contentRoot;
        private TextMeshProUGUI statusText;
        private string lastSignature;
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
            lastSignature = null;
            Refresh(true);
        }

        public override void ClearTarget()
        {
            targetObject = null;
            targetEgress = null;
            targetStorage = null;
            lastSignature = null;
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

            refreshTimer -= Time.deltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = 1f;
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

            List<LiquidOption> options = BuildOptions();
            string signature = BuildSignature(options);
            SetStatusText(options);
            if (!force && signature == lastSignature)
            {
                return;
            }

            lastSignature = signature;
            ClearOptions();
            foreach (LiquidOption option in options)
            {
                CreateOptionRow(option);
            }

            if (options.Count <= 1)
            {
                CreateEmptyHint();
            }
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
            List<LiquidOption> options = new List<LiquidOption>
            {
                new LiquidOption(null, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY), string.Empty, !selected.HasValue, 0f)
            };

            foreach (SimHashes elementHash in NetworkStorageTransferService.GetAvailableLiquidElementsInNetwork(targetStorage, specificSource))
            {
                Element element = ElementLoader.FindElementByHash(elementHash);
                string name = element != null ? element.name : elementHash.ToString();
                float amount = GetAvailableElementAmount(elementHash, specificSource);
                options.Add(new LiquidOption(
                    elementHash,
                    name,
                    GameUtil.GetFormattedMass(amount),
                    selected == elementHash,
                    amount));
            }

            return options;
        }

        private float GetAvailableElementAmount(SimHashes elementHash, Storage specificSource)
        {
            Tag tag = elementHash.CreateTag();
            float amount = 0f;
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(StorageTargetSelector.GetObjectWorldId(targetStorage?.gameObject)).Storages;
            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == targetStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source))
                {
                    continue;
                }

                amount += source.GetAmountAvailable(tag);
            }

            return amount;
        }

        private void SetStatusText(List<LiquidOption> options)
        {
            if (statusText == null)
            {
                return;
            }

            LiquidOption selected = options.FirstOrDefault(option => option.Selected);
            string selectedName = selected != null ? selected.Name : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY);
            statusText.text = string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.LIQUID_OUTPUT_SIDE_SCREEN_CURRENT),
                selectedName);
        }

        private void CreateOptionRow(LiquidOption option)
        {
            GameObject row = new GameObject("LiquidFilterOption");
            row.transform.SetParent(optionRoot, false);
            row.AddComponent<RectTransform>();
            optionRows.Add(row);

            KImage background = row.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            background.colorStyleSetting = option.Selected ? CreatePinkStyle() : CreateBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = row.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () =>
            {
                targetEgress?.SetOutputElementAndRefresh(option.Element);
                lastSignature = null;
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

            AddIcon(row.transform, option.Element);
            TextMeshProUGUI name = CreateText(row.transform, option.Name, 10f, option.Selected ? FontStyles.Bold : FontStyles.Normal, new Color(0.94f, 0.96f, 0.98f, 1f));
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI amount = CreateText(row.transform, option.Details, 8f, FontStyles.Normal, new Color(0.78f, 0.80f, 0.83f, 1f));
            amount.textWrappingMode = TextWrappingModes.NoWrap;
            amount.overflowMode = TextOverflowModes.Ellipsis;
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
        }

        private void CreateEmptyHint()
        {
            GameObject hint = new GameObject("EmptyHint");
            hint.transform.SetParent(optionRoot, false);
            hint.AddComponent<RectTransform>();
            optionRows.Add(hint);

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
            foreach (GameObject row in optionRows)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            optionRows.Clear();
        }

        private static string BuildSignature(List<LiquidOption> options)
        {
            return string.Join("|", options.Select(option =>
                string.Format("{0}:{1}:{2}", option.Element.HasValue ? ((int)option.Element.Value).ToString() : "any", option.Selected ? "1" : "0", Mathf.RoundToInt(option.AmountKg * 1000f))));
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
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = new Color(0.17f, 0.19f, 0.25f, 1f);
            style.hoverColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            style.activeColor = new Color(0.11f, 0.12f, 0.16f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private static ColorStyleSetting CreatePinkStyle()
        {
            ColorStyleSetting style = CreateBlueStyle();
            style.inactiveColor = new Color(0.53f, 0.27f, 0.40f, 1f);
            style.hoverColor = new Color(0.62f, 0.33f, 0.47f, 1f);
            style.activeColor = new Color(0.79f, 0.45f, 0.62f, 1f);
            return style;
        }

        private sealed class LiquidOption
        {
            public LiquidOption(SimHashes? element, string name, string details, bool selected, float amountKg)
            {
                Element = element;
                Name = name;
                Details = details;
                Selected = selected;
                AmountKg = amountKg;
            }

            public SimHashes? Element { get; }
            public string Name { get; }
            public string Details { get; }
            public bool Selected { get; }
            public float AmountKg { get; }
        }
    }
}
