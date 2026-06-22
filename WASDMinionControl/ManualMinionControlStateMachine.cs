using UnityEngine;

namespace WASDMinionControl
{
    [AddComponentMenu("KMonoBehaviour/scripts/ManualMinionControlStateMachine")]
    internal sealed class ManualMinionControlStateMachine : KMonoBehaviour
    {
        private const float RepeatDelaySeconds = 0.08f;

        private enum ManualState
        {
            Disabled,
            Idle,
            Moving
        }

        private ManualState state = ManualState.Disabled;
        private float nextMoveTime;

        internal void SetControlled(bool controlled)
        {
            if (controlled)
            {
                if (state == ManualState.Disabled)
                {
                    state = ManualState.Idle;
                }

                ManualControlState.Activate(gameObject);
                return;
            }

            if (state != ManualState.Disabled)
            {
                Navigator navigator = GetComponent<Navigator>();
                if (navigator != null && navigator.transitionDriver.GetTransition == null)
                {
                    PlayIdle(navigator);
                }

                state = ManualState.Disabled;
                ManualControlState.Clear(gameObject);
            }
        }

        private void Update()
        {
            if (state == ManualState.Disabled)
            {
                return;
            }

            if (WASDMinionInputGuards.IsInputFieldFocused())
            {
                return;
            }

            Navigator navigator = GetComponent<Navigator>();
            if (navigator == null)
            {
                return;
            }

            ManualControlState.Activate(gameObject);
            StopCurrentChore();

            if (navigator.IsMoving() && navigator.transitionDriver.GetTransition != null)
            {
                state = ManualState.Moving;
                return;
            }

            if (navigator.IsMoving())
            {
                FinishManualMove(navigator);
            }

            if (!ManualControlInput.TryGetDirection(out int directionX, out int directionY))
            {
                if (state == ManualState.Moving)
                {
                    PlayIdle(navigator);
                }

                state = ManualState.Idle;
                return;
            }

            state = ManualState.Idle;
            if (Time.unscaledTime < nextMoveTime)
            {
                return;
            }

            if (!TryMountVerticalNavigation(navigator, directionY))
            {
                return;
            }

            if (!TryGetNextTransition(navigator, directionX, directionY, out NavGrid.Transition transition))
            {
                return;
            }

            using (ManualControlState.AllowNavigation(gameObject))
            {
                ManualControlState.MarkManualTransition(gameObject);
                navigator.BeginTransition(transition);
            }

            state = ManualState.Moving;
            nextMoveTime = Time.unscaledTime + RepeatDelaySeconds;
        }

        private bool TryMountVerticalNavigation(Navigator navigator, int directionY)
        {
            if (directionY == 0)
            {
                return true;
            }

            int cell = Grid.PosToCell(navigator.transform.GetPosition());
            if (navigator.NavGrid.NavTable.IsValid(cell, NavType.Ladder))
            {
                MountNavType(navigator, cell, NavType.Ladder);
                return true;
            }

            if (navigator.NavGrid.NavTable.IsValid(cell, NavType.Pole))
            {
                MountNavType(navigator, cell, NavType.Pole);
                return true;
            }

            if (directionY < 0)
            {
                int belowCell = Grid.CellBelow(cell);
                if (navigator.NavGrid.NavTable.IsValid(belowCell, NavType.Ladder))
                {
                    MountNavType(navigator, belowCell, NavType.Ladder);
                    return true;
                }

                if (navigator.NavGrid.NavTable.IsValid(belowCell, NavType.Pole))
                {
                    MountNavType(navigator, belowCell, NavType.Pole);
                    return true;
                }
            }

            return false;
        }

        private static bool IsVerticalNavType(NavType navType)
        {
            return navType == NavType.Ladder || navType == NavType.Pole;
        }

        private static bool IsAllowedVerticalTransition(Navigator navigator, NavGrid.Transition transition, int linkCell)
        {
            if (!IsVerticalNavType(transition.start))
            {
                return false;
            }

            if (transition.start == transition.end)
            {
                return true;
            }

            if (transition.end != NavType.Floor || navigator?.NavGrid == null)
            {
                return false;
            }

            return IsStableFloorCell(linkCell) &&
                   !navigator.NavGrid.NavTable.IsValid(linkCell, transition.start);
        }

        private static bool IsStableFloorCell(int cell)
        {
            int belowCell = Grid.CellBelow(cell);
            if (!Grid.IsValidCell(cell) || !Grid.IsValidCell(belowCell))
            {
                return false;
            }

            return Grid.FakeFloor[belowCell] ||
                   (Grid.Solid[belowCell] && !Grid.DupePassable[belowCell]);
        }

        private static bool IsSolidCell(int cell)
        {
            return Grid.IsValidCell(cell) && Grid.Solid[cell];
        }

        private static bool WouldEmbedInSolid(int linkCell)
        {
            return IsSolidCell(linkCell);
        }

        private void MountNavType(Navigator navigator, int cell, NavType navType)
        {
            if (navigator.CurrentNavType == navType)
            {
                return;
            }

            using (ManualControlState.AllowNavigation(gameObject))
            {
                navigator.Stop(false, false);
                navigator.SetCurrentNavType(navType);
                navigator.transform.SetPosition(Grid.CellToPosCBC(cell, Grid.SceneLayer.Move));
            }
        }

