using System.Collections;
using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    public class StorageNetworkCable : KMonoBehaviour, IHaveUtilityNetworkMgr
    {
        public int Cell => Grid.PosToCell(this);

        public IUtilityNetworkMgr GetNetworkManager()
        {
            return Game.Instance.logicCircuitSystem;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (GetComponent<BuildingComplete>() != null)
            {
                StorageNetworkRegistry.Register(this);
            }

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

            StorageNetworkCable cable = StorageNetworkRegistry.GetCableAtCell(cell);
            KBatchedAnimController controller = cable?.GetComponent<KBatchedAnimController>();
            if (controller == null)
            {
                return;
            }

            controller.Play(GetVisualizerString(GetConnections(cell)), KAnim.PlayMode.Once, 1f, 0f);
        }

        private static UtilityConnections GetConnections(int cell)
        {
            UtilityConnections connections = (UtilityConnections)0;
            if (StorageNetworkRegistry.IsCableCell(Grid.CellLeft(cell)))
            {
                connections |= UtilityConnections.Left;
            }

            if (StorageNetworkRegistry.IsCableCell(Grid.CellRight(cell)))
            {
                connections |= UtilityConnections.Right;
            }

            if (StorageNetworkRegistry.IsCableCell(Grid.CellAbove(cell)))
            {
                connections |= UtilityConnections.Up;
            }

            if (StorageNetworkRegistry.IsCableCell(Grid.CellBelow(cell)))
            {
                connections |= UtilityConnections.Down;
            }

            return connections;
        }

        private static string GetVisualizerString(UtilityConnections connections)
        {
            string animation = string.Empty;
            if ((connections & UtilityConnections.Left) != (UtilityConnections)0)
            {
                animation += "L";
            }

            if ((connections & UtilityConnections.Right) != (UtilityConnections)0)
            {
                animation += "R";
            }

            if ((connections & UtilityConnections.Up) != (UtilityConnections)0)
            {
                animation += "U";
            }

            if ((connections & UtilityConnections.Down) != (UtilityConnections)0)
            {
                animation += "D";
            }

            return string.IsNullOrEmpty(animation) ? "None" : animation;
        }

        private static IEnumerable<int> GetCardinalCells(int cell)
        {
            yield return Grid.CellAbove(cell);
            yield return Grid.CellBelow(cell);
            yield return Grid.CellLeft(cell);
            yield return Grid.CellRight(cell);
        }
    }
}
