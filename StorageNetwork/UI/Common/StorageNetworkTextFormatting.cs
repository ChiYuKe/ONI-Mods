using System;
using System.Text.RegularExpressions;
using StorageNetwork.Services;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkTextFormatting
    {
        private static readonly Regex TagRegex = new Regex("<.*?>", RegexOptions.Compiled);

        public static bool ContainsSearchText(string text, string query)
        {
            return NormalizeSearchText(text).Contains(query);
        }

        public static string NormalizeSearchText(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : CleanDisplayName(text).ToLowerInvariant();
        }

        public static string CleanDisplayName(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.IndexOf('<') >= 0)
            {
                text = TagRegex.Replace(text, string.Empty);
            }

            return text.Trim();
        }

        public static string StripKleiLinkFormatting(string text)
        {
            return CleanDisplayName(text);
        }

        public static int CompareDisplayNames(string left, string right)
        {
            string cleanLeft = CleanDisplayName(left);
            string cleanRight = CleanDisplayName(right);
            int comp = string.Compare(cleanLeft, cleanRight, StringComparison.CurrentCultureIgnoreCase);
            if (comp != 0)
            {
                return comp;
            }

            return string.Compare(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static int CompareTagDisplayNames(Tag left, Tag right)
        {
            string leftName = StorageItemUtility.GetTagDisplayName(left);
            string rightName = StorageItemUtility.GetTagDisplayName(right);
            int comp = CompareDisplayNames(leftName, rightName);
            if (comp != 0)
            {
                return comp;
            }

            return string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase);
        }

        public static int CompareElementDisplayNames(SimHashes left, SimHashes right)
        {
            Element elemLeft = ElementLoader.FindElementByHash(left);
            Element elemRight = ElementLoader.FindElementByHash(right);
            string leftName = elemLeft != null ? elemLeft.name : left.ToString();
            string rightName = elemRight != null ? elemRight.name : right.ToString();
            int comp = CompareDisplayNames(leftName, rightName);
            if (comp != 0)
            {
                return comp;
            }

            return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public static int CompareStorageDisplayNames(Storage left, Storage right)
        {
            string leftName = left != null ? left.GetProperName() : string.Empty;
            string rightName = right != null ? right.GetProperName() : string.Empty;
            int comp = CompareDisplayNames(leftName, rightName);
            if (comp != 0)
            {
                return comp;
            }

            return (left != null ? left.GetInstanceID() : 0).CompareTo(right != null ? right.GetInstanceID() : 0);
        }
    }
}
