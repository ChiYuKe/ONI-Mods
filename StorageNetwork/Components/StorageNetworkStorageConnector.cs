using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public class StorageNetworkStorageConnector : KMonoBehaviour, IStorageNetworkConnectable
    {
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkStorageConnector> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkStorageConnector>((component, data) => component.OnStorageChange(data));

        private Building building;
        private Storage storage;
        private StorageNetworkFabricatorSettings fabricatorSettings;
        public bool HasOutputPort { get; set; } = StorageNetworkUiOptions.DefaultConnectorHasOutputPort;

        public int Cell => Grid.PosToCell(this);

        public int InputCell => GetPortCell(true);

        public int OutputCell => GetPortCell(false);

        public string DisplayName => gameObject.GetProperName();

        public Storage Storage => storage;

        public bool AllowsNetworkPull => true;

        public bool CanShareStorage => storage != null && storage.enabled;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            building = GetComponent<Building>();
            storage = GetComponent<Storage>();
            fabricatorSettings = GetComponent<StorageNetworkFabricatorSettings>();
            StorageNetworkRegistry.Register(this);
            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            base.OnCleanUp();
        }

        private int GetPortCell(bool input)
        {
            Building currentBuilding = GetBuilding();
            if (currentBuilding == null)
            {
                return GetFallbackPortCell(input);
            }

            Extents extents = currentBuilding.GetExtents();
            if (extents.width <= 0 || extents.height <= 0)
            {
                return GetFallbackPortCell(input);
            }

            if (extents.height >= 2)
            {
                int x = extents.x + (extents.width - 1) / 2;
                int y = input ? extents.y : extents.y + Mathf.Min(extents.height - 1, 3);
                return Grid.XYToCell(x, y);
            }

            if (extents.width >= 2)
            {
                int x = input ? extents.x : extents.x + extents.width - 1;
                int y = extents.y;
                return Grid.XYToCell(x, y);
            }

            return GetFallbackPortCell(input);
        }

        private Building GetBuilding()
        {
            if (building == null)
            {
                building = GetComponent<Building>();
            }

            return building;
        }

        private int GetFallbackPortCell(bool input)
        {
            int cell = Cell;
            return input ? cell : Grid.CellAbove(cell);
        }

        private CellOffset GetPortOffset(int portCell)
        {
            int cell = Cell;
            if (!Grid.IsValidCell(cell) || !Grid.IsValidCell(portCell))
            {
                return CellOffset.none;
            }

            Grid.CellToXY(cell, out int originX, out int originY);
            Grid.CellToXY(portCell, out int portX, out int portY);
            return new CellOffset(portX - originX, portY - originY);
        }

        public void QueueNetworkRefresh()
        {
            if (fabricatorSettings == null)
            {
                fabricatorSettings = GetComponent<StorageNetworkFabricatorSettings>();
            }

            fabricatorSettings?.QueueNetworkRefresh();
        }

        private void OnStorageChange(object data)
        {
            StorageNetworkRegistry.QueueNetworkRefreshes(storage);
        }
    }
}
