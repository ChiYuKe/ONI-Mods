using UnityEngine;

namespace CykModUtils.Game
{
    /// <summary>
    /// KAnim 资源和逐实例符号覆盖的安全包装。
    /// </summary>
    public static class KAnimUtility
    {
        /// <summary>
        /// 从 KAnimFile 的 Build 中查找符号。
        /// </summary>
        public static bool TryGetSymbol(
            KAnimFile animFile,
            HashedString symbolName,
            out KAnim.Build.Symbol symbol)
        {
            symbol = animFile?.GetData()?.build?.GetSymbol(symbolName);
            return symbol != null;
        }

        /// <summary>
        /// 从 KAnimFile 的 Build 中按字符串查找符号。
        /// </summary>
        public static bool TryGetSymbol(
            KAnimFile animFile,
            string symbolName,
            out KAnim.Build.Symbol symbol)
        {
            symbol = null;
            return !string.IsNullOrWhiteSpace(symbolName)
                && TryGetSymbol(animFile, new HashedString(symbolName), out symbol);
        }

        /// <summary>
        /// 给指定对象添加逐实例符号覆盖。
        /// </summary>
        public static bool TryApplySymbolOverride(
            GameObject target,
            HashedString targetSymbol,
            KAnim.Build.Symbol sourceSymbol,
            int priority = 0,
            bool replaceSamePriority = true)
        {
            SymbolOverrideController controller =
                target != null ? target.GetComponent<SymbolOverrideController>() : null;
            if (controller == null || !targetSymbol.IsValid || sourceSymbol == null)
            {
                return false;
            }

            if (replaceSamePriority)
            {
                controller.RemoveSymbolOverride(targetSymbol, priority);
            }

            controller.AddSymbolOverride(targetSymbol, sourceSymbol, priority);
            return true;
        }

        /// <summary>
        /// 删除指定对象的逐实例符号覆盖。
        /// </summary>
        public static bool TryRemoveSymbolOverride(
            GameObject target,
            HashedString targetSymbol,
            int priority = 0)
        {
            SymbolOverrideController controller =
                target != null ? target.GetComponent<SymbolOverrideController>() : null;
            if (controller == null || !targetSymbol.IsValid)
            {
                return false;
            }

            controller.RemoveSymbolOverride(targetSymbol, priority);
            return true;
        }
    }
}
