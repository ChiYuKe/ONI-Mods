using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkKeyedRowCache
    {
        private readonly Transform parent;
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private readonly List<KeyValuePair<string, Entry>> inactiveEntries =
            new List<KeyValuePair<string, Entry>>();
        private readonly List<string> evictedKeys = new List<string>();
        private readonly int maxInactiveEntries;
        private readonly int maxInactiveGenerations;
        private int order;
        private int generation;

        public StorageNetworkKeyedRowCache(Transform parent, int maxInactiveEntries = 32, int maxInactiveGenerations = 3)
        {
            this.parent = parent;
            this.maxInactiveEntries = Math.Max(0, maxInactiveEntries);
            this.maxInactiveGenerations = Math.Max(0, maxInactiveGenerations);
        }

        public void Begin()
        {
            order = 0;
            generation++;
            foreach (Entry entry in entries.Values)
            {
                entry.Used = false;
            }
        }

        public GameObject Use(string key, Func<GameObject> create, bool recreate = false)
        {
            if (string.IsNullOrEmpty(key))
            {
                key = "empty";
            }

            if (entries.TryGetValue(key, out Entry existing) && recreate && existing.GameObject != null)
            {
                UnityEngine.Object.Destroy(existing.GameObject);
                entries.Remove(key);
            }

            if (!entries.TryGetValue(key, out Entry entry) || entry.GameObject == null)
            {
                entry = new Entry(create());
                entries[key] = entry;
            }

            return MarkUsed(entry);
        }

        public bool TryUse(string key, out GameObject gameObject)
        {
            gameObject = null;
            if (string.IsNullOrEmpty(key))
            {
                key = "empty";
            }

            if (!entries.TryGetValue(key, out Entry entry) || entry.GameObject == null)
            {
                return false;
            }

            gameObject = MarkUsed(entry);
            return true;
        }

        public bool TryGetGameObject(string key, out GameObject gameObject)
        {
            gameObject = null;
            if (string.IsNullOrEmpty(key))
            {
                key = "empty";
            }

            if (!entries.TryGetValue(key, out Entry entry) || entry.GameObject == null)
            {
                return false;
            }

            gameObject = entry.GameObject;
            return true;
        }

        public bool TryGetMetadata<T>(string key, out T value)
        {
            value = default;
            if (!entries.TryGetValue(key, out Entry entry) || !(entry.Metadata is T typed))
            {
                return false;
            }

            value = typed;
            return true;
        }

        public void SetMetadata(string key, object metadata)
        {
            if (entries.TryGetValue(key, out Entry entry))
            {
                entry.Metadata = metadata;
            }
        }

        public void Commit()
        {
            inactiveEntries.Clear();
            evictedKeys.Clear();
            foreach (KeyValuePair<string, Entry> pair in entries)
            {
                Entry entry = pair.Value;
                if (entry.GameObject == null)
                {
                    evictedKeys.Add(pair.Key);
                    continue;
                }

                if (entry.Used)
                {
                    if (!entry.GameObject.activeSelf)
                    {
                        entry.GameObject.SetActive(true);
                    }
                }
                else if (entry.GameObject.activeSelf)
                {
                    entry.GameObject.SetActive(false);
                }

                if (!entry.Used)
                {
                    inactiveEntries.Add(pair);
                }
            }

            inactiveEntries.Sort(InactiveEntryComparer.Instance);
            int retainedInactive = inactiveEntries.Count;
            int inactiveLimit = maxInactiveEntries > 0
                ? Math.Max(maxInactiveEntries, order * 2)
                : 0;
            foreach (KeyValuePair<string, Entry> pair in inactiveEntries)
            {
                bool expired = generation - pair.Value.LastUsedGeneration > maxInactiveGenerations;
                bool overLimit = retainedInactive > inactiveLimit;
                if (!expired && !overLimit)
                {
                    continue;
                }

                if (pair.Value.GameObject != null)
                {
                    UnityEngine.Object.Destroy(pair.Value.GameObject);
                }
                evictedKeys.Add(pair.Key);
                retainedInactive--;
            }

            foreach (string key in evictedKeys)
            {
                entries.Remove(key);
            }
        }

        public void ClearDestroy()
        {
            foreach (Entry entry in entries.Values)
            {
                if (entry.GameObject != null)
                {
                    UnityEngine.Object.Destroy(entry.GameObject);
                }
            }

            entries.Clear();
            inactiveEntries.Clear();
            evictedKeys.Clear();
            order = 0;
        }

        private GameObject MarkUsed(Entry entry)
        {
            entry.Used = true;
            entry.LastUsedGeneration = generation;
            GameObject row = entry.GameObject;
            if (!row.activeSelf)
            {
                row.SetActive(true);
            }

            if (row.transform.GetSiblingIndex() != order)
            {
                row.transform.SetSiblingIndex(order);
            }

            order++;
            return row;
        }

        private sealed class Entry
        {
            public Entry(GameObject gameObject)
            {
                GameObject = gameObject;
            }

            public GameObject GameObject { get; }

            public bool Used { get; set; }

            public int LastUsedGeneration { get; set; }

            public object Metadata { get; set; }
        }

        private sealed class InactiveEntryComparer : IComparer<KeyValuePair<string, Entry>>
        {
            public static readonly InactiveEntryComparer Instance = new InactiveEntryComparer();

            public int Compare(KeyValuePair<string, Entry> left, KeyValuePair<string, Entry> right)
            {
                return left.Value.LastUsedGeneration.CompareTo(right.Value.LastUsedGeneration);
            }
        }
    }
}
