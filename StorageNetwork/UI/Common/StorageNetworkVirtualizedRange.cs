using System;

namespace StorageNetwork.UI
{
    internal readonly struct StorageNetworkVirtualizedRange
    {
        public StorageNetworkVirtualizedRange(int first, int lastExclusive)
        {
            First = first;
            LastExclusive = lastExclusive;
        }

        public int First { get; }

        public int LastExclusive { get; }

        public static StorageNetworkVirtualizedRange Calculate(
            int totalCount,
            int virtualizationThreshold,
            int overscan,
            float rowHeight,
            float spacing,
            float verticalPadding,
            float scrollOffset,
            float viewportHeight)
        {
            int count = Math.Max(0, totalCount);
            if (count <= Math.Max(0, virtualizationThreshold) ||
                rowHeight <= 0f ||
                viewportHeight <= 0f)
            {
                return new StorageNetworkVirtualizedRange(0, count);
            }

            float stride = Math.Max(0.001f, rowHeight + spacing);
            float normalizedOffset = Math.Max(0f, scrollOffset - verticalPadding);
            int first = Clamp(
                (int)Math.Floor(normalizedOffset / stride) - Math.Max(0, overscan),
                0,
                Math.Max(0, count - 1));
            int lastExclusive = Clamp(
                (int)Math.Ceiling(
                    Math.Max(0f,
                        scrollOffset + viewportHeight - verticalPadding) /
                    stride) + Math.Max(0, overscan),
                Math.Min(count, first + 1),
                count);
            return new StorageNetworkVirtualizedRange(first, lastExclusive);
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum
                    ? maximum
                    : value;
        }
    }
}
