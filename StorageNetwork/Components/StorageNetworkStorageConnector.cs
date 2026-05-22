using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public class StorageNetworkStorageConnector : KMonoBehaviour, IStorageNetworkConnectable
    {
        private Storage storage;
        private Building building;

        public int Cell => Grid.PosToCell(this);

        public int InputCell => GetPortCell(true);

        public int OutputCell => GetPortCell(false);

        public string DisplayName => gameObject.GetProperName();

        public Storage Storage => storage;

        public bool CanShareStorage => storage != null && storage.enabled;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            storage = GetComponent<Storage>();
            building = GetComponent<Building>();
            StorageNetworkRegistry.Register(this);
            AddPortVisuals();
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            base.OnCleanUp();
        }

        private int GetPortCell(bool input)
        {
            if (building == null || building.PlacementCells == null || building.PlacementCells.Length == 0)
            {
                int cell = Cell;
                return input ? Grid.CellBelow(cell) : Grid.CellAbove(cell);
            }

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            foreach (int cell in building.PlacementCells)
            {
                if (!Grid.IsValidCell(cell))
                {
                    continue;
                }

                Grid.CellToXY(cell, out int x, out int y);
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }

            if (minX == int.MaxValue)
            {
                int cell = Cell;
                return input ? Grid.CellBelow(cell) : Grid.CellAbove(cell);
            }

            int portX = (minX + maxX) / 2;
            return Grid.XYToCell(portX, input ? minY - 1 : maxY + 1);
        }

        private void AddPortVisuals()
        {
            EntityCellVisualizer visualizer = gameObject.AddOrGet<EntityCellVisualizer>();
            visualizer.AddPort(EntityCellVisualizer.Ports.PowerIn, GetPortOffset(InputCell), new Color(0.25f, 0.70f, 1f, 1f));
            visualizer.AddPort(EntityCellVisualizer.Ports.PowerOut, GetPortOffset(OutputCell), new Color(1f, 0.82f, 0.20f, 1f));
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
    }
}
