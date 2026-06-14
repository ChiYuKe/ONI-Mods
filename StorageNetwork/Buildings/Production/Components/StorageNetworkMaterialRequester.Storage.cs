using System.Collections.Generic;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed partial class StorageNetworkMaterialRequester
    {
        private HashSet<Storage> BuildSourceExclusions()
        {
            HashSet<Storage> excluded = new HashSet<Storage>();
            foreach (Storage storage in GetFabricatorStorages())
            {
                if (storage != null)
                {
                    excluded.Add(storage);
                }
            }

            return excluded;
        }

        private static float GetAmountAvailable(Storage storage, Tag tag)
        {
            return storage != null ? storage.GetAmountAvailable(tag) : 0f;
        }

        internal static Tag GetStorageTransferTag(GameObject item)
        {
            return StorageItemUtility.GetStorageTransferTag(item);
        }

        internal static bool MatchesStorageTag(GameObject item, Tag tag)
        {
            return StorageItemUtility.MatchesStorageTag(item, tag);
        }

        private IEnumerable<Storage> GetFabricatorStorages()
        {
            if (fabricator == null)
            {
                yield break;
            }

            yield return fabricator.inStorage;
            yield return fabricator.buildStorage;
            yield return fabricator.outStorage;
        }

        internal static int GetStorageInstanceId(Storage storage)
        {
            return StorageItemUtility.GetStorageInstanceId(storage);
        }

        private static string GetTagDisplayName(Tag tag)
        {
            return StorageItemUtility.GetTagDisplayName(tag);
        }
    }
}
