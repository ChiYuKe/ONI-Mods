using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace StorageNetwork.ProductionOrders
{
    internal static class ProductionOrderPersistence
    {
        private const int Version = 2;
        private const string Extension = ".StorageNetworkOrders.json";

        public static string GetStorePath()
        {
            string savePath = SaveLoader.GetActiveSaveFilePath();
            return string.IsNullOrEmpty(savePath) ? null : savePath + Extension;
        }

        public static void Save(IReadOnlyCollection<ProductionOrderRecord> orders, IReadOnlyCollection<ProductionKeepRule> keepRules = null)
        {
            string path = GetStorePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                ProductionOrderSaveData data = new ProductionOrderSaveData
                {
                    version = Version,
                    orders = (orders ?? Array.Empty<ProductionOrderRecord>())
                        .Where(order => order != null)
                        .Select(ToSaveRecord)
                        .ToList(),
                    keepRules = (keepRules ?? Array.Empty<ProductionKeepRule>())
                        .Where(rule => rule != null)
                        .Select(ToKeepRuleSaveRecord)
                        .ToList()
                };
                File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to save production orders: " + ex);
            }
        }

        public static List<ProductionOrderRecord> Load()
        {
            string path = GetStorePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new List<ProductionOrderRecord>();
            }

            try
            {
                ProductionOrderSaveData data = JsonConvert.DeserializeObject<ProductionOrderSaveData>(File.ReadAllText(path));
                if (data == null || data.orders == null)
                {
                    return new List<ProductionOrderRecord>();
                }

                return data.orders
                    .Select(FromSaveRecord)
                    .Where(order => order != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load production orders: " + ex);
                return new List<ProductionOrderRecord>();
            }
        }

        public static List<ProductionKeepRule> LoadKeepRules()
        {
            string path = GetStorePath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return new List<ProductionKeepRule>();
            }

            try
            {
                ProductionOrderSaveData data = JsonConvert.DeserializeObject<ProductionOrderSaveData>(File.ReadAllText(path));
                return (data != null ? data.keepRules : null) == null
                    ? new List<ProductionKeepRule>()
                    : data.keepRules
                        .Select(FromKeepRuleSaveRecord)
                        .Where(rule => rule != null)
                        .ToList();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[StorageNetwork] Failed to load production keep rules: " + ex);
                return new List<ProductionKeepRule>();
            }
        }

        private static ProductionOrderSaveRecord ToSaveRecord(ProductionOrderRecord order)
        {
            return new ProductionOrderSaveRecord
            {
                key = order.Key,
                displayId = order.DisplayId,
                productTag = order.ProductTag.Name,
                productName = order.ProductName,
                recipeKey = order.RecipeKey,
                requestedAmount = order.RequestedAmount,
                lastSubmittedAmount = order.LastSubmittedAmount,
                orderCount = order.OrderCount,
                stockAtSubmit = order.StockAtSubmit,
                allocationOffsetAtSubmit = order.AllocationOffsetAtSubmit,
                producedAtSubmit = order.ProducedAtSubmit,
                reservedMaterials = order.ReservedMaterials
                    .Select(pair => new ProductionOrderMaterialSaveRecord
                    {
                        tag = pair.Key.Name,
                        amount = pair.Value
                    })
                    .ToList(),
                queueAssignments = order.QueueAssignments
                    .Where(assignment => assignment != null && assignment.Fabricator != null && assignment.Recipe != null)
                    .Select(assignment => new ProductionOrderQueueSaveRecord
                    {
                        fabricatorInstanceId = GetInstanceId(assignment.Fabricator),
                        recipeId = assignment.Recipe.id,
                        orderCount = assignment.OrderCount,
                        outputTag = assignment.OutputTag == Tag.Invalid ? string.Empty : assignment.OutputTag.Name,
                        outputName = assignment.OutputName,
                        consumerName = assignment.ConsumerName,
                        primary = assignment.Primary
                    })
                    .ToList(),
                materialLeases = order.MaterialLeases
                    .Select(lease => new ProductionOrderMaterialLeaseSaveRecord
                    {
                        tag = lease.Material.Name,
                        amount = lease.Amount,
                        sourceStorageInstanceId = lease.SourceStorageInstanceId,
                        consumerName = lease.ConsumerName
                    })
                    .ToList(),
                outputLeases = order.OutputLeases
                    .Select(lease => new ProductionOrderOutputLeaseSaveRecord
                    {
                        productTag = lease.ProductTag.Name,
                        amount = lease.Amount,
                        fabricatorInstanceId = lease.FabricatorInstanceId,
                        producerName = lease.ProducerName
                    })
                    .ToList(),
                createdCycle = order.CreatedCycle,
                completedCycle = order.CompletedCycle,
                lastActivityCycle = order.LastActivityCycle,
                lastObservedProducedAmount = order.LastObservedProducedAmount,
                lastObservedQueueLoad = order.LastObservedQueueLoad,
                abnormalReason = order.AbnormalReason,
                mergeCount = order.MergeCount,
                isAutomatic = order.IsAutomatic,
                state = (int)order.State
            };
        }

        private static ProductionKeepRuleSaveRecord ToKeepRuleSaveRecord(ProductionKeepRule rule)
        {
            return new ProductionKeepRuleSaveRecord
            {
                productTag = rule.ProductTag.Name,
                productName = rule.ProductName,
                recipeKey = rule.RecipeKey,
                targetAmount = rule.TargetAmount
            };
        }

        private static ProductionOrderRecord FromSaveRecord(ProductionOrderSaveRecord saved)
        {
            if (saved == null || string.IsNullOrEmpty(saved.key))
            {
                return null;
            }

            Dictionary<Tag, float> reservedMaterials = (saved.reservedMaterials ?? new List<ProductionOrderMaterialSaveRecord>())
                .Where(material => material != null && !string.IsNullOrEmpty(material.tag))
                .GroupBy(material => material.tag)
                .ToDictionary(group => group.Key.ToTag(), group => group.Sum(material => material.amount));
            List<ProductionOrderQueueAssignment> assignments = (saved.queueAssignments ?? new List<ProductionOrderQueueSaveRecord>())
                .Select(FromQueueRecord)
                .Where(assignment => assignment != null)
                .ToList();
            List<ProductionOrderMaterialLease> materialLeases = (saved.materialLeases ?? new List<ProductionOrderMaterialLeaseSaveRecord>())
                .Where(lease => lease != null && !string.IsNullOrEmpty(lease.tag) && lease.amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Select(lease => new ProductionOrderMaterialLease(lease.tag.ToTag(), lease.amount, lease.sourceStorageInstanceId, lease.consumerName))
                .ToList();
            List<ProductionOrderOutputLease> outputLeases = (saved.outputLeases ?? new List<ProductionOrderOutputLeaseSaveRecord>())
                .Where(lease => lease != null && !string.IsNullOrEmpty(lease.productTag) && lease.amount > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .Select(lease => new ProductionOrderOutputLease(lease.productTag.ToTag(), lease.amount, lease.fabricatorInstanceId, lease.producerName))
                .ToList();

            return new ProductionOrderRecord(
                saved.key,
                saved.displayId,
                saved.productTag.ToTag(),
                saved.productName,
                saved.recipeKey,
                saved.requestedAmount,
                saved.lastSubmittedAmount,
                saved.orderCount,
                saved.stockAtSubmit,
                saved.allocationOffsetAtSubmit,
                saved.producedAtSubmit,
                reservedMaterials,
                assignments,
                materialLeases,
                outputLeases,
                saved.createdCycle,
                saved.completedCycle,
                saved.lastActivityCycle,
                saved.lastObservedProducedAmount,
                saved.lastObservedQueueLoad,
                saved.abnormalReason,
                saved.mergeCount,
                (ProductionOrderState)Mathf.Clamp(saved.state, 0, (int)ProductionOrderState.Cancelled),
                saved.isAutomatic);
        }

        private static ProductionKeepRule FromKeepRuleSaveRecord(ProductionKeepRuleSaveRecord saved)
        {
            if (saved == null || string.IsNullOrEmpty(saved.productTag) || saved.targetAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return null;
            }

            return new ProductionKeepRule(saved.productTag.ToTag(), saved.productName, saved.recipeKey, saved.targetAmount);
        }

        private static ProductionOrderQueueAssignment FromQueueRecord(ProductionOrderQueueSaveRecord saved)
        {
            if (saved == null || saved.orderCount <= 0 || string.IsNullOrEmpty(saved.recipeId))
            {
                return null;
            }

            ComplexFabricator fabricator = FindFabricator(saved.fabricatorInstanceId);
            ComplexRecipe recipe = ComplexRecipeManager.Get().GetRecipe(saved.recipeId);
            return fabricator != null && recipe != null
                ? new ProductionOrderQueueAssignment(
                    fabricator,
                    recipe,
                    saved.orderCount,
                    string.IsNullOrEmpty(saved.outputTag) ? Tag.Invalid : saved.outputTag.ToTag(),
                    saved.outputName,
                    saved.consumerName,
                    saved.primary)
                : null;
        }

        private static ComplexFabricator FindFabricator(int instanceId)
        {
            if (instanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            return global::Components.ComplexFabricators.Items
                .FirstOrDefault(fabricator => fabricator != null && GetInstanceId(fabricator) == instanceId);
        }

        private static int GetInstanceId(Component component)
        {
            KPrefabID prefabId = component != null ? component.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        [Serializable]
        private sealed class ProductionOrderSaveData
        {
            public int version;
            public List<ProductionOrderSaveRecord> orders = new List<ProductionOrderSaveRecord>();
            public List<ProductionKeepRuleSaveRecord> keepRules = new List<ProductionKeepRuleSaveRecord>();
        }

        [Serializable]
        private sealed class ProductionOrderSaveRecord
        {
            public string key;
            public int displayId;
            public string productTag;
            public string productName;
            public string recipeKey;
            public float requestedAmount;
            public float lastSubmittedAmount;
            public int orderCount;
            public float stockAtSubmit;
            public float allocationOffsetAtSubmit;
            public float producedAtSubmit;
            public List<ProductionOrderMaterialSaveRecord> reservedMaterials = new List<ProductionOrderMaterialSaveRecord>();
            public List<ProductionOrderQueueSaveRecord> queueAssignments = new List<ProductionOrderQueueSaveRecord>();
            public List<ProductionOrderMaterialLeaseSaveRecord> materialLeases = new List<ProductionOrderMaterialLeaseSaveRecord>();
            public List<ProductionOrderOutputLeaseSaveRecord> outputLeases = new List<ProductionOrderOutputLeaseSaveRecord>();
            public float createdCycle;
            public float completedCycle;
            public float lastActivityCycle;
            public float lastObservedProducedAmount;
            public float lastObservedQueueLoad;
            public string abnormalReason;
            public int mergeCount;
            public bool isAutomatic;
            public int state;
        }

        [Serializable]
        private sealed class ProductionKeepRuleSaveRecord
        {
            public string productTag;
            public string productName;
            public string recipeKey;
            public float targetAmount;
        }

        [Serializable]
        private sealed class ProductionOrderMaterialSaveRecord
        {
            public string tag;
            public float amount;
        }

        [Serializable]
        private sealed class ProductionOrderQueueSaveRecord
        {
            public int fabricatorInstanceId;
            public string recipeId;
            public int orderCount;
            public string outputTag;
            public string outputName;
            public string consumerName;
            public bool primary;
        }

        [Serializable]
        private sealed class ProductionOrderMaterialLeaseSaveRecord
        {
            public string tag;
            public float amount;
            public int sourceStorageInstanceId;
            public string consumerName;
        }

        [Serializable]
        private sealed class ProductionOrderOutputLeaseSaveRecord
        {
            public string productTag;
            public float amount;
            public int fabricatorInstanceId;
            public string producerName;
        }
    }
}
