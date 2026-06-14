using System;
using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkKeyedRowCache
    {
        private readonly Transform parent;
        private readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
        private int order;

        public StorageNetworkKeyedRowCache(Transform parent)
        {
            this.parent = parent;
        }

        public void Begin()
        {
            order = 0;
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
                entry.GameObject.name = key;
                entries[key] = entry;
            }

            entry.Used = true;
            GameObject row = entry.GameObject;
            if (!row.activeSelf)
            {
                row.SetActive(true);
            }

            row.transform.SetSiblingIndex(order++);
            return row;
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
            foreach (Entry entry in entries.Values)
            {
                if (entry.GameObject == null)
                {
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
            order = 0;
        }

        private sealed class Entry
        {
            public Entry(GameObject gameObject)
            {
                GameObject = gameObject;
            }

            public GameObject GameObject { get; }

            public bool Used { get; set; }

            public object Metadata { get; set; }
        }
    }
}
