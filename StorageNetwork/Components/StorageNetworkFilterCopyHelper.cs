using System.Collections.Generic;
using UnityEngine;

namespace StorageNetwork.Components
{
    internal static class StorageNetworkFilterCopyHelper
    {
        public static void CopyFilters(GameObject targetObject, GameObject sourceObject)
        {
            if (targetObject == null || sourceObject == null || targetObject == sourceObject)
            {
                return;
            }

            TreeFilterable target = targetObject.GetComponent<TreeFilterable>();
            TreeFilterable source = sourceObject.GetComponent<TreeFilterable>();
            if (target == null || source == null)
            {
                return;
            }

            target.UpdateFilters(new HashSet<Tag>(source.GetTags()));
            StorageNetworkFilterState.Ensure(target)?.MarkUserConfigured();
        }
    }
}
