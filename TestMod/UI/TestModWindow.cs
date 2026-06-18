using UnityEngine;

namespace TestMod
{
    internal static class TestModWindow
    {
        public static bool IsOpen => UiPrefabToolkit.IsBaseScreenOpen();

        public static void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Show();
        }

        public static void Show()
        {
            UiPrefabScreen screen = UiPrefabToolkit.OpenBaseScreen(
                new BaseScreenOptions("AB UI 工具包")
                    .SetSize(1900f, 664f),
                null);

            if (screen == null)
            {
                Debug.LogWarning("[TestMod] Failed to open default AB screen.");
            }

            TestModToggleButton.UpdateState();
        }

        public static void Close()
        {
            UiPrefabToolkit.Close();
            TestModToggleButton.UpdateState();
        }
    }
}
