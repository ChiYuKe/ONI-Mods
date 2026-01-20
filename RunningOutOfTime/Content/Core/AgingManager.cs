using System.Collections.Generic;
using RunningOutOfTime.Content.Components;
using UnityEngine;

namespace RunningOutOfTime.Content.Core
{
    public static class AgingManager
    {
        // 使用 HashSet 确保不会重复添加，且查询极快
        private static readonly HashSet<AgingMonitor> _monitors = new HashSet<AgingMonitor>();

        /// <summary>
        /// 当组件加载(OnSpawn)时注册
        /// </summary>
        public static void Register(AgingMonitor monitor)
        {
            if (monitor != null)
                _monitors.Add(monitor);
        }

        /// <summary>
        /// 当组件销毁(OnCleanUp)时注销
        /// </summary>
        public static void Unregister(AgingMonitor monitor)
        {
            if (_monitors.Contains(monitor))
                _monitors.Remove(monitor);
        }

        /// <summary>
        /// 获取所有受寿命影响的复制人对象
        /// </summary>
        public static IEnumerable<AgingMonitor> GetAllAgingMinions() => _monitors; 

        /// <summary>
        /// 获取当前带寿命组件的总人数
        /// </summary>
        public static int Count => _monitors.Count;


        /// <summary>
        /// 统计已进入“衰老”阶段的人数
        /// </summary>
        public static int GetElderCount()
        {
            int count = 0;
            foreach (var m in _monitors)
            {
               
                if (m.hasAppliedAgingEffects) count++;
            }
            return count;
        }
    }
}