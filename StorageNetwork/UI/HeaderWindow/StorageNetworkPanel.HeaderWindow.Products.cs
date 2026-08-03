using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private const int ProductVirtualizationThreshold = 32;
        private const int ProductVirtualizationOverscan = 3;
        private const float ProductRowHeight = 56f;
        private const float ProductRowSpacing = 6f;
        private const float ProductVerticalPadding = 12f;
        private static ColorStyleSetting productNormalStyle;
        private static ColorStyleSetting productSelectedStyle;

        private void RebuildProductList()
        {
            productRows ??= new StorageNetworkKeyedRowCache(productListContent);
            productRows.Begin();
            if (IsOrderWorldFilterBlockedByRelay())
            {
                AddProductListText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.CROSS_WORLD_RELAY_OFFLINE));
                productRows.Commit();
                return;
            }

            if (orderProducts.Count == 0)
            {
                AddProductListText(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.RECIPE_WINDOW_EMPTY));
                productRows.Commit();
                return;
            }

            int totalCount = Mathf.Min(orderProducts.Count, MaxDisplayedProducts);
            GetProductVisibleRange(
                totalCount,
                out int firstVisible,
                out int lastVisibleExclusive);
            if (firstVisible > 0)
            {
                UseProductSpacer(
                    "\0virtual-top",
                    firstVisible * (ProductRowHeight + ProductRowSpacing) -
                    ProductRowSpacing);
            }

            for (int index = firstVisible; index < lastVisibleExclusive; index++)
            {
                CreateProductButton(orderProducts[index]);
            }

            int hiddenAfter = totalCount - lastVisibleExclusive;
            if (hiddenAfter > 0)
            {
                UseProductSpacer(
                    "\0virtual-bottom",
                    hiddenAfter * (ProductRowHeight + ProductRowSpacing) -
                    ProductRowSpacing);
            }

            productRows.Commit();
        }

        private void GetProductVisibleRange(
            int totalCount,
            out int firstVisible,
            out int lastVisibleExclusive)
        {
            firstVisible = 0;
            lastVisibleExclusive = totalCount;
            if (totalCount <= ProductVirtualizationThreshold ||
                productListContent == null ||
                productScrollRect?.viewport == null)
            {
                return;
            }

            float viewportHeight = productScrollRect.viewport.rect.height;
            if (viewportHeight <= 1f)
            {
                viewportHeight = 600f;
            }

            StorageNetworkVirtualizedRange range =
                StorageNetworkVirtualizedRange.Calculate(
                    totalCount,
                    ProductVirtualizationThreshold,
                    ProductVirtualizationOverscan,
                    ProductRowHeight,
                    ProductRowSpacing,
                    ProductVerticalPadding,
                    Mathf.Max(0f, productListContent.anchoredPosition.y),
                    viewportHeight);
            firstVisible = range.First;
            lastVisibleExclusive = range.LastExclusive;
        }

        private void UseProductSpacer(string key, float height)
        {
            GameObject spacer = productRows.Use(key, () =>
            {
                GameObject created = new GameObject("VirtualSpacer");
                created.transform.SetParent(productListContent, false);
                created.AddComponent<RectTransform>();
                created.AddComponent<LayoutElement>();
                return created;
            });
            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredHeight = Mathf.Max(0f, height);
            }
        }

        private void OnProductListScroll(Vector2 _)
        {
            productViewportDirty = true;
        }

        private void CreateProductButton(ProductDisplayGroup product)
        {
            bool selected = product.ProductKey == selectedProductKey;
            GameObject button = productRows.Use(
                "product:" + product.ProductKey,
                () => CreateProductButtonObject(product.ProductKey));

            ProductButtonView view = button.GetComponent<ProductButtonView>();
            if (view != null)
            {
                view.SetSelected(selected);
                if (view.Icon != null)
                {
                    view.Icon.sprite = product.Icon;
                }

                SetTextIfChanged(view.Name, product.ProductName);
                view.SetMeta(
                    productionOrderService.GetNetworkAvailableAmount(product.ProductTag),
                    product.Routes.Count);
            }
        }

        private void AddProductListText(string text)
        {
            GameObject row = productRows.Use("empty", () =>
            {
                TextMeshProUGUI created = CreateOrderText("ProductInfo", productListContent, string.Empty, 10, TextAlignmentOptions.MidlineLeft);
                created.color = MutedTextColor();
                created.fontStyle = FontStyles.Italic;
                created.richText = true;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Ellipsis;
                created.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
                return created.gameObject;
            });

            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = text;
            }
        }

        private GameObject CreateProductButtonObject(string productKey)
        {
            GameObject button = new GameObject("ProductButton");
            button.transform.SetParent(productListContent, false);
            button.AddComponent<RectTransform>();
            button.AddComponent<LayoutElement>().preferredHeight = 56f;

            KImage background = button.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = CreateProductRowStyle(false);
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton kButton = button.AddComponent<KButton>();
            kButton.bgImage = background;
            kButton.additionalKImages = new KImage[0];
            kButton.soundPlayer = new ButtonSoundPlayer();
            kButton.onClick += () => SelectProduct(productKey);

            HorizontalLayoutGroup layout = button.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 8, 4, 4);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject accentObject = new GameObject("Accent");
            accentObject.transform.SetParent(button.transform, false);
            RectTransform accentRect = accentObject.AddComponent<RectTransform>();
            accentRect.sizeDelta = new Vector2(5f, 40f);
            LayoutElement accentLayout = accentObject.AddComponent<LayoutElement>();
            accentLayout.preferredWidth = 5f;
            accentLayout.minWidth = 5f;
            accentLayout.preferredHeight = 40f;
            accentLayout.minHeight = 40f;
            KImage accent = accentObject.AddComponent<KImage>();
            accent.color = OniPinkActive();
            accent.raycastTarget = false;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(button.transform, false);
            RectTransform iconRect = iconObject.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(34f, 34f);
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 34f;
            iconLayout.preferredHeight = 34f;
            Image icon = iconObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;

            GameObject textColumn = new GameObject("TextColumn");
            textColumn.transform.SetParent(button.transform, false);
            textColumn.AddComponent<RectTransform>();
            textColumn.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddVerticalLayout(textColumn, 1f, 0, 0, 0, 0);

            TextMeshProUGUI name = CreateText("Name", textColumn.transform, string.Empty, 12, TextAlignmentOptions.MidlineLeft);
            name.color = NeutralTextColor();
            name.fontStyle = FontStyles.Bold;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            TextMeshProUGUI meta = CreateText("Meta", textColumn.transform, string.Empty, 10, TextAlignmentOptions.MidlineLeft);
            meta.color = MutedTextColor();
            meta.textWrappingMode = TextWrappingModes.NoWrap;
            meta.overflowMode = TextOverflowModes.Ellipsis;
            meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            button.AddComponent<ProductButtonView>().Configure(background, accent.gameObject, icon, name, meta);
            return button;
        }

        private static ColorStyleSetting CreateProductRowStyle(bool selected)
        {
            ColorStyleSetting cached = selected
                ? productSelectedStyle
                : productNormalStyle;
            if (cached != null)
            {
                return cached;
            }

            Color normal = selected
                ? OniPinkInactive()
                : new Color(0.17f, 0.19f, 0.25f, 1f);
            cached = CreateColorStyle(
                normal,
                selected ? OniPinkHover() : new Color(0.25f, 0.28f, 0.35f, 1f),
                selected ? OniPinkActive() : new Color(0.11f, 0.12f, 0.16f, 1f));
            if (selected)
            {
                productSelectedStyle = cached;
            }
            else
            {
                productNormalStyle = cached;
            }

            return cached;
        }

        private void SelectProduct(string productKey, bool rebuild = true)
        {
            selectedProductKey = productKey;
            selectedRouteIndex = 0;
            lastOrderStatus = null;
            orderDetailsSignature = null;
            orderTrackingSignature = null;
            ProductDisplayGroup product = orderProducts.FirstOrDefault(item => item.ProductKey == productKey);
            requestedProductAmount = product?.Routes.Count > 0 ? ProductionRecipeCatalog.GetRecipeResultForProduct(product.Routes[0].Recipe, product.ProductTag)?.amount ?? 1f : 1f;
            if (rebuild)
            {
                if (productListContent != null)
                {
                    RebuildProductList();
                }

                RebuildOrderDetails();
            }
        }

        private ProductDisplayGroup GetSelectedProduct()
        {
            return orderProducts.FirstOrDefault(product => product.ProductKey == selectedProductKey);
        }

        private sealed class ProductButtonView : MonoBehaviour
        {
            private KImage background;

            private GameObject accent;
            private bool? selectedState;
            private int lastMetaFingerprint = int.MinValue;

            public Image Icon { get; private set; }

            public TextMeshProUGUI Name { get; private set; }

            public TextMeshProUGUI Meta { get; private set; }

            public void Configure(KImage background, GameObject accent, Image icon, TextMeshProUGUI name, TextMeshProUGUI meta)
            {
                this.background = background;
                this.accent = accent;
                Icon = icon;
                Name = name;
                Meta = meta;
            }

            public void SetSelected(bool selected)
            {
                if (selectedState == selected)
                {
                    return;
                }

                selectedState = selected;
                if (background != null)
                {
                    background.colorStyleSetting = CreateProductRowStyle(selected);
                    background.ColorState = KImage.ColorSelector.Inactive;
                    background.ApplyColorStyleSetting();
                }

                if (accent != null)
                {
                    accent.SetActive(selected);
                }

                if (Name != null)
                {
                    Name.color = selected ? Color.white : new Color(0.90f, 0.92f, 0.95f, 1f);
                }

                if (Meta != null)
                {
                    Meta.color = selected ? new Color(0.93f, 0.86f, 0.90f, 1f) : new Color(0.70f, 0.73f, 0.78f, 1f);
                }
            }

            public void SetMeta(float availableAmount, int routeCount)
            {
                int fingerprint;
                unchecked
                {
                    fingerprint = (availableAmount.GetHashCode() * 397) ^ routeCount;
                }

                if (lastMetaFingerprint == fingerprint)
                {
                    return;
                }

                lastMetaFingerprint = fingerprint;
                SetTextIfChanged(Meta, string.Format(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_PRODUCT_META),
                    GameUtil.GetFormattedMass(availableAmount),
                    routeCount));
            }
        }

    }
}
