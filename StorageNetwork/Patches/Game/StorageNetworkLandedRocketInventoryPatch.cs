using System;
using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkLandedRocketInventoryPatch
    {
        private static readonly Dictionary<Tag, float> LastLoggedTagTime = new Dictionary<Tag, float>();
        private static float lastZeroSampleTime = 0f;
        private static readonly Dictionary<Tag, float> LastLoggedPickupableTime = new Dictionary<Tag, float>();

        private static bool ShouldLogAmountQuery(Tag element)
        {
            float now = Time.unscaledTime;
            return !LastLoggedTagTime.TryGetValue(element, out float lastTime) || (now - lastTime) >= 4.0f;
        }

        private static void LogAmountQuery(
            Tag element,
            int targetWorldId,
            float totalRaw,
            float totalNeeds,
            float finalResult,
            int connectedWorldsCount,
            string breakdown)
        {
            float now = Time.unscaledTime;
            if (LastLoggedTagTime.TryGetValue(element, out float lastTime) && (now - lastTime) < 4.0f)
            {
                return;
            }

            LastLoggedTagTime[element] = now;
            Debug.Log("[StorageNetwork] GetAmountFromRelatedWorlds: tag='" + element.Name +
                      "', targetWorld=" + targetWorldId +
                      ", connectedWorlds=" + connectedWorldsCount +
                      ", totalRaw=" + totalRaw.ToString("F1") +
                      breakdown +
                      ", totalNeeds=" + totalNeeds.ToString("F1") +
                      " -> available=" + finalResult.ToString("F1"));
        }

        private static void LogZeroAmountSample(Tag element, int targetWorldId, int rocketsCount, float initialVanilla, float finalResult)
        {
            float now = Time.unscaledTime;
            if ((now - lastZeroSampleTime) < 5.0f || LastLoggedTagTime.ContainsKey(element))
            {
                return;
            }

            lastZeroSampleTime = now;
            Debug.Log("[StorageNetwork] GetAmountFromRelatedWorlds (zero sample): tag='" + element.Name +
                      "', targetWorld=" + targetWorldId +
                      ", rocketsConnected=" + rocketsCount +
                      " -> available=0.0");
        }

        private static void LogPickupablesAdded(Tag tag, int targetWorldId, int rocketWorldId, int count)
        {
            float now = Time.unscaledTime;
            if (LastLoggedPickupableTime.TryGetValue(tag, out float lastTime) && (now - lastTime) < 4.0f)
            {
                return;
            }

            LastLoggedPickupableTime[tag] = now;
            Debug.Log("[StorageNetwork] GetPickupablesFromRelatedWorlds: tag='" + tag.Name +
                      "', targetWorld=" + targetWorldId +
                      ", rocketWorld=" + rocketWorldId +
                      " -> added " + count + " pickupables");
        }

        [HarmonyPatch(typeof(ClusterUtil), nameof(ClusterUtil.GetAmountFromRelatedWorlds))]
        public static class ClusterUtilGetAmountFromRelatedWorldsPatch
        {
            [System.ThreadStatic]
            private static List<int> connectedWorldsWorkspace;

            [HarmonyPrefix]
            public static bool Prefix(WorldInventory worldInventory, Tag element, ref float __result)
            {
                StorageNetworkWorldUtility.EnsureCacheFresh();

                if (worldInventory == null || element == Tag.Invalid)
                {
                    return true;
                }

                WorldContainer worldContainer = worldInventory.WorldContainer;
                if (worldContainer == null)
                {
                    return true;
                }

                int targetWorldId = worldContainer.id;
                if (targetWorldId < 0 || !StorageNetworkWorldUtility.HasLandedRocketsOrIsLandedRocket(targetWorldId))
                {
                    return true;
                }

                List<int> connectedWorlds = connectedWorldsWorkspace ?? (connectedWorldsWorkspace = new List<int>());
                connectedWorlds.Clear();
                StorageNetworkWorldUtility.GetConnectedWorldIds(targetWorldId, connectedWorlds);
                if (connectedWorlds.Count <= 1)
                {
                    return true;
                }

                float totalRaw = 0f;
                float totalNeeds = 0f;
                bool shouldLog = ShouldLogAmountQuery(element);
                System.Text.StringBuilder breakdownSb = shouldLog ? new System.Text.StringBuilder(" [") : null;

                for (int i = 0; i < connectedWorlds.Count; i++)
                {
                    int wId = connectedWorlds[i];
                    WorldContainer world = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(wId) : null;
                    if (world == null || world.worldInventory == null)
                    {
                        continue;
                    }

                    float worldRaw = 0f;
                    ICollection<Pickupable> pickups = world.worldInventory.GetPickupables(element, false);
                    if (pickups != null && pickups.Count > 0)
                    {
                        foreach (Pickupable p in pickups)
                        {
                            if (p != null && !p.KPrefabID.HasTag(GameTags.StoredPrivate) &&
                                StorageNetworkWorldUtility.GetObjectWorldId(p.gameObject) == wId)
                            {
                                worldRaw += p.TotalAmount;
                            }
                        }
                    }

                    if (worldRaw <= 0f)
                    {
                        worldRaw = world.worldInventory.GetTotalAmount(element, includeRelatedWorlds: false);
                    }

                    totalRaw += worldRaw;

                    if (world.materialNeeds != null && world.materialNeeds.TryGetValue(element, out float need))
                    {
                        totalNeeds += need;
                    }

                    if (shouldLog && (worldRaw > 0f || (world.materialNeeds != null && world.materialNeeds.ContainsKey(element))))
                    {
                        if (breakdownSb.Length > 2)
                        {
                            breakdownSb.Append(", ");
                        }
                        breakdownSb.Append("w").Append(wId).Append(": raw=").Append(worldRaw.ToString("F1"));
                    }
                }

                __result = Mathf.Max(0f, totalRaw - totalNeeds);

                if (shouldLog && (totalRaw > 0f || totalNeeds > 0f))
                {
                    string breakdown = breakdownSb != null ? breakdownSb.Append("]").ToString() : string.Empty;
                    LogAmountQuery(element, targetWorldId, totalRaw, totalNeeds, __result, connectedWorlds.Count, breakdown);
                }
                else if (totalRaw <= 0f && totalNeeds <= 0f)
                {
                    LogZeroAmountSample(element, targetWorldId, connectedWorlds.Count, 0f, __result);
                }

                return false;
            }
        }

        [HarmonyPatch(typeof(ClusterUtil), nameof(ClusterUtil.GetPickupablesFromRelatedWorlds), new Type[] { typeof(WorldInventory), typeof(Tag) })]
        public static class ClusterUtilGetPickupablesFromRelatedWorldsListPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                StorageNetworkWorldUtility.EnsureCacheFresh();
            }

            [HarmonyPostfix]
            public static void Postfix(WorldInventory worldInventory, Tag tag, ref List<Pickupable> __result)
            {
                if (worldInventory == null || tag == Tag.Invalid)
                {
                    return;
                }

                WorldContainer worldContainer = worldInventory.WorldContainer;
                if (worldContainer == null)
                {
                    return;
                }

                int targetWorldId = worldContainer.id;
                if (targetWorldId < 0 || !StorageNetworkWorldUtility.HasLandedRocketsOrIsLandedRocket(targetWorldId))
                {
                    return;
                }

                int parentWorldId = StorageNetworkWorldUtility.GetParentWorldId(targetWorldId);
                int currentParentWorldId = worldContainer.ParentWorldId;

                IReadOnlyList<int> rockets = StorageNetworkWorldUtility.GetLandedRocketWorldIds(parentWorldId);
                for (int i = 0; i < rockets.Count; i++)
                {
                    int rWorldId = rockets[i];
                    if (rWorldId == targetWorldId)
                    {
                        continue;
                    }

                    WorldContainer rocketWorld = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(rWorldId) : null;
                    if (rocketWorld == null || rocketWorld.worldInventory == null)
                    {
                        continue;
                    }

                    if (rocketWorld.ParentWorldId == currentParentWorldId)
                    {
                        continue;
                    }

                    ICollection<Pickupable> pickups = rocketWorld.worldInventory.GetPickupables(tag, false);
                    if (pickups != null && pickups.Count > 0)
                    {
                        if (__result == null)
                        {
                            __result = new List<Pickupable>();
                        }
                        int added = 0;
                        foreach (Pickupable p in pickups)
                        {
                            if (p != null && !__result.Contains(p))
                            {
                                __result.Add(p);
                                added++;
                            }
                        }
                        if (added > 0)
                        {
                            LogPickupablesAdded(tag, targetWorldId, rWorldId, added);
                        }
                    }
                }

                if (targetWorldId != parentWorldId && currentParentWorldId != parentWorldId)
                {
                    WorldContainer parentWorld = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(parentWorldId) : null;
                    if (parentWorld != null && parentWorld.worldInventory != null && parentWorld.ParentWorldId != currentParentWorldId)
                    {
                        ICollection<Pickupable> pickups = parentWorld.worldInventory.GetPickupables(tag, false);
                        if (pickups != null && pickups.Count > 0)
                        {
                            if (__result == null)
                            {
                                __result = new List<Pickupable>();
                            }
                            foreach (Pickupable p in pickups)
                            {
                                if (p != null && !__result.Contains(p))
                                {
                                    __result.Add(p);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ClusterUtil), nameof(ClusterUtil.GetPickupablesFromRelatedWorlds), new Type[] { typeof(WorldInventory), typeof(Tag), typeof(List<Pickupable>) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref })]
        public static class ClusterUtilGetPickupablesFromRelatedWorldsRefPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                StorageNetworkWorldUtility.EnsureCacheFresh();
            }

            [HarmonyPostfix]
            public static void Postfix(WorldInventory worldInventory, Tag tag, ref List<Pickupable> pickupables)
            {
                if (worldInventory == null || tag == Tag.Invalid || pickupables == null)
                {
                    return;
                }

                WorldContainer worldContainer = worldInventory.WorldContainer;
                if (worldContainer == null)
                {
                    return;
                }

                int targetWorldId = worldContainer.id;
                if (targetWorldId < 0 || !StorageNetworkWorldUtility.HasLandedRocketsOrIsLandedRocket(targetWorldId))
                {
                    return;
                }

                int parentWorldId = StorageNetworkWorldUtility.GetParentWorldId(targetWorldId);
                int currentParentWorldId = worldContainer.ParentWorldId;

                IReadOnlyList<int> rockets = StorageNetworkWorldUtility.GetLandedRocketWorldIds(parentWorldId);
                for (int i = 0; i < rockets.Count; i++)
                {
                    int rWorldId = rockets[i];
                    if (rWorldId == targetWorldId)
                    {
                        continue;
                    }

                    WorldContainer rocketWorld = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(rWorldId) : null;
                    if (rocketWorld == null || rocketWorld.worldInventory == null)
                    {
                        continue;
                    }

                    if (rocketWorld.ParentWorldId == currentParentWorldId)
                    {
                        continue;
                    }

                    ICollection<Pickupable> pickups = rocketWorld.worldInventory.GetPickupables(tag, false);
                    if (pickups != null && pickups.Count > 0)
                    {
                        int added = 0;
                        foreach (Pickupable p in pickups)
                        {
                            if (p != null && !pickupables.Contains(p))
                            {
                                pickupables.Add(p);
                                added++;
                            }
                        }
                        if (added > 0)
                        {
                            LogPickupablesAdded(tag, targetWorldId, rWorldId, added);
                        }
                    }
                }

                if (targetWorldId != parentWorldId && currentParentWorldId != parentWorldId)
                {
                    WorldContainer parentWorld = ClusterManager.Instance != null ? ClusterManager.Instance.GetWorld(parentWorldId) : null;
                    if (parentWorld != null && parentWorld.worldInventory != null && parentWorld.ParentWorldId != currentParentWorldId)
                    {
                        ICollection<Pickupable> pickups = parentWorld.worldInventory.GetPickupables(tag, false);
                        if (pickups != null && pickups.Count > 0)
                        {
                            foreach (Pickupable p in pickups)
                            {
                                if (p != null && !pickupables.Contains(p))
                                {
                                    pickupables.Add(p);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Clustercraft), nameof(Clustercraft.Sim1000ms))]
        public static class ClustercraftSim1000msPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                StorageNetworkWorldUtility.EnsureCacheFresh();
            }
        }

        [HarmonyPatch(typeof(Clustercraft), "OnSpawn")]
        public static class ClustercraftOnSpawnPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                StorageNetworkWorldUtility.EnsureCacheFresh();
            }
        }

        [HarmonyPatch(typeof(Clustercraft), nameof(Clustercraft.SetCraftStatus))]
        public static class ClustercraftSetCraftStatusPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                StorageNetworkWorldUtility.InvalidateCache();
                StorageNetworkWorldUtility.EnsureCacheFresh();
            }
        }

        [HarmonyPatch(typeof(Pickupable), nameof(Pickupable.Take), new[] { typeof(float) })]
        public static class PickupableTakePatch
        {
            [HarmonyPostfix]
            public static void Postfix(Pickupable __instance)
            {
                if (__instance != null && __instance.storage != null &&
                    StorageNetworkMembership.IsCollectableStorage(__instance.storage))
                {
                    StorageNetworkContentIndexService.Invalidate(__instance.storage);
                }
            }
        }
    }
}
