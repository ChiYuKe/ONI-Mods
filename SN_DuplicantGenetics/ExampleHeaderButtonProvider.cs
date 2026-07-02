using System.Collections.Generic;
using StorageNetwork.API;
using UnityEngine;

namespace SN_DuplicantGenetics
{
    internal sealed class ExampleHeaderButtonProvider : IStorageNetworkPanelHeaderButtonProvider
    {
        private static DuplicantGeneticsPanel openPanel;

        public IEnumerable<StorageNetworkPanelHeaderButton> GetHeaderButtons()
        {
            yield return new StorageNetworkPanelHeaderButton(
                "sn_duplicant_genetics",
                "基因",
                OnClick,
                "打开复制人档案面板",
                "crew_state_learning",
                "DNA",
                72f,
                10);
        }

        private static void OnClick(StorageNetworkPanelHeaderButtonContext context)
        {
            Debug.Log("[SN_DuplicantGenetics] 基因按钮点击。Canvas=" + (context.CanvasObject != null ? context.CanvasObject.name : "null"));
            if (openPanel != null && openPanel.gameObject != null)
            {
                Debug.Log("[SN_DuplicantGenetics] 关闭已打开的复制人档案面板。");
                Object.Destroy(openPanel.gameObject);
                openPanel = null;
                return;
            }

            if (context.Canvas == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] 无法打开复制人档案面板：缺少Canvas上下文。");
                return;
            }

            openPanel = DuplicantGeneticsPanel.Create(context.Canvas, context.PanelRootRect);
            if (openPanel == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] 面板创建失败，详见上方AB加载日志。");
            }
        }
    }
}
