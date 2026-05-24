using KSerialization;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkCableBridge : KMonoBehaviour
    {
        public bool VisualizeOnly { get; set; }

        public int InputCell => GetUtilityCell(true);

        public int OutputCell => GetUtilityCell(false);

        public int Link1Cell => GetLinkCell(new CellOffset(-1, 0));

        public int Link2Cell => GetLinkCell(new CellOffset(1, 0));

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (!VisualizeOnly && GetComponent<BuildingComplete>() != null)
            {
                StorageNetworkRegistry.Register(this);
            }
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            base.OnCleanUp();
        }

        private int GetUtilityCell(bool input)
        {
            Building building = GetComponent<Building>();
            if (building == null)
            {
                return Grid.InvalidCell;
            }

            return input ? building.GetUtilityInputCell() : building.GetUtilityOutputCell();
        }

        private int GetLinkCell(CellOffset offset)
        {
            Building building = GetComponent<Building>();
            if (building == null)
            {
                return Grid.InvalidCell;
            }

            return Grid.OffsetCell(Grid.PosToCell(this), building.GetRotatedOffset(offset));
        }
    }
}
