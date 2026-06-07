namespace StorageNetwork.UI
{
    internal static class StorageNetworkTextFormatting
    {
        public static string NormalizeSearchText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : StripKleiLinkFormatting(text).Trim().ToLowerInvariant();
        }

        public static string StripKleiLinkFormatting(string text)
        {
            return StripKleiTagFormatting(StripKleiTagFormatting(text, "link"), "LINK");
        }

        private static string StripKleiTagFormatting(string text, string tag)
        {
            if (string.IsNullOrEmpty(text))
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
                if (openEndIndex >= 0)
                {
                    text = text.Remove(openIndex, openEndIndex - openIndex + 2);
                }
                else
                {
                    text = text.Remove(openIndex, openTag.Length);
                }
            }

            return text;
        }
    }
}
