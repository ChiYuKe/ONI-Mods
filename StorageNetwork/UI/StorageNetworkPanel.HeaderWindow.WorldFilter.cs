using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.ProductionOrders;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void CreateOrderWorldFilter(Transform pane)
        {
            GameObject filter = new GameObject("OrderWorldFilter");
            filter.transform.SetParent(pane, false);
            orderWorldFilterContent = filter.AddComponent<RectTransform>();
            orderWorldFilterContent.anchorMin = new Vector2(1f, 1f);
            orderWorldFilterContent.anchorMax = new Vector2(1f, 1f);
            orderWorldFilterContent.pivot = new Vector2(1f, 1f);
            orderWorldFilterContent.anchoredPosition = new Vector2(-28f, -10f);
            orderWorldFilterContent.sizeDelta = new Vector2(compactOrderWindow ? 116f : 136f, 22f);
        }

        private void RebuildOrderWorldFilter()
        {
            if (orderWorldFilterContent == null)
            {
                return;
            }

            for (int i = orderWorldFilterContent.childCount - 1; i >= 0; i--)
            {
                Destroy(orderWorldFilterContent.GetChild(i).gameObject);
            }

            GameObject dropdownButton = CreateStyledButton(
                "OrderWorldFilterDropdown",
                orderWorldFilterContent,
                GetSelectedOrderWorldFilterText(),
                ToggleOrderWorldDropdown,
                CreateColorStyle(
                    new Color(0.17f, 0.19f, 0.25f, 1f),
                    new Color(0.25f, 0.28f, 0.35f, 1f),
                    new Color(0.11f, 0.12f, 0.16f, 1f)));
            RectTransform rect = dropdownButton.GetComponent<RectTransform>();
            Stretch(rect, 0f, 0f);
            SetButtonLabelColor(dropdownButton, new Color(0.92f, 0.93f, 0.90f, 1f), FontStyles.Normal);
            AddDropdownArrowIcon(dropdownButton.transform);
        }

        private void ToggleOrderWorldDropdown()
        {
            if (orderWorldDropdownRoot != null)
            {
                CloseOrderWorldDropdown();
                return;
            }

            ShowOrderWorldDropdown();
        }

        private void ShowOrderWorldDropdown()
        {
            if (headerWindowRoot == null)
            {
                return;
            }

            CloseOrderWorldDropdown();
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            List<int> worldIds = GetOrderWorldIds(relayOnline);
            int optionCount = worldIds.Count + 1;
            float height = Mathf.Min(20f + Mathf.Max(1, optionCount) * 30f, 250f);

            Transform dropdownParent = orderWorldFilterContent != null && orderWorldFilterContent.parent != null
                ? orderWorldFilterContent.parent
                : headerWindowRoot.transform;
            orderWorldDropdownRoot = CreatePlainImage("OrderWorldFilterDropdownPanel", dropdownParent, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            orderWorldDropdownRoot.AddComponent<ScrollWheelBlocker>();
            ApplyThinBoxSprite(orderWorldDropdownRoot.GetComponent<Image>());
            RectTransform dropdownRect = orderWorldDropdownRoot.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(1f, 1f);
            dropdownRect.anchorMax = new Vector2(1f, 1f);
            dropdownRect.pivot = new Vector2(1f, 1f);
            dropdownRect.anchoredPosition = new Vector2(-28f, -34f);
            dropdownRect.sizeDelta = new Vector2(compactOrderWindow ? 128f : 148f, height);

            GameObject viewport = CreatePlainImage("Viewport", orderWorldDropdownRoot.transform, new Color(0.73f, 0.73f, 0.67f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 6f, 6f, 8f, 8f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateOrderWorldDropdownOption(content.transform, AllEnrollableWorldsFilterId, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL));

            foreach (int worldId in worldIds)
            {
                CreateOrderWorldDropdownOption(content.transform, worldId, StorageNetworkWorldDisplay.GetWorldName(worldId));
            }

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRect;
            ConfigureSmoothVerticalScroll(scrollRect, 22f);
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        private void CreateOrderWorldDropdownOption(Transform parent, int worldId, string text)
        {
            bool selected = orderWorldFilterId == worldId;
            GameObject row = CreateStyledButton("OrderWorldFilterOption", parent, text, () =>
            {
                orderWorldFilterId = worldId;
                CloseOrderWorldDropdown();
                orderDetailsSignature = null;
                orderTrackingSignature = null;
                RefreshOrderPanel(true);
            }, selected ? KleiPinkStyle() : CreateColorStyle(
                new Color(0.80f, 0.80f, 0.73f, 1f),
                new Color(0.87f, 0.87f, 0.80f, 1f),
                new Color(0.67f, 0.68f, 0.62f, 1f)));
            SetButtonLabelColor(row, selected ? Color.white : new Color(0.23f, 0.26f, 0.26f, 1f), FontStyles.Bold);
            AddWorldFilterOptionIcon(row.transform, worldId);
            LayoutElement layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 26f;
        }

        private void CloseOrderWorldDropdown()
        {
            if (orderWorldDropdownRoot != null)
            {
                Destroy(orderWorldDropdownRoot);
                orderWorldDropdownRoot = null;
            }
        }

        private string GetSelectedOrderWorldFilterText()
        {
            return orderWorldFilterId == AllEnrollableWorldsFilterId
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_WORLD_ALL)
                : StorageNetworkWorldDisplay.GetWorldName(orderWorldFilterId);
        }

        private void EnsureValidOrderWorldFilter()
        {
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            int activeWorldId = GetActiveWorldFilterId();
            if (orderWorldFilterId == UnsetEnrollableWorldFilterId)
            {
                orderWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
                return;
            }

            if (orderWorldFilterId == AllEnrollableWorldsFilterId)
            {
                return;
            }

            if (GetOrderWorldIds(relayOnline).Contains(orderWorldFilterId))
            {
                return;
            }

            orderWorldFilterId = activeWorldId != UnsetEnrollableWorldFilterId ? activeWorldId : AllEnrollableWorldsFilterId;
        }

        private List<ProductDisplayGroup> GetFilteredOrderProductGroups()
        {
            bool relayOnline = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            IEnumerable<RecipeDisplayInfo> recipes = craftableRecipes.SelectMany(recipe => BuildReachableOrderRoutes(recipe, relayOnline));
            return ProductionRecipeCatalog.BuildProductGroups(recipes.ToList());
        }

        private IEnumerable<RecipeDisplayInfo> BuildReachableOrderRoutes(RecipeDisplayInfo recipe, bool relayOnline)
        {
            List<ComplexFabricator> fabricators = recipe.Fabricators
                .Where(fabricator => fabricator != null && IsOrderFabricatorReachable(fabricator, relayOnline))
                .ToList();
            if (fabricators.Count == 0)
            {
                yield break;
            }

            yield return new RecipeDisplayInfo(
                recipe.Name,
                ProductionOrderFormatting.FormatFabricatorGroupName(fabricators),
                recipe.Details,
                recipe.Recipe,
                fabricators,
                recipe.Icon,
                recipe.ProductKey,
                recipe.ProductName,
                recipe.ProductTag,
                fabricators
                    .Select(fabricator => StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject))
                    .Where(worldId => worldId >= 0)
                    .Distinct()
                    .OrderBy(worldId => worldId)
                    .ToList());
        }

        private bool IsOrderFabricatorReachable(ComplexFabricator fabricator, bool relayOnline)
        {
            if (fabricator == null)
            {
                return false;
            }

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(fabricator.gameObject);
            if (!StorageNetworkWorldDisplay.IsWorldDiscovered(worldId))
            {
                return false;
            }

            if (orderWorldFilterId == AllEnrollableWorldsFilterId)
            {
                return relayOnline || worldId == GetActiveWorldFilterId();
            }

            return worldId == orderWorldFilterId &&
                   (relayOnline || worldId == GetActiveWorldFilterId());
        }

        private List<int> GetOrderWorldIds(bool relayOnline)
        {
            return GetEnrollableWorldIds(StorageSceneRegistry
                .GetEnrollments()
                .Where(enrollment => enrollment != null && enrollment.CanShowInEnrollableList()));
        }

        private bool IsOrderWorldFilterBlockedByRelay()
        {
            int activeWorldId = GetActiveWorldFilterId();
            return orderWorldFilterId != AllEnrollableWorldsFilterId &&
                   orderWorldFilterId != activeWorldId &&
                   !StorageSceneRegistry.IsCrossPlanetRelayOnline();
        }
    }
}
