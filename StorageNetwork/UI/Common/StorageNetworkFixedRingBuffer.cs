using System;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkFixedRingBuffer<T>
    {
        private readonly T[] values;
        private int start;

        public StorageNetworkFixedRingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            values = new T[capacity];
        }

        public int Capacity => values.Length;

        public int Count { get; private set; }

        public void Add(T value)
        {
            int index = (start + Count) % values.Length;
            if (Count == values.Length)
            {
                values[index] = value;
                start = (start + 1) % values.Length;
                return;
            }

            values[index] = value;
            Count++;
        }

        public bool TryGetFirst(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }

            value = values[start];
            return true;
        }

        public bool TryGetLast(out T value)
        {
            if (Count == 0)
            {
                value = default;
                return false;
            }

            value = values[(start + Count - 1) % values.Length];
            return true;
        }

        public void Clear()
        {
            Array.Clear(values, 0, values.Length);
            start = 0;
            Count = 0;
        }
    }
}
