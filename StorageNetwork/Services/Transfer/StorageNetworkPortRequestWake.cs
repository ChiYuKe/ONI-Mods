using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal sealed class StorageNetworkPortRequestWake : IDisposable
    {
        private readonly StorageNetworkRequestWakeGate gate = new StorageNetworkRequestWakeGate();
        private Storage owner;
        private readonly Action<GameObject> onChange;
        public bool IsWaiting => gate.IsWaiting;
        public StorageNetworkPortRequestWake() { onChange = _ => gate.Reset(); }
        public void Reset() => gate.Reset();
        public void Bind(Storage storage)
        {
            if (owner == storage) return;
            Dispose();
            owner = storage;
            if (owner != null) owner.OnStorageChange += onChange;
        }
        public bool ShouldAttempt(Storage storage, int worldId, IEnumerable<Tag> tags,
            Storage specific, out long version, long settings = 0)
        {
            Bind(storage);
            if (!gate.IsWaiting) { version = 0; return true; }
            version = unchecked(StorageNetworkContentIndexService.GetRequestWakeVersion(worldId, tags, specific) * 397 ^ settings);
            return gate.ShouldAttempt(version, Time.time);
        }
        public void RecordResult(bool succeeded, int worldId, IEnumerable<Tag> tags, Storage specific,
            long settings = 0, float fallbackSeconds = 5f)
        {
            if (succeeded) { gate.Reset(); return; }
            gate.RecordResult(false,
                unchecked(StorageNetworkContentIndexService.GetRequestWakeVersion(worldId, tags, specific) * 397 ^ settings),
                Time.time, fallbackSeconds);
        }
        public void Dispose()
        {
            if (!ReferenceEquals(owner, null)) owner.OnStorageChange -= onChange;
            owner = null;
            gate.Reset();
        }
    }
}
