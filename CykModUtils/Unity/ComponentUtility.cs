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
