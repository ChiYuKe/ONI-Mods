using System.Collections.Generic;

namespace StorageNetwork.Services
{
    // Ports are appended in the original priority order. Ordinals preserve even
    // duplicate IDs' old last-entry-wins semantics without copying whole prefixes.
    internal sealed class StorageNetworkSparsePrefixIndex<TKey>
    {
        private readonly Dictionary<int, int> portOrdinals = new Dictionary<int, int>();
        private readonly Dictionary<TKey, List<Entry>> entries = new Dictionary<TKey, List<Entry>>();
        private int nextOrdinal;
        public int EntryCount { get; private set; }

        public void AddPort(int portId, Dictionary<TKey, float> amounts, float minimum)
        {
            int ordinal = nextOrdinal++;
            portOrdinals[portId] = ordinal;
            foreach (KeyValuePair<TKey, float> pair in amounts)
            {
                if (pair.Value <= minimum) continue;
                if (!entries.TryGetValue(pair.Key, out List<Entry> values))
                {
                    values = new List<Entry>();
                    entries.Add(pair.Key, values);
                }
                float previous = values.Count == 0 ? 0f : values[values.Count - 1].Amount;
                values.Add(new Entry(ordinal, previous + pair.Value));
                EntryCount++;
            }
        }

        public float GetEarlierAmount(int portId, TKey tag)
        {
            if (!portOrdinals.TryGetValue(portId, out int ordinal) ||
                !entries.TryGetValue(tag, out List<Entry> values)) return 0f;
            int low = 0, high = values.Count;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (values[middle].Ordinal < ordinal) low = middle + 1;
                else high = middle;
            }
            return low == 0 ? 0f : values[low - 1].Amount;
        }

        private readonly struct Entry
        {
            public Entry(int ordinal, float amount) { Ordinal = ordinal; Amount = amount; }
            public int Ordinal { get; }
            public float Amount { get; }
        }
    }
}
