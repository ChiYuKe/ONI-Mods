using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 世界文字面板扩展注册表。补丁作者可在自己的 OnLoad/Postfix 中注册内容提供器。
    /// </summary>
    public static class StorageNetworkWorldPanelRegistry
    {
        private static readonly List<IStorageNetworkWorldPanelContentProvider> Providers = new List<IStorageNetworkWorldPanelContentProvider>();

        /// <summary>
        /// 注册一个内容提供器。priority 越小越早执行，适合外部模组抢先处理自己的建筑。
        /// </summary>
        public static void Register(IStorageNetworkWorldPanelContentProvider provider, int priority = 100)
        {
            if (provider == null || Providers.Contains(provider))
            {
                return;
            }

            ProviderPriorities[provider] = priority;
            Providers.Add(provider);
            Providers.Sort((left, right) => GetPriority(left).CompareTo(GetPriority(right)));
        }

        /// <summary>
        /// 移除内容提供器，常用于热重载或测试环境清理。
        /// </summary>
        public static void Unregister(IStorageNetworkWorldPanelContentProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            Providers.Remove(provider);
            ProviderPriorities.Remove(provider);
        }

        /// <summary>
        /// 依次询问已注册提供器，获取第一个可用的面板内容。
        /// </summary>
        public static bool TryBuildContent(GameObject target, out StorageNetworkWorldPanelContent content)
        {
            foreach (IStorageNetworkWorldPanelContentProvider provider in Providers.ToList())
            {
                if (provider != null && provider.TryBuild(target, out content) && content != null)
                {
                    return true;
                }
            }

            content = null;
            return false;
        }

        private static readonly Dictionary<IStorageNetworkWorldPanelContentProvider, int> ProviderPriorities =
            new Dictionary<IStorageNetworkWorldPanelContentProvider, int>();

        private static int GetPriority(IStorageNetworkWorldPanelContentProvider provider)
        {
            return ProviderPriorities.TryGetValue(provider, out int priority) ? priority : 100;
        }
    }
}
