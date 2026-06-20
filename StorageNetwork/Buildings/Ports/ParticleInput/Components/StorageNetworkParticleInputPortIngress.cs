using System;
using StorageNetwork.Services;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkParticleInputPortIngress : KMonoBehaviour
    {
        [MyCmpReq]
        private HighEnergyParticlePort port;

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
            if (particle == null || particle.payload <= 0f)
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
