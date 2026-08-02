using UnityEngine;

namespace CykModUtils.Unity
{
    /// <summary>
    /// Unity 组件和 ONI 常见对象引用转换辅助方法。
    /// </summary>
    public static class ComponentUtility
    {
        /// <summary>
        /// 安全地从 GameObject 上获取指定组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="target">目标对象。</param>
        /// <param name="component">找到的组件。</param>
        /// <returns>组件存在时返回 true。</returns>
        public static bool TryGet<T>(GameObject target, out T component) where T : Component
        {
            component = target != null ? target.GetComponent<T>() : null;
            return component != null;
        }

        /// <summary>
        /// 获取组件；不存在时添加。目标为空时返回 null。
        /// </summary>
        public static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target == null ? null : target.AddOrGet<T>();
        }

        /// <summary>
        /// 在对象及其父级中查找组件。
        /// </summary>
        public static bool TryGetInParent<T>(
            GameObject target,
            out T component,
            bool includeInactive = false)
            where T : Component
        {
            component = target != null
                ? target.GetComponentInParent<T>(includeInactive)
                : null;
            return component != null;
        }

        /// <summary>
        /// 在对象及其子级中查找组件。
        /// </summary>
        public static bool TryGetInChildren<T>(
            GameObject target,
            out T component,
            bool includeInactive = false)
            where T : Component
        {
            component = target != null
                ? target.GetComponentInChildren<T>(includeInactive)
                : null;
            return component != null;
        }

        /// <summary>
        /// 把常见的 ONI/Unity 对象引用转换为 GameObject。
        /// </summary>
        /// <param name="value">GameObject、Component、Pickupable 或 KPrefabID。</param>
        /// <returns>可解析时返回对应 GameObject，否则返回 null。</returns>
        public static GameObject ToGameObject(object value)
        {
            if (value is GameObject gameObject)
            {
                return gameObject;
            }

            if (value is Component component)
            {
                return component.gameObject;
            }

            if (value is Pickupable pickupable)
            {
                return pickupable.gameObject;
            }

            if (value is KPrefabID prefabId)
            {
                return prefabId.gameObject;
            }

            return null;
        }
    }
}
