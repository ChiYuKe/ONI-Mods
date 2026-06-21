using System;
using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkParticleInputPortIngress : KMonoBehaviour
    {
        public bool InputStoreEnabled = true;

        [MyCmpReq]
        private HighEnergyParticlePort port;

        [MyCmpGet]
        private Operational operational;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            port.onParticleCapture = (HighEnergyParticlePort.OnParticleCapture)Delegate.Combine(
                port.onParticleCapture,
                new HighEnergyParticlePort.OnParticleCapture(OnParticleCapture));
        }

        protected override void OnCleanUp()
        {
            if (port != null)
            {
                port.onParticleCapture = (HighEnergyParticlePort.OnParticleCapture)Delegate.Remove(
                    port.onParticleCapture,
                    new HighEnergyParticlePort.OnParticleCapture(OnParticleCapture));
            }

            base.OnCleanUp();
        }

        private void OnParticleCapture(HighEnergyParticle particle)
        {
            if (!InputStoreEnabled ||
                particle == null ||
                particle.payload <= 0f ||
                operational != null && !operational.IsOperational)
            {
                return;
            }

            float moved = StorageNetworkParticleStorageService.Store(gameObject, particle.payload);
            particle.payload -= moved;
            if (particle.payload > 0f)
            {
                port.Uncapture(particle);
            }
        }
    }
}
