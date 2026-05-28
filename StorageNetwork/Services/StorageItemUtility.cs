using System.Linq;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal static class StorageItemUtility
    {
        /// <summary>
        /// 获取物品在储存网络中用于匹配过滤器和转运的 Tag。
        /// </summary>
        public static Tag GetStorageTransferTag(GameObject item)
        {
            KPrefabID prefabID = item != null ? item.GetComponent<KPrefabID>() : null;
            if (prefabID != null)
            {
                Tag prefabTag = prefabID.PrefabID();
                if (prefabTag != Tag.Invalid)
                {
                    return prefabTag;
                }
            }

            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            if (primaryElement != null)
            {
                Tag elementTag = primaryElement.ElementID.CreateTag();
                if (elementTag != Tag.Invalid && item.HasTag(elementTag))
                {
                    return elementTag;
                }
            }

            return prefabID != null ? prefabID.PrefabTag : Tag.Invalid;
        }

        public static bool MatchesStorageTag(GameObject item, Tag tag)
        {
            return item != null && tag != Tag.Invalid && item.HasTag(tag);
        }

        /// <summary>
        /// 获取物品质量。没有 PrimaryElement 的对象按 0 处理。
        /// </summary>
        public static float GetMass(GameObject item)
        {
            PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
            return primaryElement != null ? primaryElement.Mass : 0f;
        }

        /// <summary>
        /// 统计 Storage 当前直接持有物品的总质量。
        /// </summary>
        public static float GetStoredMass(Storage storage)
        {
            return storage != null && storage.items != null
                ? storage.items.Where(item => item != null).Sum(GetMass)
                : 0f;
        }

        /// <summary>
        /// 获取 Storage 对应 KPrefabID 的实例 ID，用于跨 UI 刷新保存选择目标。
        /// </summary>
        public static int GetStorageInstanceId(Storage storage)
        {
            KPrefabID prefabId = storage != null ? storage.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }

        /// <summary>
        /// 获取物品显示名。物品不存在时使用 Tag 名称作为回退。
        /// </summary>
        public static string GetItemDisplayName(GameObject item, Tag fallbackTag)
        {
            return item != null && !string.IsNullOrEmpty(item.GetProperName())
                ? item.GetProperName()
                : GetTagDisplayName(fallbackTag);
        }

        public static string GetTagDisplayName(Tag tag)
        {
            Element element = ElementLoader.FindElementByHash((SimHashes)tag.GetHash());
            if (element != null && !string.IsNullOrEmpty(element.name))
            {
                return element.name;
            }

            GameObject prefab = Assets.GetPrefab(tag);
            if (prefab != null)
            {
                return prefab.GetProperName();
            }

            string key = "STRINGS.MISC.TAGS." + tag.Name.ToUpperInvariant();
            if (Strings.TryGet(key, out StringEntry entry) && entry != null && !string.IsNullOrEmpty(entry.String))
            {
                return entry.String;
            }

            return tag.Name;
        }
    }
}
