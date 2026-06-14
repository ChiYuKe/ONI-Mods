using UnityEngine;

namespace StorageNetwork.UI
{
    internal readonly struct StorageNetworkCategorySummaryItemTotal
    {
        public StorageNetworkCategorySummaryItemTotal(string key, string name, float massKg, GameObject representative)
        {
            Key = key;
            Name = name;
            MassKg = massKg;
            Representative = representative;
        }

        public string Key { get; }

        public string Name { get; }

        public float MassKg { get; }

        public GameObject Representative { get; }
    }
}