        internal static void FinishManualMove(Navigator navigator)
        {
            if (navigator?.smi == null)
            {
                return;
            }

            navigator.smi.sm.moveTarget.Set(null, navigator.smi);
            navigator.smi.GoTo(navigator.smi.sm.normal.stopped);
        }

        private static void PlayIdle(Navigator navigator)
        {
            if (navigator?.animController == null || navigator.NavGrid == null)
            {
                return;
            }

            HashedString idleAnim = navigator.NavGrid.GetIdleAnim(navigator.CurrentNavType);
            navigator.animController.Play(idleAnim, KAnim.PlayMode.Loop, 1f, 0f);
        }

        private bool TryGetNextTransition(Navigator navigator, int directionX, int directionY, out NavGrid.Transition transition)
        {
            int originCell = Grid.PosToCell(navigator.transform.GetPosition());
            int bestTransitionId = -1;
            int bestScore = int.MaxValue;
            int linkIndex = originCell * navigator.NavGrid.maxLinksPerCell;
            int maxLinkIndex = linkIndex + navigator.NavGrid.maxLinksPerCell;

            for (; linkIndex < maxLinkIndex; linkIndex++)
            {
                NavGrid.Link link = navigator.NavGrid.Links[linkIndex];
                if (link.link == Grid.InvalidCell)
                {
                    break;
                }

                if (link.startNavType != navigator.CurrentNavType || !Grid.IsValidCell(link.link))
                {
                    continue;
                }

                Grid.CellToXY(originCell, out int originX, out int originY);
                Grid.CellToXY(link.link, out int linkX, out int linkY);
                int deltaX = linkX - originX;
                int deltaY = linkY - originY;
                if (!IsInPressedDirection(deltaX, deltaY, directionX, directionY))
                {
                    continue;
                }

                if (WouldEmbedInSolid(link.link))
                {
                    continue;
                }

                if (!TryGetPathFirstTransition(navigator, originCell, directionY, link.link, out NavGrid.Transition pathTransition))
                {
                    continue;
                }

                int score = GetDirectionalScore(deltaX, deltaY, directionX, directionY, link.cost);
                score += GetNavTypePenalty(pathTransition, directionY);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTransitionId = pathTransition.id;
                }
            }

            if (bestTransitionId < 0)
            {
                transition = default(NavGrid.Transition);
                return false;
            }

            transition = navigator.NavGrid.transitions[bestTransitionId];
            return true;
        }

        private static bool TryGetPathFirstTransition(Navigator navigator, int originCell, int directionY, int targetCell, out NavGrid.Transition transition)
        {
            transition = default(NavGrid.Transition);

            PathFinder.PotentialPath potentialPath = new PathFinder.PotentialPath(originCell, navigator.CurrentNavType, navigator.flags);
            PathFinder.Path path = default(PathFinder.Path);
            PathFinder.UpdatePath(navigator.NavGrid, navigator.GetCurrentAbilities(), potentialPath, PathFinderQueries.cellQuery.Reset(targetCell), ref path);
            if (!path.IsValid())
            {
                return false;
            }

            NavGrid.Transition candidateTransition = navigator.NavGrid.transitions[(int)path.nodes[1].transitionId];
            if (directionY != 0 && !IsAllowedVerticalTransition(navigator, candidateTransition, path.nodes[1].cell))
            {
                return false;
            }

            if (WouldEmbedInSolid(path.nodes[1].cell))
            {
                return false;
            }

            transition = candidateTransition;
            return true;
        }

        private static bool IsInPressedDirection(int deltaX, int deltaY, int directionX, int directionY)
        {
            if (directionX != 0)
            {
                return deltaX * directionX > 0 && Mathf.Abs(deltaX) >= Mathf.Abs(deltaY);
            }

            return deltaY * directionY > 0 && Mathf.Abs(deltaY) >= Mathf.Abs(deltaX);
        }

        private static int GetDirectionalScore(int deltaX, int deltaY, int directionX, int directionY, byte navCost)
        {
            int forwardDistance = Mathf.Abs(directionX != 0 ? deltaX : deltaY);
            int sidewaysDistance = Mathf.Abs(directionX != 0 ? deltaY : deltaX);
            return forwardDistance * 100 + sidewaysDistance * 25 + navCost;
        }

        private static int GetNavTypePenalty(NavGrid.Transition transition, int directionY)
        {
            if (directionY == 0)
            {
                return 0;
            }

            if (transition.start == NavType.Ladder && transition.end == NavType.Ladder)
            {
                return -1000;
            }

            if (transition.start == NavType.Pole && transition.end == NavType.Pole)
            {
                return -1000;
            }

            if (transition.end == NavType.Floor)
            {
                return 300;
            }

            return 0;
        }

        private void StopCurrentChore()
        {
            ChoreDriver choreDriver = GetComponent<ChoreDriver>();
            if (choreDriver?.GetCurrentChore() != null)
            {
                choreDriver.StopChore();
            }
        }
    }
}
