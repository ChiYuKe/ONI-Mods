namespace WASDMinionControl
{
    internal static class ManualControlInput
    {
        private static bool up;
        private static bool down;
        private static bool left;
        private static bool right;

        internal static void HandleKeyDown(KButtonEvent e)
        {
            SetPressedActions(e, true);
        }

        internal static void HandleKeyUp(KButtonEvent e)
        {
            SetPressedActions(e, false);
        }

        internal static bool TryGetDirection(out int directionX, out int directionY)
        {
            if (up)
            {
                directionX = 0;
                directionY = 1;
                return true;
            }

            if (down)
            {
                directionX = 0;
                directionY = -1;
                return true;
            }

            if (left)
            {
                directionX = -1;
                directionY = 0;
                return true;
            }

            if (right)
            {
                directionX = 1;
                directionY = 0;
                return true;
            }

            directionX = 0;
            directionY = 0;
            return false;
        }

        internal static void Clear()
        {
            up = false;
            down = false;
            left = false;
            right = false;
        }

        private static void SetPressedActions(KButtonEvent e, bool pressed)
        {
            if (e == null)
            {
                return;
            }

            if (e.IsAction(global::Action.PanUp))
            {
                up = pressed;
            }

            if (e.IsAction(global::Action.PanDown))
            {
                down = pressed;
            }

            if (e.IsAction(global::Action.PanLeft))
            {
                left = pressed;
            }

            if (e.IsAction(global::Action.PanRight))
            {
                right = pressed;
            }
        }
    }
}
