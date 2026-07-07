using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.Services
{
    public static class StorageNetworkBuildingRegistry
    {
        private static readonly Dictionary<int, GameObject> buildingsByInstanceId = new Dictionary<int, GameObject>();
        private static readonly Dictionary<int, GameObject> logicOutputBuildingsByInstanceId = new Dictionary<int, GameObject>();

        public static void Register(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            if (prefabId == null || prefabId.InstanceID == KPrefabID.InvalidInstanceID)
            {
                return;
            }

            buildingsByInstanceId[prefabId.InstanceID] = target;
            if (HasLogicOutputPort(target))
            {
                logicOutputBuildingsByInstanceId[prefabId.InstanceID] = target;
            }
            else
            {
                logicOutputBuildingsByInstanceId.Remove(prefabId.InstanceID);
            }
        }

        public static void Unregister(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            if (prefabId == null || prefabId.InstanceID == KPrefabID.InvalidInstanceID)
            {
                return;
            }

            buildingsByInstanceId.Remove(prefabId.InstanceID);
            logicOutputBuildingsByInstanceId.Remove(prefabId.InstanceID);
        }

        public static bool TryGetBuilding(int instanceId, out GameObject target)
        {
            return TryGetFrom(buildingsByInstanceId, instanceId, out target);
        }

        public static bool TryGetLogicOutputBuilding(int instanceId, out GameObject target)
        {
            return TryGetFrom(logicOutputBuildingsByInstanceId, instanceId, out target);
        }

        public static List<GameObject> GetBuildingsForWorld(int worldId)
        {
            return GetValuesForWorld(buildingsByInstanceId, worldId);
        }

        public static List<GameObject> GetLogicOutputBuildingsForWorld(int worldId)
        {
            return GetValuesForWorld(logicOutputBuildingsByInstanceId, worldId);
        }

        public static bool IsLogicOutputBuilding(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            return prefabId != null &&
                   prefabId.InstanceID != KPrefabID.InvalidInstanceID &&
                   logicOutputBuildingsByInstanceId.TryGetValue(prefabId.InstanceID, out GameObject registered) &&
                   registered == target;
        }

        public static void RebuildFromScene()
        {
            Clear();
            BuildingComplete[] buildings = Object.FindObjectsByType<BuildingComplete>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (BuildingComplete building in buildings)
            {
                Register(building != null ? building.gameObject : null);
            }
        }

        public static void Clear()
        {
            buildingsByInstanceId.Clear();
            logicOutputBuildingsByInstanceId.Clear();
        }

        private static bool TryGetFrom(Dictionary<int, GameObject> source, int instanceId, out GameObject target)
        {
            target = null;
            if (instanceId == KPrefabID.InvalidInstanceID || instanceId <= 0)
            {
                return false;
            }

            if (!source.TryGetValue(instanceId, out target) || target == null)
            {
                source.Remove(instanceId);
                return false;
            }

            return true;
        }

        private static List<GameObject> GetValuesForWorld(Dictionary<int, GameObject> source, int worldId)
        {
            List<GameObject> results = new List<GameObject>();
            List<int> staleIds = null;
            foreach (KeyValuePair<int, GameObject> pair in source)
            {
                GameObject target = pair.Value;
                if (target == null)
                {
                    staleIds = staleIds ?? new List<int>();
                    staleIds.Add(pair.Key);
                    continue;
                }

                if (worldId < 0 || target.GetMyWorldId() == worldId)
                {
                    results.Add(target);
                }
            }

            if (staleIds != null)
            {
                foreach (int id in staleIds)
                {
                    source.Remove(id);
                }
            }

            return results;
        }

        private static bool HasLogicOutputPort(GameObject target)
        {
            LogicPorts ports = target != null ? target.GetComponent<LogicPorts>() : null;
            return ports?.outputPortInfo != null && ports.outputPortInfo.Length > 0;
        }
    }
}
