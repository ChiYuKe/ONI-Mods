using UnityEngine;

namespace WASDMinionControl
{
    [AddComponentMenu("KMonoBehaviour/scripts/WASDMinionController")]
    internal sealed class WASDMinionController : KMonoBehaviour
    {
        private GameObject manualTarget;

        private void Update()
        {
            GameObject target = GetControlledDuplicant();
            if (manualTarget != target)
            {
                SetManualTarget(manualTarget, false);
                ManualControlInput.Clear();
                manualTarget = target;
            }

            if (manualTarget == null)
            {
                return;
            }

            SetManualTarget(manualTarget, true);
        }

        private new void OnDestroy()
        {
            SetManualTarget(manualTarget, false);
            ManualControlInput.Clear();
            manualTarget = null;
        }

        private static void SetManualTarget(GameObject target, bool controlled)
        {
            if (target == null)
            {
                return;
            }

            ManualMinionControlStateMachine manualStateMachine = target.GetComponent<ManualMinionControlStateMachine>();
            if (controlled)
            {
                if (manualStateMachine == null)
                {
                    manualStateMachine = target.AddComponent<ManualMinionControlStateMachine>();
                }

                manualStateMachine.SetControlled(true);
                return;
            }

            if (manualStateMachine != null)
            {
                manualStateMachine.SetControlled(false);
            }
        }

        internal static GameObject GetControlledDuplicant()
        {
            return GetFollowCamDuplicant();
        }

        internal static GameObject GetFollowCamDuplicant()
        {
            CameraController camera = CameraController.Instance;
            GameObject followTarget = camera?.followTarget != null ? camera.followTarget.gameObject : null;
            return IsControllableDuplicant(followTarget) ? followTarget : null;
        }

        private static bool IsControllableDuplicant(GameObject target)
        {
            return target != null &&
                   target.GetComponent<MinionIdentity>() != null &&
                   target.GetComponent<Navigator>() != null;
        }
    }
}
