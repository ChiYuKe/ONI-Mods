using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private GameObject dragPreviewRoot;
        private Image dragPreviewIcon;
        private TextMeshProUGUI dragPreviewText;
        private Storage dragSourceStorage;
        private string dragSourceItemKey;
        private string dragSourceItemName;
        private GameObject dragSourceRepresentative;
        private bool dragActive;
        private readonly List<StorageDropArea> storageDropAreas = new List<StorageDropArea>();
        private readonly List<CategoryDropArea> categoryDropAreas = new List<CategoryDropArea>();

        private bool IsDraggingItem => dragSourceStorage != null && !string.IsNullOrEmpty(dragSourceItemKey);

        private void RegisterItemDragSource(GameObject row, Storage storage, string itemKey, string itemName, GameObject representative)
        {
            if (row == null || storage == null || string.IsNullOrEmpty(itemKey))
            {
                return;
            }

            StorageItemDragSource dragSource = row.GetComponent<StorageItemDragSource>() ?? row.AddComponent<StorageItemDragSource>();
            dragSource.Bind(this, storage, itemKey, itemName, representative);
        }

        private void RegisterStorageDropTarget(GameObject row, Storage storage)
        {
            if (row == null || storage == null)
            {
                return;
            }

            StorageDropTarget dropTarget = row.GetComponent<StorageDropTarget>() ?? row.AddComponent<StorageDropTarget>();
            dropTarget.Bind(this, storage);

            RectTransform rect = row.GetComponent<RectTransform>();
            if (rect != null)
            {
                storageDropAreas.Add(new StorageDropArea(rect, storage));
            }
        }

        private void RegisterCategoryDropTarget(GameObject button, string categoryKey)
        {
            if (button == null || string.IsNullOrEmpty(categoryKey))
            {
                return;
            }

            StorageCategoryDropTarget dropTarget = button.GetComponent<StorageCategoryDropTarget>() ?? button.AddComponent<StorageCategoryDropTarget>();
            dropTarget.Bind(this, categoryKey);

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                categoryDropAreas.Add(new CategoryDropArea(rect, categoryKey));
            }
        }

        private void BeginItemDrag(Storage storage, string itemKey, string itemName, GameObject representative)
        {
            dragSourceStorage = storage;
            dragSourceItemKey = itemKey;
            dragSourceItemName = itemName;
            dragSourceRepresentative = representative;
            dragActive = true;
            EnsureDragPreview();
            dragPreviewRoot.SetActive(true);
            dragPreviewText.text = itemName;
            StorageNetworkStorageDisplay.SetStoredItemIcon(dragPreviewIcon, representative);
            UpdateDragPreviewPosition(KInputManager.GetMousePos());
        }

        private void UpdateItemDrag(Vector2 screenPosition)
        {
            if (dragPreviewRoot == null || !dragPreviewRoot.activeSelf)
            {
                return;
            }

            UpdateDragPreviewPosition(screenPosition);
            SelectCategoryAt(screenPosition);
        }

        private void CompleteItemDrag(Vector2 screenPosition)
        {
            if (!dragActive)
            {
                return;
            }

            Storage target = FindStorageDropTargetAt(screenPosition);
            if (target != null)
            {
                TryDropDraggedItem(target);
            }

            if (dragPreviewRoot != null)
            {
                dragPreviewRoot.SetActive(false);
            }

            dragSourceStorage = null;
            dragSourceItemKey = null;
            dragSourceItemName = null;
            dragSourceRepresentative = null;
            dragActive = false;
        }

        private void UpdatePanelDrag()
        {
            if (!dragActive)
            {
                return;
            }

            Vector2 mousePosition = KInputManager.GetMousePos();
            UpdateItemDrag(mousePosition);
            if (Input.GetMouseButtonUp(0))
            {
                CompleteItemDrag(mousePosition);
            }
        }

        private void TryDropDraggedItem(Storage targetStorage)
        {
            if (dragSourceStorage == null || string.IsNullOrEmpty(dragSourceItemKey) || targetStorage == null || targetStorage == dragSourceStorage)
            {
                return;
            }

            List<GameObject> items = FindStoredItems(dragSourceStorage, dragSourceItemKey);
            if (items.Count == 0)
            {
                return;
            }

            float amount = Mathf.Min(GetStoredItemsMass(items), Mathf.Max(0f, targetStorage.RemainingCapacity()));
            if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                ShowMessageDialog(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_ITEM_TITLE),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TARGET_NOT_ENOUGH_CAPACITY));
                return;
            }

            TransferSelectedItem(dragSourceStorage, dragSourceItemKey, targetStorage, amount);
        }

        private void SelectCategoryDuringDrag(string categoryKey)
        {
            if (!IsDraggingItem || string.IsNullOrEmpty(categoryKey) || selectedCategoryKey == categoryKey)
            {
                return;
            }

            selectedCategoryKey = categoryKey;
            selectedItemStorage = null;
            selectedItemKey = null;
            lastListSignature = null;
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);

            if (dragPreviewRoot != null)
            {
                dragPreviewRoot.transform.SetAsLastSibling();
            }
        }

        private void SelectCategoryAt(Vector2 screenPosition)
        {
            if (!IsDraggingItem)
            {
                return;
            }

            for (int i = categoryDropAreas.Count - 1; i >= 0; i--)
            {
                CategoryDropArea area = categoryDropAreas[i];
                if (area.Rect == null || !area.Rect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(area.Rect, screenPosition))
                {
                    SelectCategoryDuringDrag(area.CategoryKey);
                    return;
                }
            }
        }

        private void ClearStorageDropAreas()
        {
            storageDropAreas.Clear();
            categoryDropAreas.Clear();
        }

        private Storage FindStorageDropTargetAt(Vector2 screenPosition)
        {
            for (int i = storageDropAreas.Count - 1; i >= 0; i--)
            {
                StorageDropArea area = storageDropAreas[i];
                if (area.Storage == null || area.Rect == null || !area.Rect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(area.Rect, screenPosition))
                {
                    return area.Storage;
                }
            }

            return null;
        }

        private void EnsureDragPreview()
        {
            if (dragPreviewRoot != null)
            {
                return;
            }

            dragPreviewRoot = CreatePlainImage("DragPreview", transform, new Color(0.18f, 0.20f, 0.24f, 0.92f));
            RectTransform rect = dragPreviewRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(190f, 28f);
            dragPreviewRoot.transform.SetAsLastSibling();
            dragPreviewRoot.AddComponent<CanvasGroup>().blocksRaycasts = false;

            HorizontalLayoutGroup layout = dragPreviewRoot.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 3, 3);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            GameObject iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(dragPreviewRoot.transform, false);
            iconObject.AddComponent<RectTransform>();
            LayoutElement iconLayout = iconObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 22f;
            iconLayout.preferredHeight = 22f;
            dragPreviewIcon = iconObject.AddComponent<Image>();
            dragPreviewIcon.raycastTarget = false;
            dragPreviewIcon.preserveAspect = true;

            dragPreviewText = CreateText("Text", dragPreviewRoot.transform, string.Empty, 12, TextAlignmentOptions.MidlineLeft);
            dragPreviewText.color = Color.white;
            dragPreviewText.textWrappingMode = TextWrappingModes.NoWrap;
            dragPreviewText.overflowMode = TextOverflowModes.Ellipsis;
            dragPreviewText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            dragPreviewRoot.SetActive(false);
        }

        private void UpdateDragPreviewPosition(Vector2 screenPosition)
        {
            if (dragPreviewRoot == null)
            {
                return;
            }

            RectTransform rect = dragPreviewRoot.GetComponent<RectTransform>();
            rect.position = screenPosition + new Vector2(18f, -18f);
        }

        private sealed class StorageItemDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private StorageNetworkPanel panel;
            private Storage storage;
            private string itemKey;
            private string itemName;
            private GameObject representative;

            public void Bind(StorageNetworkPanel owner, Storage sourceStorage, string sourceItemKey, string sourceItemName, GameObject sourceRepresentative)
            {
                panel = owner;
                storage = sourceStorage;
                itemKey = sourceItemKey;
                itemName = sourceItemName;
                representative = sourceRepresentative;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                panel?.BeginItemDrag(storage, itemKey, itemName, representative);
            }

            public void OnDrag(PointerEventData eventData)
            {
                panel?.UpdateItemDrag(eventData.position);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                panel?.CompleteItemDrag(eventData.position);
            }
        }

        private sealed class StorageDropTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private StorageNetworkPanel panel;
            private Storage storage;
            private Image background;
            private Color originalColor;

            public void Bind(StorageNetworkPanel owner, Storage targetStorage)
            {
                panel = owner;
                storage = targetStorage;
                background = GetComponent<Image>();
                if (background != null)
                {
                    originalColor = background.color;
                }
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (panel != null && panel.IsDraggingItem && background != null && storage != panel.dragSourceStorage)
                {
                    background.color = new Color(0.77f, 0.82f, 0.84f, 1f);
                }
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                if (background != null)
                {
                    background.color = originalColor;
                }
            }
        }

        private sealed class StorageCategoryDropTarget : MonoBehaviour, IPointerEnterHandler
        {
            private StorageNetworkPanel panel;
            private string categoryKey;

            public void Bind(StorageNetworkPanel owner, string key)
            {
                panel = owner;
                categoryKey = key;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                panel?.SelectCategoryDuringDrag(categoryKey);
            }
        }

        private readonly struct StorageDropArea
        {
            public StorageDropArea(RectTransform rect, Storage storage)
            {
                Rect = rect;
                Storage = storage;
            }

            public RectTransform Rect { get; }

            public Storage Storage { get; }
        }

        private readonly struct CategoryDropArea
        {
            public CategoryDropArea(RectTransform rect, string categoryKey)
            {
                Rect = rect;
                CategoryKey = categoryKey;
            }

            public RectTransform Rect { get; }

            public string CategoryKey { get; }
        }
    }
}

