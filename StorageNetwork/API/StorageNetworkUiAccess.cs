using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>附属界面使用的世界上下文、图标与输入焦点接口。</summary>
    public static partial class StorageNetworkUiAccess
    {
        private static readonly Dictionary<string, Func<bool>> TextInputProviders =
            new Dictionary<string, Func<bool>>(StringComparer.Ordinal);

        /// <summary>与储存网络面板一致的当前世界上下文。</summary>
        public static int CurrentWorldId => StorageNetworkPanel.CurrentWorldFilterId;

        /// <summary>获取游戏或前置模组图标，找不到时返回 null。</summary>
        public static Sprite GetSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name == "storage_network_overlay")
            {
                Sprite icon = StorageNetworkSprites.GetOverviewIcon();
                if (icon != null) return icon;
            }
            return StorageNetworkSpriteLoader.GetSprite(name);
        }

        /// <summary>按模组 ID 注册输入焦点查询；重复 ID 替换原查询，不覆盖其他模组。</summary>
        public static void RegisterTextInputFocus(string id, Func<bool> isFocused)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("A stable provider ID is required.", nameof(id));
            if (isFocused == null) throw new ArgumentNullException(nameof(isFocused));
            TextInputProviders[id] = isFocused;
        }

        /// <summary>卸载附属界面时撤销其焦点查询。</summary>
        public static void UnregisterTextInputFocus(string id)
        {
            if (!string.IsNullOrEmpty(id)) TextInputProviders.Remove(id);
        }

        internal static bool IsExternalTextInputFocused()
        {
            foreach (var query in TextInputProviders.Values)
                if (query()) return true;
            return false;
        }
    }
}
