using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    public static class StorageNetworkRegistry
    {
        private static readonly HashSet<StorageNetworkCable> Cables = new HashSet<StorageNetworkCable>();
        private static readonly HashSet<StorageNetworkHub> Hubs = new HashSet<StorageNetworkHub>();
        private static readonly HashSet<StorageNetworkStorageConnector> StorageConnectors = new HashSet<StorageNetworkStorageConnector>();
        private static readonly Dictionary<int, StorageNetworkCable> CablesByCell = new Dictionary<int, StorageNetworkCable>();

        public static int Revision { get; private set; }

        public static IReadOnlyCollection<StorageNetworkHub> RegisteredHubs => Hubs;

        public static IReadOnlyCollection<StorageNetworkCable> RegisteredCables => Cables;

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

        public static void Register(StorageNetworkStorageConnector connector)
        {
            if (connector != null)
            {
                StorageConnectors.Add(connector);
                MarkDirty();
            }
        }

        public static void Unregister(StorageNetworkStorageConnector connector)
        {
            if (connector != null)
            {
                StorageConnectors.Remove(connector);
                MarkDirty();
            }
        }

        public static void MarkDirty()
        {
            Revision++;
        }

        public static bool IsCableConnectedToAnyHub(StorageNetworkCable cable)
        {
            if (!IsLiveCable(cable))
            {
                return false;
            }

            int cell = cable.Cell;
            return Hubs.ToList().Any(hub => hub != null && FindConnectedCableCells(hub).Contains(cell));
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
            return Cables.ToList().Where(cable => IsLiveCable(cable) && networkCells.Contains(cable.Cell));
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
            return FindConnectedStorages(networkCells).Where(storage => storage != requesterStorage);
        }

        public static float GetMassAvailable(Storage requesterStorage, Tag tag)
        {
            if (!CanPullFromNetwork(requesterStorage))
            {
                return 0f;
            }

            return GetSharedStorages(requesterStorage)
                .SelectMany(storage => storage.items)
                .Where(item => item != null && item.TryGetComponent(out KPrefabID prefabId) && prefabId.HasTag(tag))
                .Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
        }

        public static bool CanPullFromNetwork(Storage requesterStorage)
        {
            HashSet<int> networkCells = FindConnectedCableCells(requesterStorage);
            if (networkCells.Count == 0)
            {
                return false;
            }

            return Hubs.Any(hub => hub != null && hub.AllowsNetworkPull && IsHubConnectedToNetwork(hub, networkCells));
        }

        private static HashSet<int> FindConnectedCableCells(StorageNetworkHub hub)
        {
            HashSet<int> startCells = new HashSet<int>();
            if (hub == null)
            {
                return startCells;
            }

            if (IsConnectedCableCell(hub.InputCell))
            {
                startCells.Add(hub.InputCell);
            }

            if (IsConnectedCableCell(hub.OutputCell))
            {
                startCells.Add(hub.OutputCell);
            }

            Building building = hub.GetComponent<Building>();
            if (building != null)
            {
                foreach (int cell in building.PlacementCells)
                {
                    AddAdjacentCableCells(cell, startCells);
                }
            }

            return ExpandCableNetwork(startCells);
        }

        private static HashSet<int> FindConnectedCableCells(Storage requesterStorage)
        {
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

            if (connector.Storage == null && IsConnectedCableCell(connector.OutputCell))
            {
                startCells.Add(connector.OutputCell);
            }

            return ExpandCableNetwork(startCells);
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

                foreach (StorageNetworkStorageConnector connector in StorageConnectors)
                {
                    if (connector == null || !connector.CanShareStorage || connector.InputCell != cell)
                    {
                        continue;
                    }

                    if (IsConnectedCableCell(connector.OutputCell) && !visited.Contains(connector.OutputCell))
                    {
                        pending.Enqueue(connector.OutputCell);
                    }
                }
            }

            return visited;
        }

        private static IEnumerable<Storage> FindConnectedStorages(HashSet<int> networkCells)
        {
            HashSet<Storage> storages = new HashSet<Storage>();
            foreach (StorageNetworkStorageConnector connector in StorageConnectors)
            {
                if (connector?.Storage != null &&
                    connector.CanShareStorage &&
                    networkCells.Contains(connector.InputCell))
                {
                    storages.Add(connector.Storage);
                }
            }

            return storages;
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
                ? buildingObject.AddOrGet<StorageNetworkStorageConnector>()
                : null;
        }

        private static void AddAdjacentCableCells(int cell, HashSet<int> result)
        {
            foreach (int adjacentCell in GetCardinalCells(cell))
            {
                if (IsConnectedCableCell(adjacentCell))
                {
                    result.Add(adjacentCell);
                }
            }
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
            if (networkCells.Contains(hub.InputCell) || networkCells.Contains(hub.OutputCell))
            {
                return true;
            }

            Building building = hub.GetComponent<Building>();
            return building != null && building.PlacementCells.Any(cell => GetCardinalCells(cell).Any(networkCells.Contains));
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
    }
}
