using UnityEngine;

namespace TestMod
{
    public static class UiPrefabToolkit
    {
        private const string BaseScreenId = "cyk_base_screen";

        private static UiPrefabScreen currentScreen;
        private static string currentScreenId;

        public static UiPrefabScreen CurrentScreen => currentScreen;

        public static bool IsOpen(string screenId = null)
        {
            if (currentScreen == null)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(screenId) || currentScreenId == screenId;
        }

        public static bool IsBaseScreenOpen()
        {
            return IsOpen(BaseScreenId);
        }

        public static UiPrefabScreen OpenBaseScreen(string title = "AB UI 工具包", System.Action<UiPrefabScreen> onCreated = null)
        {
            return Open(new BaseScreenOptions(title), onCreated);
        }

        public static UiPrefabScreen OpenBaseScreen(BaseScreenOptions options, System.Action<UiPrefabScreen> onCreated = null)
        {
            return Open(options ?? new BaseScreenOptions(), onCreated);
        }

        public static UiPrefabScreen Open(UiPrefabWindowOptions definition, System.Action<UiPrefabScreen> onCreated = null)
        {
            if (definition == null)
            {
                Debug.LogWarning("[UiPrefabToolkit] Definition is null.");
                return null;
            }

            Close();

            Transform parent = UiPrefabToolkitHost.GetDefaultParent();
            if (parent == null)
            {
                Debug.LogWarning("[UiPrefabToolkit] Could not find a default parent.");
                return null;
            }

            GameObject prefab = UiPrefabAssetBundleLoader.LoadPrefab(definition.BundleNames, definition.PrefabNames, out string bundleName, out string prefabName);
            if (prefab == null)
            {
                Debug.LogWarning("[UiPrefabToolkit] Could not load requested prefab.");
                return null;
            }

            GameObject instance = Util.KInstantiateUI(prefab, parent.gameObject, true);
            instance.name = "UiPrefabScreen_" + (string.IsNullOrWhiteSpace(definition.Id) ? "Screen" : definition.Id);

            UiPrefabScreen screen = new UiPrefabScreen(instance);
            if (definition.CenterOnScreen)
            {
                screen.CenterOnScreen();
            }

            if (definition.Size.HasValue)
            {
                Vector2 size = definition.Size.Value;
                screen.SetSize(size.x, size.y);
            }

            if (!string.IsNullOrWhiteSpace(definition.Title))
            {
                screen.SetHeaderText(definition.Title);
            }

            if (definition.AutoBindClose)
            {
                screen.BindClose(Close);
            }

            currentScreen = screen;
            currentScreenId = definition.Id;

            Debug.Log("[UiPrefabToolkit] Opened screen. Bundle=" + bundleName + ", Prefab=" + prefabName);
            onCreated?.Invoke(screen);
            return screen;
        }

        public static void Close()
        {
            if (currentScreen != null)
            {
                currentScreen.Destroy();
                currentScreen = null;
                currentScreenId = null;
            }
        }
    }
}
