using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkRegistry
    {
        private static readonly HashSet<StorageNetworkCable> Cables = new HashSet<StorageNetworkCable>();
        private static readonly HashSet<StorageNetworkCableBridge> Bridges = new HashSet<StorageNetworkCableBridge>();
        private static readonly HashSet<StorageNetworkHub> Hubs = new HashSet<StorageNetworkHub>();
        private static readonly HashSet<StorageNetworkStorageConnector> StorageConnectors = new HashSet<StorageNetworkStorageConnector>();
        private static readonly Dictionary<int, StorageNetworkCable> CablesByCell = new Dictionary<int, StorageNetworkCable>();
        private static readonly Dictionary<int, List<StorageNetworkCableBridge>> BridgesByEndpointCell =
            new Dictionary<int, List<StorageNetworkCableBridge>>();
        private static readonly Dictionary<int, List<StorageNetworkStorageConnector>> ConnectorsByInputCell =
            new Dictionary<int, List<StorageNetworkStorageConnector>>();
        private static readonly Dictionary<int, List<StorageNetworkStorageConnector>> ConnectorsByOutputCell =
            new Dictionary<int, List<StorageNetworkStorageConnector>>();
        private static readonly Dictionary<StorageNetworkHub, HashSet<int>> HubCableCellsCache =
            new Dictionary<StorageNetworkHub, HashSet<int>>();
        private static readonly Dictionary<Storage, HashSet<int>> StorageCableCellsCache =
            new Dictionary<Storage, HashSet<int>>();
        private static readonly Dictionary<StorageMassKey, float> MassAvailableCache =
            new Dictionary<StorageMassKey, float>();
        private static bool dirtyQueued;

        public static int Revision { get; private set; }

        public static IReadOnlyCollection<StorageNetworkHub> RegisteredHubs => Hubs;

        public static IReadOnlyCollection<StorageNetworkCable> RegisteredCables => Cables;

        public static IReadOnlyCollection<StorageNetworkCableBridge> RegisteredBridges => Bridges;

        public static IReadOnlyCollection<StorageNetworkStorageConnector> RegisteredStorageConnectors => StorageConnectors;

        public static void Register(StorageNetworkHub hub)
        {
            if (hub != null)
            {
                Hubs.Add(hub);
            }
        }

        public static void Unregister(StorageNetworkHub hub)
        {
            if (hub != null)
            {
                Hubs.Remove(hub);
            }
        }

        public static void Register(StorageNetworkCable cable)
        {
            if (cable != null)
            {
                Cables.Add(cable);
                CacheCable(cable);
                MarkDirty();
            }
        }

        public static void Unregister(StorageNetworkCable cable)
        {
            if (cable != null)
            {
                Cables.Remove(cable);
                if (Grid.IsValidCell(cable.Cell) &&
                    CablesByCell.TryGetValue(cable.Cell, out StorageNetworkCable cachedCable) &&
                    cachedCable == cable)
                {
                    CablesByCell.Remove(cable.Cell);
                }

                MarkDirty();
            }
        }

        public static void Register(StorageNetworkCableBridge bridge)
        {
            if (bridge != null)
            {
                Bridges.Add(bridge);
                CacheBridge(bridge);
                MarkDirty();
            }
        }

        public static void Unregister(StorageNetworkCableBridge bridge)
        {
            if (bridge != null)
            {
                Bridges.Remove(bridge);
                UncacheBridge(bridge);
                MarkDirty();
            }
        }

        public static void Register(StorageNetworkStorageConnector connector)
        {
            if (connector != null)
            {
                StorageConnectors.Add(connector);
                CacheConnector(connector);
                MarkDirty();
            }
        }

        public static void Unregister(StorageNetworkStorageConnector connector)
        {
            if (connector != null)
            {
                StorageConnectors.Remove(connector);
                UncacheConnector(connector);
                MarkDirty();
            }
        }

        public static void MarkDirty()
        {
            if (dirtyQueued)
            {
                return;
            }

            dirtyQueued = true;
            if (GameScheduler.Instance != null)
            {
                GameScheduler.Instance.ScheduleNextFrame(
                    "StorageNetworkRegistryDirty",
                    _ => FlushDirty());
                return;
            }

            FlushDirty();
        }

        private static void FlushDirty()
        {
            dirtyQueued = false;
            Revision++;
            HubCableCellsCache.Clear();
            StorageCableCellsCache.Clear();
            MassAvailableCache.Clear();
            QueueAllFabricatorRefreshes();
        }

        public static void QueueNetworkRefreshes(Storage changedStorage)
        {
            if (changedStorage == null)
            {
                return;
            }

            MassAvailableCache.Clear();
            HashSet<int> networkCells = FindConnectedCableCells(changedStorage);
            if (networkCells.Count == 0)
            {
                return;
            }

            QueueFabricatorRefreshes(networkCells);
        }

        public static bool IsCableConnectedToAnyHub(StorageNetworkCable cable)
        {
            if (!IsLiveCable(cable))
            {
                return false;
            }

            int cell = cable.Cell;
            foreach (StorageNetworkHub hub in Hubs)
            {
                if (hub != null && FindConnectedCableCells(hub).Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsCableConnectedToHub(StorageNetworkCable cable, StorageNetworkHub hub)
        {
            return IsLiveCable(cable) && hub != null && FindConnectedCableCells(hub).Contains(cable.Cell);
        }

        public static StorageNetworkSnapshot BuildSnapshot(StorageNetworkHub hub)
        {
            if (hub == null)
            {
                return StorageNetworkSnapshot.Empty;
            }

            HashSet<int> networkCells = FindConnectedCableCells(hub);
            List<StorageNetworkStorageInfo> storages = FindConnectedStorages(networkCells)
                .Select(storage => new StorageNetworkStorageInfo(storage))
                .OrderBy(info => info.Name)
                .ToList();

            return new StorageNetworkSnapshot(
                storages,
                storages.Sum(info => info.StoredKg),
                storages.Sum(info => info.CapacityKg));
        }

        public static IEnumerable<StorageNetworkCable> GetConnectedCables(StorageNetworkHub hub)
        {
            HashSet<int> networkCells = FindConnectedCableCells(hub);
            return Cables.Where(cable => IsLiveCable(cable) && networkCells.Contains(cable.Cell));
        }

        public static HashSet<int> GetConnectedCableCells(StorageNetworkHub hub)
        {
            return FindConnectedCableCells(hub);
        }

        public static IEnumerable<Storage> GetSharedStorages(Storage requesterStorage)
        {
            if (requesterStorage == null)
            {
                return Enumerable.Empty<Storage>();
            }

            HashSet<int> networkCells = FindConnectedCableCells(requesterStorage);
            return FindConnectedStorages(networkCells).Where(storage => storage != requesterStorage).ToList();
        }

        public static float GetMassAvailable(Storage requesterStorage, Tag tag)
        {
            if (requesterStorage == null || !tag.IsValid)
            {
                return 0f;
            }

            StorageMassKey cacheKey = new StorageMassKey(requesterStorage, tag);
            if (MassAvailableCache.TryGetValue(cacheKey, out float cachedMass))
            {
                return cachedMass;
            }

            HashSet<int> networkCells = FindConnectedCableCells(requesterStorage);
            if (!CanPullFromNetwork(networkCells))
            {
                MassAvailableCache[cacheKey] = 0f;
                return 0f;
            }

            float mass = 0f;
            foreach (Storage storage in FindConnectedStorages(networkCells))
            {
                if (storage != requesterStorage)
                {
                    mass += storage.GetAmountAvailable(tag);
                }
            }

            MassAvailableCache[cacheKey] = mass;
            return mass;
        }

        public static bool CanPullFromNetwork(Storage requesterStorage)
        {
            HashSet<int> networkCells = FindConnectedCableCells(requesterStorage);
            if (networkCells.Count == 0)
            {
                return false;
            }

            return CanPullFromNetwork(networkCells);
        }

        private static bool CanPullFromNetwork(HashSet<int> networkCells)
        {
            if (networkCells == null || networkCells.Count == 0)
            {
                return false;
            }

            foreach (StorageNetworkHub hub in Hubs)
            {
                if (hub != null && hub.AllowsNetworkPull && IsHubConnectedToNetwork(hub, networkCells))
                {
                    return true;
                }
            }

            return false;
        }

        public static StorageNetworkHub GetConnectedHub(StorageNetworkStorageConnector connector)
        {
            if (connector == null)
            {
                return null;
            }

            HashSet<int> networkCells = FindConnectedCableCells(connector.Storage);
            StorageNetworkHub connectedHub = null;
            foreach (StorageNetworkHub hub in Hubs)
            {
                if (hub == null || !IsHubConnectedToNetwork(hub, networkCells))
                {
                    continue;
                }

                if (connectedHub == null || hub.GetInstanceID() < connectedHub.GetInstanceID())
                {
                    connectedHub = hub;
                }
            }

            return connectedHub;
        }

        private static HashSet<int> FindConnectedCableCells(StorageNetworkHub hub)
        {
            if (hub != null && HubCableCellsCache.TryGetValue(hub, out HashSet<int> cachedCells))
            {
                return cachedCells;
            }

            HashSet<int> startCells = new HashSet<int>();
            if (hub == null)
            {
                return startCells;
            }

            if (IsConnectedCableCell(hub.OutputCell))
            {
                startCells.Add(hub.OutputCell);
            }

            HashSet<int> result = ExpandCableNetwork(startCells);
            HubCableCellsCache[hub] = result;
            return result;
        }

        private static HashSet<int> FindConnectedCableCells(Storage requesterStorage)
        {
            if (requesterStorage != null && StorageCableCellsCache.TryGetValue(requesterStorage, out HashSet<int> cachedCells))
            {
                return cachedCells;
            }

            IStorageNetworkConnectable connector = GetConnector(requesterStorage?.gameObject);
            HashSet<int> startCells = new HashSet<int>();
            if (connector == null)
            {
                return startCells;
            }

            if (IsConnectedCableCell(connector.InputCell))
            {
                startCells.Add(connector.InputCell);
            }

            if (connector.HasOutputPort && connector.Storage == null && IsConnectedCableCell(connector.OutputCell))
            {
                startCells.Add(connector.OutputCell);
            }

            HashSet<int> result = ExpandCableNetwork(startCells);
            if (requesterStorage != null)
            {
                StorageCableCellsCache[requesterStorage] = result;
            }

            return result;
        }

        private static HashSet<int> ExpandCableNetwork(HashSet<int> startCells)
        {
            HashSet<int> visited = new HashSet<int>();
            Queue<int> pending = new Queue<int>(startCells);

            while (pending.Count > 0)
            {
                int cell = pending.Dequeue();
                if (!visited.Add(cell))
                {
                    continue;
                }

                foreach (int adjacentCell in GetCardinalCells(cell))
                {
                    if (AreCableCellsConnected(cell, adjacentCell) && !visited.Contains(adjacentCell))
                    {
                        pending.Enqueue(adjacentCell);
                    }
                }

                EnqueueConnectorBridges(cell, visited, pending);
                EnqueueCableBridges(cell, visited, pending);
            }

            return visited;
        }

        private static void EnqueueCableBridges(int cell, HashSet<int> visited, Queue<int> pending)
        {
            if (!BridgesByEndpointCell.TryGetValue(cell, out List<StorageNetworkCableBridge> bridges))
            {
                return;
            }

            foreach (StorageNetworkCableBridge bridge in bridges)
            {
                if (!IsLiveBridge(bridge))
                {
                    continue;
                }

                int otherCell = ResolveBridgeOppositeCableCell(bridge, cell);
                if (Grid.IsValidCell(otherCell) && IsConnectedCableCell(otherCell) && !visited.Contains(otherCell))
                {
                    pending.Enqueue(otherCell);
                }
            }
        }

        private static int ResolveBridgeOppositeCableCell(StorageNetworkCableBridge bridge, int cableCell)
        {
            if (bridge == null)
            {
                return Grid.InvalidCell;
            }

            int inputCableCell = ResolveBridgeCableCell(bridge.Link1Cell);
            int outputCableCell = ResolveBridgeCableCell(bridge.Link2Cell);

            if (cableCell == inputCableCell)
            {
                return outputCableCell;
            }

            if (cableCell == outputCableCell)
            {
                return inputCableCell;
            }

            return Grid.InvalidCell;
        }

        private static int ResolveBridgeCableCell(int linkCell)
        {
            if (!Grid.IsValidCell(linkCell))
            {
                return Grid.InvalidCell;
            }

            return IsConnectedCableCell(linkCell) ? linkCell : Grid.InvalidCell;
        }

        private static void EnqueueConnectorBridges(int cell, HashSet<int> visited, Queue<int> pending)
        {
            if (ConnectorsByInputCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> inputConnectors))
            {
                foreach (StorageNetworkStorageConnector connector in inputConnectors)
                {
                    if (connector != null &&
                        connector.HasOutputPort &&
                        connector.CanShareStorage &&
                        IsConnectedCableCell(connector.OutputCell) &&
                        !visited.Contains(connector.OutputCell))
                    {
                        pending.Enqueue(connector.OutputCell);
                    }
                }
            }

            if (ConnectorsByOutputCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> outputConnectors))
            {
                foreach (StorageNetworkStorageConnector connector in outputConnectors)
                {
                    if (connector != null &&
                        connector.HasOutputPort &&
                        connector.CanShareStorage &&
                        IsConnectedCableCell(connector.InputCell) &&
                        !visited.Contains(connector.InputCell))
                    {
                        pending.Enqueue(connector.InputCell);
                    }
                }
            }
        }

        private static IEnumerable<Storage> FindConnectedStorages(HashSet<int> networkCells)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            foreach (int cell in networkCells)
            {
                if (!ConnectorsByInputCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> connectors))
                {
                    continue;
                }

                foreach (StorageNetworkStorageConnector connector in connectors)
                {
                    if (connector?.Storage != null && connector.CanShareStorage)
                    {
                        storages.Add(connector.Storage);
                    }
                }
            }

            return storages;
        }

        private static void QueueAllFabricatorRefreshes()
        {
            foreach (StorageNetworkStorageConnector connector in StorageConnectors)
            {
                connector?.QueueNetworkRefresh();
            }
        }

        private static void QueueFabricatorRefreshes(HashSet<int> networkCells)
        {
            HashSet<StorageNetworkStorageConnector> queuedConnectors = null;
            foreach (int cell in networkCells)
            {
                if (!ConnectorsByInputCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> connectors))
                {
                    continue;
                }

                foreach (StorageNetworkStorageConnector connector in connectors)
                {
                    if (connector?.Storage == null || !connector.CanShareStorage)
                    {
                        continue;
                    }

                    if (queuedConnectors == null)
                    {
                        queuedConnectors = new HashSet<StorageNetworkStorageConnector>();
                    }

                    if (queuedConnectors.Add(connector))
                    {
                        connector.QueueNetworkRefresh();
                    }
                }
            }
        }

        private static IStorageNetworkConnectable GetConnector(GameObject buildingObject)
        {
            if (buildingObject == null)
            {
                return null;
            }

            IStorageNetworkConnectable connector = buildingObject.GetComponent<IStorageNetworkConnectable>();
            if (connector != null)
            {
                return connector;
            }

            return buildingObject.GetComponent<Storage>() != null
                ? GetStorageConnector(buildingObject)
                : null;
        }

        private static IStorageNetworkConnectable GetStorageConnector(GameObject buildingObject)
        {
            if (!StorageNetworkTags.CanConnectToNetwork(buildingObject))
            {
                return null;
            }

            return buildingObject.AddOrGet<StorageNetworkStorageConnector>();
        }

        public static StorageNetworkCable GetCableAtCell(int cell)
        {
            if (!Grid.IsValidCell(cell))
            {
                return null;
            }

            if (CablesByCell.TryGetValue(cell, out StorageNetworkCable cachedCable))
            {
                if (IsLiveCable(cachedCable) && cachedCable.Cell == cell)
                {
                    return cachedCable;
                }

                CablesByCell.Remove(cell);
                Cables.Remove(cachedCable);
            }

            List<StorageNetworkCable> staleCables = null;
            foreach (StorageNetworkCable cable in Cables)
            {
                if (!IsLiveCable(cable))
                {
                    if (staleCables == null)
                    {
                        staleCables = new List<StorageNetworkCable>();
                    }

                    staleCables.Add(cable);
                    continue;
                }

                if (cable.Cell == cell)
                {
                    CacheCable(cable);
                    RemoveStaleCables(staleCables);
                    return cable;
                }
            }

            RemoveStaleCables(staleCables);
            return null;
        }

        public static bool IsCableCell(int cell)
        {
            return GetCableAtCell(cell) != null;
        }

        private static bool IsConnectedCableCell(int cell)
        {
            StorageNetworkCable cable = GetCableAtCell(cell);
            return IsLiveCable(cable) && !cable.IsDisconnected();
        }

        public static bool AreCableCellsConnected(int cell, int adjacentCell)
        {
            StorageNetworkCable cable = GetCableAtCell(cell);
            StorageNetworkCable adjacentCable = GetCableAtCell(adjacentCell);
            if (!IsLiveCable(cable) || !IsLiveCable(adjacentCable))
            {
                return false;
            }

            UtilityConnections direction = UtilityConnectionsExtensions.DirectionFromToCell(cell, adjacentCell);
            return direction != (UtilityConnections)0 &&
                HasConnection(cable, direction) &&
                HasConnection(adjacentCable, direction.InverseDirection());
        }

        private static IEnumerable<int> GetCardinalCells(int cell)
        {
            yield return Grid.CellAbove(cell);
            yield return Grid.CellBelow(cell);
            yield return Grid.CellLeft(cell);
            yield return Grid.CellRight(cell);
        }

        private static bool IsHubConnectedToNetwork(StorageNetworkHub hub, HashSet<int> networkCells)
        {
            return hub != null && networkCells.Contains(hub.OutputCell);
        }

        private static bool IsLiveCable(StorageNetworkCable cable)
        {
            if (cable == null || cable.gameObject == null || !cable.isSpawned || cable.GetComponent<BuildingComplete>() == null)
            {
                return false;
            }

            int cell = cable.Cell;
            return Grid.IsValidCell(cell) && Grid.Objects[cell, (int)ObjectLayer.LogicWire] == cable.gameObject;
        }

        private static bool IsLiveBridge(StorageNetworkCableBridge bridge)
        {
            if (bridge == null || bridge.gameObject == null || !bridge.isSpawned || bridge.GetComponent<BuildingComplete>() == null)
            {
                return false;
            }

            return Grid.IsValidCell(bridge.Link1Cell) && Grid.IsValidCell(bridge.Link2Cell);
        }

        private static bool HasConnection(StorageNetworkCable cable, UtilityConnections direction)
        {
            if (cable == null || cable.IsDisconnected())
            {
                return false;
            }

            KAnimGraphTileVisualizer visualizer = cable.GetComponent<KAnimGraphTileVisualizer>();
            UtilityConnections connections = visualizer != null
                ? visualizer.Connections
                : (UtilityConnections)0;
            return (connections & direction) != (UtilityConnections)0;
        }

        private static void CacheCable(StorageNetworkCable cable)
        {
            if (IsLiveCable(cable))
            {
                CablesByCell[cable.Cell] = cable;
            }
        }

        private static void CacheConnector(StorageNetworkStorageConnector connector)
        {
            AddConnectorCell(ConnectorsByInputCell, connector.InputCell, connector);
            if (connector.HasOutputPort)
            {
                AddConnectorCell(ConnectorsByOutputCell, connector.OutputCell, connector);
            }
        }

        private static void UncacheConnector(StorageNetworkStorageConnector connector)
        {
            RemoveConnectorCell(ConnectorsByInputCell, connector.InputCell, connector);
            RemoveConnectorCell(ConnectorsByOutputCell, connector.OutputCell, connector);
        }

        private static void CacheBridge(StorageNetworkCableBridge bridge)
        {
            AddBridgeCell(bridge.Link1Cell, bridge);
            AddBridgeCell(bridge.Link2Cell, bridge);
        }

        private static void UncacheBridge(StorageNetworkCableBridge bridge)
        {
            RemoveBridgeCell(bridge.Link1Cell, bridge);
            RemoveBridgeCell(bridge.Link2Cell, bridge);
        }

        public static bool IsStorageNetworkPortCell(int cell)
        {
            if (!Grid.IsValidCell(cell))
            {
                return false;
            }

            foreach (StorageNetworkHub hub in Hubs)
            {
                if (hub != null && IsConnectablePortCell(hub, cell))
                {
                    return true;
                }
            }

            foreach (StorageNetworkStorageConnector connector in StorageConnectors)
            {
                if (connector != null && IsConnectablePortCell(connector, cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsConnectablePortCell(IStorageNetworkConnectable connectable, int cell)
        {
            return connectable != null &&
                (connectable.InputCell == cell || (connectable.HasOutputPort && connectable.OutputCell == cell));
        }

        private static void AddBridgeCell(int cell, StorageNetworkCableBridge bridge)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            if (!BridgesByEndpointCell.TryGetValue(cell, out List<StorageNetworkCableBridge> bridges))
            {
                bridges = new List<StorageNetworkCableBridge>();
                BridgesByEndpointCell[cell] = bridges;
            }

            if (!bridges.Contains(bridge))
            {
                bridges.Add(bridge);
            }
        }

        private static void RemoveBridgeCell(int cell, StorageNetworkCableBridge bridge)
        {
            if (!BridgesByEndpointCell.TryGetValue(cell, out List<StorageNetworkCableBridge> bridges))
            {
                return;
            }

            bridges.Remove(bridge);
            if (bridges.Count == 0)
            {
                BridgesByEndpointCell.Remove(cell);
            }
        }

        private static void AddConnectorCell(
            Dictionary<int, List<StorageNetworkStorageConnector>> connectorsByCell,
            int cell,
            StorageNetworkStorageConnector connector)
        {
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            if (!connectorsByCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> connectors))
            {
                connectors = new List<StorageNetworkStorageConnector>();
                connectorsByCell[cell] = connectors;
            }

            if (!connectors.Contains(connector))
            {
                connectors.Add(connector);
            }
        }

        private static void RemoveConnectorCell(
            Dictionary<int, List<StorageNetworkStorageConnector>> connectorsByCell,
            int cell,
            StorageNetworkStorageConnector connector)
        {
            if (!connectorsByCell.TryGetValue(cell, out List<StorageNetworkStorageConnector> connectors))
            {
                return;
            }

            connectors.Remove(connector);
            if (connectors.Count == 0)
            {
                connectorsByCell.Remove(cell);
            }
        }

        private static void RemoveStaleCables(List<StorageNetworkCable> staleCables)
        {
            if (staleCables == null)
            {
                return;
            }

            foreach (StorageNetworkCable staleCable in staleCables)
            {
                Cables.Remove(staleCable);
                if (staleCable != null && Grid.IsValidCell(staleCable.Cell) &&
                    CablesByCell.TryGetValue(staleCable.Cell, out StorageNetworkCable cachedCable) &&
                    cachedCable == staleCable)
                {
                    CablesByCell.Remove(staleCable.Cell);
                }
            }
        }

        private struct StorageMassKey
        {
            private readonly Storage storage;
            private readonly Tag tag;

            public StorageMassKey(Storage storage, Tag tag)
            {
                this.storage = storage;
                this.tag = tag;
            }

            public override bool Equals(object obj)
            {
                return obj is StorageMassKey other && storage == other.storage && tag == other.tag;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((storage != null ? storage.GetInstanceID() : 0) * 397) ^ tag.GetHashCode();
                }
            }
        }
    }
}
