namespace StorageNetwork.Core
{
    /// <summary>Only failed requests sleep. Relevant changes wake at the next normal tick.</summary>
    internal sealed class StorageNetworkRequestWakeGate
    {
        private const float FallbackSeconds = 5f;
        private bool waiting;
        private long observedVersion;
        private float retryAt;
        public bool IsWaiting => waiting;

        public void Reset() => waiting = false;
        public bool ShouldAttempt(long version, float now)
            => !waiting || version != observedVersion || now >= retryAt;
        public void RecordResult(bool succeeded, long version, float now, float fallbackSeconds = FallbackSeconds)
        {
            waiting = !succeeded;
            observedVersion = version;
            retryAt = now + fallbackSeconds;
        }
    }
}
