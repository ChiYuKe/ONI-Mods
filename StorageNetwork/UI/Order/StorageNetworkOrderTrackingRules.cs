using System.Collections.Generic;
using System.Linq;
using StorageNetwork.ProductionOrders;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkOrderTrackingRules
    {
        public static string BuildListSignature(ProductDisplayGroup product, IEnumerable<ProductionOrderRecord> records, string searchText, StorageNetworkPanel.TrackingFilterMode filterMode)
        {
            string recordsSignature = string.Join("|", records.Select(record => string.Format(
                "{0}:{1}:{2}:{3:0.###}:{4:0.###}:{5}:{6}:{7:0.###}:{8}",
                record.Key,
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty)));

            return string.Format("{0}|{1}|{2}|{3}", product?.ProductKey ?? string.Empty, searchText ?? string.Empty, filterMode, recordsSignature);
        }

        public static string BuildCardSignature(ProductionOrderRecord record)
        {
            return string.Format(
                "{0}:{1}:{2:0.###}:{3:0.###}:{4}:{5}:{6:0.###}:{7}:{8}",
                record.DisplayId,
                record.State,
                record.ProducedAtSubmit,
                record.RequestedAmount,
                record.OrderCount,
                record.MergeCount,
                record.LastActivityCycle,
                record.AbnormalReason ?? string.Empty,
                string.Join(",", (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                    .Where(assignment => assignment != null)
                    .Select(assignment => string.Format("{0}:{1}",
                        assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID() : 0,
                        assignment.Primary))));
        }

        public static bool MatchesFilter(ProductionOrderRecord record, StorageNetworkPanel.TrackingFilterMode filterMode, string searchText)
        {
            if (record == null)
            {
                return false;
            }

            switch (filterMode)
            {
                case StorageNetworkPanel.TrackingFilterMode.Current:
                case StorageNetworkPanel.TrackingFilterMode.All:
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Abnormal:
                    if (record.State != ProductionOrderState.Abnormal)
                    {
                        return false;
                    }
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Completed:
                    if (record.State != ProductionOrderState.Completed)
                    {
                        return false;
                    }
                    break;
                case StorageNetworkPanel.TrackingFilterMode.Running:
                    if (!IsActive(record))
                    {
                        return false;
                    }
                    break;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string needle = searchText.Trim();
            return ContainsIgnoreCase(record.DisplayId.ToString(), needle) ||
                   ContainsIgnoreCase(record.ProductName, needle) ||
                   ContainsIgnoreCase(record.AbnormalReason, needle) ||
                   (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>()).Any(assignment =>
                       assignment != null &&
                       (ContainsIgnoreCase(assignment.Fabricator != null ? assignment.Fabricator.GetProperName() : null, needle) ||
                        ContainsIgnoreCase(assignment.OutputName, needle) ||
                        ContainsIgnoreCase(assignment.ConsumerName, needle) ||
                        ContainsIgnoreCase(assignment.Recipe != null ? assignment.Recipe.GetUIName(false) : null, needle)));
        }

        public static bool IsActive(ProductionOrderRecord order)
        {
            return order.State != ProductionOrderState.Completed &&
                   order.State != ProductionOrderState.Abnormal &&
                   order.State != ProductionOrderState.Cancelled;
        }

        public static string GetSummaryLine(ProductionOrderRecord record)
        {
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_ORDER_SOURCE_BATCH), GetOrderSourceLabel(record), record.OrderCount);
        }

        public static string GetDetailLine(ProductionOrderRecord record)
        {
            if (record.State == ProductionOrderState.Abnormal && !string.IsNullOrEmpty(record.AbnormalReason))
            {
                return record.AbnormalReason;
            }

            int primaryMachines = (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && assignment.Primary)
                .Select(assignment => assignment.Fabricator)
                .Where(fabricator => fabricator != null)
                .Distinct()
                .Count();
            int materialMachines = (record.QueueAssignments ?? new List<ProductionOrderQueueAssignment>())
                .Where(assignment => assignment != null && !assignment.Primary)
                .Select(assignment => assignment.Fabricator)
                .Where(fabricator => fabricator != null)
                .Distinct()
                .Count();

            if (materialMachines > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_WAITING_MATERIALS), Mathf.Max(1, primaryMachines), materialMachines);
            }

            if (primaryMachines > 0)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MACHINES_RUNNING), primaryMachines);
            }

            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_CREATED), GetOrderStateLabel(record.State), ProductionOrderFormatting.FormatCycle(record.CreatedCycle));
        }

        public static string GetOrderSourceLabel(ProductionOrderRecord record)
        {
            return record.IsAutomatic ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SOURCE_KEEP) : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_SOURCE_MANUAL);
        }

        public static string GetOrderStateLabel(ProductionOrderState state)
        {
            switch (state)
            {
                case ProductionOrderState.Submitted:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_SUBMITTED);
                case ProductionOrderState.WaitingMaterials:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_WAITING);
                case ProductionOrderState.Producing:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_PRODUCING);
                case ProductionOrderState.Completed:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_COMPLETED);
                case ProductionOrderState.Abnormal:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_ABNORMAL);
                case ProductionOrderState.Cancelled:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_CANCELLED);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_STATE_TRACKING);
            }
        }

        public static string GetBuildingStateLabel(ProductionOrderQueueAssignment assignment)
        {
            switch (GetBuildingStateKind(assignment))
            {
                case BuildingStateKind.Running:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_RUNNING);
                case BuildingStateKind.WaitingMaterials:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_WAITING_MATERIALS);
                case BuildingStateKind.NoPower:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_NO_POWER);
                case BuildingStateKind.Disabled:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_DISABLED);
                case BuildingStateKind.NoRecipe:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_NO_RECIPE);
                case BuildingStateKind.Abnormal:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_ABNORMAL);
                default:
                    return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_QUEUED);
            }
        }

        public static string BuildBuildingDetailLine(ProductionOrderQueueAssignment assignment, float progress, string currentProductionState)
        {
            ComplexFabricator fabricator = assignment?.Fabricator;
            if (fabricator == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_MISSING);
            }

            if (fabricator.CurrentWorkingOrder == assignment.Recipe)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_PROGRESS), Mathf.Clamp01(progress));
            }

            return string.IsNullOrEmpty(currentProductionState) ? GetBuildingStateLabel(assignment) : currentProductionState;
        }

        public static string BuildBuildingQueueLine(ProductionOrderQueueAssignment assignment, string dispatchLabel, string autoDispatchLabel, string queuedText)
        {
            string assignmentLabel = assignment != null && assignment.Primary ? dispatchLabel : autoDispatchLabel;
            int orderCount = assignment != null ? assignment.OrderCount : 0;
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_QUEUE), assignmentLabel, orderCount, queuedText);
        }

        public static string BuildMaterialDetailLine(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_MISSING);
            }

            if (assignment != null && assignment.Primary)
            {
                return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_DETAIL_TARGET), GameUtil.GetFormattedMass(record.RequestedAmount));
            }

            string supplyName = string.IsNullOrEmpty(assignment?.ConsumerName) ? record.ProductName : assignment.ConsumerName;
            return string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_DETAIL_SUPPLY), supplyName);
        }

        public static string BuildLeaseSummary(ProductionOrderRecord record, ProductionOrderQueueAssignment assignment)
        {
            if (record == null || assignment == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_BUILDING_MISSING);
            }

            if (assignment.Primary)
            {
                int fabricatorId = assignment.Fabricator != null ? assignment.Fabricator.GetInstanceID() : 0;
                float leased = (record.OutputLeases ?? new List<ProductionOrderOutputLease>())
                    .Where(lease => lease != null && fabricatorId != 0 && lease.FabricatorInstanceId == fabricatorId)
                    .Sum(lease => lease.Amount);
                return leased > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_OUTPUT_RESERVED), GameUtil.GetFormattedMass(leased))
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MANUAL_DESC);
            }

            float materialLease = (record.MaterialLeases ?? new List<ProductionOrderMaterialLease>())
                .Where(lease => lease != null && lease.ConsumerName == assignment.ConsumerName)
                .Sum(lease => lease.Amount);
            return materialLease > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT
                ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRACKING_MATERIAL_DISPATCH), GameUtil.GetFormattedMass(materialLease))
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
        }

        public static BuildingStateKind GetBuildingStateKind(ProductionOrderQueueAssignment assignment)
        {
            ComplexFabricator fabricator = assignment?.Fabricator;
            if (fabricator == null)
            {
                return BuildingStateKind.Abnormal;
            }

            if (assignment.Recipe == null)
            {
                return BuildingStateKind.NoRecipe;
            }

            Operational operational = fabricator.GetComponent<Operational>();
            if (operational != null && !operational.IsOperational)
            {
                return BuildingStateKind.Disabled;
            }

            if (fabricator.CurrentWorkingOrder == assignment.Recipe)
            {
                return BuildingStateKind.Running;
            }

            int queued = fabricator.GetRecipeQueueCount(assignment.Recipe);
            if (queued != 0)
            {
                return BuildingStateKind.WaitingMaterials;
            }

            return BuildingStateKind.NoRecipe;
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack) &&
                   !string.IsNullOrEmpty(needle) &&
                   haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public enum BuildingStateKind
        {
            Queued,
            Running,
            WaitingMaterials,
            NoPower,
            Disabled,
            NoRecipe,
            Abnormal
        }
    }
}
