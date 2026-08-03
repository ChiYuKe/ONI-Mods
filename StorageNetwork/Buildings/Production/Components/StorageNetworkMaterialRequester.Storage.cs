using System.Collections.Generic;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed partial class StorageNetworkMaterialRequester
    {
        private HashSet<Storage> BuildSourceExclusions()
        {
            HashSet<Storage> excluded = fabricatorStorageExclusions;
            excluded.Clear();
            if (fabricator != null)
            {
                AddStorageIfPresent(excluded, fabricator.inStorage);
                AddStorageIfPresent(excluded, fabricator.buildStorage);
                AddStorageIfPresent(excluded, fabricator.outStorage);
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

        private HashSet<Storage> GetFabricatorStorages()
        {
            return BuildSourceExclusions();
        }

        private static void AddStorageIfPresent(
            HashSet<Storage> storages,
            Storage storage)
        {
            if (storage != null)
            {
                storages.Add(storage);
            }
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
