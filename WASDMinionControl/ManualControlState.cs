using System.Collections.Generic;
using UnityEngine;

namespace WASDMinionControl
{
    internal static class ManualControlState
    {
        private static readonly HashSet<int> ActiveInstanceIds = new HashSet<int>();
        private static readonly Dictionary<int, int> AuthorizedNavigationDepthByInstanceId = new Dictionary<int, int>();
        private static readonly HashSet<int> PendingManualAdvanceInstanceIds = new HashSet<int>();

        internal static void Activate(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            KPrefabID prefabId = target.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                ActiveInstanceIds.Add(prefabId.InstanceID);
            }
        }

        internal static bool IsActive(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            KPrefabID prefabId = target.GetComponent<KPrefabID>();
            if (prefabId == null)
            {
                return false;
            }

            return ActiveInstanceIds.Contains(prefabId.InstanceID);
        }

        internal static void Clear(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            KPrefabID prefabId = target.GetComponent<KPrefabID>();
            if (prefabId != null)
            {
                ActiveInstanceIds.Remove(prefabId.InstanceID);
                AuthorizedNavigationDepthByInstanceId.Remove(prefabId.InstanceID);
                PendingManualAdvanceInstanceIds.Remove(prefabId.InstanceID);
            }
        }

        internal static void ClearAll()
        {
            ActiveInstanceIds.Clear();
            AuthorizedNavigationDepthByInstanceId.Clear();
            PendingManualAdvanceInstanceIds.Clear();
        }

        internal static void MarkManualTransition(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            if (prefabId != null)
            {
                PendingManualAdvanceInstanceIds.Add(prefabId.InstanceID);
            }
        }

        internal static bool ConsumeManualTransition(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            return prefabId != null && PendingManualAdvanceInstanceIds.Remove(prefabId.InstanceID);
        }

        internal static AuthorizedNavigationScope AllowNavigation(GameObject target)
        {
            KPrefabID prefabId = target != null ? target.GetComponent<KPrefabID>() : null;
            if (prefabId == null)
            {
                return new AuthorizedNavigationScope(-1);
            }

            AuthorizedNavigationDepthByInstanceId.TryGetValue(prefabId.InstanceID, out int depth);
            AuthorizedNavigationDepthByInstanceId[prefabId.InstanceID] = depth + 1;
            return new AuthorizedNavigationScope(prefabId.InstanceID);
        }

        internal static bool IsNavigationAllowed(GameObject target)
        {
            if (!IsActive(target))
            {
                return true;
            }

            KPrefabID prefabId = target.GetComponent<KPrefabID>();
            return prefabId != null &&
                   AuthorizedNavigationDepthByInstanceId.TryGetValue(prefabId.InstanceID, out int depth) &&
                   depth > 0;
        }

        internal readonly struct AuthorizedNavigationScope : System.IDisposable
        {
            private readonly int instanceId;

            internal AuthorizedNavigationScope(int instanceId)
            {
                this.instanceId = instanceId;
            }

            public void Dispose()
            {
                if (instanceId != -1)
                {
                    if (!AuthorizedNavigationDepthByInstanceId.TryGetValue(instanceId, out int depth) || depth <= 1)
                    {
                        AuthorizedNavigationDepthByInstanceId.Remove(instanceId);
                    }
                    else
                    {
                        AuthorizedNavigationDepthByInstanceId[instanceId] = depth - 1;
                    }
                }
            }
        }
    }
}
