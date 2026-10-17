using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StorageNetwork.UI
{
    /// <summary>Cooperative frame budget: one indivisible row may exceed it.</summary>
    internal sealed class StorageNetworkFrameWorkQueue
    {
        private readonly Queue<Work> pending = new Queue<Work>();
        private readonly Func<long> clock;
        private readonly long frequency;
        public StorageNetworkFrameWorkQueue() : this(Stopwatch.GetTimestamp, Stopwatch.Frequency) { }
        internal StorageNetworkFrameWorkQueue(Func<long> clock, long frequency)
        { this.clock = clock; this.frequency = frequency; }
        public int Count => pending.Count;
        public void Clear() => pending.Clear();
        public void Enqueue(System.Action action, Func<bool> alive, Func<bool> ready)
            => pending.Enqueue(new Work(action, alive, ready));

        public int Run(int maxItems, double milliseconds)
        {
            long deadline = clock() + (long)(milliseconds * frequency / 1000d);
            int completed = 0;
            int remainingChecks = Math.Min(pending.Count, 128);
            while (remainingChecks-- > 0 && pending.Count > 0 && completed < maxItems)
            {
                Work work = pending.Dequeue();
                if (work.Alive())
                {
                    if (work.Ready()) { work.Action(); completed++; }
                    else pending.Enqueue(work);
                }
                if (clock() >= deadline) break;
            }
            return completed;
        }

        private readonly struct Work
        {
            public Work(System.Action action, Func<bool> alive, Func<bool> ready)
            { Action = action; Alive = alive; Ready = ready; }
            public System.Action Action { get; }
            public Func<bool> Alive { get; }
            public Func<bool> Ready { get; }
        }
    }
}
