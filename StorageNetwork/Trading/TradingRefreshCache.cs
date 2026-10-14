using System;
using System.Collections.Generic;

namespace StorageNetwork.Trading
{
    // Reused only during one synchronous UI refresh; never across simulation ticks.
    internal sealed class TradingRefreshCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> values = new Dictionary<TKey, TValue>();
        internal TValue Get(TKey key, Func<TValue> load)
        {
            if (!values.TryGetValue(key, out TValue value)) values[key] = value = load();
            return value;
        }
        internal void Clear() => values.Clear();
    }
}
