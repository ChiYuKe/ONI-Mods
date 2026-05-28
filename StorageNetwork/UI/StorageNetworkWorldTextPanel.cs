using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.UI.WorldPanel;
using UnityEngine;

namespace StorageNetwork.UI
{
    /// <summary>
    /// 建筑世界文字面板管理器。负责响应 Shift+左键、维护当前目标，并把内容交给 View 渲染。
    /// </summary>
    public sealed class StorageNetworkWorldTextPanel : MonoBehaviour
    {
        private const float RefreshSeconds = 0.5f;
        private const float DiagnosticSeconds = 2f;
        private static readonly bool EnableDiagnosticLogging = false;

        private static readonly DefaultStorageNetworkWorldPanelContentProvider DefaultContentProvider =
            new DefaultStorageNetworkWorldPanelContentProvider();

        private static StorageNetworkWorldTextPanel instance;
        private readonly StorageNetworkWorldTextPanelView view = new StorageNetworkWorldTextPanelView();
        private GameObject target;
        private GameObject lastHandledSelection;
        private int lastHandledSelectionFrame = -1;
        private float refreshTimer;
        private float diagnosticTimer;

        /// <summary>
        /// 在稳定存在的 UI 对象上安装世界面板管理器。多次调用是安全的。
        /// </summary>
        public static void EnsureInstalled(GameObject owner)
        {
            if (owner == null || owner.GetComponent<StorageNetworkWorldTextPanel>() != null)
            {
                return;
            }

            StorageNetworkWorldPanelRegistry.Register(DefaultContentProvider);
            owner.AddComponent<StorageNetworkWorldTextPanel>();
            Debug.Log("[StorageNetworkWorldTextPanel] Installed on " + owner.name);
        }

        /// <summary>
        /// 供 SelectToolPatch 调用：玩家 Shift+左键选中建筑时尝试显示或隐藏面板。
        /// </summary>
        public static void HandleSelectionClick()
        {
            if (instance == null)
            {
                Debug.Log("[StorageNetworkWorldTextPanel] shift-left-click ignored; manager is not installed.");
                return;
            }

            KSelectable selected = SelectTool.Instance != null ? SelectTool.Instance.selected : null;
            GameObject selectedObject = selected != null ? selected.gameObject : null;
            if (instance.WasAlreadyHandledThisFrame(selectedObject))
            {
                return;
            }

            instance.ToggleForTarget(selectedObject);
        }

        private void Awake()
        {
            instance = this;
            StorageNetworkWorldPanelRegistry.Register(DefaultContentProvider);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void LateUpdate()
        {
            if (target == null || DebugHandler.HideUI)
            {
                view.SetVisible(false);
                return;
            }

            if (!StorageNetworkMembership.IsNetworkMember(target, out string reason))
            {
                Debug.Log(string.Format(
                    "[StorageNetworkWorldTextPanel] Hiding panel; target={0}, reason={1}",
                    target != null ? target.name : "<null>",
                    reason));
                ClearTarget();
                return;
            }

            if (!view.EnsureCreated())
            {
                return;
            }

            refreshTimer += Time.unscaledDeltaTime;
            if (refreshTimer >= RefreshSeconds)
            {
                refreshTimer = 0f;
                RefreshContent();
            }

            view.UpdatePosition(target);
            view.SetVisible(true);
            // LogDiagnostic();
        }

        private void ToggleForTarget(GameObject selectedObject)
        {
            bool eligible = StorageNetworkMembership.IsNetworkMember(selectedObject, out string reason);
            Debug.Log(string.Format(
                "[StorageNetworkWorldTextPanel] shift-left-click selected={0}, eligible={1}, reason={2}, worldCanvas={3}",
                selectedObject != null ? selectedObject.name : "<null>",
                eligible,
                reason,
                GameScreenManager.Instance?.worldSpaceCanvas != null));

            if (!eligible)
            {
                ClearTarget();
                return;
            }

            if (target == selectedObject && view.IsVisible)
            {
                ClearTarget();
                return;
            }

            target = selectedObject;
            refreshTimer = RefreshSeconds;
            if (view.EnsureCreated())
            {
                RefreshContent();
                view.UpdatePosition(target);
                view.SetVisible(true);
            }
        }

        private bool WasAlreadyHandledThisFrame(GameObject selectedObject)
        {
            if (lastHandledSelectionFrame == Time.frameCount && lastHandledSelection == selectedObject)
            {
                return true;
            }

            lastHandledSelectionFrame = Time.frameCount;
            lastHandledSelection = selectedObject;
            return false;
        }

        private void ClearTarget()
        {
            target = null;
            view.SetVisible(false);
        }

        private void RefreshContent()
        {
            if (target == null)
            {
                return;
            }

            if (StorageNetworkWorldPanelRegistry.TryBuildContent(target, out StorageNetworkWorldPanelContent content))
            {
                view.SetContent(content);
            }
        }

        private void LogDiagnostic()
        {
            if (!EnableDiagnosticLogging)
            {
                return;
            }

            diagnosticTimer += Time.unscaledDeltaTime;
            if (diagnosticTimer < DiagnosticSeconds)
            {
                return;
            }

            diagnosticTimer = 0f;
            view.LogDiagnostic(target);
        }
    }
}
