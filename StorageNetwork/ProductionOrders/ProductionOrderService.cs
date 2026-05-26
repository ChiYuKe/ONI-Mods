using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed class ProductionOrderService
    {
        private const int MaxPlanDepth = 4;
        private const float AbnormalTimeoutCycles = 0.5f;
        private static readonly Dictionary<string, ProductionOrderRecord> ActiveOrders = new Dictionary<string, ProductionOrderRecord>();
        private static readonly Dictionary<int, OrderAutomationLease> AutomationLeases = new Dictionary<int, OrderAutomationLease>();

        private List<RecipeDisplayInfo> craftableRecipes = new List<RecipeDisplayInfo>();
        private Dictionary<Tag, float> networkAmountCache = new Dictionary<Tag, float>();
        private List<Storage> networkSourceStorageCache = new List<Storage>();

        public IReadOnlyCollection<ProductionOrderRecord> Orders => ActiveOrders.Values;

        public List<Storage> NetworkSourceStorages => networkSourceStorageCache;

        public void Refresh()
        {
            RefreshNetworkStorageCache();
            craftableRecipes = ProductionRecipeCatalog.GetCraftableRecipeDisplayInfos();
            UpdateProductionOrderStates();
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
            return Mathf.Max(0f, GetNetworkRawAmount(tag) - GetReservedAmount(tag));
        }

        public float GetNetworkRawAmount(Tag tag)
        {
            return networkAmountCache.TryGetValue(tag, out float amount) ? amount : 0f;
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
                .Where(order => order.ProductTag == productTag && order.State != ProductionOrderState.Cancelled)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount)
        {
            return BuildProductionPlan(recipe, fabricators, productTag, requestedAmount, 0);
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
                draft.ValidationMessages.Add("草案校验通过：库存、设备、材料请求均可执行。");
            }

            return draft;
        }

        public ProductionOrderSubmitResult SubmitOrder(ProductDisplayGroup product, RecipeDisplayInfo route, float requestedAmount, float currentCycle)
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
            if (duplicate != null)
            {
                ApplyProductionPlan(plan, duplicate.Key);
                duplicate.Merge(requestedAmount, plan.OrderCount, reservedMaterials, queueAssignments, currentCycle);
                duplicate.ObserveActivity(currentCycle, duplicate.ProducedAtSubmit, CalculateOrderQueueLoad(duplicate));
                return ProductionOrderSubmitResult.MergeSuccess(duplicate, plan, string.Format("订单追踪：已合并到活动订单 #{0}，新增批次 {1}", duplicate.DisplayId, plan.OrderCount));
            }

            string orderKey = BuildOrderKey(product.ProductTag, route.Recipe, requestedAmount, currentCycle);
            ApplyProductionPlan(plan, orderKey);
            ProductionOrderRecord record = new ProductionOrderRecord(
                orderKey,
                ActiveOrders.Count + 1,
                product.ProductTag,
                product.ProductName,
                ProductionRecipeCatalog.GetRecipeKey(route.Recipe),
                requestedAmount,
                plan.OrderCount,
                GetProducedAmountForOrder(product.ProductTag),
                reservedMaterials,
                queueAssignments,
                currentCycle);
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
                    GameUtil.GetFormattedMass(Mathf.Min(requirement.AvailableAmount, requirement.RequiredAmount)),
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
            networkSourceStorageCache = StorageSceneCollector.Collect().Storages
                .SelectMany(info => info.ContentStorages)
                .Where(storage => storage != null)
                .Distinct()
                .ToList();
            networkAmountCache = new Dictionary<Tag, float>();
            foreach (GameObject item in networkSourceStorageCache
                .Where(storage => storage.GetComponent<ComplexFabricator>() == null)
                .SelectMany(storage => storage.items.Where(item => item != null)))
            {
                PrimaryElement primaryElement = item.GetComponent<PrimaryElement>();
                if (primaryElement == null)
                {
                    continue;
                }

                AddNetworkAmount(primaryElement.ElementID.CreateTag(), primaryElement.Mass);
                KPrefabID prefabID = item.GetComponent<KPrefabID>();
                if (prefabID != null)
                {
                    AddNetworkAmount(prefabID.PrefabTag, primaryElement.Mass);
                }
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

        private ProductionPlanNode BuildProductionPlan(ComplexRecipe recipe, List<ComplexFabricator> fabricators, Tag productTag, float requestedAmount, int depth)
        {
            ComplexRecipe.RecipeElement result = ProductionRecipeCatalog.GetRecipeResultForProduct(recipe, productTag) ?? ProductionRecipeCatalog.GetPrimaryResult(recipe);
            float outputAmount = result != null ? Mathf.Max(result.amount, PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT) : 1f;
            int orderCount = Mathf.Max(1, Mathf.CeilToInt(requestedAmount / outputAmount));
            ProductionPlanNode node = new ProductionPlanNode(recipe, fabricators, productTag, outputAmount, orderCount);
            if (recipe.ingredients == null || depth >= MaxPlanDepth)
            {
                return node;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                Tag tag = GetPreferredMaterial(ingredient);
                float required = ingredient.amount * orderCount;
                float available = GetNetworkAvailableAmount(tag);
                RecipeDisplayInfo producer = ProductionRecipeCatalog.FindConnectedRecipeProducing(craftableRecipes, tag);
                ProductionPlanRequirement requirement = new ProductionPlanRequirement(tag, required, available);
                if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < required && producer.Recipe != null)
                {
                    requirement.Child = BuildProductionPlan(producer.Recipe, producer.Fabricators, tag, required - available, depth + 1);
                }

                node.Requirements.Add(requirement);
            }

            return node;
        }

        private Tag GetPreferredMaterial(ComplexRecipe.RecipeElement element)
        {
            if (element.material != Tag.Invalid)
            {
                return element.material;
            }

            return element.possibleMaterials == null || element.possibleMaterials.Length == 0
                ? Tag.Invalid
                : element.possibleMaterials.OrderByDescending(GetNetworkAvailableAmount).FirstOrDefault();
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

        private static List<ProductionOrderQueueAssignment> BuildQueueAssignments(ProductionPlanNode node)
        {
            List<ProductionOrderQueueAssignment> assignments = new List<ProductionOrderQueueAssignment>();
            AddQueueAssignments(node, assignments);
            return assignments
                .Where(assignment => assignment.Fabricator != null && assignment.Recipe != null && assignment.OrderCount > 0)
                .GroupBy(assignment => string.Format("{0}|{1}", assignment.Fabricator.GetInstanceID(), assignment.Recipe.id))
                .Select(group => new ProductionOrderQueueAssignment(group.First().Fabricator, group.First().Recipe, group.Sum(assignment => assignment.OrderCount)))
                .ToList();
        }

        private static void AddQueueAssignments(ProductionPlanNode node, List<ProductionOrderQueueAssignment> assignments)
        {
            if (node == null)
            {
                return;
            }

            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                AddQueueAssignments(requirement.Child, assignments);
            }

            foreach (ProductionPlanAssignment assignment in node.Assignments)
            {
                if (assignment.Fabricator != null && node.Recipe != null && assignment.OrderCount > 0)
                {
                    assignments.Add(new ProductionOrderQueueAssignment(assignment.Fabricator, node.Recipe, assignment.OrderCount));
                }
            }
        }

        private void UpdateProductionOrderStates()
        {
            if (ActiveOrders.Count == 0)
            {
                return;
            }

            float currentCycle = GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0f;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (!IsOrderActive(order))
                {
                    continue;
                }

                float currentStock = GetProducedAmountForOrder(order.ProductTag);
                float gained = Mathf.Max(0f, currentStock - order.StockAtSubmit);
                order.ProducedAtSubmit = Mathf.Min(order.RequestedAmount, gained);
                order.ObserveActivity(currentCycle, order.ProducedAtSubmit, CalculateOrderQueueLoad(order));
                if (gained + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT >= order.RequestedAmount)
                {
                    order.State = ProductionOrderState.Completed;
                    order.CompletedCycle = currentCycle;
                    ReleaseOrderAutomation(order.Key);
                }
                else if (currentCycle - order.LastActivityCycle >= AbnormalTimeoutCycles)
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
            order.AbnormalReason = string.Format("半周期内无进度变动，已自动取消建筑排产。最后变动周期 {0}", ProductionOrderFormatting.FormatCycle(order.LastActivityCycle));
            CancelOrderQueues(order);
            ReleaseOrderAutomation(order.Key);
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

                assignment.Fabricator.SetRecipeQueueCount(assignment.Recipe, Mathf.Max(0, queued - assignment.OrderCount));
            }
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

        private void ApplyProductionPlan(ProductionPlanNode node, string orderKey)
        {
            foreach (ProductionPlanRequirement requirement in node.Requirements)
            {
                if (requirement.Child != null)
                {
                    ApplyProductionPlan(requirement.Child, orderKey);
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
                DispatchRecipeIngredients(node, assignment);
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

        private void DispatchRecipeIngredients(ProductionPlanNode node, ProductionPlanAssignment assignment)
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
                TransferMaterialToStorage(requirement.Material, target, needed);
            }
        }

        private float TransferMaterialToStorage(Tag tag, Storage target, float amount)
        {
            float moved = 0f;
            if (target == null || amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return moved;
            }

            foreach (Storage source in networkSourceStorageCache
                .Where(storage => storage != target && storage.GetComponent<ComplexFabricator>() == null && storage.GetAmountAvailable(tag) > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .OrderByDescending(storage => storage.GetAmountAvailable(tag)))
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

        private static float GetReservedAmount(Tag tag)
        {
            return ActiveOrders.Values
                .Where(IsOrderActive)
                .Sum(order => order.GetReservedAmount(tag));
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
