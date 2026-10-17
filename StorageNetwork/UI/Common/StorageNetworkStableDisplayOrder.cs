using System;
using System.Collections.Generic;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkStableDisplayOrder<T>
    {
        private readonly Dictionary<string, Entry> order = new Dictionary<string, Entry>(StringComparer.Ordinal);
        private readonly List<T> workspace = new List<T>();
        private StringComparer observedComparer;

        public void Clear() { order.Clear(); observedComparer = null; workspace.Clear(); }

        public bool Apply(List<T> values, Func<T, string> key, Func<T, string> name, StringComparer comparer)
        {
            bool changed = values.Count != order.Count || !Equals(observedComparer, comparer);
            if (!changed)
                foreach (T value in values)
                    if (!order.TryGetValue(key(value), out Entry old) || old.Name != name(value))
                    { changed = true; break; }
            if (changed)
            {
                values.Sort((a, b) =>
                {
                    int comparison = comparer.Compare(name(a), name(b));
                    return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(key(a), key(b));
                });
                order.Clear();
                for (int i = 0; i < values.Count; i++) order.Add(key(values[i]), new Entry(i, name(values[i])));
                observedComparer = comparer;
                return true;
            }
            workspace.Clear();
            workspace.AddRange(values);
            foreach (T value in workspace) values[order[key(value)].Index] = value;
            workspace.Clear();
            return false;
        }

        private readonly struct Entry
        {
            public Entry(int index, string name) { Index = index; Name = name; }
            public int Index { get; }
            public string Name { get; }
        }
    }
}
