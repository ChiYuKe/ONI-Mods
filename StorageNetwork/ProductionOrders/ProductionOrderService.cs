using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderService
    {
        private static readonly Dictionary<string, ProductionOrderRecord> ActiveOrders = new Dictionary<string, ProductionOrderRecord>();
        private static readonly Dictionary<int, OrderAutomationLease> AutomationLeases = new Dictionary<int, OrderAutomationLease>();
        private static readonly Dictionary<Tag, ProductionKeepRule> KeepRules = new Dictionary<Tag, ProductionKeepRule>();
        private static readonly FieldInfo RecipeQueueCountsField = typeof(ComplexFabricator).GetField("recipeQueueCounts", BindingFlags.Instance | BindingFlags.NonPublic);
        private static string loadedStorePath;

        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private Dictionary<Tag, float> networkAmountCache = new Dictionary<Tag, float>();
        private List<Storage> networkSourceStorageCache = new List<Storage>();
        private readonly HashSet<Storage> networkSourceStorageSet = new HashSet<Storage>();
        private string ignoredReservationOrderKey;

        public IReadOnlyCollection<ProductionOrderRecord> Orders => ActiveOrders.Values;

        public List<Storage> NetworkSourceStorages => networkSourceStorageCache;

        public void LoadOrdersForDisplay()
        {
            EnsureOrdersLoaded();
        }

        public void Refresh()
        {
            EnsureOrdersLoaded();
            RefreshNetworkStorageCache();
            craftableRecipes = ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos();
            UpdateProductionOrderStates();
            PurgeExpiredFinishedOrders();
            RunKeepRules();
        }

        public List<RecipeDisplayInfo> GetCraftableRecipes()
        {
            return craftableRecipes;
        }

        public List<ProductDisplayGroup> GetProductGroups()
        {
            return ProductionRecipeCatalog.BuildProductGroups(craftableRecipes);
        }

        public float GetNetworkAvailableAmount(Tag tag)
        {
            return Mathf.Max(0f, GetNetworkRawAmount(tag) - GetReservedAmount(tag, ignoredReservationOrderKey));
        }

        public float GetNetworkRawAmount(Tag tag)
        {
            return networkAmountCache.TryGetValue(tag, out float amount) ? amount : 0f;
        }

        public static float RequestLeasedMaterial(ComplexFabricator fabricator, ComplexRecipe recipe, Tag tag, float amount, Storage target)
        {
            if (fabricator == null ||
                recipe == null ||
                tag == Tag.Invalid ||
                target == null ||
                amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return 0f;
            }

            EnsureOrdersLoaded();
            float moved = 0f;
            List<ProductionOrderRecord> orders = new List<ProductionOrderRecord>();
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order))
                {
                    orders.Add(order);
                }
            }

            orders.Sort((left, right) => left.DisplayId.CompareTo(right.DisplayId));
            foreach (ProductionOrderRecord order in orders)
            {
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                bool hasMatchingQueue = false;
                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    if (assignment.Fabricator == fabricator &&
                        assignment.Recipe == recipe &&
                        GetRemainingQueueCount(order, assignment) > 0)
                    {
                        hasMatchingQueue = true;
                        break;
                    }
                }

                if (!hasMatchingQueue)
                {
                    continue;
                }

                foreach (ProductionOrderMaterialLease lease in order.MaterialLeases)
                {
                    if (lease.Material != tag)
                    {
                        continue;
                    }

                    if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    Storage source = FindNetworkStorageByInstanceIdStatic(lease.SourceStorageInstanceId);
                    if (source == null || source == target || source.GetComponent<ComplexFabricator>() != null)
                    {
                        continue;
                    }

                    float sourceAmount = source.GetAmountAvailable(tag);
                    float transferAmount = Mathf.Min(amount - moved, lease.Amount, sourceAmount, Mathf.Max(0f, target.RemainingCapacity()));
                    if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    moved += source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                }
            }

            return moved;
        }

        public ProductionOrderRecord FindDuplicateOrder(Tag productTag, ComplexRecipe recipe, float requestedAmount)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            return ActiveOrders.Values
                .Where(IsOrderActive)
                .FirstOrDefault(order =>
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    Mathf.RoundToInt(order.LastSubmittedAmount * 1000f) == amountBucket);
        }

        public IReadOnlyList<ProductionOrderRecord> GetActiveOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag && IsOrderActive(order))
                .OrderByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<ProductionOrderRecord> GetRecentOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<string> GetActiveOrderUsagesForFabricator(ComplexFabricator fabricator, int limit)
        {
            EnsureOrdersLoaded();
            return ActiveOrders.Values
                .Where(order => IsOrderActive(order) && order.QueueAssignments.Any(assignment => assignment.Fabricator == fabricator))
                .OrderBy(order => order.DisplayId)
                .Take(limit)
                .Select(order => FormatOrderUsage(order, fabricator))
                .ToList();
        }

        public ProductionKeepRule GetKeepRule(Tag productTag)
        {
            return KeepRules.TryGetValue(productTag, out ProductionKeepRule rule) ? rule : null;
        }

        public void SetKeepRule(ProductDisplayGroup product, RecipeDisplayInfo route, float targetAmount)
        {
            if (product == null || route.Recipe == null || targetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            KeepRules[product.ProductTag] = new ProductionKeepRule(
                product.ProductTag,
                product.ProductName,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                targetAmount);
        }

        public void ClearKeepRule(Tag productTag)
        {
            KeepRules.Remove(productTag);
        }

        public string CancelOrder(string orderKey, float currentCycle)
        {
            if (string.IsNullOrEmpty(orderKey) || !ActiveOrders.TryGetValue(orderKey, out ProductionOrderRecord order))
            {
                return "订单取消失败：找不到目标订单。";
            }

            if (!IsOrderActive(order))
            {
                return string.Format("订单 #{0} 已经结束，无需取消。", order.DisplayId);
            }

            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
            order.State = ProductionOrderState.Cancelled;
            order.CompletedCycle = currentCycle;
            order.AbnormalReason = "用户手动取消。";
            return string.Format("订单追踪：已手动取消订单 #{0}，并释放剩余排队批次。", order.DisplayId);
        }

        public ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount)
        {
            return BuildProductionPlan(recipe, fabricators, productTag, requestedAmount, 0, new HashSet<string>(), null);
        }

        public ProductionOrderDraft BuildDraft(ProductDisplayGroup product, RecipeDisplayInfo route, float requestedAmount)
        {
            ProductionOrderDraft draft = new ProductionOrderDraft
            {
                Product = product,
                Route = route,
                RequestedAmount = Mathf.Max(0f, requestedAmount),
                NetworkRawAmount = product != null ? GetNetworkRawAmount(product.ProductTag) : 0f,
                NetworkAvailableAmount = product != null ? GetNetworkAvailableAmount(product.ProductTag) : 0f,
                ReservedOutputAmount = product != null ? GetReservedAmount(product.ProductTag) : 0f,
                DuplicatePolicy = ProductionOrderDuplicatePolicy.CreateNew
            };
            if (product == null || route.Recipe == null)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add("未选择可生产的成品或配方。");
                return draft;
            }

            if (requestedAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add("订单数量必须大于 0。");
                return draft;
            }

            draft.Plan = BuildProductionPlan(route.Recipe, route.Fabricators, product.ProductTag, requestedAmount);
            draft.DuplicateOrder = FindDuplicateOrder(product.ProductTag, route.Recipe, requestedAmount);
            draft.DuplicatePolicy = draft.DuplicateOrder == null ? ProductionOrderDuplicatePolicy.CreateNew : ProductionOrderDuplicatePolicy.MergeIntoExisting;
            if (draft.Plan.Assignments.Count == 0)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add("没有可用生产设备，无法提交。");
            }

            if (draft.BlockedRequirementCount > 0)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Blocked;
                draft.ValidationMessages.Add(string.Format("有 {0} 项材料既无库存也无可接入补产路线。", draft.BlockedRequirementCount));
            }
            else if (draft.ProducedRequirementCount > 0 || draft.DuplicateOrder != null)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Warning;
            }
            else if (draft.RiskLevel != ProductionOrderRiskLevel.Blocked)
            {
                draft.RiskLevel = ProductionOrderRiskLevel.Ready;
            }

            if (draft.DuplicateOrder != null)
            {
                draft.ValidationMessages.Add(string.Format("检测到活动订单 #{0}，提交将合并数量而不是创建重复订单。", draft.DuplicateOrder.DisplayId));
            }

            if (draft.ProducedRequirementCount > 0)
            {
                draft.ValidationMessages.Add(string.Format("{0} 项材料缺口会自动补产。", draft.ProducedRequirementCount));
            }

            if (draft.ValidationMessages.Count == 0)
            {
                draft.ValidationMessages.Add("库存、设备、材料请求可执行。");
            }

            return draft;
        }

        public ProductionOrderSubmitResult SubmitOrder(ProductDisplayGroup product, RecipeDisplayInfo route, float requestedAmount, float currentCycle, bool isAutomatic = false)
        {
            ProductionOrderDraft draft = BuildDraft(product, route, requestedAmount);
            ProductionPlanNode plan = draft.Plan;
            if (plan == null || !draft.CanSubmit)
            {
                return ProductionOrderSubmitResult.Fail(string.Join(" ", draft.ValidationMessages.ToArray()));
            }

            if (plan.Assignments.Count == 0)
            {
                return ProductionOrderSubmitResult.Fail("订单追踪：提交失败，没有可用生产设备");
            }

            ProductionOrderRecord duplicate = FindDuplicateOrder(product.ProductTag, route.Recipe, requestedAmount);
            Dictionary<Tag, float> reservedMaterials = BuildReservedMaterials(plan);
            List<ProductionOrderQueueAssignment> queueAssignments = BuildQueueAssignments(plan);
            List<ProductionOrderMaterialLease> materialLeases = BuildMaterialLeases(plan);
            List<ProductionOrderOutputLease> outputLeases = BuildOutputLeases(queueAssignments, product.ProductTag, requestedAmount);
            if (duplicate != null)
            {
                ApplyProductionPlan(plan, duplicate.Key, materialLeases);
                duplicate.Merge(requestedAmount, plan.OrderCount, reservedMaterials, queueAssignments, materialLeases, outputLeases, currentCycle, isAutomatic);
                duplicate.ObserveActivity(currentCycle, duplicate.ProducedAtSubmit, CalculateOrderQueueLoad(duplicate));
                return ProductionOrderSubmitResult.MergeSuccess(duplicate, plan, string.Format("订单追踪：已合并到活动订单 #{0}，新增批次 {1}", duplicate.DisplayId, plan.OrderCount));
            }

            string orderKey = BuildOrderKey(product.ProductTag, route.Recipe, requestedAmount, currentCycle);
            float stockAtSubmit = GetProducedAmountForOrder(product.ProductTag);
            float allocationOffsetAtSubmit = GetPendingProducedAmountAhead(product.ProductTag);
            ApplyProductionPlan(plan, orderKey, materialLeases);
            ProductionOrderRecord record = new ProductionOrderRecord(
                orderKey,
                ActiveOrders.Count + 1,
                product.ProductTag,
                product.ProductName,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                requestedAmount,
                plan.OrderCount,
                stockAtSubmit,
                allocationOffsetAtSubmit,
                reservedMaterials,
                queueAssignments,
                materialLeases,
                outputLeases,
                currentCycle,
                isAutomatic);
            ActiveOrders[orderKey] = record;
            record.ObserveActivity(currentCycle, record.ProducedAtSubmit, CalculateOrderQueueLoad(record));
            return ProductionOrderSubmitResult.Created(record, plan, string.Format("订单追踪：已创建活动订单 #{0}，批次 {1}", record.DisplayId, plan.OrderCount));
        }

        public float EstimatePlanSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null)
            {
                return 0f;
            }

            float seconds = 0f;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child == null)
                {
                    continue;
                }

                seconds += EstimatePlanSeconds(requirement.Child, out bool childHasInfiniteQueue);
                hasInfiniteQueue |= childHasInfiniteQueue;
            }

            seconds += EstimateQueuedSeconds(node, out bool fabricatorHasInfiniteQueue);
            hasInfiniteQueue |= fabricatorHasInfiniteQueue;
            int busiestAssignedCount = node.Assignments.Count == 0 ? node.OrderCount : node.Assignments.Max(assignment => assignment.OrderCount);
            seconds += Mathf.Max(0f, node.Recipe.time) * busiestAssignedCount;
            return seconds;
        }

        public List<string> FormatPlanLines(ProductionPlanNode node, int depth)
        {
            List<string> lines = new List<string>();
            string indent = new string(' ', depth * 4);
            lines.Add(string.Format("{0}<b>{1}</b> x{2} -> {3}", indent, node.Recipe.GetUIName(false), node.OrderCount, node.FabricatorName));
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float missing = Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
                lines.Add(string.Format(
                    "{0}{1}: {2}/{3}{4}",
                    indent,
                    ProductionOrderFormatting.GetTagDisplayName(requirement.Material),
                    GameUtil.GetFormattedMass(requirement.AvailableAmount),
                    GameUtil.GetFormattedMass(requirement.RequiredAmount),
                    missing > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ? string.Format("  缺 {0}", GameUtil.GetFormattedMass(missing)) : string.Empty));

                if (requirement.Child != null)
                {
                    lines.AddRange(FormatPlanLines(requirement.Child, depth + 1));
                }
            }

            return lines;
        }

        private void RefreshNetworkStorageCache()
        {
            networkSourceStorageCache.Clear();
            networkSourceStorageSet.Clear();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                if (info?.ContentStorages == null)
                {
                    continue;
                }

                foreach (Storage storage in info.ContentStorages)
                {
                    if (storage != null && networkSourceStorageSet.Add(storage))
                    {
                        networkSourceStorageCache.Add(storage);
                    }
                }
            }

            networkAmountCache.Clear();
            foreach (Storage storage in networkSourceStorageCache)
            {
                if (storage == null || storage.GetComponent<ComplexFabricator>() != null || storage.items == null)
                {
                    continue;
                }

                foreach (GameObject item in storage.items)
                {
                    if (item != null)
                    {
                        AddNetworkItemAmount(item);
                    }
                }
            }
        }

        private void AddNetworkItemAmount(GameObject item)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            if (primaryElement == null)
            {
                return;
            }

            AddNetworkAmount(StorageItemUtility.GetStorageTransferTag(item), primaryElement.Mass);
            Tag elementTag = primaryElement.ElementID.CreateTag();
            if (elementTag != Tag.Invalid && elementTag != StorageItemUtility.GetStorageTransferTag(item))
            {
                AddNetworkAmount(elementTag, primaryElement.Mass);
            }
        }

        private void AddNetworkAmount(Tag tag, float amount)
        {
            if (tag == Tag.Invalid || amount <= 0f)
            {
                return;
            }

            networkAmountCache[tag] = networkAmountCache.TryGetValue(tag, out float existing) ? existing + amount : amount;
        }

        private ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag) ?? ProductionRecipeCatalog.GetPrimaryResult(recipe);
            float outputAmount = result != null ? Mathf.Max(result.amount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) : 1f;
            int orderCount = Mathf.Max(1, Mathf.CeilToInt(requestedAmount / outputAmount));
            ProductionPlanNode node = new ProductionPlanNode(recipe, fabricators, productTag, outputAmount, orderCount);
            AssignPlan(node, reservedFabricators);
            string pathKey = BuildPlanPathKey(recipe, productTag);
            HashSet<string> childPath = recipePath != null ? new HashSet<string>(recipePath) : new HashSet<string>();
            childPath.Add(pathKey);
            if (recipe.ingredients == null || depth >= Config.Instance.ProductionPlanMaxDepth)
            {
                return node;
            }

            HashSet<ComplexFabricator> childReservedFabricators = MergeReservedFabricators(reservedFabricators, node.Assignments);

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                Tag tag = GetPreferredMaterial(ingredient, orderCount, depth, childPath, childReservedFabricators);
                float required = ingredient.amount * orderCount;
                float available = GetNetworkAvailableAmount(tag);
                ProductionPlanRequirement requirement = new ProductionPlanRequirement(tag, required, available);
                if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required)
                {
                    requirement.Child = BuildBestChildPlan(tag, required - available, depth + 1, childPath, childReservedFabricators);
                }

                node.Requirements.Add(requirement);
            }

            return node;
        }

        private static HashSet<ComplexFabricator> MergeReservedFabricators(HashSet<ComplexFabricator> reservedFabricators, List<ProductionPlanAssignment> assignments)
        {
            HashSet<ComplexFabricator> merged = reservedFabricators != null
                ? new HashSet<ComplexFabricator>(reservedFabricators)
                : new HashSet<ComplexFabricator>();
            foreach (ProductionPlanAssignment assignment in assignments ?? new List<ProductionPlanAssignment>())
            {
                if (assignment.Fabricator != null)
                {
                    merged.Add(assignment.Fabricator);
                }
            }

            return merged;
        }

        private static void AssignPlan(ProductionPlanNode node, HashSet<ComplexFabricator> reservedFabricators)
        {
            if (node == null)
            {
                return;
            }

            List<ComplexFabricator> available = node.Fabricators
                .Where(fabricator => fabricator != null && (reservedFabricators == null || !reservedFabricators.Contains(fabricator)))
                .ToList();
            if (available.Count == 0)
            {
                available = node.Fabricators.Where(fabricator => fabricator != null).ToList();
            }

            node.Assignments.Clear();
            node.Assignments.AddRange(BuildAssignmentsForFabricators(node.Recipe, available, node.OutputAmount, node.OrderCount));
        }

        private static List<ProductionPlanAssignment> BuildAssignmentsForFabricators(ComplexRecipe recipe, List<ComplexFabricator> fabricators, float outputAmount, int orderCount)
        {
            List<ProductionPlanAssignment> assignments = new List<ProductionPlanAssignment>();
            if (fabricators == null || fabricators.Count == 0 || orderCount <= 0)
            {
                return assignments;
            }

            List<ComplexFabricator> orderedFabricators = fabricators
                .Where(fabricator => fabricator != null)
                .OrderBy(GetFiniteTotalQueueCount)
                .ThenBy(fabricator => GetFiniteRecipeQueueCount(fabricator, recipe))
                .ThenBy(fabricator => fabricator.gameObject.GetProperName())
                .ToList();
            if (orderedFabricators.Count == 0)
            {
                return assignments;
            }

            int baseCount = orderCount / orderedFabricators.Count;
            int remainder = orderCount % orderedFabricators.Count;
            for (int i = 0; i < orderedFabricators.Count; i++)
            {
                int count = baseCount + (i < remainder ? 1 : 0);
                if (count > 0)
                {
                    assignments.Add(new ProductionPlanAssignment(orderedFabricators[i], count, outputAmount * count));
                }
            }

            return assignments;
        }

        private static int GetFiniteTotalQueueCount(ComplexFabricator fabricator)
        {
            int queued = GetTotalRecipeQueueCount(fabricator);
            return queued == ComplexFabricator.QUEUE_INFINITE ? int.MaxValue : Mathf.Max(0, queued);
        }

        private static int GetTotalRecipeQueueCount(ComplexFabricator fabricator)
        {
            if (fabricator == null || RecipeQueueCountsField == null)
            {
                return 0;
            }

            Dictionary<string, int> queueCounts = RecipeQueueCountsField.GetValue(fabricator) as Dictionary<string, int>;
            if (queueCounts == null)
            {
                return 0;
            }

            int total = 0;
            foreach (int count in queueCounts.Values)
            {
                if (count == ComplexFabricator.QUEUE_INFINITE)
                {
                    return ComplexFabricator.QUEUE_INFINITE;
                }

                total += Mathf.Max(0, count);
            }

            if (fabricator.CurrentWorkingOrder != null)
            {
                total++;
            }

            return total;
        }

        private Tag GetPreferredMaterial(ComplexRecipe.RecipeElement element, int orderCount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
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
            return element.possibleMaterials
                .Select(tag => new
                {
                    Tag = tag,
                    Available = GetNetworkAvailableAmount(tag),
                    Child = GetNetworkAvailableAmount(tag) + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required
                        ? BuildBestChildPlan(tag, required - GetNetworkAvailableAmount(tag), depth + 1, recipePath, reservedFabricators)
                        : null
                })
                .OrderBy(candidate => CountBlockedRequirements(candidate.Child))
                .ThenBy(candidate => candidate.Child == null && candidate.Available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required ? 1 : 0)
                .ThenBy(candidate => EstimateMissingAmount(candidate.Child))
                .ThenByDescending(candidate => candidate.Available)
                .ThenBy(candidate => ProductionOrderFormatting.GetTagDisplayName(candidate.Tag))
                .Select(candidate => candidate.Tag)
                .FirstOrDefault();
        }

        private ProductionPlanNode BuildBestChildPlan(Tag productTag, float missingAmount, int depth, HashSet<string> recipePath, HashSet<ComplexFabricator> reservedFabricators)
        {
            if (productTag == Tag.Invalid || missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT || depth > Config.Instance.ProductionPlanMaxDepth)
            {
                return null;
            }

            List<ProductionPlanNode> candidates = ProductionRecipeCatalog.FindConnectedRecipesProducing(craftableRecipes, productTag)
                .Where(route => route.Recipe != null && route.Fabricators.Count > 0 && !IsRecipeInPath(route.Recipe, productTag, recipePath))
                .Select(route => BuildProductionPlan(route.Recipe, route.Fabricators, productTag, missingAmount, depth, recipePath, reservedFabricators))
                .Where(plan => plan != null && plan.Assignments.Count > 0)
                .ToList();
            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates
                .OrderBy(CountBlockedRequirements)
                .ThenBy(EstimateMissingAmount)
                .ThenBy(CountProducedRequirements)
                .ThenBy(EstimateQueueLoad)
                .ThenBy(plan => plan.Recipe.GetUIName(false))
                .FirstOrDefault();
        }

        private static bool IsRecipeInPath(ComplexRecipe recipe, Tag productTag, HashSet<string> recipePath)
        {
            return recipePath != null && recipePath.Contains(BuildPlanPathKey(recipe, productTag));
        }

        private static string BuildPlanPathKey(ComplexRecipe recipe, Tag productTag)
        {
            return string.Format("{0}|{1}", ProductionRecipeCatalog.GetRecipeKey(recipe), productTag);
        }

        private static int CountBlockedRequirements(ProductionPlanNode node)
        {
            return CountRequirements(node, requirement =>
                requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
                requirement.Child == null);
        }

        private static int CountProducedRequirements(ProductionPlanNode node)
        {
            return CountRequirements(node, requirement =>
                requirement.AvailableAmount + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < requirement.RequiredAmount &&
                requirement.Child != null);
        }

        private static int CountRequirements(ProductionPlanNode node, System.Func<ProductionPlanRequirement, bool> predicate)
        {
            if (node == null || predicate == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (predicate(requirement))
                {
                    count++;
                }

                count += CountRequirements(requirement.Child, predicate);
            }

            return count;
        }

        private static float EstimateMissingAmount(ProductionPlanNode node)
        {
            if (node == null)
            {
                return 0f;
            }

            float missing = 0f;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child == null)
                {
                    missing += Mathf.Max(0f, requirement.RequiredAmount - requirement.AvailableAmount);
                }

                missing += EstimateMissingAmount(requirement.Child);
            }

            return missing;
        }

        private static int EstimateQueueLoad(ProductionPlanNode node)
        {
            if (node == null)
            {
                return 0;
            }

            int load = node.Assignments.Sum(assignment => assignment.Fabricator != null && node.Recipe != null
                ? Mathf.Max(0, assignment.Fabricator.GetRecipeQueueCount(node.Recipe))
                : 0);
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                load += EstimateQueueLoad(requirement.Child);
            }

            return load;
        }

        private static Dictionary<Tag, float> BuildReservedMaterials(ProductionPlanNode node)
        {
            Dictionary<Tag, float> reservations = new Dictionary<Tag, float>();
            AddReservations(node, reservations);
            return reservations;
        }

        private static void AddReservations(ProductionPlanNode node, Dictionary<Tag, float> reservations)
        {
            if (node == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Material != Tag.Invalid && requirement.RequiredAmount > 0f)
                {
                    float reserved = Mathf.Min(requirement.RequiredAmount, requirement.AvailableAmount);
                    reservations[requirement.Material] = reservations.TryGetValue(requirement.Material, out float existing) ? existing + reserved : reserved;
                }

                AddReservations(requirement.Child, reservations);
            }
        }

        private List<ProductionOrderMaterialLease> BuildMaterialLeases(ProductionPlanNode node)
        {
            List<ProductionOrderMaterialLease> leases = new List<ProductionOrderMaterialLease>();
            Dictionary<Tag, float> reservations = BuildReservedMaterials(node);
            List<Storage> sources = new List<Storage>();
            foreach (KeyValuePair<Tag, float> pair in reservations)
            {
                float remaining = pair.Value;
                sources.Clear();
                foreach (Storage storage in networkSourceStorageCache)
                {
                    if (storage != null && storage.GetComponent<ComplexFabricator>() == null)
                    {
                        sources.Add(storage);
                    }
                }

                sources.Sort((left, right) => right.GetAmountAvailable(pair.Key).CompareTo(left.GetAmountAvailable(pair.Key)));
                foreach (Storage storage in sources)
                {
                    if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        break;
                    }

                    float amount = Mathf.Min(remaining, Mathf.Max(0f, storage.GetAmountAvailable(pair.Key)));
                    if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    leases.Add(new ProductionOrderMaterialLease(pair.Key, amount, GetComponentInstanceId(storage), string.Empty));
                    remaining -= amount;
                }
            }

            return leases;
        }

        private static List<ProductionOrderOutputLease> BuildOutputLeases(List<ProductionOrderQueueAssignment> assignments, Tag productTag, float requestedAmount)
        {
            List<ProductionOrderOutputLease> leases = new List<ProductionOrderOutputLease>();
            List<ProductionOrderQueueAssignment> primaryAssignments = (assignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && assignment.Primary && assignment.Fabricator != null)
                .ToList();
            int totalCount = primaryAssignments.Sum(assignment => Mathf.Max(0, assignment.OrderCount));
            foreach (ProductionOrderQueueAssignment assignment in primaryAssignments)
            {
                float amount = totalCount > 0 ? requestedAmount * assignment.OrderCount / totalCount : requestedAmount;
                leases.Add(new ProductionOrderOutputLease(productTag, amount, GetComponentInstanceId(assignment.Fabricator), assignment.Fabricator.GetProperName()));
            }

            return leases;
        }

        private static List<ProductionOrderQueueAssignment> BuildQueueAssignments(ProductionPlanNode node)
        {
            List<ProductionOrderQueueAssignment> assignments = new List<ProductionOrderQueueAssignment>();
            AddQueueAssignments(node, assignments, null, true);
            return assignments
                .Where(assignment => assignment.Fabricator != null && assignment.Recipe != null && assignment.OrderCount > 0)
                .GroupBy(assignment => string.Format(
                    "{0}|{1}|{2}|{3}|{4}",
                    assignment.Fabricator.GetInstanceID(),
                    assignment.Recipe.id,
                    assignment.OutputTag.Name,
                    assignment.ConsumerName,
                    assignment.Primary))
                .Select(group => new ProductionOrderQueueAssignment(
                    group.First().Fabricator,
                    group.First().Recipe,
                    group.Sum(assignment => assignment.OrderCount),
                    group.First().OutputTag,
                    group.First().OutputName,
                    group.First().ConsumerName,
                    group.First().Primary))
                .ToList();
        }

        private static void AddQueueAssignments(ProductionPlanNode node, List<ProductionOrderQueueAssignment> assignments, string consumerName, bool primary)
        {
            if (node == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                AddQueueAssignments(requirement.Child, assignments, node.FabricatorName, false);
            }

            Tag outputTag = GetPlanOutputTag(node);
            string outputName = ProductionOrderFormatting.GetTagDisplayName(outputTag);
            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (assignment.Fabricator != null && node.Recipe != null && assignment.OrderCount > 0)
                {
                    assignments.Add(new ProductionOrderQueueAssignment(
                        assignment.Fabricator,
                        node.Recipe,
                        assignment.OrderCount,
                        outputTag,
                        outputName,
                        primary ? assignment.Fabricator.GetProperName() : consumerName,
                        primary));
                }
            }
        }

        private static Tag GetPlanOutputTag(ProductionPlanNode node)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(node?.Recipe, node != null ? node.ProductTag : Tag.Invalid) ??
                                                 ProductionRecipeCatalog.GetPrimaryResult(node?.Recipe);
            return result != null && result.material != Tag.Invalid ? result.material : Tag.Invalid;
        }

        private void UpdateProductionOrderStates()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            EnsureActiveOrderAutomationLeases();
            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order))
                {
                    continue;
                }

                UpdateOrderProducedAmount(order);
                bool planChanged = MaintainActiveOrderPlan(order);
                float queueLoad = CalculateOrderQueueLoad(order);
                order.ObserveActivity(currentCycle, order.ProducedAtSubmit, queueLoad, planChanged || HasActiveOrderWork(order));
                if (order.ProducedAtSubmit + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= order.RequestedAmount)
                {
                    order.State = ProductionOrderState.Completed;
                    order.CompletedCycle = currentCycle;
                    CancelOrderQueues(order);
                    ReleaseOrderAutomation(order.Key);
                }
                else if (currentCycle - order.LastActivityCycle >= Config.Instance.AbnormalOrderTimeoutCycles)
                {
                    CancelAbnormalOrder(order, currentCycle);
                }
                else if (HasMissingReservedMaterial(order))
                {
                    order.State = ProductionOrderState.WaitingMaterials;
                }
                else
                {
                    order.State = ProductionOrderState.Producing;
                }
            }
        }

        private static bool IsOrderActive(ProductionOrderRecord order)
        {
            return order != null &&
                   order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        private static string FormatOrderUsage(ProductionOrderRecord order, ComplexFabricator fabricator)
        {
            ProductionOrderQueueAssignment localAssignment = order.QueueAssignments.FirstOrDefault(assignment => assignment.Fabricator == fabricator);
            if (localAssignment == null || localAssignment.Recipe == null)
            {
                return string.Format("#{0} {1}", order.DisplayId, order.ProductName);
            }

            if (localAssignment.Primary)
            {
                return string.Format("#{0} 执行 {1} x{2}", order.DisplayId, order.ProductName, localAssignment.OrderCount);
            }

            return string.Format(
                "#{0} 为 {1} 提供 {2} x{3}",
                order.DisplayId,
                string.IsNullOrEmpty(localAssignment.ConsumerName) ? FormatPrimaryFabricators(order) : localAssignment.ConsumerName,
                string.IsNullOrEmpty(localAssignment.OutputName) ? GetRecipeOutputName(localAssignment.Recipe, order.ProductTag) : localAssignment.OutputName,
                localAssignment.OrderCount);
        }

        private static string FormatPrimaryFabricators(ProductionOrderRecord order)
        {
            List<string> names = order.QueueAssignments
                .Where(assignment => assignment.Fabricator != null &&
                                     assignment.Recipe != null &&
                                     assignment.Primary)
                .Select(assignment => assignment.Fabricator.GetProperName())
                .Distinct()
                .Take(2)
                .ToList();
            if (names.Count == 0)
            {
                return order.ProductName;
            }

            return names.Count == 1 ? names[0] : string.Join("+", names.ToArray());
        }

        private static string GetRecipeOutputName(ComplexRecipe recipe, Tag fallbackTag)
        {
            ComplexRecipe.RecipeElement result = recipe?.results?.FirstOrDefault();
            if (result != null)
            {
                if (result.material != Tag.Invalid)
                {
                    return ProductionOrderFormatting.GetTagDisplayName(result.material);
                }

                if (!string.IsNullOrEmpty(result.facadeID))
                {
                    return ProductionOrderFormatting.GetTagDisplayName(result.facadeID.ToTag());
                }
            }

            return ProductionOrderFormatting.GetTagDisplayName(fallbackTag);
        }

        private static void PurgeExpiredFinishedOrders()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            List<string> expiredKeys = ActiveOrders.Values
                .Where(order => !IsOrderActive(order) &&
                                order.CompletedCycle > 0f &&
                                currentCycle - order.CompletedCycle > Config.Instance.FinishedOrderRecordLifetimeCycles)
                .Select(order => order.Key)
                .ToList();
            if (expiredKeys.Count == 0)
            {
                return;
            }

            foreach (string key in expiredKeys)
            {
                ActiveOrders.Remove(key);
            }
        }

        private static float CalculateOrderQueueLoad(ProductionOrderRecord order)
        {
            float load = 0f;
            if (order == null)
            {
                return load;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment.Fabricator == null || assignment.Recipe == null)
                {
                    continue;
                }

                int queued = assignment.Fabricator.GetRecipeQueueCount(assignment.Recipe);
                load += queued == ComplexFabricator.QUEUE_INFINITE ? ComplexFabricator.MAX_QUEUE_SIZE : Mathf.Max(0, queued);
                if (assignment.Fabricator.CurrentWorkingOrder == assignment.Recipe)
                {
                    load += Mathf.Clamp01(assignment.Fabricator.OrderProgress);
                }

                load += GetRecipeIngredientLoad(assignment.Fabricator.inStorage, assignment.Recipe);
                load += GetRecipeIngredientLoad(assignment.Fabricator.buildStorage, assignment.Recipe);
            }

            return load;
        }

        private static bool HasActiveOrderWork(ProductionOrderRecord order)
        {
            if (order == null || order.QueueAssignments == null)
            {
                return false;
            }

            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment == null || assignment.Fabricator == null || assignment.Recipe == null)
                {
                    continue;
                }

                if (GetFiniteRecipeQueueCount(assignment.Fabricator, assignment.Recipe) > 0 ||
                    assignment.Fabricator.CurrentWorkingOrder == assignment.Recipe)
                {
                    return true;
                }
            }

            return false;
        }

        private static float GetRecipeIngredientLoad(Storage storage, ComplexRecipe recipe)
        {
            if (storage == null || recipe == null || recipe.ingredients == null)
            {
                return 0f;
            }

            float load = 0f;
            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                load += storage.GetAmountAvailable(ingredient.material);
            }

            return load;
        }

        private static void CancelAbnormalOrder(ProductionOrderRecord order, float currentCycle)
        {
            order.State = ProductionOrderState.Abnormal;
            order.CompletedCycle = currentCycle;
            order.AbnormalReason = string.Format("{0:0.##} 周期内无进度变动，已自动取消建筑排产。最后变动周期 {1}", Config.Instance.AbnormalOrderTimeoutCycles, ProductionOrderFormatting.FormatCycle(order.LastActivityCycle));
            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
            StorageNetworkNotifications.ShowAbnormalOrder(order);
        }

        private static void CancelOrderQueues(ProductionOrderRecord order)
        {
            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
            {
                if (assignment.Fabricator == null || assignment.Recipe == null || assignment.OrderCount <= 0)
                {
                    continue;
                }

                int queued = assignment.Fabricator.GetRecipeQueueCount(assignment.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    queued = ComplexFabricator.MAX_QUEUE_SIZE;
                }

                bool cancelCurrentWorkingOrder = ShouldCancelCurrentWorkingOrder(order, assignment);
                int protectedQueued = GetProtectedQueueCount(order, assignment);
                int activeOwnedCount = Mathf.Max(0, assignment.OrderCount) + (cancelCurrentWorkingOrder ? 1 : 0);
                int removableQueued = Mathf.Max(0, queued - protectedQueued);
                int cancelCount = Mathf.Min(removableQueued + (cancelCurrentWorkingOrder ? 1 : 0), activeOwnedCount);
                if (cancelCount <= 0)
                {
                    continue;
                }

                if (cancelCurrentWorkingOrder)
                {
                    assignment.Fabricator.SetRecipeQueueCount(assignment.Recipe, 0);
                }

                int finalQueued = Mathf.Max(protectedQueued, queued - Mathf.Max(0, cancelCount - (cancelCurrentWorkingOrder ? 1 : 0)));
                assignment.Fabricator.SetRecipeQueueCount(assignment.Recipe, finalQueued);
            }
        }

        private void UpdateOrderProducedAmount(ProductionOrderRecord order)
        {
            float availableProduct = Mathf.Max(GetProducedAmountForOrder(order.ProductTag), GetLeasedPrimaryOutputAmount(order));
            float producedAfterSubmit = availableProduct - order.StockAtSubmit - order.AllocationOffsetAtSubmit;
            float allocatedToOrder = Mathf.Clamp(producedAfterSubmit, 0f, order.RequestedAmount);
            order.SetProducedAmount(allocatedToOrder);
        }

        private static float GetLeasedPrimaryOutputAmount(ProductionOrderRecord order)
        {
            float amount = 0f;
            foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments.Where(assignment => assignment.Primary))
            {
                if (assignment.Fabricator == null || assignment.Fabricator.outStorage == null || assignment.Fabricator.outStorage.items == null)
                {
                    continue;
                }

                Tag outputTag = assignment.OutputTag != Tag.Invalid ? assignment.OutputTag : order.ProductTag;
                foreach (GameObject item in assignment.Fabricator.outStorage.items)
                {
                    PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                    if (primaryElement != null && StorageItemUtility.MatchesStorageTag(item, outputTag))
                    {
                        amount += primaryElement.Mass;
                    }
                }
            }

            return amount;
        }

        private static bool ShouldCancelCurrentWorkingOrder(ProductionOrderRecord cancelledOrder, ProductionOrderQueueAssignment cancelledAssignment)
        {
            if (cancelledAssignment.Fabricator == null ||
                cancelledAssignment.Recipe == null ||
                cancelledAssignment.Fabricator.CurrentWorkingOrder != cancelledAssignment.Recipe)
            {
                return false;
            }

            return !ActiveOrders.Values.Any(order =>
                IsOrderActive(order) &&
                order.Key != cancelledOrder.Key &&
                IsOrderAheadOf(order, cancelledOrder) &&
                order.QueueAssignments.Any(assignment => IsSameQueue(assignment, cancelledAssignment) && GetRemainingQueueCount(order, assignment) > 0));
        }

        private static bool IsOrderAheadOf(ProductionOrderRecord candidate, ProductionOrderRecord order)
        {
            if (candidate.CreatedCycle < order.CreatedCycle - 0.001f)
            {
                return true;
            }

            return Mathf.Abs(candidate.CreatedCycle - order.CreatedCycle) <= 0.001f &&
                   candidate.DisplayId < order.DisplayId;
        }

        private static int GetProtectedQueueCount(ProductionOrderRecord cancelledOrder, ProductionOrderQueueAssignment cancelledAssignment)
        {
            int protectedCount = 0;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.Key == cancelledOrder.Key)
                {
                    continue;
                }

                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    if (IsSameQueue(assignment, cancelledAssignment))
                    {
                        protectedCount += GetRemainingQueueCount(order, assignment);
                    }
                }
            }

            return Mathf.Max(0, protectedCount);
        }

        private static int GetRemainingQueueCount(ProductionOrderRecord order, ProductionOrderQueueAssignment assignment)
        {
            float outputAmount = GetRecipeOutputAmount(assignment.Recipe, order.ProductTag);
            if (outputAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return assignment.OrderCount;
            }

            int totalAssigned = order.QueueAssignments
                .Where(candidate => candidate.Recipe == assignment.Recipe && GetRecipeOutputAmount(candidate.Recipe, order.ProductTag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Sum(candidate => candidate.OrderCount);
            if (totalAssigned <= 0)
            {
                return assignment.OrderCount;
            }

            float remainingAmount = Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit);
            int totalRemaining = Mathf.CeilToInt(remainingAmount / outputAmount);
            int remainingForAssignment = Mathf.CeilToInt(totalRemaining * assignment.OrderCount / (float)totalAssigned);
            return Mathf.Clamp(remainingForAssignment, 0, assignment.OrderCount);
        }

        private static float GetRecipeOutputAmount(ComplexRecipe recipe, Tag productTag)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag);
            return result != null ? Mathf.Max(0f, result.amount) : 0f;
        }

        private static bool IsSameQueue(ProductionOrderQueueAssignment left, ProductionOrderQueueAssignment right)
        {
            return left != null &&
                   right != null &&
                   left.Fabricator == right.Fabricator &&
                   left.Recipe == right.Recipe;
        }

        private float GetProducedAmountForOrder(Tag productTag)
        {
            return GetNetworkRawAmount(productTag) + GetConnectedFabricatorOutputAmount(productTag);
        }

        private static float GetConnectedFabricatorOutputAmount(Tag productTag)
        {
            float amount = 0f;
            foreach (ComplexFabricator fabricator in global::Components.ComplexFabricators.Items)
            {
                if (fabricator == null || fabricator.outStorage == null || fabricator.outStorage.items == null)
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
                    if (item == null)
                    {
                        continue;
                    }

                    PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                    if (primaryElement == null || !StorageNetworkMaterialRequester.MatchesStorageTag(item, productTag))
                    {
                        continue;
                    }

                    amount += primaryElement.Mass;
                }
            }

            return amount;
        }

        private bool HasMissingReservedMaterial(ProductionOrderRecord order)
        {
            foreach (KeyValuePair<Tag, float> pair in order.ReservedMaterials)
            {
                if (GetNetworkRawAmount(pair.Key) + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < pair.Value)
                {
                    return true;
                }
            }

            return false;
        }

        private bool MaintainActiveOrderPlan(ProductionOrderRecord order)
        {
            if (order == null || order.RequestedAmount - order.ProducedAtSubmit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return false;
            }

            RecipeDisplayInfo route = FindRouteForOrder(order);
            if (route.Recipe == null || route.Fabricators.Count == 0)
            {
                return false;
            }

            ProductionPlanNode plan = BuildProductionPlanIgnoringOrderReservations(
                route.Recipe,
                route.Fabricators,
                order.ProductTag,
                Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit),
                order.Key);
            if (plan == null || plan.Assignments.Count == 0)
            {
                return false;
            }

            List<ProductionOrderQueueAssignment> queueAssignments = BuildQueueAssignments(plan);
            List<ProductionOrderMaterialLease> materialLeases = BuildMaterialLeases(plan);
            bool queued = EnsureProductionPlanQueued(plan, order.Key, materialLeases);
            bool refreshed = order.RefreshPlan(
                plan.OrderCount,
                BuildReservedMaterials(plan),
                queueAssignments,
                materialLeases,
                BuildOutputLeases(queueAssignments, order.ProductTag, Mathf.Max(0f, order.RequestedAmount - order.ProducedAtSubmit)));
            return queued || refreshed;
        }

        private RecipeDisplayInfo FindRouteForOrder(ProductionOrderRecord order)
        {
            return craftableRecipes.FirstOrDefault(route =>
                route.ProductTag == order.ProductTag &&
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe) == order.RecipeKey);
        }

        private ProductionPlanNode BuildProductionPlanIgnoringOrderReservations(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, string orderKey)
        {
            string previousIgnoredReservationOrderKey = ignoredReservationOrderKey;
            ignoredReservationOrderKey = orderKey;
            try
            {
                return BuildProductionPlan(recipe, fabricators, productTag, requestedAmount);
            }
            finally
            {
                ignoredReservationOrderKey = previousIgnoredReservationOrderKey;
            }
        }

        private bool EnsureProductionPlanQueued(ProductionPlanNode node, string orderKey, List<ProductionOrderMaterialLease> materialLeases)
        {
            if (node == null)
            {
                return false;
            }

            bool changed = false;
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                changed |= EnsureProductionPlanQueued(requirement.Child, orderKey, materialLeases);
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (assignment.Fabricator == null || node.Recipe == null || assignment.OrderCount <= 0)
                {
                    continue;
                }

                int deficit = GetQueueDeficit(assignment.Fabricator, node.Recipe, assignment.OrderCount);
                if (deficit <= 0)
                {
                    EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                    continue;
                }

                int queued = GetFiniteRecipeQueueCount(assignment.Fabricator, node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, queued + deficit);
                EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                DispatchRecipeIngredients(node, new ProductionPlanAssignment(assignment.Fabricator, deficit, node.OutputAmount * deficit), materialLeases);
                changed = true;
            }

            return changed;
        }

        private static int GetQueueDeficit(ComplexFabricator fabricator, ComplexRecipe recipe, int desiredCount)
        {
            int activeCount = GetFiniteRecipeQueueCount(fabricator, recipe);
            if (fabricator.CurrentWorkingOrder == recipe)
            {
                activeCount++;
            }

            return Mathf.Max(0, desiredCount - activeCount);
        }

        private static int GetFiniteRecipeQueueCount(ComplexFabricator fabricator, ComplexRecipe recipe)
        {
            int queued = fabricator != null && recipe != null ? fabricator.GetRecipeQueueCount(recipe) : 0;
            return queued == ComplexFabricator.QUEUE_INFINITE ? ComplexFabricator.MAX_QUEUE_SIZE : Mathf.Max(0, queued);
        }

        private void ApplyProductionPlan(ProductionPlanNode node, string orderKey, List<ProductionOrderMaterialLease> materialLeases)
        {
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child != null)
                {
                    ApplyProductionPlan(requirement.Child, orderKey, materialLeases);
                }
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (assignment.Fabricator == null || node.Recipe == null)
                {
                    continue;
                }

                int queued = assignment.Fabricator.GetRecipeQueueCount(node.Recipe);
                assignment.Fabricator.SetRecipeQueueCount(node.Recipe, (queued == ComplexFabricator.QUEUE_INFINITE ? 0 : Mathf.Max(0, queued)) + assignment.OrderCount);
                EnsureOrderAutomationEnabled(assignment.Fabricator, orderKey);
                DispatchRecipeIngredients(node, assignment, materialLeases);
            }
        }

        private static void EnsureOrderAutomationEnabled(ComplexFabricator fabricator, string orderKey)
        {
            StorageNetworkMaterialRequester requester = fabricator != null ? fabricator.GetComponent<StorageNetworkMaterialRequester>() : null;
            if (requester != null)
            {
                int instanceId = StorageNetworkMaterialRequester.GetStorageInstanceId(fabricator.inStorage);
                if (instanceId != KPrefabID.InvalidInstanceID)
                {
                    if (!AutomationLeases.TryGetValue(instanceId, out OrderAutomationLease lease))
                    {
                        lease = new OrderAutomationLease(requester);
                        AutomationLeases[instanceId] = lease;
                    }

                    lease.OrderKeys.Add(orderKey);
                }

                requester.RequestEnabled = true;
                requester.CurrentMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
                requester.OutputStoreEnabled = true;
                requester.CurrentOutputStoreMode = StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork;
            }
        }

        private static void EnsureActiveOrderAutomationLeases()
        {
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order))
                {
                    continue;
                }

                foreach (ProductionOrderQueueAssignment assignment in order.QueueAssignments)
                {
                    if (assignment.Fabricator != null)
                    {
                        EnsureOrderAutomationEnabled(assignment.Fabricator, order.Key);
                    }
                }
            }
        }

        private static void ReleaseOrderAutomation(string orderKey)
        {
            List<int> emptyLeases = new List<int>();
            foreach (KeyValuePair<int, OrderAutomationLease> pair in AutomationLeases)
            {
                if (!pair.Value.OrderKeys.Remove(orderKey) || pair.Value.OrderKeys.Count > 0)
                {
                    continue;
                }

                pair.Value.Restore();
                emptyLeases.Add(pair.Key);
            }

            foreach (int instanceId in emptyLeases)
            {
                AutomationLeases.Remove(instanceId);
            }
        }

        private void DispatchRecipeIngredients(ProductionPlanNode node, ProductionPlanAssignment assignment, List<ProductionOrderMaterialLease> materialLeases)
        {
            Storage target = assignment.Fabricator.inStorage;
            if (target == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                float required = requirement.RequiredAmount * assignment.OrderCount / Mathf.Max(1, node.OrderCount);
                float needed = Mathf.Max(0f, required - target.GetAmountAvailable(requirement.Material));
                TransferMaterialToStorage(requirement.Material, target, needed, materialLeases);
            }
        }

        private float TransferMaterialToStorage(Tag tag, Storage target, float amount, List<ProductionOrderMaterialLease> materialLeases)
        {
            float moved = 0f;
            if (target == null || amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return moved;
            }

            List<Storage> sources = new List<Storage>();
            HashSet<Storage> seen = new HashSet<Storage>();
            if (materialLeases != null)
            {
                foreach (ProductionOrderMaterialLease lease in materialLeases)
                {
                    if (lease.Material != tag || lease.Amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                    {
                        continue;
                    }

                    AddTransferSource(sources, seen, FindNetworkStorageByInstanceId(lease.SourceStorageInstanceId), target, tag);
                }
            }

            foreach (Storage storage in networkSourceStorageCache)
            {
                AddTransferSource(sources, seen, storage, target, tag);
            }

            sources.Sort((left, right) => right.GetAmountAvailable(tag).CompareTo(left.GetAmountAvailable(tag)));
            foreach (Storage source in sources)
            {
                float transferAmount = Mathf.Min(amount - moved, source.GetAmountAvailable(tag), Mathf.Max(0f, target.RemainingCapacity()));
                if (transferAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                moved += source.Transfer(target, tag, transferAmount, block_events: false, hide_popups: true);
                if (amount - moved <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }
            }

            return moved;
        }

        private static void AddTransferSource(List<Storage> sources, HashSet<Storage> seen, Storage storage, Storage target, Tag tag)
        {
            if (storage == null ||
                storage == target ||
                seen.Contains(storage) ||
                storage.GetComponent<ComplexFabricator>() != null ||
                storage.GetAmountAvailable(tag) <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            seen.Add(storage);
            sources.Add(storage);
        }

        private static float GetReservedAmount(Tag tag, string ignoredOrderKey = null)
        {
            float reserved = 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.Key == ignoredOrderKey)
                {
                    continue;
                }

                if (order.MaterialLeases.Count > 0)
                {
                    foreach (ProductionOrderMaterialLease lease in order.MaterialLeases)
                    {
                        if (lease.Material == tag)
                        {
                            reserved += lease.Amount;
                        }
                    }
                }
                else
                {
                    reserved += order.GetReservedAmount(tag);
                }
            }

            return reserved;
        }

        private static float GetPendingProducedAmountAhead(Tag productTag)
        {
            float pending = 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order) || order.ProductTag != productTag)
                {
                    continue;
                }

                float leased = 0f;
                if (order.OutputLeases.Count > 0)
                {
                    foreach (ProductionOrderOutputLease lease in order.OutputLeases)
                    {
                        if (lease.ProductTag == productTag)
                        {
                            leased += lease.Amount;
                        }
                    }
                }
                else
                {
                    leased = order.RequestedAmount;
                }

                pending += Mathf.Max(0f, leased - order.ProducedAtSubmit);
            }

            return pending;
        }

        private void RunKeepRules()
        {
            if (KeepRules.Count == 0 || craftableRecipes.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            Dictionary<Tag, ProductDisplayGroup> products = GetProductGroups().ToDictionary(product => product.ProductTag);
            foreach (ProductionKeepRule rule in KeepRules.Values.ToList())
            {
                if (rule.TargetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT ||
                    !products.TryGetValue(rule.ProductTag, out ProductDisplayGroup product))
                {
                    continue;
                }

                RecipeDisplayInfo route = product.Routes.FirstOrDefault(candidate => ProductionRecipeCatalog.GetRecipeKey(candidate.Recipe) == rule.RecipeKey);
                if (route.Recipe == null)
                {
                    route = product.Routes.FirstOrDefault();
                }

                if (route.Recipe == null)
                {
                    continue;
                }

                float committedAmount = GetProducedAmountForOrder(rule.ProductTag) + GetPendingProducedAmountAhead(rule.ProductTag);
                float missingAmount = rule.TargetAmount - committedAmount;
                if (missingAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                SubmitOrder(product, route, missingAmount, currentCycle, true);
            }
        }

        private static float EstimateQueuedSeconds(ProductionPlanNode node, out bool hasInfiniteQueue)
        {
            hasInfiniteQueue = false;
            if (node == null || node.Recipe == null || node.Assignments.Count == 0)
            {
                return 0f;
            }

            float maxQueuedSeconds = 0f;
            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                int queued = assignment.Fabricator.GetRecipeQueueCount(node.Recipe);
                if (queued == ComplexFabricator.QUEUE_INFINITE)
                {
                    hasInfiniteQueue = true;
                    continue;
                }

                maxQueuedSeconds = Mathf.Max(maxQueuedSeconds, Mathf.Max(0, queued) * Mathf.Max(0f, node.Recipe.time));
            }

            return maxQueuedSeconds;
        }

        private static string BuildOrderKey(Tag productTag, ComplexRecipe recipe, float requestedAmount, float createdCycle)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            int cycleBucket = Mathf.RoundToInt(createdCycle * 100f);
            return string.Format("{0}|{1}|{2}|{3}", productTag, recipeKey, amountBucket, cycleBucket);
        }

        private static int GetComponentInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        private Storage FindNetworkStorageByInstanceId(int instanceId)
        {
            return instanceId == KPrefabID.InvalidInstanceID
                ? null
                : networkSourceStorageCache.FirstOrDefault(storage => GetComponentInstanceId(storage) == instanceId);
        }

        private static Storage FindNetworkStorageByInstanceIdStatic(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            HashSet<Storage> visited = new HashSet<Storage>();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                if (info?.ContentStorages == null)
                {
                    continue;
                }

                foreach (Storage storage in info.ContentStorages)
                {
                    if (storage != null && visited.Add(storage) && GetComponentInstanceId(storage) == instanceId)
                    {
                        return storage;
                    }
                }
            }

            return null;
        }

        private static void EnsureOrdersLoaded()
        {
            string storePath = ProductionOrderPersistence.GetStorePath();
            if (loadedStorePath == storePath)
            {
                return;
            }

            ActiveOrders.Clear();
            AutomationLeases.Clear();
            KeepRules.Clear();
            loadedStorePath = storePath;
            foreach (ProductionOrderRecord order in ProductionOrderPersistence.Load())
            {
                ActiveOrders[order.Key] = order;
            }

            foreach (ProductionKeepRule rule in ProductionOrderPersistence.LoadKeepRules())
            {
                KeepRules[rule.ProductTag] = rule;
            }
        }

        public static void SaveOrders()
        {
            ProductionOrderPersistence.Save(ActiveOrders.Values.ToList(), KeepRules.Values.ToList());
        }
    }

    internal sealed class ProductionOrderSubmitResult
    {
        private ProductionOrderSubmitResult(bool success, bool merged, ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            Success = success;
            Merged = merged;
            Order = order;
            Plan = plan;
            Message = message;
        }

        public bool Success { get; }

        public bool Merged { get; }

        public ProductionOrderRecord Order { get; }

        public ProductionPlanNode Plan { get; }

        public string Message { get; }

        public static ProductionOrderSubmitResult Fail(string message)
        {
            return new ProductionOrderSubmitResult(false, false, null, null, message);
        }

        public static ProductionOrderSubmitResult Created(ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            return new ProductionOrderSubmitResult(true, false, order, plan, message);
        }

        public static ProductionOrderSubmitResult MergeSuccess(ProductionOrderRecord order, ProductionPlanNode plan, string message)
        {
            return new ProductionOrderSubmitResult(true, true, order, plan, message);
        }
    }

    internal sealed class OrderAutomationLease
    {
        private readonly StorageNetworkMaterialRequester requester;
        private readonly bool requestEnabled;
        private readonly int mode;
        private readonly int sourceStorageInstanceId;
        private readonly bool limitEnabled;
        private readonly float limitKg;
        private readonly float requestedKg;
        private readonly bool outputStoreEnabled;
        private readonly int outputStoreModeValue;
        private readonly int outputStorageInstanceId;

        public OrderAutomationLease(StorageNetworkMaterialRequester requester)
        {
            this.requester = requester;
            requestEnabled = requester.RequestEnabled;
            mode = requester.Mode;
            sourceStorageInstanceId = requester.SourceStorageInstanceId;
            limitEnabled = requester.LimitEnabled;
            limitKg = requester.LimitKg;
            requestedKg = requester.RequestedKg;
            outputStoreEnabled = requester.OutputStoreEnabled;
            outputStoreModeValue = requester.OutputStoreModeValue;
            outputStorageInstanceId = requester.OutputStorageInstanceId;
        }

        public HashSet<string> OrderKeys { get; } = new HashSet<string>();

        public void Restore()
        {
            if (requester == null)
            {
                return;
            }

            requester.RequestEnabled = requestEnabled;
            requester.Mode = mode;
            requester.SourceStorageInstanceId = sourceStorageInstanceId;
            requester.LimitEnabled = limitEnabled;
            requester.LimitKg = limitKg;
            requester.RequestedKg = requestedKg;
            requester.OutputStoreEnabled = outputStoreEnabled;
            requester.OutputStoreModeValue = outputStoreModeValue;
            requester.OutputStorageInstanceId = outputStorageInstanceId;
        }
    }
}
