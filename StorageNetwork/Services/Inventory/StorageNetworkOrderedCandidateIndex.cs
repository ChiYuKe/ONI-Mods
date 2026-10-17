using System;
using System.Collections.Generic;

namespace StorageNetwork.Services
{
    /// <summary>
    /// Lazy inverted buckets over a native-order snapshot. Callers invalidate on
    /// membership/tag mutations; returned candidates are copied before any writes.
    /// Multiple tags retain native order and never duplicate a native list slot.
    /// </summary>
    internal sealed class StorageNetworkOrderedCandidateIndex<TItem, TTag>
    {
        private const int MaxCachedTags = 64;
        private readonly List<TItem> items = new List<TItem>();
        private readonly Dictionary<TTag, List<int>> buckets = new Dictionary<TTag, List<int>>();
        private readonly List<int> merged = new List<int>();
        private readonly Stack<List<int>> unusedBuckets = new Stack<List<int>>();
        private bool valid;

        public void Invalidate()
        {
            valid = false;
            items.Clear();
            foreach (List<int> bucket in buckets.Values)
            {
                bucket.Clear();
                if (unusedBuckets.Count < MaxCachedTags && bucket.Capacity <= 4096) unusedBuckets.Push(bucket);
            }
            buckets.Clear();
        }

        // Returns predicate evaluations so production profiling and linked tests
        // can distinguish cold scans from warm candidate-only reads.
        public int Fill(IReadOnlyList<TItem> nativeItems, IEnumerable<TTag> tags,
            Func<TItem, TTag, bool> matches, List<TItem> destination)
        {
            destination.Clear();
            if (!valid || items.Count != nativeItems.Count)
            {
                Invalidate();
                for (int i = 0; i < nativeItems.Count; i++) items.Add(nativeItems[i]);
                buckets.Clear();
                valid = true;
            }
            if (tags == null)
            {
                destination.AddRange(items);
                return 0;
            }
            int checkedItems = 0;
            merged.Clear();
            List<int> singleBucket = null;
            int tagCount = 0;
            foreach (TTag tag in tags)
            {
                if (!buckets.TryGetValue(tag, out List<int> bucket))
                {
                    bucket = unusedBuckets.Count > 0 ? unusedBuckets.Pop() : new List<int>();
                    for (int i = 0; i < items.Count; i++)
                    {
                        checkedItems++;
                        if (matches(items[i], tag)) bucket.Add(i);
                    }
                    if (buckets.Count < MaxCachedTags) buckets.Add(tag, bucket);
                }
                if (tagCount++ == 0) singleBucket = bucket;
                else
                {
                    if (tagCount == 2) merged.AddRange(singleBucket);
                    merged.AddRange(bucket);
                }
            }
            if (tagCount == 1)
            {
                foreach (int index in singleBucket) destination.Add(items[index]);
                return checkedItems;
            }
            merged.Sort();
            int previous = -1;
            for (int i = 0; i < merged.Count; i++)
            {
                int index = merged[i];
                if (index != previous) destination.Add(items[index]);
                previous = index;
            }
            return checkedItems;
        }
    }
}
