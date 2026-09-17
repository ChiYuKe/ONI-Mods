using System.Collections.Generic;
using System.Linq;
using System.Text;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal sealed partial class ProductionOrderService
    {
        public struct MissingMaterialEntry
        {
            public Tag Tag;
            public string Name;
            public float Required;
            public float Available;
            public float Missing;
        }

        public List<MissingMaterialEntry> GetMissingMaterials(ProductionOrderRecord order)
        {
            List<MissingMaterialEntry> list = new List<MissingMaterialEntry>();
            if (order == null || order.ReservedMaterials == null)
            {
                return list;
            }

            foreach (KeyValuePair<Tag, float> pair in order.ReservedMaterials)
            {
                float available = GetNetworkRawAmount(pair.Key);
                if (available + PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT < pair.Value)
                {
                    list.Add(new MissingMaterialEntry
                    {
                        Tag = pair.Key,
                        Name = ProductionOrderFormatting.GetTagDisplayName(pair.Key),
                        Required = pair.Value,
                        Available = Mathf.Max(0f, available),
                        Missing = pair.Value - available
                    });
                }
            }

            return list;
        }

        public string GetMissingMaterialsSummary(ProductionOrderRecord order)
        {
            List<MissingMaterialEntry> missing = GetMissingMaterials(order);
            if (missing.Count == 0)
            {
                return null;
            }

            return string.Join(", ", missing.Select(m =>
                string.Format("{0} ({1} / {2})",
                    m.Name,
                    GameUtil.GetFormattedMass(m.Available),
                    GameUtil.GetFormattedMass(m.Required))
            ));
        }

        public string GetMissingMaterialsTooltip(ProductionOrderRecord order)
        {
            List<MissingMaterialEntry> missing = GetMissingMaterials(order);
            if (missing.Count == 0)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MISSING_MATERIALS_TITLE));
            foreach (MissingMaterialEntry m in missing)
            {
                sb.AppendLine(string.Format(
                    StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MISSING_MATERIAL_LINE),
                    m.Name,
                    GameUtil.GetFormattedMass(m.Available),
                    GameUtil.GetFormattedMass(m.Required),
                    GameUtil.GetFormattedMass(m.Missing)));
            }

            return sb.ToString().TrimEnd();
        }

        public IReadOnlyList<RecipeDisplayInfo> GetCraftableRecipes()
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
            return networkInventory.GetRawAmount(tag);
        }

        private float GetProducedAmountForOrder(Tag productTag)
        {
            return GetNetworkRawAmount(productTag) + GetConnectedFabricatorOutputAmount(productTag);
        }

        private float GetConnectedFabricatorOutputAmount(Tag productTag)
        {
            return connectedFabricatorOutputAmounts.TryGetValue(productTag, out float amount) ? amount : 0f;
        }

        public ProductionOrderRecord FindDuplicateOrder(Tag productTag, ComplexRecipe recipe, float requestedAmount)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            int amountBucket = Mathf.RoundToInt(requestedAmount * 1000f);
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order) &&
                    IsOrderInCurrentScope(order) &&
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    Mathf.RoundToInt(order.LastSubmittedAmount * 1000f) == amountBucket)
                {
                    return order;
                }
            }

            return null;
        }

        private ProductionOrderRecord FindAutomaticDuplicateOrder(Tag productTag, ComplexRecipe recipe)
        {
            string recipeKey = ProductionRecipeCatalog.GetRecipeKey(recipe);
            ProductionOrderRecord result = null;
            foreach (ProductionOrderRecord order in ActiveOrders.Values)
            {
                if (IsOrderActive(order) &&
                    IsOrderInCurrentScope(order) &&
                    order.IsAutomatic &&
                    order.ProductTag == productTag &&
                    order.RecipeKey == recipeKey &&
                    (result == null || order.DisplayId < result.DisplayId))
                {
                    result = order;
                }
            }

            return result;
        }

        public IReadOnlyList<ProductionOrderRecord> GetActiveOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag && IsOrderActive(order))
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<ProductionOrderRecord> GetRecentOrdersForProduct(Tag productTag, int limit)
        {
            return ActiveOrders.Values
                .Where(order => order.ProductTag == productTag)
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle)
                .Take(limit)
                .ToList();
        }

        public IReadOnlyList<ProductionOrderRecord> GetRecentOrders(int limit)
        {
            IEnumerable<ProductionOrderRecord> orders = ActiveOrders.Values
                .Where(IsOrderInCurrentScope)
                .OrderByDescending(order => order.State == ProductionOrderState.Completed ? order.CompletedCycle : float.MaxValue)
                .ThenByDescending(order => order.CreatedCycle);

            return limit > 0 ? orders.Take(limit).ToList() : orders.ToList();
        }

        public IReadOnlyList<string> GetActiveOrderUsagesForFabricator(ComplexFabricator fabricator, int limit)
        {
            if (!IsOrderProductionFabricator(fabricator))
            {
                return new List<string>();
            }

            EnsureOrdersLoaded();
            return ActiveOrders.Values
                .Where(order => IsOrderActive(order) && order.QueueAssignments.Any(assignment => assignment.Fabricator == fabricator))
                .OrderBy(order => order.DisplayId)
                .Take(limit)
                .Select(order => FormatOrderUsage(order, fabricator))
                .ToList();
        }

        private bool IsOrderInCurrentScope(ProductionOrderRecord order)
        {
            if (orderCenterScope == null)
            {
                return IsOrderReachableFromCurrentWorld(order);
            }

            ComplexFabricator scopedFabricator = orderCenterScope.GetComponent<ComplexFabricator>();
            return scopedFabricator != null &&
                   order != null &&
                   order.QueueAssignments.Any(assignment => assignment.Fabricator == scopedFabricator);
        }
    }
}
