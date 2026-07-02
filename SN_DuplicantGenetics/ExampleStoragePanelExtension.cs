using System.Collections.Generic;
using StorageNetwork.API;
using UnityEngine;

namespace SN_DuplicantGenetics
{
    /// <summary>
    /// 主面板仓库行扩展示例。这里不主动挂到任何建筑；附属模组作者可把这个组件加到自己的 prefab 上。
    /// </summary>
    internal sealed class ExampleStoragePanelExtension : KMonoBehaviour,
        IStorageNetworkCategoryProvider,
        IStorageNetworkDisplayProvider,
        IStorageNetworkStorageRowButtonProvider
    {
        private const string CategoryKey = "sn_duplicant_genetics";

        public StorageNetworkCategoryDescriptor GetStorageNetworkCategory(Storage storage)
        {
            return new StorageNetworkCategoryDescriptor(CategoryKey, "复制人遗传", 2);
        }

        public StorageNetworkDisplayInfo GetStorageNetworkDisplayInfo(Storage storage)
        {
            return new StorageNetworkDisplayInfo
            {
                TypeKey = CategoryKey + "_storage",
                TypeName = "遗传样本库",
                RowName = gameObject.GetProperName(),
                TypeIcon = Assets.GetSprite("crew_state_learning"),
                TypeIconTint = Color.white
            };
        }

        public IEnumerable<StorageNetworkStorageRowButton> GetStorageNetworkStorageRowButtons(Storage storage)
        {
            yield return new StorageNetworkStorageRowButton(
                "open_genetics_panel",
                "遗传",
                OnOpenGeneticsPanel,
                "打开复制人遗传面板",
                "crew_state_learning",
                "DNA",
                58f,
                10);
        }

        private static void OnOpenGeneticsPanel(StorageNetworkStorageRowButtonContext context)
        {
            Debug.Log("[SN_DuplicantGenetics] 点击仓库行扩展按钮: " +
                      (context.Storage != null ? context.Storage.GetProperName() : "null"));
        }
    }
}
