using System;
using System.Collections.Generic;
using StorageNetwork.Components;

namespace StorageNetwork.Core
{
    public sealed class StorageNetworkUtilityNetworkManager : IUtilityNetworkMgr
    {
        public static readonly StorageNetworkUtilityNetworkManager Instance = new StorageNetworkUtilityNetworkManager();

        private readonly Dictionary<int, UtilityConnections> pendingConnections =
            new Dictionary<int, UtilityConnections>();

        private StorageNetworkUtilityNetworkManager()
        {
        }

        public bool CanAddConnection(
            UtilityConnections new_connection,
            int cell,
            bool is_physical_building,
            out string fail_reason)
        {
            fail_reason = string.Empty;
            return Grid.IsValidCell(cell);
        }

        public void AddConnection(UtilityConnections new_connection, int cell, bool is_physical_building)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            pendingConnections[cell] = GetConnections(cell, is_physical_building) | new_connection;
        }

        public void StashVisualGrids()
        {
        }

        public void UnstashVisualGrids()
        {
        }

        public string GetVisualizerString(int cell)
        {
            return GetVisualizerString(GetConnections(cell, false));
        }

        public string GetVisualizerString(UtilityConnections connections)
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

        public UtilityConnections GetConnections(int cell, bool is_physical_building)
        {
            if (!Grid.IsValidCell(cell))
            {
                return (UtilityConnections)0;
            }

            UtilityConnections connections;
            if (pendingConnections.TryGetValue(cell, out connections))
            {
                return connections;
            }

            StorageNetworkCable cable = StorageNetworkRegistry.GetCableAtCell(cell);
            return cable != null ? cable.GetActiveConnections() : (UtilityConnections)0;
        }

        public UtilityConnections GetDisplayConnections(int cell)
        {
            return GetConnections(cell, false);
        }

        public void SetConnections(UtilityConnections connections, int cell, bool is_physical_building)
        {
            if (Grid.IsValidCell(cell))
            {
                pendingConnections[cell] = connections;
            }
        }

        public void ClearCell(int cell, bool is_physical_building)
        {
            pendingConnections.Remove(cell);
        }

        public void ForceRebuildNetworks()
        {
            StorageNetworkRegistry.MarkDirty();
        }

        public void AddToNetworks(int cell, object item, bool is_endpoint)
        {
        }

        public void RemoveFromNetworks(int cell, object vent, bool is_endpoint)
        {
            pendingConnections.Remove(cell);
        }

        public object GetEndpoint(int cell)
        {
            return null;
        }

        public UtilityNetwork GetNetworkForDirection(int cell, Direction direction)
        {
            return null;
        }

        public UtilityNetwork GetNetworkForCell(int cell)
        {
            return null;
        }

        public void AddNetworksRebuiltListener(Action<IList<UtilityNetwork>, ICollection<int>> listener)
        {
        }

        public void RemoveNetworksRebuiltListener(Action<IList<UtilityNetwork>, ICollection<int>> listener)
        {
        }

        public IList<UtilityNetwork> GetNetworks()
        {
            return new List<UtilityNetwork>();
        }
    }
}
