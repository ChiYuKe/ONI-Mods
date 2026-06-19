using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        public static void Show(Storage focusStorage = null)
        {
            if (instance == null)
            {
                instance = Create();
            }

            instance.boundOrderProductionCenter = null;
            instance.productionOrderService.SetOrderCenterScope(null);
            if (instance.windowRect != null)
            {
                instance.windowRect.gameObject.SetActive(true);
            }

            instance.SetSnapshot(focusStorage);
            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            if (!instance.IsActive())
            {
                instance.Activate();
            }
        }

        public static void ShowOrderProductionCenter(Components.StorageNetworkOrderProductionCenter center)
        {
            if (center == null)
            {
                return;
            }

            if (instance == null)
            {
                instance = Create();
            }

            instance.boundOrderProductionCenter = center;
            instance.productionOrderService.SetOrderCenterScope(center);
            if (instance.windowRect != null)
            {
                instance.windowRect.gameObject.SetActive(false);
            }

            if (!instance.gameObject.activeSelf)
            {
                instance.gameObject.SetActive(true);
            }

            if (!instance.IsActive())
            {
                instance.Activate();
            }

            instance.EnsureHeaderWindow();
            instance.headerWindowRoot.SetActive(true);
            instance.orderDetailsSignature = null;
            instance.orderTrackingSignature = null;
            instance.orderPanelRefreshElapsed = 0f;
            instance.RefreshOrderPanel(true);
        }

        public static void ShowSettings(Storage focusStorage)
        {
            if (focusStorage == null)
            {
                return;
            }

            Show(focusStorage);
            if (instance != null)
            {
                instance.ShowStorageSettingsDialog(focusStorage);
            }
        }

        public static void ShowLiquidOutputFilterPicker(Storage storage, Components.StorageNetworkLiquidOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowStandaloneOutputFilterPicker(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortLiquidFilterOptions(storage, egress, CloseStandaloneOutputFilterPickerAction, null));
        }

        public static void ShowGasOutputFilterPicker(Storage storage, Components.StorageNetworkGasOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowStandaloneOutputFilterPicker(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortGasFilterOptions(storage, egress, CloseStandaloneOutputFilterPickerAction, null));
        }

        public static void ShowMaterialOutputFilterPicker(Storage storage, Components.StorageNetworkSolidOutputPortEgress egress)
        {
            if (storage == null || egress == null)
            {
                return;
            }

            ShowStandaloneOutputFilterPicker(
                StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortMaterialFilterOptions(storage, egress, CloseStandaloneOutputFilterPickerAction, null));
        }

        public static bool IsOpen()
        {
            return instance != null && instance.gameObject != null && instance.gameObject.activeInHierarchy;
        }

        public static void ResetRuntimeState()
        {
            if (instance != null && instance.gameObject != null)
            {
                Destroy(instance.gameObject);
            }

            instance = null;
            CloseStandaloneOutputFilterPicker();
            spriteCache?.Clear();
        }

        public static bool IsTextInputFocused()
        {
            return IsOpen() && (StorageNetworkNumberInputField.IsAnyEditing || StorageNetworkTextInputGuard.IsAnyFocused);
        }

        public static bool CloseFromRightClick()
        {
            if (standaloneOutputFilterPickerRoot != null)
            {
                CloseStandaloneOutputFilterPicker();
                return true;
            }

            if (!IsOpen())
            {
                return false;
            }

            if (instance.modalRoot != null)
            {
                instance.CloseModal();
            }
            else if (instance.productionPickerRoot != null)
            {
                instance.CloseProductionPicker();
            }
            else if (instance.productionSettingsRoot != null && instance.productionSettingsRoot.activeSelf)
            {
                instance.CloseProductionSettingsPanel();
            }
            else if (instance.enrollableWindowRoot != null && instance.enrollableWindowRoot.activeSelf)
            {
                instance.CloseEnrollableWindow();
            }
            else if (instance.headerWindowRoot != null && instance.headerWindowRoot.activeSelf)
            {
                instance.CloseHeaderWindow();
            }
            else
            {
                instance.Close();
            }

            return true;
        }

        public static bool BeginRightClickCloseCandidate()
        {
            if (!IsOpen() && standaloneOutputFilterPickerRoot == null)
            {
                return false;
            }

            if (standaloneOutputFilterPickerRoot != null)
            {
                standaloneRightClickCloseCandidate = true;
                standaloneRightClickStartPosition = KInputManager.GetMousePos();
                return true;
            }

            if (instance != null)
            {
                instance.rightClickCloseCandidate = true;
                instance.rightClickStartPosition = KInputManager.GetMousePos();
            }

            return true;
        }

        public static bool FinishRightClickCloseCandidate(out bool closed)
        {
            closed = false;
            if (standaloneOutputFilterPickerRoot != null)
            {
                bool closeCandidate = standaloneRightClickCloseCandidate;
                standaloneRightClickCloseCandidate = false;
                if (!closeCandidate)
                {
                    return true;
                }

                Vector3 standaloneDelta = KInputManager.GetMousePos() - standaloneRightClickStartPosition;
                if (standaloneDelta.sqrMagnitude > RightClickDragThresholdPixels * RightClickDragThresholdPixels)
                {
                    return true;
                }

                closed = CloseStandaloneOutputFilterPicker();
                return true;
            }

            if (!IsOpen() || !instance.rightClickCloseCandidate)
            {
                return false;
            }

            instance.rightClickCloseCandidate = false;
            Vector3 delta = KInputManager.GetMousePos() - instance.rightClickStartPosition;
            if (delta.sqrMagnitude > RightClickDragThresholdPixels * RightClickDragThresholdPixels)
            {
                return true;
            }

            closed = CloseFromRightClick();
            return true;
        }

        private static StorageNetworkPanel Create()
        {
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkPanel");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.08f);
            blocker.raycastTarget = false;

            StorageNetworkPanel panel = root.AddComponent<StorageNetworkPanel>();
            panel.activateOnSpawn = false;
            panel.ConsumeMouseScroll = true;
            panel.BuildWindow(root.transform);
            root.SetActive(false);
            return panel;
        }

        private void TrackRightClickCloseGesture()
        {
            if (Input.GetMouseButtonDown(1))
            {
                BeginRightClickCloseCandidate();
                return;
            }

            if (Input.GetMouseButtonUp(1))
            {
                FinishRightClickCloseCandidate(out _);
            }
        }

        private bool IsMouseOverAnyPanel()
        {
            Vector2 mousePosition = KInputManager.GetMousePos();
            return ContainsScreenPoint(windowRect, mousePosition) ||
                ContainsScreenPoint(productionSettingsRoot, mousePosition) ||
                ContainsScreenPoint(categorySummaryRoot, mousePosition) ||
                ContainsScreenPoint(enrollableWindowRoot, mousePosition) ||
                ContainsScreenPoint(headerWindowRoot, mousePosition) ||
                ContainsScreenPoint(modalRoot, mousePosition);
        }

        private static bool ContainsScreenPoint(GameObject gameObject, Vector2 screenPoint)
        {
            return gameObject != null &&
                gameObject.activeInHierarchy &&
                ContainsScreenPoint(gameObject.GetComponent<RectTransform>(), screenPoint);
        }

        private static bool ContainsScreenPoint(RectTransform rectTransform, Vector2 screenPoint)
        {
            return rectTransform != null &&
                rectTransform.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, null);
        }

        private void CloseModal()
        {
            if (modalRoot != null)
            {
                Destroy(modalRoot);
                modalRoot = null;
            }
        }

        private void Close()
        {
            CloseModal();
            CloseCategorySummaryPanel();
            CloseProductionSettingsPanel();
            CloseGeyserSettingsPanel();
            CloseEnrollableWindow();
            CloseMainWorldDropdown();
            CloseHeaderWindow();
            if (IsActive())
            {
                Deactivate();
            }

            gameObject.SetActive(false);
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed)
            {
                return;
            }

            if (IsTextInputFocused())
            {
                e.Consumed = true;
                return;
            }

            if (modalRoot != null)
            {
                if (e.TryConsume(global::Action.Escape))
                {
                    CloseModal();
                    return;
                }

                e.Consumed = true;
                return;
            }

            if (e.TryConsume(global::Action.Escape))
            {
                if (enrollableWindowRoot != null && enrollableWindowRoot.activeSelf)
                {
                    CloseEnrollableWindow();
                    return;
                }

                if (headerWindowRoot != null && headerWindowRoot.activeSelf)
                {
                    CloseHeaderWindow();
                    return;
                }

                Close();
                return;
            }

            if (!IsMouseOverAnyPanel())
            {
                return;
            }

            if (!e.TryConsume(global::Action.ZoomIn))
            {
                e.TryConsume(global::Action.ZoomOut);
            }
        }
    }
}
