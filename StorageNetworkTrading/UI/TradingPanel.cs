using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Trading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel : KScreen, IInputHandler
    {
        private static TradingPanel instance;
        private static Dictionary<string, Sprite> spriteCache;
        private GameObject modalRoot;
        internal static void Show()
        {
            if (instance == null)
            {
                var root = new GameObject("StorageNetworkTrading", typeof(RectTransform));
                root.transform.SetParent(GameScreenManager.Instance.ssOverlayCanvas.transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                instance = root.AddComponent<TradingPanel>();
                instance.activateOnSpawn = false;
                instance.ConsumeMouseScroll = true;
            }

            instance.gameObject.SetActive(true);
            if (!instance.IsActive())
                instance.Activate();
            instance.ToggleTradingWindow();
        }

        private void Update()
        {
            StorageNetworkUiAccess.TrackRightClickClose(out _);
            if (!gameObject.activeInHierarchy) return;
            UpdateTradingGroupChat(Time.unscaledDeltaTime);
            UpdateTradingWindow(Time.unscaledDeltaTime);
            RunDeferredTradingLayout();
        }

        internal static bool IsOpen() => instance != null && instance.gameObject.activeInHierarchy;
        internal static void CloseTopWindow()
        {
            if (!IsOpen()) return;
            if (instance.modalRoot != null) instance.CloseModal();
            else instance.CloseTradingWindow();
        }
        internal static bool IsTextInputFocused() => StorageNetworkNumberInputField.IsAnyEditing || StorageNetworkTextInputGuard.IsAnyFocused;
        private int GetActiveWorldFilterId() => StorageNetworkUiAccess.CurrentWorldId;
        private void CloseModal()
        {
            if (modalRoot == tradingGroupRoot && modalRoot != null)
                StorageNetworkTradingState.Instance?.CancelGroupChat();
            if (modalRoot != null)
            {
                modalRoot.SetActive(false);
                Destroy(modalRoot);
                modalRoot = null;
            }
        }

        public override void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed)
                return;
            if (TryConsumeCameraZoom(e))
                return;
            if (IsTextInputFocused())
            {
                e.Consumed = true;
                return;
            }

            if (e.TryConsume(global::Action.Escape))
            {
                if (modalRoot != null)
                    CloseModal();
                else
                {
                    CloseTradingWindow();
                    Deactivate();
                    gameObject.SetActive(false);
                }

                return;
            }

            if (modalRoot != null)
                e.Consumed = true;
        }

        public override void OnKeyUp(KButtonEvent e)
        {
            if (!e.Consumed && !TryConsumeCameraZoom(e)) base.OnKeyUp(e);
        }

        internal static void ResetRuntimeState()
        {
            if (instance == null)
                return;
            instance.CloseTradingGroupChat();
            Destroy(instance.gameObject);
            instance = null;
        }
    }
}
