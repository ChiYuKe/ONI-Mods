using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ConstructionNetworkMaterialPatch
    {
        [HarmonyPatch(typeof(FetchList2), nameof(FetchList2.Submit))]
        public static class FetchListSubmitPatch
        {
            public static void Prefix(FetchList2 __instance)
            {
                if (!Config.Instance.HasAnyMinionAllowedRequestMaterialsFromNetwork())
                {
                    return;
                }

                try
                {
                    TransferConstructionMaterials(__instance);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to request construction materials from network: " + ex);
                }
            }
        }

        private static void TransferConstructionMaterials(FetchList2 fetchList)
        {
            Storage destination = GetFetchListDestination(fetchList);
            if (destination == null || destination.GetComponent<Constructable>() == null)
            {
                return;
            }

            foreach (object order in GetFetchOrders(fetchList))
            {
                List<Tag> tags = GetOrderTags(order);
                if (tags.Count == 0)
                {
                    continue;
                }

                float missing = GetOrderAmount(order) - GetAmountAvailable(destination, tags);
                if (missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                NetworkStorageTransferService.TransferFromNetworkToStorage(tags, missing, destination);
            }
        }

        private static Storage GetFetchListDestination(FetchList2 fetchList)
        {
            return GetMemberValue<Storage>(fetchList, "Destination", "destination", "Storage", "storage");
        }

        private static IEnumerable<object> GetFetchOrders(FetchList2 fetchList)
        {
            IEnumerable orders = GetMemberValue<IEnumerable>(fetchList, "FetchOrders", "fetchOrders", "orders", "Orders");
            if (orders == null)
            {
                yield break;
            }

            foreach (object order in orders)
            {
                if (order != null)
                {
                    yield return order;
                }
            }
        }

        private static List<Tag> GetOrderTags(object order)
        {
            List<Tag> tags = new List<Tag>();
            AddTags(tags, GetMemberValue<object>(order, "Tags", "tags", "MatchTags", "matchTags"));
            AddTags(tags, GetMemberValue<object>(order, "Tag", "tag", "MatchTag", "matchTag", "MatchID", "matchID"));
            return tags;
        }

        private static float GetOrderAmount(object order)
        {
            return GetMemberValue<float>(order, "TotalAmount", "totalAmount", "Amount", "amount", "OriginalAmount", "originalAmount");
        }

        private static float GetAmountAvailable(Storage storage, IEnumerable<Tag> tags)
        {
            float available = 0f;
            foreach (Tag tag in tags)
            {
                available = Mathf.Max(available, storage.GetAmountAvailable(tag));
            }

            return available;
        }

        private static void AddTags(List<Tag> tags, object value)
        {
            if (value == null)
            {
                return;
            }

            if (value is Tag tag)
            {
                AddTag(tags, tag);
                return;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is Tag itemTag)
                    {
                        AddTag(tags, itemTag);
                    }
                }
            }
        }

        private static void AddTag(List<Tag> tags, Tag tag)
        {
            if (tag != Tag.Invalid && !tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        private static T GetMemberValue<T>(object instance, params string[] names)
        {
            if (instance == null)
            {
                return default;
            }

            Type type = instance.GetType();
            foreach (string name in names)
            {
                FieldInfo field = AccessTools.Field(type, name);
                if (field != null)
                {
                    object value = field.GetValue(instance);
                    if (value is T typedFieldValue)
                    {
                        return typedFieldValue;
                    }
                }

                PropertyInfo property = AccessTools.Property(type, name);
                if (property != null)
                {
                    object value = property.GetValue(instance, null);
                    if (value is T typedPropertyValue)
                    {
                        return typedPropertyValue;
                    }
                }
            }

            return default;
        }
    }
}
