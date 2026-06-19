using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkEngravingDiskSideScreen : SideScreenContent
    {
        private const float RecipeCardHeight = 56f;
        private const float RecipeCardSpacing = 4f;
        private const float MaxVisibleRecipes = 8f;
        private const float OuterPaddingHeight = 10f;

        private StorageNetworkEngravingDisk disk;
        private Transform root;
        private Transform listRoot;
        private LayoutElement screenLayout;
        private LayoutElement containerLayout;
        private LayoutElement listLayout;
        private RectTransform viewportRect;
        private RectTransform contentRect;
        private ScrollRect scrollRect;
        private GameObject scrollbarObject;

        public StorageNetworkEngravingDiskSideScreen()
        {
            titleKey = "STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE";
            CheckShouldShowTopTitle = () => true;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureBuilt();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkEngravingDisk>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            disk = target != null ? target.GetComponent<StorageNetworkEngravingDisk>() : null;
            EnsureBuilt();
            Refresh();
        }

        public override void ClearTarget()
        {
            disk = null;
            base.ClearTarget();
        }

        public override int GetSideScreenSortOrder()
        {
            return 20;
        }

        private void Build()
        {
            RectTransform screenRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0f, 1f);
            screenRect.anchorMax = new Vector2(1f, 1f);
            screenRect.pivot = new Vector2(0.5f, 1f);
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            screenLayout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            screenLayout.minHeight = RecipeCardHeight + OuterPaddingHeight;
            screenLayout.preferredHeight = screenLayout.minHeight;
            screenLayout.flexibleWidth = 1f;

            VerticalLayoutGroup screenGroup = gameObject.GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
            screenGroup.childControlWidth = true;
            screenGroup.childControlHeight = true;
            screenGroup.childForceExpandWidth = true;
            screenGroup.childForceExpandHeight = false;

            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;
            GameObject container = new GameObject("EngravingDiskContent");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(0.5f, 1f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.minHeight = RecipeCardHeight + OuterPaddingHeight;
            containerLayout.preferredHeight = containerLayout.minHeight;
            containerLayout.flexibleWidth = 1f;

            VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            root = container.transform;

            CreateRecipeList();
            ContentSizeFitter fitter = container.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void EnsureBuilt()
        {
            if (root == null || listRoot == null || scrollRect == null)
            {
                Build();
            }
        }

        private void Refresh()
        {
            EnsureBuilt();
            if (root == null || listRoot == null || disk == null)
            {
                return;
            }

            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = listRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            var recipeIds = disk.EngravedRecipeIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            UpdateHeights(recipeIds.Count);

            if (disk.IsBlank)
            {
                CreateText(listRoot, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK), 12, TextAlignmentOptions.MidlineLeft, new Color(0.18f, 0.18f, 0.18f, 1f), true);
                return;
            }

            foreach (string recipeId in recipeIds)
            {
                ComplexRecipe recipe = ComplexRecipeManager.Get()?.GetRecipe(recipeId);
                CreateRecipeCard(recipeId, recipe);
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void UpdateHeights(int recipeCount)
        {
            float contentHeight = recipeCount <= 0
                ? RecipeCardHeight
                : recipeCount * RecipeCardHeight + Mathf.Max(0, recipeCount - 1) * RecipeCardSpacing;
            float listHeight = Mathf.Min(contentHeight, MaxVisibleRecipes * RecipeCardHeight + (MaxVisibleRecipes - 1f) * RecipeCardSpacing);
            float totalHeight = listHeight + OuterPaddingHeight;

            if (listLayout != null)
            {
                listLayout.minHeight = listHeight;
                listLayout.preferredHeight = listHeight;
            }

            if (contentRect != null)
            {
                contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            }

            bool needsScrollbar = contentHeight > listHeight + 0.5f;
            if (scrollbarObject != null)
            {
                scrollbarObject.SetActive(needsScrollbar);
            }

            if (viewportRect != null)
            {
                viewportRect.offsetMax = new Vector2(needsScrollbar ? -14f : 0f, 0f);
            }

            if (containerLayout != null)
            {
                containerLayout.minHeight = totalHeight;
                containerLayout.preferredHeight = totalHeight;
            }

            if (screenLayout != null)
            {
                screenLayout.minHeight = totalHeight;
                screenLayout.preferredHeight = totalHeight;
            }
        }

        private void CreateRecipeList()
        {
            GameObject list = new GameObject("ButtonScrollView");
            list.transform.SetParent(root, false);
            RectTransform listRect = list.AddComponent<RectTransform>();
            Stretch(listRect);
            listLayout = list.AddComponent<LayoutElement>();
            listLayout.flexibleWidth = 1f;

            scrollRect = list.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(list.transform, false);
            viewportRect = viewport.AddComponent<RectTransform>();
            Stretch(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = RecipeCardSpacing;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateScrollbar(list.transform);

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.verticalScrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            listRoot = content.transform;
        }

        private void CreateScrollbar(Transform parent)
        {
            scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.sizeDelta = new Vector2(12f, 0f);
            scrollbarRect.anchoredPosition = Vector2.zero;

            Image background = scrollbarObject.AddComponent<Image>();
            ApplyWebBoxSprite(background);
            background.color = new Color(0.18f, 0.19f, 0.23f, 0.75f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
            Stretch(slidingRect);
            slidingRect.offsetMin = new Vector2(2f, 2f);
            slidingRect.offsetMax = new Vector2(-2f, -2f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            Stretch(handleRect);
            Image handleImage = handle.AddComponent<Image>();
            ApplyWebBoxSprite(handleImage);
            handleImage.color = new Color(0.70f, 0.70f, 0.74f, 1f);
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
        }

        private void CreateRecipeCard(string recipeId, ComplexRecipe recipe)
        {
            GameObject card = new GameObject("RecipeCard");
            card.transform.SetParent(listRoot, false);
            card.AddComponent<RectTransform>();
            Image background = card.AddComponent<Image>();
            ApplyWebBoxSprite(background);
            background.color = new Color(0.96f, 0.93f, 0.86f, 1f);
            LayoutElement cardLayout = card.AddComponent<LayoutElement>();
            cardLayout.preferredHeight = RecipeCardHeight;
            cardLayout.minHeight = RecipeCardHeight;

            HorizontalLayoutGroup layout = card.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 5, 5);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            string title = recipe != null ? recipe.GetUIName(false) : recipeId;
            CreateRecipeIcon(card.transform, recipe);
            TextMeshProUGUI name = CreateText(card.transform, title, 11, TextAlignmentOptions.MidlineLeft, new Color(0.18f, 0.16f, 0.13f, 1f), true);
            name.fontStyle = FontStyles.Bold;
            card.AddComponent<ToolTip>().SetSimpleTooltip(GetRecipeTooltip(recipe, recipeId));

            CreateButton(card.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_RECIPE_REMOVE), () =>
            {
                disk.RemoveRecipe(recipeId);
                Refresh();
            });
        }

        private static void CreateRecipeIcon(Transform parent, ComplexRecipe recipe)
        {
            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(parent, false);
            Image image = iconObject.AddComponent<Image>();
            image.sprite = GetRecipeSprite(recipe);
            image.preserveAspect = true;
            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 38f;
            layout.preferredHeight = 38f;
            layout.minWidth = 38f;
            layout.minHeight = 38f;
        }

        private static Sprite GetRecipeSprite(ComplexRecipe recipe)
        {
            Tag tag = GetPrimaryResultTag(recipe);
            GameObject prefab = tag != Tag.Invalid ? Assets.GetPrefab(tag) : null;
            if (prefab != null)
            {
                Tuple<Sprite, Color> sprite = Def.GetUISprite(prefab, "ui", false);
                if (sprite != null && sprite.first != null)
                {
                    return sprite.first;
                }
            }

            return Assets.GetSprite("unknown");
        }

        private static Tag GetPrimaryResultTag(ComplexRecipe recipe)
        {
            if (recipe?.results == null || recipe.results.Length == 0 || recipe.results[0] == null)
            {
                return Tag.Invalid;
            }

            return recipe.results[0].material != Tag.Invalid ? recipe.results[0].material : Tag.Invalid;
        }

        private static string GetRecipeTooltip(ComplexRecipe recipe, string recipeId)
        {
            if (recipe == null)
            {
                return recipeId;
            }

            return string.Format(
                "{0}\n材料：{1}\n产出：{2}",
                recipe.GetUIName(false),
                ProductionOrderFormatting.FormatRecipeElements(recipe.ingredients),
                ProductionOrderFormatting.FormatRecipeElements(recipe.results));
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, int size, TextAlignmentOptions alignment, Color color, bool flexible)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            LayoutElement layout = textObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexible ? 1f : 0f;
            layout.preferredHeight = text.Contains("\n") ? 38f : 24f;
            return label;
        }

        private static void CreateButton(Transform parent, string text, System.Action onClick, bool enabled = true)
        {
            GameObject buttonObject = new GameObject("Button");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            KImage image = buttonObject.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
            ApplyWebButtonSprite(image);
            image.colorStyleSetting = CreateButtonStyle();
            image.ColorState = KImage.ColorSelector.Inactive;
            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.isInteractable = enabled;
            button.onClick += () =>
            {
                if (enabled)
                {
                    onClick?.Invoke();
                }
            };
            image.ColorState = enabled ? KImage.ColorSelector.Inactive : KImage.ColorSelector.Disabled;
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 58f;
            layout.preferredHeight = 24f;
            TextMeshProUGUI label = CreateText(buttonObject.transform, text, 10, TextAlignmentOptions.Center, Color.white, false);
            label.fontStyle = FontStyles.Bold;
        }

        private static ColorStyleSetting CreateButtonStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = new Color(0.22f, 0.25f, 0.34f, 1f);
            style.hoverColor = new Color(0.31f, 0.35f, 0.45f, 1f);
            style.activeColor = new Color(0.16f, 0.19f, 0.27f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private static void ApplyWebBoxSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite("web_box");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
        }

        private static void ApplyWebButtonSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite("web_button") ?? Assets.GetSprite("web_box");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
