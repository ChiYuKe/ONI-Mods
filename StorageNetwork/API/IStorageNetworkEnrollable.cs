using UnityEngine;

namespace StorageNetwork.API
{
    /// <summary>
    /// 可由附属模组组件实现的“可接入储存网络”接口。主模组会根据这个接口显示标准的“接入/移除网络”用户菜单按钮。
    /// </summary>
    public interface IStorageNetworkEnrollable
    {
        /// <summary>
        /// 返回当前对象是否应该显示储存网络接入按钮。
        /// </summary>
        bool CanShowStorageNetworkEnrollmentButton();

        /// <summary>
        /// 返回当前对象是否已经接入储存网络。
        /// </summary>
        bool IsStorageNetworkIncluded();

        /// <summary>
        /// 设置当前对象是否接入储存网络。实现方应在状态变化后刷新自己的运行时状态。
        /// </summary>
        void SetStorageNetworkIncluded(bool included);

        /// <summary>
        /// 返回用于添加用户菜单按钮和刷新 UI 的目标对象。通常直接返回组件所在的 gameObject。
        /// </summary>
        GameObject GetStorageNetworkEnrollmentTarget();
    }
}
