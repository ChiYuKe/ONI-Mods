using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkParticleOutputPortEgress : KMonoBehaviour, ISim1000ms
    {
        private const float PayloadPerShot = 50f;
        private const float MinPayload = 0.1f;

        [MyCmpReq]
        private Building building;

        [MyCmpGet]
        private Operational operational;

        public void Sim1000ms(float dt)
        {
            if (operational != null && !operational.IsOperational)
            {
                return;
            }

            float payload = StorageNetworkParticleStorageService.Consume(gameObject, PayloadPerShot);
            if (payload < MinPayload)
            {
                return;
            }

            GameObject particleObject = GameUtil.KInstantiate(
                Assets.GetPrefab("HighEnergyParticle"),
                Grid.CellToPosCCC(building.GetHighEnergyParticleOutputCell(), Grid.SceneLayer.FXFront2),
                Grid.SceneLayer.FXFront2);

            if (particleObject == null)
            {
                return;
            }

            particleObject.SetActive(true);
            HighEnergyParticle particle = particleObject.GetComponent<HighEnergyParticle>();
            if (particle != null)
            {
                particle.payload = payload;
                particle.SetDirection(EightDirection.Right);
            }
        }
    }
}
