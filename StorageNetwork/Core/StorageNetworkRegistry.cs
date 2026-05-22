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
                MarkDirty();
            }
        }

        public static void Unregister(StorageNetworkCable cable)
        {
            if (cable != null)
            {
                Cables.Remove(cable);
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
            return Cables.Where(cable => cable != null && networkCells.Contains(cable.Cell));
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
            return GetSharedStorages(requesterStorage)
                .SelectMany(storage => storage.items)
                .Where(item => item != null && item.TryGetComponent(out KPrefabID prefabId) && prefabId.HasTag(tag))
                .Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
        }

        private static HashSet<int> FindConnectedCableCells(StorageNetworkHub hub)
        {
            HashSet<int> startCells = new HashSet<int>();
            Building building = hub.GetComponent<Building>();
            if (building == null)
            {
                return startCells;
            }

            foreach (int cell in building.PlacementCells)
            {
                AddAdjacentCableCells(cell, startCells);
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

            if (IsCableCell(connector.InputCell))
            {
                startCells.Add(connector.InputCell);
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
                    if (IsCableCell(adjacentCell) && !visited.Contains(adjacentCell))
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

                    if (IsCableCell(connector.OutputCell) && !visited.Contains(connector.OutputCell))
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
                if (IsCableCell(adjacentCell))
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

            return Cables.FirstOrDefault(cable => cable != null && cable.Cell == cell);
        }

        public static bool IsCableCell(int cell)
        {
            return GetCableAtCell(cell) != null;
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
