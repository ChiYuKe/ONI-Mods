using System.Collections.Generic;
using System.Linq;

namespace StorageNetwork.API
{
    /// <summary>
    /// 储存网络主面板顶部按钮注册表。附属模组可在 OnLoad 中注册按钮提供器。
    /// </summary>
    public static class StorageNetworkPanelHeaderButtonRegistry
    {
        private static readonly List<IStorageNetworkPanelHeaderButtonProvider> Providers =
            new List<IStorageNetworkPanelHeaderButtonProvider>();

        /// <summary>
        /// 注册一个主面板顶部按钮提供器。重复注册同一个实例会被忽略。
        /// </summary>
        public static void Register(IStorageNetworkPanelHeaderButtonProvider provider)
        {
            if (provider == null || Providers.Contains(provider))
            {
                return;
            }

            Providers.Add(provider);
        }

        /// <summary>
        /// 移除一个已注册的主面板顶部按钮提供器。
        /// </summary>
        public static void Unregister(IStorageNetworkPanelHeaderButtonProvider provider)
        {
            if (provider != null)
            {
                Providers.Remove(provider);
            }
        }

        /// <summary>
        /// 获取当前所有按钮描述，并按 Order 和 Id 排序。
        /// </summary>
        public static IReadOnlyList<StorageNetworkPanelHeaderButton> GetButtons()
        {
            return Providers
                .Where(provider => provider != null)
                .SelectMany(provider => provider.GetHeaderButtons() ?? Enumerable.Empty<StorageNetworkPanelHeaderButton>())
                .Where(button => button != null)
                .OrderBy(button => button.Order)
                .ThenBy(button => button.Id)
                .ToList();
        }

        /// <summary>
        /// 清空注册表。通常仅测试或热重载场景需要调用。
        /// </summary>
        public static void ResetRuntimeState()
        {
            Providers.Clear();
        }
    }
}
