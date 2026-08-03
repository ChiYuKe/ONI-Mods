using System.Collections.Generic;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    /// <summary>
    /// Owns the expensive, read-mostly production-order snapshots. Service instances are
    /// lightweight scoped facades; inventory/catalog state is built once and shared by the
    /// UI, background maintenance and world-panel consumers.
    /// </summary>
    internal sealed class ProductionOrderRuntime
    {
        private const float InventorySnapshotLifetimeSeconds = 10f;
        private const float RecipeSnapshotLifetimeSeconds = 60f;

        private readonly Dictionary<int, WorldInventorySnapshot> inventorySnapshots =
            new Dictionary<int, WorldInventorySnapshot>();
        private readonly Dictionary<int, ProductionRecipeSnapshot> scopedRecipeSnapshots =
            new Dictionary<int, ProductionRecipeSnapshot>();

        private ProductionRecipeSnapshot globalRecipeSnapshot = ProductionRecipeSnapshot.Empty;
        private int recipeCatalogVersion = -1;
        private int recipeCapabilityVersion = -1;
        private int recipeConnectivityVersion = -1;
        private int planningEpoch;
        private float lastGlobalRecipeRefreshTime = float.NegativeInfinity;

        public ProductionNetworkInventoryCache GetNetworkInventory(int worldId)
        {
            return EnsureInventorySnapshot(worldId).NetworkInventory;
        }

        public Dictionary<Tag, float> GetConnectedFabricatorOutputAmounts(int worldId)
        {
            return EnsureInventorySnapshot(worldId).ConnectedFabricatorOutputAmounts;
        }

        public void RefreshInventorySnapshot(int worldId)
        {
            RefreshInventorySnapshot(GetOrCreateInventorySnapshot(worldId), worldId, Time.unscaledTime);
        }

        public void InvalidateInventorySnapshot(int worldId)
        {
            if (StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                foreach (WorldInventorySnapshot snapshot in inventorySnapshots.Values)
                {
                    snapshot.HasSnapshot = false;
                }

                return;
            }

            if (inventorySnapshots.TryGetValue(worldId, out WorldInventorySnapshot worldSnapshot))
            {
                worldSnapshot.HasSnapshot = false;
            }
        }

        public IReadOnlyList<RecipeDisplayInfo> GetRecipes(StorageNetworkOrderProductionCenter center, bool forceRebuild)
        {
            return GetRecipeSnapshot(center, forceRebuild).Recipes;
        }

        public ProductionRecipeSnapshot GetRecipeSnapshot(
            StorageNetworkOrderProductionCenter center,
            bool forceRebuild)
        {
            EnsureRecipeVersion();
            float now = Time.unscaledTime;
            if (center == null)
            {
                if (forceRebuild ||
                    globalRecipeSnapshot.Recipes.Count == 0 ||
                    now < lastGlobalRecipeRefreshTime ||
                    now - lastGlobalRecipeRefreshTime >= RecipeSnapshotLifetimeSeconds)
                {
                    globalRecipeSnapshot = new ProductionRecipeSnapshot(
                        ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos(),
                        now,
                        GetPlanningVersion());
                    lastGlobalRecipeRefreshTime = now;
                    recipeCatalogVersion = ProductionOrderCenterCatalog.Version;
                }

                return globalRecipeSnapshot;
            }

            int instanceId = ProductionOrderCenterCatalog.GetInstanceId(center);
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return new ProductionRecipeSnapshot(
                    ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos(center),
                    now,
                    GetPlanningVersion());
            }

            if (!scopedRecipeSnapshots.TryGetValue(instanceId, out ProductionRecipeSnapshot snapshot) ||
                forceRebuild ||
                now < snapshot.RefreshedAt ||
                now - snapshot.RefreshedAt >= RecipeSnapshotLifetimeSeconds)
            {
                snapshot = new ProductionRecipeSnapshot(
                    ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos(center),
                    now,
                    GetPlanningVersion());
                scopedRecipeSnapshots[instanceId] = snapshot;
                recipeCatalogVersion = ProductionOrderCenterCatalog.Version;
            }

            return snapshot;
        }

        public int GetPlanningVersion()
        {
            unchecked
            {
                int result = ProductionOrderCenterCatalog.Version;
                result = (result * 397) ^ StorageSceneRegistry.CapabilityVersion;
                result = (result * 397) ^ StorageSceneRegistry.ConnectivityVersion;
                result = (result * 397) ^ StorageNetworkContentIndexService.ChangeVersion;
                return (result * 397) ^ planningEpoch;
            }
        }

        public void InvalidateRecipeSnapshots()
        {
            unchecked
            {
                planningEpoch++;
            }

            globalRecipeSnapshot = ProductionRecipeSnapshot.Empty;
            scopedRecipeSnapshots.Clear();
            lastGlobalRecipeRefreshTime = float.NegativeInfinity;
            recipeCatalogVersion = -1;
        }

        public void Reset()
        {
            foreach (WorldInventorySnapshot snapshot in inventorySnapshots.Values)
            {
                snapshot.NetworkInventory.Clear();
                snapshot.ConnectedFabricatorOutputAmounts.Clear();
            }

            inventorySnapshots.Clear();
            ProductionNetworkInventoryCache.InvalidateSceneStorageIndex();
            globalRecipeSnapshot = ProductionRecipeSnapshot.Empty;
            scopedRecipeSnapshots.Clear();
            recipeCatalogVersion = -1;
            recipeCapabilityVersion = -1;
            recipeConnectivityVersion = -1;
            planningEpoch = 0;
            lastGlobalRecipeRefreshTime = float.NegativeInfinity;
        }

        private WorldInventorySnapshot EnsureInventorySnapshot(int worldId)
        {
            WorldInventorySnapshot snapshot = GetOrCreateInventorySnapshot(worldId);
            float now = Time.unscaledTime;
            if (!snapshot.HasSnapshot ||
                snapshot.ContentChangeVersion != StorageNetworkContentIndexService.ChangeVersion ||
                snapshot.MembershipVersion != StorageSceneRegistry.MembershipVersion ||
                snapshot.CapabilityVersion != StorageSceneRegistry.CapabilityVersion ||
                snapshot.ConnectivityVersion != StorageSceneRegistry.ConnectivityVersion ||
                now < snapshot.RefreshedAt ||
                now - snapshot.RefreshedAt >= InventorySnapshotLifetimeSeconds)
            {
                RefreshInventorySnapshot(snapshot, worldId, now);
            }

            return snapshot;
        }

        private WorldInventorySnapshot GetOrCreateInventorySnapshot(int worldId)
        {
            if (!inventorySnapshots.TryGetValue(worldId, out WorldInventorySnapshot snapshot))
            {
                snapshot = new WorldInventorySnapshot();
                inventorySnapshots.Add(worldId, snapshot);
            }

            return snapshot;
        }

        private static void RefreshInventorySnapshot(
            WorldInventorySnapshot snapshot,
            int worldId,
            float now)
        {
            StorageNetworkFabricatorProgress.BeginRefresh();
            snapshot.NetworkInventory.Refresh(worldId);
            RefreshConnectedFabricatorOutputAmounts(snapshot.ConnectedFabricatorOutputAmounts, worldId);
            snapshot.ContentChangeVersion = StorageNetworkContentIndexService.ChangeVersion;
            snapshot.MembershipVersion = StorageSceneRegistry.MembershipVersion;
            snapshot.CapabilityVersion = StorageSceneRegistry.CapabilityVersion;
            snapshot.ConnectivityVersion = StorageSceneRegistry.ConnectivityVersion;
            snapshot.RefreshedAt = now;
            snapshot.HasSnapshot = true;
        }

        private static void RefreshConnectedFabricatorOutputAmounts(
            Dictionary<Tag, float> amounts,
            int destinationWorldId)
        {
            amounts.Clear();
            if (destinationWorldId < 0 || !StorageSceneRegistry.HasOnlineCoreInWorld(destinationWorldId))
            {
                return;
            }

            IReadOnlyList<ComplexFabricator> fabricators =
                ProductionOrderCenterCatalog.GetFabricators();
            for (int fabricatorIndex = 0;
                 fabricatorIndex < fabricators.Count;
                 fabricatorIndex++)
            {
                ComplexFabricator fabricator = fabricators[fabricatorIndex];
                if (!IsReachableConnectedFabricator(fabricator, destinationWorldId) ||
                    fabricator.outStorage?.items == null)
                {
                    continue;
                }

                StorageNetworkEnrollment enrollment = fabricator.GetComponent<StorageNetworkEnrollment>();
                if (enrollment == null || !enrollment.IncludedInSceneNetwork)
                {
                    continue;
                }

                foreach (GameObject item in fabricator.outStorage.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement == null)
                    {
                        continue;
                    }

                    Tag storageTag = StorageItemUtility.GetStorageTransferTag(item);
                    AddConnectedFabricatorOutputAmount(amounts, storageTag, primaryElement.Mass);
                    Tag elementTag = primaryElement.ElementID.CreateTag();
                    if (elementTag != Tag.Invalid && elementTag != storageTag)
                    {
                        AddConnectedFabricatorOutputAmount(amounts, elementTag, primaryElement.Mass);
                    }
                }
            }
        }

        private static bool IsReachableConnectedFabricator(
            ComplexFabricator fabricator,
            int destinationWorldId)
        {
            if (!StorageSceneRegistry.IsLive(fabricator))
            {
                return false;
            }

            int fabricatorWorldId = StorageTargetSelector.GetObjectWorldId(fabricator.gameObject);
            if (fabricatorWorldId < 0 ||
                !StorageSceneRegistry.HasOnlineCoreInWorld(fabricatorWorldId) ||
                fabricatorWorldId != destinationWorldId && !StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return false;
            }

            StorageNetworkEnrollment enrollment = fabricator.GetComponent<StorageNetworkEnrollment>();
            return enrollment != null && enrollment.IncludedInSceneNetwork;
        }

        private static void AddConnectedFabricatorOutputAmount(
            Dictionary<Tag, float> amounts,
            Tag tag,
            float amount)
        {
            if (tag == Tag.Invalid || amount <= 0f)
            {
                return;
            }

            amounts[tag] =
                amounts.TryGetValue(tag, out float existing)
                    ? existing + amount
                    : amount;
        }

        private void EnsureRecipeVersion()
        {
            int currentVersion = ProductionOrderCenterCatalog.Version;
            int currentCapabilityVersion = StorageSceneRegistry.CapabilityVersion;
            int currentConnectivityVersion = StorageSceneRegistry.ConnectivityVersion;
            if (recipeCatalogVersion == currentVersion &&
                recipeCapabilityVersion == currentCapabilityVersion &&
                recipeConnectivityVersion == currentConnectivityVersion)
            {
                return;
            }

            recipeCatalogVersion = currentVersion;
            recipeCapabilityVersion = currentCapabilityVersion;
            recipeConnectivityVersion = currentConnectivityVersion;
            globalRecipeSnapshot = ProductionRecipeSnapshot.Empty;
            scopedRecipeSnapshots.Clear();
            lastGlobalRecipeRefreshTime = float.NegativeInfinity;
        }

        private sealed class WorldInventorySnapshot
        {
            public readonly ProductionNetworkInventoryCache NetworkInventory =
                new ProductionNetworkInventoryCache();
            public readonly Dictionary<Tag, float> ConnectedFabricatorOutputAmounts =
                new Dictionary<Tag, float>();

            public int ContentChangeVersion = -1;
            public int MembershipVersion = -1;
            public int CapabilityVersion = -1;
            public int ConnectivityVersion = -1;
            public float RefreshedAt = float.NegativeInfinity;
            public bool HasSnapshot;
        }

    }

    internal sealed class ProductionRecipeSnapshot
    {
        private static readonly RecipeDisplayInfo[] EmptyRoutes = new RecipeDisplayInfo[0];
        private readonly Dictionary<Tag, RecipeDisplayInfo[]> routesByProduct;

        public static readonly ProductionRecipeSnapshot Empty =
            new ProductionRecipeSnapshot(new List<RecipeDisplayInfo>(), float.NegativeInfinity, -1);

        public ProductionRecipeSnapshot(
            List<RecipeDisplayInfo> recipes,
            float refreshedAt,
            int version)
        {
            Recipes = (recipes ?? new List<RecipeDisplayInfo>()).AsReadOnly();
            RefreshedAt = refreshedAt;
            Version = version;
            routesByProduct = BuildRouteIndex(Recipes);
        }

        public IReadOnlyList<RecipeDisplayInfo> Recipes { get; }

        public float RefreshedAt { get; }

        public int Version { get; }

        public RecipeDisplayInfo[] GetRoutes(Tag productTag)
        {
            return routesByProduct.TryGetValue(productTag, out RecipeDisplayInfo[] routes)
                ? routes
                : EmptyRoutes;
        }

        private static Dictionary<Tag, RecipeDisplayInfo[]> BuildRouteIndex(
            IReadOnlyList<RecipeDisplayInfo> recipes)
        {
            Dictionary<Tag, List<RecipeDisplayInfo>> mutable =
                new Dictionary<Tag, List<RecipeDisplayInfo>>();
            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDisplayInfo route = recipes[i];
                ComplexRecipe.RecipeElement[] results = route.Recipe?.results;
                if (results == null || results.Length == 0)
                {
                    AddRoute(mutable, route.ProductTag, route);
                    continue;
                }

                for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
                {
                    ComplexRecipe.RecipeElement result = results[resultIndex];
                    if (result != null)
                    {
                        AddRoute(mutable, result.material, route);
                    }
                }
            }

            Dictionary<Tag, RecipeDisplayInfo[]> immutable =
                new Dictionary<Tag, RecipeDisplayInfo[]>(mutable.Count);
            foreach (KeyValuePair<Tag, List<RecipeDisplayInfo>> pair in mutable)
            {
                immutable.Add(pair.Key, pair.Value.ToArray());
            }

            return immutable;
        }

        private static void AddRoute(
            Dictionary<Tag, List<RecipeDisplayInfo>> index,
            Tag productTag,
            RecipeDisplayInfo route)
        {
            if (productTag == Tag.Invalid || route.Recipe == null)
            {
                return;
            }

            if (!index.TryGetValue(productTag, out List<RecipeDisplayInfo> routes))
            {
                routes = new List<RecipeDisplayInfo>();
                index.Add(productTag, routes);
            }

            for (int i = 0; i < routes.Count; i++)
            {
                if (routes[i].Recipe == route.Recipe &&
                    ReferenceEquals(routes[i].Fabricators, route.Fabricators))
                {
                    return;
                }
            }

            routes.Add(route);
        }
    }
}
