using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Components;

namespace StorageNetwork.Core
{
    /// <summary>
    /// 储存网络的 Storage 规则集合。公开给其他模组复用，避免补丁作者复制一套不一致的判断。
    /// </summary>
    public static class StorageNetworkStorageRules
    {
        /// <summary>
        /// 判断 Storage 是否带有“外部模组储存建筑”适配标签。
        /// </summary>
        public static bool HasModStorageTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.ModStorage);
        }

        /// <summary>
        /// 判断 Storage 是否希望显示 StorageNetwork 设置按钮。
        /// </summary>
        public static bool HasSettingsButtonTag(Storage storage)
        {
            return storage != null &&
                   storage.GetComponent<Refrigerator>() == null &&
                   storage.GetComponent<Reservoir>() == null &&
                   HasTag(storage, StorageNetworkTags.ShowSettingsButton);
        }

        /// <summary>
        /// 判断 Storage 是否属于生产建筑内部储存。
        /// </summary>
        public static bool IsProductionStorage(Storage storage, StorageNetworkEnrollment enrollment = null)
        {
            return (enrollment != null && enrollment.IsComplexRecipeBuilding()) ||
                   storage?.GetComponent<ComplexFabricator>() != null;
        }

        /// <summary>
        /// 判断 Storage 是否能作为自动入网目标。
        /// </summary>
        public static bool IsNetworkStorageTarget(Storage storage, Storage ownerStorage = null)
        {
            return storage != null &&
                   storage != ownerStorage &&
                   storage.GetComponent<ComplexFabricator>() == null;
        }

        /// <summary>
        /// 获取当前网络中可作为目标箱子的 Storage 列表。
        /// </summary>
        public static List<Storage> GetNetworkStorageTargets(Storage ownerStorage)
        {
            return StorageSceneCollector.Collect().Storages
                .Select(info => info.Storage)
                .Where(storage => IsNetworkStorageTarget(storage, ownerStorage))
                .OrderBy(storage => storage.GetProperName())
                .ToList();
        }

        /// <summary>
        /// 判断给定 Storage 是否是 ComplexFabricator 的主输入储存，避免同一生产建筑被重复统计。
        /// </summary>
        public static bool IsPrimaryComplexFabricatorStorage(Storage storage)
        {
            ComplexFabricator fabricator = storage != null ? storage.GetComponent<ComplexFabricator>() : null;
            if (fabricator == null)
            {
                return false;
            }

            return fabricator.inStorage == null || fabricator.inStorage == storage;
        }

        private static bool HasTag(Storage storage, Tag tag)
        {
            return storage?.GetComponent<KPrefabID>()?.HasTag(tag) == true;
        }
    }
}
