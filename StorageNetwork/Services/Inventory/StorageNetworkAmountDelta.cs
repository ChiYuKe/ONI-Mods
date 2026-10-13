using System;
using System.Collections.Generic;

namespace StorageNetwork.Services
{
    // Shared by the live index and deterministic tests. Return the actual change
    // after threshold removal, so world totals cannot retain discarded residues.
    internal static class StorageNetworkAmountDelta
    {
        public static float Apply<TKey>(Dictionary<TKey, float> amounts, TKey key, float delta, float minimum)
        {
            if (Math.Abs(delta) <= float.Epsilon) return 0f;
            bool exists = amounts.TryGetValue(key, out float before);
            if (!exists && !(delta > minimum)) return 0f;
            float after = before + delta;
            if (after <= minimum)
            {
                amounts.Remove(key);
                after = 0f;
            }
            else
            {
                amounts[key] = after;
            }
            return after - before;
        }
    }
}
