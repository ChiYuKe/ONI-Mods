using System.Collections.Generic;
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
        /// 判断模组服务器是否在线。没有 ModStorage 标签的普通储存不受这个规则影响。
        /// </summary>
        public static bool IsModStorageOnline(Storage storage)
        {
            if (storage == null || !HasModStorageTag(storage))
            {
                return true;
            }

            Operational operational = storage.GetComponent<Operational>();
            return operational == null || operational.IsOperational;
        }

        /// <summary>
        /// 判断储存网络服务器是否离线。普通储存不属于服务器，始终不会被视为离线服务器。
        /// </summary>
        public static bool IsOfflineNetworkServer(StorageInfo storageInfo)
        {
            return storageInfo?.Storage != null && IsOfflineNetworkServer(storageInfo.Storage);
        }

        /// <summary>
        /// 判断储存网络服务器是否离线。普通储存不属于服务器，始终不会被视为离线服务器。
        /// </summary>
        public static bool IsOfflineNetworkServer(Storage storage)
        {
            return storage != null &&
                   HasModStorageTag(storage) &&
                   !IsModStorageOnline(storage);
        }

        /// <summary>
        /// 判断 Storage 是否能参与网络容量、来源和目标计算。断电的服务器会被显示，但不会接入网络。
        /// </summary>
        public static bool IsConnectedNetworkStorage(Storage storage)
        {
            return storage != null && IsModStorageOnline(storage);
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
                   (enrollment != null && enrollment.IsEnergyGeneratorBuilding()) ||
                   StorageNetworkEnergyGeneratorRequester.HasFuelInputs(storage?.GetComponent<EnergyGenerator>()) ||
                   storage?.GetComponent<ComplexFabricator>() != null;
        }

        /// <summary>
        /// 判断 Storage 是否能作为自动入网目标。
        /// </summary>
        public static bool IsNetworkStorageTarget(Storage storage, Storage ownerStorage = null)
        {
            return storage != null &&
                   storage != ownerStorage &&
                   IsConnectedNetworkStorage(storage) &&
                   !IsMinionStorage(storage) &&
                   !IsProductionStorage(storage);
        }

        /// <summary>
        /// 判断 Storage 是否属于复制人的随身储存。复制人可显示为分类，但不能作为入网/转移目标。
        /// </summary>
        public static bool IsMinionStorage(Storage storage)
        {
            return storage?.GetComponent<MinionIdentity>() != null;
        }

        /// <summary>
        /// 获取当前网络中可作为目标箱子的 Storage 列表。
        /// </summary>
        public static List<Storage> GetNetworkStorageTargets(Storage ownerStorage)
        {
            List<Storage> targets = new List<Storage>();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage storage = info?.Storage;
                if (info?.Minion == null && IsNetworkStorageTarget(storage, ownerStorage))
                {
                    targets.Add(storage);
                }
            }

            targets.Sort((left, right) => string.Compare(
                left != null ? left.GetProperName() : string.Empty,
                right != null ? right.GetProperName() : string.Empty,
                System.StringComparison.CurrentCulture));
            return targets;
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
