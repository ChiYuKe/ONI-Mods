using StorageNetwork.API;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Core
{
    /// <summary>
    /// 储存网络成员判断入口。UI、转运逻辑和外部补丁都应优先复用这里，避免各处判断条件漂移。
    /// </summary>
    public static class StorageNetworkMembership
    {
        /// <summary>
        /// 判断目标对象是否已经属于储存网络，并给出便于日志排查的原因。
        /// </summary>
        public static bool IsNetworkMember(GameObject candidate, out string reason)
        {
            if (candidate == null)
            {
                reason = "no selection";
                return false;
            }

            KPrefabID prefabId = candidate.GetComponent<KPrefabID>();
            if (prefabId != null && prefabId.HasTag(StorageNetworkTags.ModStorage))
            {
                reason = "mod storage tag";
                return true;
            }

            StorageNetworkEnrollment enrollment = candidate.GetComponent<StorageNetworkEnrollment>();
            if (enrollment != null && enrollment.IncludedInSceneNetwork)
            {
                reason = "included in network";
                return true;
            }

            if (enrollment != null)
            {
                reason = "not included in network";
                return false;
            }

            reason = "not StorageNetwork object";
            return false;
        }

        /// <summary>
        /// 判断指定 Storage 是否应该被场景收集器纳入网络快照。
        /// </summary>
        public static bool IsCollectableStorage(Storage storage)
        {
            if (!StorageSceneRegistry.IsLive(storage))
            {
                return false;
            }

            if (!IsNetworkMember(storage.gameObject, out _))
            {
                return false;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            if (enrollment == null)
            {
                return true;
            }

            if (enrollment.IsStorageLocker())
            {
                return true;
            }

            if (enrollment.IsComplexRecipeBuilding())
            {
                return StorageNetworkStorageRules.IsPrimaryComplexFabricatorStorage(storage);
            }

            return true;
        }
    }
}
