using System;
using System.Collections.Generic;

namespace StorageNetwork.UI
{
    /// <summary>
    /// Replaces only changed source contributions and folds only affected keys.
    /// Sources are folded in their original order, preserving representative choice
    /// and floating-point addition order instead of repeatedly subtracting totals.
    /// </summary>
    internal sealed class StorageNetworkIncrementalTotals<TKey, TValue>
    {
        private readonly Dictionary<int, HashSet<TKey>> sourceKeys = new Dictionary<int, HashSet<TKey>>();
        private readonly Dictionary<TKey, SortedDictionary<int, TValue>> contributions =
            new Dictionary<TKey, SortedDictionary<int, TValue>>();
        private readonly HashSet<TKey> dirty = new HashSet<TKey>();

        public void Clear()
        {
            sourceKeys.Clear(); contributions.Clear(); dirty.Clear();
        }

        public void SetSource(int source, IEnumerable<KeyValuePair<TKey, TValue>> values)
        {
            if (!sourceKeys.TryGetValue(source, out HashSet<TKey> keys))
            {
                keys = new HashSet<TKey>();
                sourceKeys.Add(source, keys);
            }
            foreach (TKey key in keys)
            {
                contributions[key].Remove(source);
                dirty.Add(key);
            }
            keys.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in values)
            {
                if (!contributions.TryGetValue(pair.Key, out SortedDictionary<int, TValue> sources))
                {
                    sources = new SortedDictionary<int, TValue>();
                    contributions.Add(pair.Key, sources);
                }
                sources[source] = pair.Value;
                keys.Add(pair.Key);
                dirty.Add(pair.Key);
            }
        }

        public int ApplyChanges(Dictionary<TKey, TValue> destination, Func<TValue, TValue, TValue> merge)
        {
            int updated = dirty.Count;
            foreach (TKey key in dirty)
            {
                SortedDictionary<int, TValue> sources = contributions[key];
                if (sources.Count == 0)
                {
                    destination.Remove(key);
                    contributions.Remove(key);
                    continue;
                }
                bool first = true;
                TValue total = default;
                foreach (TValue value in sources.Values)
                {
                    total = first ? value : merge(total, value);
                    first = false;
                }
                destination[key] = total;
            }
            dirty.Clear();
            return updated;
        }
    }
}
