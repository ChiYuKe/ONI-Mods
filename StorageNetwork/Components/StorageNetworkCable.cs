using System.Collections;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public class StorageNetworkCable : KMonoBehaviour
    {
        public int Cell => Grid.PosToCell(this);

        protected override void OnSpawn()
        {
            base.OnSpawn();
            StorageNetworkRegistry.Register(this);
            StartCoroutine(RefreshVisualNextFrame());
        }

        protected override void OnCleanUp()
        {
            StorageNetworkRegistry.Unregister(this);
            base.OnCleanUp();
        }

        private IEnumerator RefreshVisualNextFrame()
        {
            yield return null;

            if (Game.Instance?.logicCircuitSystem != null)
            {
                Game.Instance.logicCircuitSystem.ForceRebuildNetworks();
            }

            RefreshVisual(Cell);
            foreach (int adjacentCell in GetCardinalCells(Cell))
            {
                RefreshVisual(adjacentCell);
            }
        }

        private static void RefreshVisual(int cell)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            GameObject wireObject = Grid.Objects[cell, (int)ObjectLayer.LogicWire];
            KAnimGraphTileVisualizer visualizer = wireObject?.GetComponent<KAnimGraphTileVisualizer>();
            visualizer?.Refresh();
        }

        private static IEnumerable GetCardinalCells(int cell)
        {
            yield return Grid.CellAbove(cell);
            yield return Grid.CellBelow(cell);
            yield return Grid.CellLeft(cell);
            yield return Grid.CellRight(cell);
        }
    }
}
