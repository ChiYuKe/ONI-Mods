using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Core;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkLandedRocketFetchPatch
    {
        [System.ThreadStatic]
        private static bool isEvaluating;

        [System.ThreadStatic]
        private static List<Pickupable> candidateWorkspace;

        [System.ThreadStatic]
        private static List<int> connectedWorldsWorkspace;

        [HarmonyPatch(typeof(FetchManager), nameof(FetchManager.FindFetchTarget), new[] { typeof(Storage), typeof(FetchChore) })]
        public static class FetchManagerFindFetchTargetPatch
        {
            private static float lastFetchLogTime = 0f;

            private static void LogFetchTargetFound(FetchChore chore, int destWorldId, int rocketWorldId, Pickupable target)
            {
                float now = UnityEngine.Time.unscaledTime;
                if (now - lastFetchLogTime < 2.0f)
                {
                    return;
                }
                lastFetchLogTime = now;
                UnityEngine.Debug.Log("[StorageNetwork] FindFetchTarget: chore='" + chore?.choreType?.Id +
                                      "', destWorld=" + destWorldId +
                                      " -> found target '" + target?.name +
                                      "' in landed rocket world " + rocketWorldId);
            }

            public static void Postfix(FetchManager __instance, Storage destination, FetchChore chore, ref Pickupable __result)
            {
                if (__result != null || isEvaluating || destination == null || chore == null || __instance == null)
                {
                    return;
                }

                int destWorldId = destination.gameObject != null
                    ? StorageNetworkWorldUtility.GetObjectWorldId(destination.gameObject)
                    : -1;
                if (destWorldId < 0 || !StorageNetworkWorldUtility.HasLandedRocketsOrIsLandedRocket(destWorldId))
                {
                    return;
                }

                isEvaluating = true;
                List<Pickupable> candidates = candidateWorkspace ?? (candidateWorkspace = new List<Pickupable>());
                List<int> connectedWorlds = connectedWorldsWorkspace ?? (connectedWorldsWorkspace = new List<int>());
                candidates.Clear();
                connectedWorlds.Clear();

                try
                {
                    StorageNetworkWorldUtility.GetConnectedWorldIds(destWorldId, connectedWorlds);
                    for (int i = 0; i < connectedWorlds.Count; i++)
                    {
                        int worldId = connectedWorlds[i];
                        if (worldId == destWorldId)
                        {
                            continue;
                        }

                        WorldContainer world = ClusterManager.Instance != null
                            ? ClusterManager.Instance.GetWorld(worldId)
                            : null;
                        if (world == null || world.worldInventory == null)
                        {
                            continue;
                        }

                        candidates.Clear();
                        if (chore.tags != null && chore.tags.Count > 0)
                        {
                            foreach (Tag tag in chore.tags)
                            {
                                if (tag != Tag.Invalid)
                                {
                                    ICollection<Pickupable> pickups = world.worldInventory.GetPickupables(tag, false);
                                    if (pickups != null && pickups.Count > 0)
                                    {
                                        foreach (Pickupable p in pickups)
                                        {
                                            if (p != null && !candidates.Contains(p))
                                            {
                                                candidates.Add(p);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (chore.tagsFirst != Tag.Invalid)
                        {
                            ICollection<Pickupable> pickups = world.worldInventory.GetPickupables(chore.tagsFirst, false);
                            if (pickups != null && pickups.Count > 0)
                            {
                                foreach (Pickupable p in pickups)
                                {
                                    if (p != null && !candidates.Contains(p))
                                    {
                                        candidates.Add(p);
                                    }
                                }
                            }
                        }

                        if (candidates.Count > 0)
                        {
                            Pickupable target = FetchManager.FindFetchTarget(candidates, destination, chore);
                            if (target != null)
                            {
                                __result = target;
                                LogFetchTargetFound(chore, destWorldId, worldId, target);
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    candidates.Clear();
                    connectedWorlds.Clear();
                    isEvaluating = false;
                }
            }
        }
    }
}
