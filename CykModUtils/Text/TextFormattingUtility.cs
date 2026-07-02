namespace CykModUtils.Text
{
    /// <summary>
    /// 处理 ONI/Klei UI 文本格式和搜索匹配的辅助方法。
    /// </summary>
    public static class TextFormattingUtility
    {
        /// <summary>
        /// 使用规范化后的文本判断是否包含搜索关键字。
        /// </summary>
        /// <param name="text">待搜索文本。</param>
        /// <param name="query">搜索关键字。</param>
        /// <returns>包含关键字时返回 true。</returns>
        public static bool ContainsSearchText(string text, string query)
        {
            return NormalizeSearchText(text).Contains(NormalizeSearchText(query));
        }

        /// <summary>
        /// 移除 Klei 链接标签、去除首尾空白并转为小写，便于搜索匹配。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <returns>规范化后的文本。</returns>
        public static string NormalizeSearchText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : StripKleiLinkFormatting(text).Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 移除 Klei 常见的 link/LINK 富文本标签。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <returns>移除链接标签后的文本。</returns>
        public static string StripKleiLinkFormatting(string text)
        {
            return StripKleiTagFormatting(StripKleiTagFormatting(text, "link"), "LINK");
        }

        /// <summary>
        /// 移除指定名称的 Klei 富文本标签，例如 &lt;link="..."&gt;文本&lt;/link&gt;。
        /// </summary>
        /// <param name="text">原始文本。</param>
        /// <param name="tag">标签名，不包含尖括号。</param>
        /// <returns>移除标签后的文本。</returns>
        public static string StripKleiTagFormatting(string text, string tag)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(tag))
            {
                return text;
            }

            string openTag = "<" + tag + "=";
            string closeTag = "</" + tag + ">";
            while (text.Contains(openTag))
            {
                int closeIndex = text.IndexOf(closeTag);
                if (closeIndex >= 0)
                {
                    text = text.Remove(closeIndex, closeTag.Length);
                }

                int openIndex = text.IndexOf(openTag);
                if (openIndex < 0)
                {
                    break;
                }

                int openEndIndex = text.IndexOf("\">", openIndex);
                text = openEndIndex >= 0
                    ? text.Remove(openIndex, openEndIndex - openIndex + 2)
                    : text.Remove(openIndex, openTag.Length);
            }

            return text;
        }
    }
}
