using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkWorldUtility
    {
        private static int cachedFrame = -1;
        private static readonly Dictionary<int, List<int>> LandedRocketsByParent = new Dictionary<int, List<int>>();
        private static readonly Dictionary<int, int> ParentByLandedRocket = new Dictionary<int, int>();
        private static readonly List<int> EmptyIntList = new List<int>();
        private static readonly Dictionary<int, string> LastLoggedCraftState = new Dictionary<int, string>();
        private static string lastLoggedMappingSummary = string.Empty;

        public static int GetObjectWorldId(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return -1;
            }

            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        public static void InvalidateCache()
        {
            cachedFrame = -1;
        }

        public static void EnsureCacheFresh()
        {
            int frame = Time.frameCount;
            if (cachedFrame == frame)
            {
                return;
            }

            cachedFrame = frame;
            foreach (List<int> list in LandedRocketsByParent.Values)
            {
                list.Clear();
            }
            ParentByLandedRocket.Clear();

            if (ClusterManager.Instance == null || global::Components.Clustercrafts == null)
            {
                return;
            }

            for (int i = 0; i < global::Components.Clustercrafts.Count; i++)
            {
                Clustercraft craft = global::Components.Clustercrafts[i];
                if (craft == null)
                {
                    continue;
                }

                WorldContainer interiorWorld = craft.GetComponent<WorldContainer>();
                if (interiorWorld == null && craft.ModuleInterface != null)
                {
                    interiorWorld = craft.ModuleInterface.GetInteriorWorld();
                }
                if (interiorWorld == null && ClusterManager.Instance != null && ClusterManager.Instance.WorldContainers != null)
                {
                    for (int w = 0; w < ClusterManager.Instance.WorldContainers.Count; w++)
                    {
                        WorldContainer candidate = ClusterManager.Instance.WorldContainers[w];
                        if (candidate != null && candidate.IsModuleInterior && candidate.GetComponent<Clustercraft>() == craft)
                        {
                            interiorWorld = candidate;
                            break;
                        }
                    }
                }
                if (interiorWorld == null)
                {
                    continue;
                }

                if (craft.Status != Clustercraft.CraftStatus.Grounded)
                {
                    if (interiorWorld.ParentWorldId != interiorWorld.id)
                    {
                        interiorWorld.SetParentIdx(interiorWorld.id);
                    }

                    string stateNotGrounded = "status=" + craft.Status + ", interior=" + interiorWorld.id;
                    if (!LastLoggedCraftState.TryGetValue(craft.GetInstanceID(), out string lastState) || lastState != stateNotGrounded)
                    {
                        LastLoggedCraftState[craft.GetInstanceID()] = stateNotGrounded;
                        Debug.Log("[StorageNetwork] Clustercraft '" + craft.Name + "' (ID " + craft.GetInstanceID() + "): status=" + craft.Status + " (interior=" + interiorWorld.id + ", not grounded)");
                    }
                    continue;
                }

                int parentId = -1;
                if (craft.ModuleInterface != null)
                {
                    LaunchPad pad = craft.ModuleInterface.CurrentPad;
                    if (pad != null)
                    {
                        parentId = pad.GetMyWorldId();
                    }

                    if (parentId < 0 || parentId == byte.MaxValue)
                    {
                        PassengerRocketModule passengerModule = craft.ModuleInterface.GetPassengerModule();
                        if (passengerModule != null)
                        {
                            parentId = passengerModule.GetMyWorldId();
                        }
                    }

                    if (parentId < 0 || parentId == byte.MaxValue)
                    {
                        RocketEngineCluster engine = craft.ModuleInterface.GetEngine();
                        if (engine != null)
                        {
                            parentId = engine.GetMyWorldId();
                        }
                    }
                }

                if (parentId < 0 || parentId == byte.MaxValue)
                {
                    parentId = ClusterUtil.GetAsteroidWorldIdAtLocation(craft.Location);
                }

                if ((parentId < 0 || parentId == byte.MaxValue) &&
                    interiorWorld.ParentWorldId >= 0 &&
                    interiorWorld.ParentWorldId != byte.MaxValue &&
                    interiorWorld.ParentWorldId != interiorWorld.id)
                {
                    parentId = interiorWorld.ParentWorldId;
                }

                if (parentId < 0 || parentId == byte.MaxValue || parentId == interiorWorld.id)
                {
                    string stateNoParent = "status=Grounded, interior=" + interiorWorld.id + ", parent=<unresolved>";
                    if (!LastLoggedCraftState.TryGetValue(craft.GetInstanceID(), out string lastState) || lastState != stateNoParent)
                    {
                        LastLoggedCraftState[craft.GetInstanceID()] = stateNoParent;
                        Debug.LogWarning("[StorageNetwork] Clustercraft '" + craft.Name + "' (ID " + craft.GetInstanceID() + ") is Grounded but parent asteroid could not be resolved (interior=" + interiorWorld.id + ", craftLocation=" + craft.Location + ")");
                    }
                    continue;
                }

                WorldContainer parentWorld = ClusterManager.Instance.GetWorld(parentId);
                if (parentWorld == null || parentWorld.IsModuleInterior)
                {
                    continue;
                }

                if (interiorWorld.ParentWorldId != parentId)
                {
                    interiorWorld.SetParentIdx(parentId);
                }

                ParentByLandedRocket[interiorWorld.id] = parentId;
                if (!LandedRocketsByParent.TryGetValue(parentId, out List<int> list))
                {
                    list = new List<int>();
                    LandedRocketsByParent[parentId] = list;
                }
                list.Add(interiorWorld.id);

                string stateGrounded = "status=Grounded, interior=" + interiorWorld.id + ", parent=" + parentId;
                if (!LastLoggedCraftState.TryGetValue(craft.GetInstanceID(), out string prev) || prev != stateGrounded)
                {
                    LastLoggedCraftState[craft.GetInstanceID()] = stateGrounded;
                    Debug.Log("[StorageNetwork] Clustercraft '" + craft.Name + "' (ID " + craft.GetInstanceID() + "): Grounded at parent world " + parentId + " (interior world " + interiorWorld.id + ", ParentWorldId synced=" + interiorWorld.ParentWorldId + ")");
                }
            }

            if (ParentByLandedRocket.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (KeyValuePair<int, int> kvp in ParentByLandedRocket)
                {
                    sb.Append("[rocket " + kvp.Key + " -> asteroid " + kvp.Value + "] ");
                }
                string currentMapping = sb.ToString();
                if (currentMapping != lastLoggedMappingSummary)
                {
                    lastLoggedMappingSummary = currentMapping;
                    Debug.Log("[StorageNetwork] Landed rockets cache active mappings: " + currentMapping);
                }
            }
            else if (!string.IsNullOrEmpty(lastLoggedMappingSummary))
            {
                lastLoggedMappingSummary = string.Empty;
                Debug.Log("[StorageNetwork] Landed rockets cache active mappings: None (no grounded rockets)");
            }
        }

        public static int GetParentWorldId(int worldId)
        {
            if (worldId < 0)
            {
                return worldId;
            }

            EnsureCacheFresh();
            return ParentByLandedRocket.TryGetValue(worldId, out int parentId) ? parentId : worldId;
        }

        public static bool IsLandedRocket(int worldId)
        {
            if (worldId < 0)
            {
                return false;
            }

            EnsureCacheFresh();
            return ParentByLandedRocket.ContainsKey(worldId);
        }

        public static bool HasLandedRockets(int parentWorldId)
        {
            if (parentWorldId < 0)
            {
                return false;
            }

            EnsureCacheFresh();
            return LandedRocketsByParent.TryGetValue(parentWorldId, out List<int> list) && list.Count > 0;
        }

        public static bool HasLandedRocketsOrIsLandedRocket(int worldId)
        {
            if (worldId < 0)
            {
                return false;
            }

            EnsureCacheFresh();
            return ParentByLandedRocket.ContainsKey(worldId) ||
                   (LandedRocketsByParent.TryGetValue(worldId, out List<int> list) && list.Count > 0);
        }

        public static IReadOnlyList<int> GetLandedRocketWorldIds(int parentWorldId)
        {
            if (parentWorldId < 0)
            {
                return EmptyIntList;
            }

            EnsureCacheFresh();
            return LandedRocketsByParent.TryGetValue(parentWorldId, out List<int> list) ? list : EmptyIntList;
        }

        public static bool AreWorldsSameOrLanded(int world1, int world2)
        {
            if (world1 == world2)
            {
                return true;
            }

            if (world1 < 0 || world2 < 0)
            {
                return false;
            }

            EnsureCacheFresh();
            int p1 = ParentByLandedRocket.TryGetValue(world1, out int parent1) ? parent1 : world1;
            int p2 = ParentByLandedRocket.TryGetValue(world2, out int parent2) ? parent2 : world2;
            return p1 == p2;
        }

        public static void GetConnectedWorldIds(int worldId, List<int> result)
        {
            if (result == null)
            {
                return;
            }

            result.Clear();
            if (worldId < 0)
            {
                return;
            }

            EnsureCacheFresh();
            int parentWorldId = ParentByLandedRocket.TryGetValue(worldId, out int p) ? p : worldId;
            result.Add(parentWorldId);

            if (LandedRocketsByParent.TryGetValue(parentWorldId, out List<int> rockets))
            {
                for (int i = 0; i < rockets.Count; i++)
                {
                    int rWorldId = rockets[i];
                    if (rWorldId != parentWorldId && !result.Contains(rWorldId))
                    {
                        result.Add(rWorldId);
                    }
                }
            }
        }
    }
}
