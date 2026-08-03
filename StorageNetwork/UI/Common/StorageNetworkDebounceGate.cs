using System;

namespace StorageNetwork.UI
{
    internal sealed class StorageNetworkDebounceGate
    {
        private readonly float delaySeconds;
        private float elapsedSeconds;
        private bool pending;

        public StorageNetworkDebounceGate(float delaySeconds)
        {
            this.delaySeconds = Math.Max(0f, delaySeconds);
        }

        public bool IsPending => pending;

        public void Request()
        {
            elapsedSeconds = 0f;
            pending = true;
        }

        public void Cancel()
        {
            elapsedSeconds = 0f;
            pending = false;
        }

        public bool Advance(float deltaSeconds)
        {
            if (!pending)
            {
                return false;
            }

            elapsedSeconds += Math.Max(0f, deltaSeconds);
            if (elapsedSeconds < delaySeconds)
            {
                return false;
            }

            Cancel();
            return true;
        }
    }
}
