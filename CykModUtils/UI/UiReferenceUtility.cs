using UnityEngine;

namespace CykModUtils.UI
{
    /// <summary>
    /// HierarchyReferences、LocText 和 ToolTip 的安全访问辅助方法。
    /// </summary>
    public static class UiReferenceUtility
    {
        /// <summary>
        /// 尝试获取对象上的 HierarchyReferences。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="references">找到的 HierarchyReferences。</param>
        /// <returns>组件存在时返回 true。</returns>
        public static bool TryGetReferences(GameObject target, out HierarchyReferences references)
        {
            references = target != null ? target.GetComponent<HierarchyReferences>() : null;
            return references != null;
        }

        /// <summary>
        /// 安全地从 HierarchyReferences 里获取指定名称和类型的引用。
        /// </summary>
        /// <typeparam name="T">目标组件类型。</typeparam>
        /// <param name="target">带有 HierarchyReferences 的对象。</param>
        /// <param name="name">引用名。</param>
        /// <param name="component">找到的组件。</param>
        /// <returns>引用存在且类型匹配时返回 true。</returns>
        public static bool TryGetReference<T>(GameObject target, string name, out T component) where T : Component
        {
            component = null;
            if (!TryGetReferences(target, out HierarchyReferences references) || string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            component = references.TryGetReference<T>(name);
            return component != null;
        }

        /// <summary>
        /// 设置指定 HierarchyReferences 引用上的 LocText 文本。
        /// </summary>
        /// <param name="target">带有 HierarchyReferences 的对象。</param>
        /// <param name="name">LocText 引用名。</param>
        /// <param name="text">要显示的文本。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetText(GameObject target, string name, string text)
        {
            if (!TryGetReference(target, name, out LocText locText))
            {
                return false;
            }

            locText.SetText(text ?? string.Empty);
            return true;
        }

        /// <summary>
        /// 设置指定对象上的简单 ToolTip，不存在 ToolTip 时自动添加。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetSimpleTooltip(GameObject target, string tooltip)
        {
            if (target == null)
            {
                return false;
            }

            target.AddOrGet<ToolTip>().SetSimpleTooltip(tooltip ?? string.Empty);
            return true;
        }

        /// <summary>
        /// 设置指定 HierarchyReferences 引用上的简单 ToolTip。
        /// </summary>
        /// <param name="target">带有 HierarchyReferences 的对象。</param>
        /// <param name="name">引用名。</param>
        /// <param name="tooltip">提示文本。</param>
        /// <returns>设置成功时返回 true。</returns>
        public static bool TrySetReferenceTooltip(GameObject target, string name, string tooltip)
        {
            return TryGetReference(target, name, out Component component) && TrySetSimpleTooltip(component.gameObject, tooltip);
        }
    }
}
