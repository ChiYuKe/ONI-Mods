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

        public static bool HasServerStorageTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.ServerStorage);
        }

        public static bool IsServerStorage(Storage storage)
        {
            return storage != null &&
                   HasModStorageTag(storage) &&
                   HasServerStorageTag(storage);
        }

        /// <summary>
        /// 判断储存网络服务器是否在线。外部模组只打 ModStorage 时按普通储存处理，不套用服务器掉线规则。
        /// </summary>
        public static bool IsModStorageOnline(Storage storage)
        {
            if (storage == null || !HasModStorageTag(storage) || !HasServerStorageTag(storage))
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
                   HasServerStorageTag(storage) &&
                   !IsModStorageOnline(storage);
        }

        /// <summary>
        /// 判断 Storage 是否能参与网络容量、来源和目标计算。断电的服务器会被显示，但不会接入网络。
        /// </summary>
        public static bool IsConnectedNetworkStorage(Storage storage)
        {
            return storage != null && IsModStorageOnline(storage);
        }

        public static bool IsNetworkPortStorage(Storage storage)
        {
            return HasInputPortTag(storage) || HasOutputPortTag(storage);
        }

        public static bool IsConfigurableMaterialPort(Storage storage)
        {
            return IsSolidInputPort(storage) || IsSolidOutputPort(storage);
        }

        public static bool IsConfigurablePort(Storage storage)
        {
            return IsNetworkPortStorage(storage);
        }

        public static bool HasInputPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryInputPort);
        }

        public static bool HasOutputPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryOutputPort);
        }

        public static bool HasSolidPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategorySolidPort);
        }

        public static bool HasLiquidPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryLiquidPort);
        }

        public static bool HasGasPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryGasPort);
        }

        public static bool HasPowerPortTag(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryPowerPort);
        }

        public static bool IsSolidInputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategorySolidInputPort);
        }

        public static bool IsSolidOutputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategorySolidOutputPort);
        }

        public static bool IsLiquidInputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryLiquidInputPort);
        }

        public static bool IsLiquidOutputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryLiquidOutputPort);
        }

        public static bool IsGasInputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryGasInputPort);
        }

        public static bool IsGasOutputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryGasOutputPort);
        }

        public static bool IsPowerInputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryPowerInputPort);
        }

        public static bool IsPowerOutputPort(Storage storage)
        {
            return HasTag(storage, StorageNetworkTags.CategoryPowerOutputPort);
        }

        public static bool IsPowerStorageServer(Storage storage)
        {
            return storage?.GetComponent<StorageNetworkPowerStorage>() != null;
        }

        public static bool CountsTowardNetworkCapacity(Storage storage)
        {
            return IsServerStorage(storage) && !IsPowerStorageServer(storage);
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
        /// 判断建筑是否启用了任意储存网络自动化入口或出口。
        /// </summary>
        public static bool IsNetworkAutomationEnabled(
            StorageNetworkMaterialRequester requester,
            StorageNetworkStorageConnector connector,
            StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (requester != null)
            {
                return requester.RequestEnabled || requester.OutputStoreEnabled;
            }

            if (connector != null)
            {
                return connector.IsOutputStoreEnabled();
            }

            if (energyRequester != null)
            {
                return energyRequester.RequestEnabled;
            }

            return false;
        }

        /// <summary>
        /// 判断 Storage 是否能作为自动入网目标。
        /// </summary>
        public static bool IsNetworkStorageTarget(Storage storage, Storage ownerStorage = null)
        {
            return storage != null &&
                   storage != ownerStorage &&
                   IsServerStorage(storage) &&
                   IsConnectedNetworkStorage(storage) &&
                   !IsPowerStorageServer(storage) &&
                   !IsNetworkPortStorage(storage) &&
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
            return GetNetworkStorageTargets(ownerStorage, null);
        }

        public static List<Storage> GetNetworkStorageTargets(Storage ownerStorage, IEnumerable<Tag> requiredFilters)
        {
            List<Storage> targets = new List<Storage>();
            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage storage = info?.Storage;
                if (info?.Minion == null &&
                    IsNetworkStorageTarget(storage, ownerStorage) &&
                    IsStorageCompatibleWithFilters(storage, requiredFilters))
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

        public static bool IsStorageCompatibleWithFilters(Storage storage, IEnumerable<Tag> requiredFilters)
        {
            if (requiredFilters == null)
            {
                return true;
            }

            HashSet<Tag> required = new HashSet<Tag>(requiredFilters.Where(tag => tag != Tag.Invalid));
            if (required.Count == 0)
            {
                return true;
            }

            return storage?.storageFilters != null &&
                   storage.storageFilters.Any(filter => required.Contains(filter));
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
