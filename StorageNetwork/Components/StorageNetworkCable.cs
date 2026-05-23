using System.Collections;
using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public class StorageNetworkCable : KMonoBehaviour, IHaveUtilityNetworkMgr, IDisconnectable
    {
        [Serialize]
        private bool disconnected;

        public int Cell => Grid.PosToCell(this);

        public bool IsConnected => !disconnected;

        public IUtilityNetworkMgr GetNetworkManager()
        {
            return Game.Instance.logicCircuitSystem;
        }

        public bool IsDisconnected()
        {
            return disconnected;
        }

        public bool Connect()
        {
            disconnected = false;
            StorageNetworkRegistry.MarkDirty();
            RefreshSelfAndNeighbours();
            return true;
        }

        public void Disconnect()
        {
            disconnected = true;
            GetComponent<KSelectable>()?.SetStatusItem(
                Db.Get().StatusItemCategories.Power,
                Db.Get().BuildingStatusItems.WireDisconnected,
                null);
            StorageNetworkRegistry.MarkDirty();
            RefreshSelfAndNeighbours();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            if (GetComponent<BuildingComplete>() != null)
            {
                StorageNetworkRegistry.Register(this);
            }

            if (!disconnected)
            {
                Connect();
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
            RefreshSelfAndNeighbours();
        }

        public void RefreshSelfAndNeighbours()
        {
            RefreshVisual(Cell);
            foreach (int adjacentCell in GetCardinalCells(Cell))
            {
                RefreshVisual(adjacentCell);
            }
        }

        public UtilityConnections GetActiveConnections()
        {
            if (disconnected)
            {
                return (UtilityConnections)0;
            }

            return GetConnections(Cell);
        }

        private static void RefreshVisual(int cell)
        {
            StorageNetworkCable cable = StorageNetworkRegistry.GetCableAtCell(cell);
            KBatchedAnimController controller = cable?.GetComponent<KBatchedAnimController>();
            if (controller == null)
            {
                return;
            }

            controller.Play(GetVisualizerString(cable.GetActiveConnections()), KAnim.PlayMode.Once, 1f, 0f);
        }

        private static UtilityConnections GetConnections(int cell)
        {
            UtilityConnections connections = (UtilityConnections)0;
            if (StorageNetworkRegistry.AreCableCellsConnected(cell, Grid.CellLeft(cell)))
            {
                connections |= UtilityConnections.Left;
            }

            if (StorageNetworkRegistry.AreCableCellsConnected(cell, Grid.CellRight(cell)))
            {
                connections |= UtilityConnections.Right;
            }

            if (StorageNetworkRegistry.AreCableCellsConnected(cell, Grid.CellAbove(cell)))
            {
                connections |= UtilityConnections.Up;
            }

            if (StorageNetworkRegistry.AreCableCellsConnected(cell, Grid.CellBelow(cell)))
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
