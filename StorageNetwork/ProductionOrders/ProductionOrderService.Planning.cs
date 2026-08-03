using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        private const int MaxPlannerNodeExpansions = 1024;
        private const int MaxPlannerCandidateEvaluations = 4096;
        private const double PlannerTickBudgetMilliseconds = 0.5d;
        private readonly PlannerContext plannerContext = new PlannerContext();
        private readonly Stack<PlannerContext> plannerContextPool = new Stack<PlannerContext>();
        private readonly Dictionary<string, DeferredProductionPlan> deferredProductionPlans =
            new Dictionary<string, DeferredProductionPlan>();
        private readonly List<string> completedDeferredPlanKeys = new List<string>();
        private readonly List<DeferredProductionPlan> deferredPlanWorkspace =
            new List<DeferredProductionPlan>();
        private readonly PlannerTickBudget maintenancePlannerBudget =
            new PlannerTickBudget();

        public ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount)
        {
            return BuildProductionPlanForWorld(
                recipe,
                fabricators,
                productTag,
                requestedAmount,
                preferredWorldId: -1);
        }

        private ProductionPlanNode BuildProductionPlanForWorld(
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            Tag productTag,
            float requestedAmount,
            int preferredWorldId)
        {
            using (StorageNetworkFrameProfileTool.BeginWork(
                       StorageNetworkPerformanceArea.ProductionPlanning))
            {
                int planWorldId = SetNetworkWorldForPlan(fabricators, preferredWorldId);
                return BuildProductionPlanSynchronous(
                    recipe,
                    fabricators,
                    productTag,
                    requestedAmount,
                    planWorldId,
                    plannerContext);
            }
        }

        private ProductionPlanNode BuildProductionPlanSynchronous(
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            Tag productTag,
            float requestedAmount,
            int worldId,
            PlannerContext context)
        {
            context.Begin(
                worldId,
                enforceTickBudget: false,
                MaxPlannerNodeExpansions,
                MaxPlannerCandidateEvaluations,
                PlannerTickBudgetMilliseconds);
            ProductionPlanNode optimized = BuildProductionPlan(
                recipe,
                fabricators,
                productTag,
                requestedAmount,
                0,
                context);
            return ValidateProductionPlanShadow(
                optimized,
                recipe,
                fabricators,
                productTag,
                requestedAmount,
                worldId);
        }

        private bool TryBuildProductionPlanForMaintenance(
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            Tag productTag,
            float requestedAmount,
            int worldId,
            string orderKey,
            out ProductionPlanNode plan)
        {
            plan = null;
            if (string.IsNullOrEmpty(orderKey) || deferredProductionPlans.ContainsKey(orderKey))
            {
                return false;
            }

            PlannerContext context = RentPlannerContext();
            context.Begin(
                worldId,
                enforceTickBudget: true,
                MaxPlannerNodeExpansions,
                MaxPlannerCandidateEvaluations,
                PlannerTickBudgetMilliseconds,
                sharedBudget: maintenancePlannerBudget.IsActive
                    ? maintenancePlannerBudget
                    : null);
            try
            {
                using (StorageNetworkFrameProfileTool.BeginWork(
                           StorageNetworkPerformanceArea.ProductionPlanning))
                {
                    plan = BuildProductionPlan(
                        recipe,
                        fabricators,
                        productTag,
                        requestedAmount,
                        0,
                        context);
                    plan = ValidateProductionPlanShadow(
                        plan,
                        recipe,
                        fabricators,
                        productTag,
                        requestedAmount,
                        worldId);
                }
                ReturnPlannerContext(context);
                return true;
            }
            catch (PlannerBudgetExceededException)
            {
                deferredProductionPlans[orderKey] = new DeferredProductionPlan(
                    orderKey,
                    context);
                return false;
            }
        }

        private void BeginMaintenancePlanningTick()
        {
            maintenancePlannerBudget.Begin(
                MaxPlannerNodeExpansions,
                MaxPlannerCandidateEvaluations,
                PlannerTickBudgetMilliseconds);
        }

        private void EndMaintenancePlanningTick()
        {
            maintenancePlannerBudget.End();
        }

        internal void ContinueDeferredProductionPlans()
        {
            if (deferredProductionPlans.Count == 0)
            {
                return;
            }

            completedDeferredPlanKeys.Clear();
            deferredPlanWorkspace.Clear();
            foreach (DeferredProductionPlan deferred in deferredProductionPlans.Values)
            {
                deferredPlanWorkspace.Add(deferred);
            }

            deferredPlanWorkspace.Sort(DeferredProductionPlanComparer.Instance);
            ProductionOrderRuntimeAllocation.BeginMaintenanceSnapshot();
            try
            {
                for (int deferredIndex = 0;
                     deferredIndex < deferredPlanWorkspace.Count;
                     deferredIndex++)
                {
                    DeferredProductionPlan deferred =
                        deferredPlanWorkspace[deferredIndex];
                    ActiveOrders.TryGetValue(deferred.OrderKey, out ProductionOrderRecord order);
                    if (!IsOrderActive(order))
                    {
                        completedDeferredPlanKeys.Add(deferred.OrderKey);
                        ReturnPlannerContext(deferred.Context);
                        continue;
                    }

                    // Deferred work is a scheduling token, not a captured plan.
                    // Re-resolve every mutable input on the next Sim tick so world,
                    // relay, recipe and fabricator changes cannot revive stale data.
                    networkWorldId = GetOrderNetworkWorldId(order);
                    Runtime.GetNetworkInventory(networkWorldId);
                    SetRecipeSnapshot(Runtime.GetRecipeSnapshot(null, false));
                    RecipeDisplayInfo route = FindRouteForOrder(order);
                    float requestedAmount = Mathf.Max(
                        0f,
                        order.RequestedAmount - order.ProducedAtSubmit);
                    if (route.Recipe == null ||
                        route.Fabricators.Count == 0 ||
                        requestedAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        completedDeferredPlanKeys.Add(deferred.OrderKey);
                        ReturnPlannerContext(deferred.Context);
                        continue;
                    }

                    deferred.Context.Begin(
                        networkWorldId,
                        enforceTickBudget: false,
                        MaxPlannerNodeExpansions,
                        MaxPlannerCandidateEvaluations,
                        PlannerTickBudgetMilliseconds);
                    string previousIgnoredReservationOrderKey = ignoredReservationOrderKey;
                    ignoredReservationOrderKey = deferred.OrderKey;
                    try
                    {
                        using (StorageNetworkFrameProfileTool.BeginWork(
                                   StorageNetworkPerformanceArea.ProductionPlanning))
                        {
                            ProductionPlanNode plan = BuildProductionPlan(
                                route.Recipe,
                                route.Fabricators,
                                order.ProductTag,
                                requestedAmount,
                                0,
                                deferred.Context);
                            plan = ValidateProductionPlanShadow(
                                plan,
                                route.Recipe,
                                route.Fabricators,
                                order.ProductTag,
                                requestedAmount,
                                networkWorldId);
                            ApplyMaintainedProductionPlan(order, plan);
                        }
                    }
                    finally
                    {
                        ignoredReservationOrderKey = previousIgnoredReservationOrderKey;
                        completedDeferredPlanKeys.Add(deferred.OrderKey);
                        ReturnPlannerContext(deferred.Context);
                    }
                }
            }
            finally
            {
                for (int i = 0; i < completedDeferredPlanKeys.Count; i++)
                {
                    deferredProductionPlans.Remove(completedDeferredPlanKeys[i]);
                }

                deferredPlanWorkspace.Clear();
                ProductionOrderRuntimeAllocation.EndMaintenanceSnapshot();
            }
        }

        private PlannerContext RentPlannerContext()
        {
            return plannerContextPool.Count > 0
                ? plannerContextPool.Pop()
                : new PlannerContext();
        }

        private void ReturnPlannerContext(PlannerContext context)
        {
            if (context != null)
            {
                plannerContextPool.Push(context);
            }
        }

        private ProductionPlanNode BuildProductionPlan(
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            Tag productTag,
            float requestedAmount,
            int depth,
            PlannerContext context)
        {
            if (recipe == null || context == null)
            {
                return null;
            }
            context.EnterNode();

            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag) ?? ProductionRecipeCatalog.GetPrimaryResult(recipe);
            float outputAmount = result != null ? Mathf.Max(result.amount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) : 1f;
            int orderCount = Mathf.Max(1, Mathf.CeilToInt(requestedAmount / outputAmount));
            ProductionPlanNode node = new ProductionPlanNode(
                recipe,
                fabricators,
                productTag,
                outputAmount,
                orderCount,
                GetCurrentNetworkWorldId());
            AssignPlan(node, context);
            int pathMark = context.PushPath(recipe, productTag);
            if (recipe.ingredients == null || depth >= Config.Instance.ProductionPlanMaxDepth)
            {
                context.PopPath(pathMark);
                return node;
            }

            int reservedMark = context.PushReservedFabricators(node.Assignments);
            try
            {
                for (int ingredientIndex = 0; ingredientIndex < recipe.ingredients.Length; ingredientIndex++)
                {
                    ComplexRecipe.RecipeElement ingredient = recipe.ingredients[ingredientIndex];
                    Tag tag = GetPreferredMaterial(
                        ingredient,
                        orderCount,
                        depth,
                        context);
                    float required = ingredient.amount * orderCount;
                    float available = context.GetAvailableAmount(this, tag);
                    ProductionPlanRequirement requirement = new ProductionPlanRequirement(tag, required, available);
                    if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required)
                    {
                        requirement.Child = BuildBestChildPlan(
                            tag,
                            required - available,
                            depth + 1,
                            context);
                    }

                    node.Requirements.Add(requirement);
                }
            }
            finally
            {
                context.PopReservedFabricators(reservedMark);
                context.PopPath(pathMark);
            }

            return node;
        }

        private static void AssignPlan(ProductionPlanNode node, PlannerContext context)
        {
            if (node == null || context == null)
            {
                return;
            }

            node.Assignments.Clear();
            BuildAssignmentsForFabricators(
                node.Recipe,
                node.Fabricators,
                node.OutputAmount,
                node.OrderCount,
                node.WorldId,
                excludeReserved: true,
                context,
                node.Assignments);
            if (node.Assignments.Count == 0)
            {
                BuildAssignmentsForFabricators(
                    node.Recipe,
                    node.Fabricators,
                    node.OutputAmount,
                    node.OrderCount,
                    node.WorldId,
                    excludeReserved: false,
                    context,
                    node.Assignments);
            }
        }

        private static void BuildAssignmentsForFabricators(
            ComplexRecipe recipe,
            List<ComplexFabricator> fabricators,
            float outputAmount,
            int orderCount,
            int worldId,
            bool excludeReserved,
            PlannerContext context,
            List<ProductionPlanAssignment> assignments)
        {
            if (fabricators == null || fabricators.Count == 0 || orderCount <= 0 ||
                context == null || assignments == null)
            {
                return;
            }

            List<FabricatorQueueSortKey> sortKeys = context.FabricatorSortBuffer;
            sortKeys.Clear();
            for (int fabricatorIndex = 0; fabricatorIndex < fabricators.Count; fabricatorIndex++)
            {
                ComplexFabricator fabricator = fabricators[fabricatorIndex];
                if (!IsFabricatorReachableFromWorld(fabricator, worldId) ||
                    excludeReserved && context.IsFabricatorReserved(fabricator))
                {
                    continue;
                }

                sortKeys.Add(context.GetFabricatorSortKey(fabricator, recipe));
            }

            sortKeys.Sort(FabricatorQueueSortKeyComparer.Instance);
            if (sortKeys.Count == 0)
            {
                return;
            }

            int baseCount = orderCount / sortKeys.Count;
            int remainder = orderCount % sortKeys.Count;
            for (int i = 0; i < sortKeys.Count; i++)
            {
                int count = baseCount + (i < remainder ? 1 : 0);
                if (count > 0)
                {
                    assignments.Add(new ProductionPlanAssignment(
                        sortKeys[i].Fabricator,
                        count,
                        outputAmount * count));
                }
            }
        }

        private Tag GetPreferredMaterial(
            ComplexRecipe.RecipeElement element,
            int orderCount,
            int depth,
            PlannerContext context)
        {
            if (element.material != Tag.Invalid)
            {
                return element.material;
            }

            if (element.possibleMaterials == null || element.possibleMaterials.Length == 0)
            {
                return Tag.Invalid;
            }

            float required = element.amount * orderCount;
            bool hasBest = false;
            Tag bestTag = Tag.Invalid;
            float bestAvailable = 0f;
            int bestBlocked = 0;
            int bestMissingChild = 0;
            float bestMissingAmount = 0f;
            string bestName = string.Empty;

            foreach (Tag tag in element.possibleMaterials)
            {
                context.ObserveCandidate();

                float available = context.GetAvailableAmount(this, tag);
                ProductionPlanNode child = available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required
                    ? BuildBestChildPlan(
                        tag,
                        required - available,
                        depth + 1,
                        context)
                    : null;
                int blocked = CountBlockedRequirements(child);
                int missingChild = child == null && available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required ? 1 : 0;
                float missingAmount = EstimateMissingAmount(child);
                string name = ProductionOrderFormatting.GetTagDisplayName(tag);

                bool better = !hasBest ||
                    blocked < bestBlocked ||
                    (blocked == bestBlocked && missingChild < bestMissingChild) ||
                    (blocked == bestBlocked && missingChild == bestMissingChild && missingAmount < bestMissingAmount) ||
                    (blocked == bestBlocked && missingChild == bestMissingChild && Mathf.Approximately(missingAmount, bestMissingAmount) && available > bestAvailable) ||
                    (blocked == bestBlocked && missingChild == bestMissingChild && Mathf.Approximately(missingAmount, bestMissingAmount) && Mathf.Approximately(available, bestAvailable) && string.Compare(name, bestName, System.StringComparison.Ordinal) < 0);
                if (!better)
                {
                    continue;
                }

                hasBest = true;
                bestTag = tag;
                bestAvailable = available;
                bestBlocked = blocked;
                bestMissingChild = missingChild;
                bestMissingAmount = missingAmount;
                bestName = name;
            }

            return hasBest ? bestTag : element.possibleMaterials[0];
        }

        private ProductionPlanNode BuildBestChildPlan(
            Tag productTag,
            float missingAmount,
            int depth,
            PlannerContext context)
        {
            if (productTag == Tag.Invalid ||
                missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                depth > Config.Instance.ProductionPlanMaxDepth ||
                context == null)
            {
                return null;
            }

            BestPlanMemoKey bestMemoKey = context.CreateBestPlanMemoKey(
                productTag,
                missingAmount,
                depth);
            if (context.TryGetBestPlan(bestMemoKey, out ProductionPlanNode memoizedBest))
            {
                return memoizedBest;
            }

            ProductionPlanNode bestPlan = null;
            int bestBlocked = 0;
            float bestMissing = 0f;
            int bestProduced = 0;
            float bestQueueLoad = 0f;
            string bestRecipeName = null;
            RecipeDisplayInfo[] routes = GetCraftableRoutesProducing(productTag);
            int routeCount = context.LegacyMode ? craftableRecipes.Count : routes.Length;
            for (int i = 0; i < routeCount; i++)
            {
                context.ObserveCandidate();

                RecipeDisplayInfo route = context.LegacyMode
                    ? craftableRecipes[i]
                    : routes[i];
                if (context.LegacyMode &&
                    route.ProductTag != productTag &&
                    ProductionRecipeCatalog.GetRecipeResultForProduct(
                        route.Recipe,
                        productTag) == null)
                {
                    continue;
                }

                if (route.Recipe == null ||
                    route.Fabricators.Count == 0 ||
                    !IsRouteReachableFromWorld(route, GetCurrentNetworkWorldId()) ||
                    context.IsRecipeInPath(route.Recipe, productTag))
                {
                    continue;
                }

                PlanMemoKey planMemoKey = context.CreatePlanMemoKey(
                    route.Recipe,
                    productTag,
                    missingAmount,
                    depth);
                if (!context.TryGetPlan(planMemoKey, out ProductionPlanNode candidate))
                {
                    candidate = BuildProductionPlan(
                        route.Recipe,
                        route.Fabricators,
                        productTag,
                        missingAmount,
                        depth,
                        context);
                    context.StorePlan(planMemoKey, candidate);
                }
                if (candidate == null || candidate.Assignments.Count == 0)
                {
                    continue;
                }

                int blocked = CountBlockedRequirements(candidate);
                float missing = EstimateMissingAmount(candidate);
                int produced = CountProducedRequirements(candidate);
                float queueLoad = EstimateQueueLoad(candidate);
                string recipeName = candidate.Recipe.GetUIName(false);
                if (bestPlan != null &&
                    !IsBetterPlanCandidate(
                        blocked,
                        missing,
                        produced,
                        queueLoad,
                        recipeName,
                        bestBlocked,
                        bestMissing,
                        bestProduced,
                        bestQueueLoad,
                        bestRecipeName))
                {
                    continue;
                }

                bestPlan = candidate;
                bestBlocked = blocked;
                bestMissing = missing;
                bestProduced = produced;
                bestQueueLoad = queueLoad;
                bestRecipeName = recipeName;
            }

            context.StoreBestPlan(bestMemoKey, bestPlan);
            return bestPlan;
        }

        private static bool IsBetterPlanCandidate(
            int blocked,
            float missing,
            int produced,
            float queueLoad,
            string recipeName,
            int bestBlocked,
            float bestMissing,
            int bestProduced,
            float bestQueueLoad,
            string bestRecipeName)
        {
            int compare = blocked.CompareTo(bestBlocked);
            if (compare != 0)
            {
                return compare < 0;
            }

            compare = missing.CompareTo(bestMissing);
            if (compare != 0)
            {
                return compare < 0;
            }

            compare = produced.CompareTo(bestProduced);
            if (compare != 0)
            {
                return compare < 0;
            }

            compare = queueLoad.CompareTo(bestQueueLoad);
            if (compare != 0)
            {
                return compare < 0;
            }

            return string.Compare(
                recipeName,
                bestRecipeName,
                System.StringComparison.CurrentCulture) < 0;
        }

        private static int GetFiniteRecipeQueueCount(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            return StorageNetworkFabricatorProgress.GetFiniteRecipeQueueCountSafe(fabricator, recipe);
        }

        private readonly struct FabricatorQueueSortKey
        {
            public readonly ComplexFabricator Fabricator;
            public readonly int TotalQueueCount;
            public readonly int RecipeQueueCount;
            public readonly string Name;
            public readonly int InstanceId;

            public FabricatorQueueSortKey(
                ComplexFabricator fabricator,
                int totalQueueCount,
                int recipeQueueCount,
                string name,
                int instanceId)
            {
                Fabricator = fabricator;
                TotalQueueCount = totalQueueCount;
                RecipeQueueCount = recipeQueueCount;
                Name = name ?? string.Empty;
                InstanceId = instanceId;
            }
        }

        private sealed class FabricatorQueueSortKeyComparer : IComparer<FabricatorQueueSortKey>
        {
            public static readonly FabricatorQueueSortKeyComparer Instance =
                new FabricatorQueueSortKeyComparer();

            public int Compare(FabricatorQueueSortKey left, FabricatorQueueSortKey right)
            {
                int compare = left.TotalQueueCount.CompareTo(right.TotalQueueCount);
                if (compare != 0)
                {
                    return compare;
                }

                compare = left.RecipeQueueCount.CompareTo(right.RecipeQueueCount);
                if (compare != 0)
                {
                    return compare;
                }

                compare = string.Compare(left.Name, right.Name, StringComparison.Ordinal);
                return compare != 0 ? compare : left.InstanceId.CompareTo(right.InstanceId);
            }
        }

        private sealed class PlannerContext
        {
            private readonly Dictionary<Tag, float> availableAmounts =
                new Dictionary<Tag, float>();
            private readonly Dictionary<BestPlanMemoKey, ProductionPlanNode> bestPlanMemo =
                new Dictionary<BestPlanMemoKey, ProductionPlanNode>();
            private readonly Dictionary<PlanMemoKey, ProductionPlanNode> planMemo =
                new Dictionary<PlanMemoKey, ProductionPlanNode>();
            private readonly HashSet<PlanPathKey> recipePath =
                new HashSet<PlanPathKey>();
            private readonly List<PathFrame> pathFrames = new List<PathFrame>();
            private readonly HashSet<ComplexFabricator> reservedFabricators =
                new HashSet<ComplexFabricator>();
            private readonly List<ComplexFabricator> reservedAdditions =
                new List<ComplexFabricator>();
            private readonly List<int> reservedTokenStack = new List<int>();
            private readonly Dictionary<ComplexFabricator, int> totalQueueCounts =
                new Dictionary<ComplexFabricator, int>();
            private readonly Dictionary<ComplexFabricator, string> fabricatorNames =
                new Dictionary<ComplexFabricator, string>();
            private readonly Dictionary<FabricatorRecipeKey, int> recipeQueueCounts =
                new Dictionary<FabricatorRecipeKey, int>();
            private int worldId;
            private int maxNodes;
            private int maxCandidates;
            private int nodes;
            private int candidates;
            private int pathToken;
            private int reservedToken;
            private int nextStateToken;
            private bool enforceTickBudget;
            private bool legacyMode;
            private long deadlineTimestamp;
            private PlannerTickBudget sharedBudget;

            public List<FabricatorQueueSortKey> FabricatorSortBuffer { get; } =
                new List<FabricatorQueueSortKey>();

            public bool LegacyMode => legacyMode;

            public void Begin(
                int planningWorldId,
                bool enforceTickBudget,
                int nodeLimit,
                int candidateLimit,
                double milliseconds,
                bool useLegacyRouteScan = false,
                PlannerTickBudget sharedBudget = null)
            {
                worldId = planningWorldId;
                this.enforceTickBudget = enforceTickBudget;
                this.sharedBudget = enforceTickBudget ? sharedBudget : null;
                legacyMode = useLegacyRouteScan;
                maxNodes = Mathf.Max(1, nodeLimit);
                maxCandidates = Mathf.Max(1, candidateLimit);
                nodes = 0;
                candidates = 0;
                pathToken = 0;
                reservedToken = 0;
                nextStateToken = 0;
                availableAmounts.Clear();
                bestPlanMemo.Clear();
                planMemo.Clear();
                recipePath.Clear();
                pathFrames.Clear();
                reservedFabricators.Clear();
                reservedAdditions.Clear();
                reservedTokenStack.Clear();
                totalQueueCounts.Clear();
                fabricatorNames.Clear();
                recipeQueueCounts.Clear();
                FabricatorSortBuffer.Clear();
                long budgetTicks = (long)Math.Max(
                    1d,
                    milliseconds * Stopwatch.Frequency / 1000d);
                deadlineTimestamp = Stopwatch.GetTimestamp() + budgetTicks;
            }

            public void ResumeWithoutBudget()
            {
                enforceTickBudget = false;
                sharedBudget = null;
                nodes = 0;
                candidates = 0;
                pathToken = 0;
                reservedToken = 0;
                nextStateToken = 0;
                recipePath.Clear();
                pathFrames.Clear();
                reservedFabricators.Clear();
                reservedAdditions.Clear();
                reservedTokenStack.Clear();
                FabricatorSortBuffer.Clear();
            }

            public void EnterNode()
            {
                nodes++;
                sharedBudget?.ObserveNode();
                CheckBudget();
            }

            public void ObserveCandidate()
            {
                candidates++;
                sharedBudget?.ObserveCandidate();
                CheckBudget();
            }

            public float GetAvailableAmount(ProductionOrderService service, Tag tag)
            {
                if (tag == Tag.Invalid)
                {
                    return 0f;
                }

                if (!availableAmounts.TryGetValue(tag, out float available))
                {
                    available = service.GetNetworkAvailableAmount(tag);
                    availableAmounts.Add(tag, available);
                }

                return available;
            }

            public int PushPath(ComplexRecipe recipe, Tag productTag)
            {
                int mark = pathFrames.Count;
                PlanPathKey key = new PlanPathKey(recipe, productTag);
                bool added = recipePath.Add(key);
                pathFrames.Add(new PathFrame(key, added, pathToken));
                if (added)
                {
                    pathToken = ++nextStateToken;
                }

                return mark;
            }

            public void PopPath(int mark)
            {
                while (pathFrames.Count > mark)
                {
                    int index = pathFrames.Count - 1;
                    PathFrame frame = pathFrames[index];
                    pathFrames.RemoveAt(index);
                    if (frame.Added)
                    {
                        recipePath.Remove(frame.Key);
                    }

                    pathToken = frame.PreviousToken;
                }
            }

            public bool IsRecipeInPath(ComplexRecipe recipe, Tag productTag)
            {
                return recipePath.Contains(new PlanPathKey(recipe, productTag));
            }

            public int PushReservedFabricators(List<ProductionPlanAssignment> assignments)
            {
                int mark = reservedAdditions.Count;
                reservedTokenStack.Add(reservedToken);
                if (assignments != null)
                {
                    for (int i = 0; i < assignments.Count; i++)
                    {
                        ComplexFabricator fabricator = assignments[i]?.Fabricator;
                        if (fabricator != null && reservedFabricators.Add(fabricator))
                        {
                            reservedAdditions.Add(fabricator);
                        }
                    }
                }

                if (reservedAdditions.Count != mark)
                {
                    reservedToken = ++nextStateToken;
                }

                return mark;
            }

            public void PopReservedFabricators(int mark)
            {
                for (int i = reservedAdditions.Count - 1; i >= mark; i--)
                {
                    reservedFabricators.Remove(reservedAdditions[i]);
                    reservedAdditions.RemoveAt(i);
                }

                int tokenIndex = reservedTokenStack.Count - 1;
                if (tokenIndex >= 0)
                {
                    reservedToken = reservedTokenStack[tokenIndex];
                    reservedTokenStack.RemoveAt(tokenIndex);
                }
            }

            public bool IsFabricatorReserved(ComplexFabricator fabricator)
            {
                return reservedFabricators.Contains(fabricator);
            }

            public FabricatorQueueSortKey GetFabricatorSortKey(
                ComplexFabricator fabricator,
                ComplexRecipe recipe)
            {
                if (!totalQueueCounts.TryGetValue(fabricator, out int totalQueueCount))
                {
                    totalQueueCount = StorageNetworkFabricatorProgress
                        .GetFiniteTotalQueueCountSafe(fabricator);
                    totalQueueCounts.Add(fabricator, totalQueueCount);
                }

                FabricatorRecipeKey recipeKey = new FabricatorRecipeKey(fabricator, recipe);
                if (!recipeQueueCounts.TryGetValue(recipeKey, out int recipeQueueCount))
                {
                    recipeQueueCount = StorageNetworkFabricatorProgress
                        .GetFiniteRecipeQueueCountSafe(fabricator, recipe);
                    recipeQueueCounts.Add(recipeKey, recipeQueueCount);
                }

                if (!fabricatorNames.TryGetValue(fabricator, out string name))
                {
                    name = fabricator.gameObject.GetProperName() ?? string.Empty;
                    fabricatorNames.Add(fabricator, name);
                }

                return new FabricatorQueueSortKey(
                    fabricator,
                    totalQueueCount,
                    recipeQueueCount,
                    name,
                    ProductionOrderCenterCatalog.GetInstanceId(fabricator));
            }

            public BestPlanMemoKey CreateBestPlanMemoKey(
                Tag productTag,
                float missingAmount,
                int depth)
            {
                return new BestPlanMemoKey(
                    productTag,
                    missingAmount,
                    depth,
                    worldId,
                    pathToken,
                    reservedToken);
            }

            public PlanMemoKey CreatePlanMemoKey(
                ComplexRecipe recipe,
                Tag productTag,
                float requestedAmount,
                int depth)
            {
                return new PlanMemoKey(
                    recipe,
                    productTag,
                    requestedAmount,
                    depth,
                    worldId,
                    pathToken,
                    reservedToken);
            }

            public bool TryGetBestPlan(
                BestPlanMemoKey key,
                out ProductionPlanNode plan)
            {
                if (legacyMode)
                {
                    plan = null;
                    return false;
                }

                return bestPlanMemo.TryGetValue(key, out plan);
            }

            public void StoreBestPlan(BestPlanMemoKey key, ProductionPlanNode plan)
            {
                if (!legacyMode)
                {
                    bestPlanMemo[key] = plan;
                }
            }

            public bool TryGetPlan(PlanMemoKey key, out ProductionPlanNode plan)
            {
                if (legacyMode)
                {
                    plan = null;
                    return false;
                }

                return planMemo.TryGetValue(key, out plan);
            }

            public void StorePlan(PlanMemoKey key, ProductionPlanNode plan)
            {
                if (!legacyMode)
                {
                    planMemo[key] = plan;
                }
            }

            private void CheckBudget()
            {
                if (enforceTickBudget &&
                    (nodes > maxNodes ||
                     candidates > maxCandidates ||
                     (sharedBudget != null
                          ? sharedBudget.IsExceeded
                          : Stopwatch.GetTimestamp() > deadlineTimestamp)))
                {
                    throw PlannerBudgetExceededException.Instance;
                }
            }
        }

        private sealed class PlannerTickBudget
        {
            private int maxNodes;
            private int maxCandidates;
            private int nodes;
            private int candidates;
            private long deadlineTimestamp;

            public bool IsActive { get; private set; }

            public bool IsExceeded =>
                !IsActive ||
                nodes > maxNodes ||
                candidates > maxCandidates ||
                Stopwatch.GetTimestamp() > deadlineTimestamp;

            public void Begin(
                int nodeLimit,
                int candidateLimit,
                double milliseconds)
            {
                maxNodes = Mathf.Max(1, nodeLimit);
                maxCandidates = Mathf.Max(1, candidateLimit);
                nodes = 0;
                candidates = 0;
                long budgetTicks = (long)Math.Max(
                    1d,
                    milliseconds * Stopwatch.Frequency / 1000d);
                deadlineTimestamp = Stopwatch.GetTimestamp() + budgetTicks;
                IsActive = true;
            }

            public void End()
            {
                IsActive = false;
            }

            public void ObserveNode()
            {
                nodes++;
            }

            public void ObserveCandidate()
            {
                candidates++;
            }
        }

        private sealed class PlannerBudgetExceededException : Exception
        {
            public static readonly PlannerBudgetExceededException Instance =
                new PlannerBudgetExceededException();

            private PlannerBudgetExceededException()
            {
            }
        }

        private sealed class DeferredProductionPlan
        {
            public DeferredProductionPlan(
                string orderKey,
                PlannerContext context)
            {
                OrderKey = orderKey;
                Context = context;
            }

            public string OrderKey { get; }

            public PlannerContext Context { get; }
        }

        private sealed class DeferredProductionPlanComparer :
            IComparer<DeferredProductionPlan>
        {
            public static readonly DeferredProductionPlanComparer Instance =
                new DeferredProductionPlanComparer();

            public int Compare(
                DeferredProductionPlan left,
                DeferredProductionPlan right)
            {
                if (ReferenceEquals(left, right))
                {
                    return 0;
                }

                if (left == null)
                {
                    return 1;
                }

                if (right == null)
                {
                    return -1;
                }

                ActiveOrders.TryGetValue(left.OrderKey, out ProductionOrderRecord leftOrder);
                ActiveOrders.TryGetValue(right.OrderKey, out ProductionOrderRecord rightOrder);
                if (leftOrder != null && rightOrder != null)
                {
                    int compare = leftOrder.CreatedCycle.CompareTo(rightOrder.CreatedCycle);
                    if (compare != 0)
                    {
                        return compare;
                    }

                    compare = leftOrder.DisplayId.CompareTo(rightOrder.DisplayId);
                    if (compare != 0)
                    {
                        return compare;
                    }
                }
                else if (leftOrder != null)
                {
                    return -1;
                }
                else if (rightOrder != null)
                {
                    return 1;
                }

                return string.Compare(
                    left.OrderKey,
                    right.OrderKey,
                    StringComparison.Ordinal);
            }
        }

        private readonly struct PathFrame
        {
            public PathFrame(PlanPathKey key, bool added, int previousToken)
            {
                Key = key;
                Added = added;
                PreviousToken = previousToken;
            }

            public PlanPathKey Key { get; }

            public bool Added { get; }

            public int PreviousToken { get; }
        }

        private readonly struct PlanPathKey : IEquatable<PlanPathKey>
        {
            private readonly ComplexRecipe recipe;
            private readonly Tag productTag;

            public PlanPathKey(ComplexRecipe recipe, Tag productTag)
            {
                this.recipe = recipe;
                this.productTag = productTag;
            }

            public bool Equals(PlanPathKey other)
            {
                return ReferenceEquals(recipe, other.recipe) && productTag == other.productTag;
            }

            public override bool Equals(object obj)
            {
                return obj is PlanPathKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (RuntimeHelpers.GetHashCode(recipe) * 397) ^ productTag.GetHashCode();
            }
        }

        private readonly struct FabricatorRecipeKey : IEquatable<FabricatorRecipeKey>
        {
            private readonly ComplexFabricator fabricator;
            private readonly ComplexRecipe recipe;

            public FabricatorRecipeKey(ComplexFabricator fabricator, ComplexRecipe recipe)
            {
                this.fabricator = fabricator;
                this.recipe = recipe;
            }

            public bool Equals(FabricatorRecipeKey other)
            {
                return ReferenceEquals(fabricator, other.fabricator) &&
                       ReferenceEquals(recipe, other.recipe);
            }

            public override bool Equals(object obj)
            {
                return obj is FabricatorRecipeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (RuntimeHelpers.GetHashCode(fabricator) * 397) ^
                       RuntimeHelpers.GetHashCode(recipe);
            }
        }

        private readonly struct BestPlanMemoKey : IEquatable<BestPlanMemoKey>
        {
            private readonly Tag productTag;
            private readonly float missingAmount;
            private readonly int depth;
            private readonly int worldId;
            private readonly int pathToken;
            private readonly int reservedToken;

            public BestPlanMemoKey(
                Tag productTag,
                float missingAmount,
                int depth,
                int worldId,
                int pathToken,
                int reservedToken)
            {
                this.productTag = productTag;
                this.missingAmount = missingAmount;
                this.depth = depth;
                this.worldId = worldId;
                this.pathToken = pathToken;
                this.reservedToken = reservedToken;
            }

            public bool Equals(BestPlanMemoKey other)
            {
                return productTag == other.productTag &&
                       missingAmount.Equals(other.missingAmount) &&
                       depth == other.depth &&
                       worldId == other.worldId &&
                       pathToken == other.pathToken &&
                       reservedToken == other.reservedToken;
            }

            public override bool Equals(object obj)
            {
                return obj is BestPlanMemoKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = productTag.GetHashCode();
                    hashCode = (hashCode * 397) ^ missingAmount.GetHashCode();
                    hashCode = (hashCode * 397) ^ depth;
                    hashCode = (hashCode * 397) ^ worldId;
                    hashCode = (hashCode * 397) ^ pathToken;
                    return (hashCode * 397) ^ reservedToken;
                }
            }
        }

        private readonly struct PlanMemoKey : IEquatable<PlanMemoKey>
        {
            private readonly ComplexRecipe recipe;
            private readonly BestPlanMemoKey planKey;

            public PlanMemoKey(
                ComplexRecipe recipe,
                Tag productTag,
                float requestedAmount,
                int depth,
                int worldId,
                int pathToken,
                int reservedToken)
            {
                this.recipe = recipe;
                planKey = new BestPlanMemoKey(
                    productTag,
                    requestedAmount,
                    depth,
                    worldId,
                    pathToken,
                    reservedToken);
            }

            public bool Equals(PlanMemoKey other)
            {
                return ReferenceEquals(recipe, other.recipe) && planKey.Equals(other.planKey);
            }

            public override bool Equals(object obj)
            {
                return obj is PlanMemoKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (RuntimeHelpers.GetHashCode(recipe) * 397) ^ planKey.GetHashCode();
            }
        }
    }
}
