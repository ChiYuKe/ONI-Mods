using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkOrderProductionCenterWorkable : ComplexFabricatorWorkable
    {
        private static KAnimFile[] metalRefineryInteractAnims;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            overrideAnims = GetMetalRefineryInteractAnims();
            workLayer = Grid.SceneLayer.Building;
        }

        public override AnimInfo GetAnim(WorkerBase worker)
        {
            return new AnimInfo
            {
                overrideAnims = GetMetalRefineryInteractAnims()
            };
        }

        private static KAnimFile[] GetMetalRefineryInteractAnims()
        {
            if (metalRefineryInteractAnims == null)
            {
                metalRefineryInteractAnims = new[] { Assets.GetAnim("anim_interacts_metalrefinery_kanim") };
            }

            return metalRefineryInteractAnims;
        }
    }
}
