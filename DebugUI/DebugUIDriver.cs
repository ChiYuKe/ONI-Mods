using UnityEngine;
using System.Reflection;

namespace DebugUI
{
    internal sealed class DebugUIDriver : MonoBehaviour
    {
        private static DebugUIDriver instance;
        private static readonly FieldInfo ShowImGuiField = typeof(DevToolManager).GetField("showImGui", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Ensure()
        {
            if (instance != null)
            {
                return;
            }

            GameObject host = new GameObject("DebugUIDriver");
            Object.DontDestroyOnLoad(host);
            instance = host.AddComponent<DebugUIDriver>();
            Debug.Log("[DebugUI] Driver created.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                OpenNativeDevTool<DebugUIDevTool>("DebugUI");
            }

            if (Input.GetKeyDown(KeyCode.F9) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            {
                OpenNativeDevTool<DebugUIEntityCaptureTool>("DebugUI Entity Capture");
            }
        }

        private static void OpenNativeDevTool<TTool>(string toolName) where TTool : DevTool, new()
        {
            DevToolManager manager = DevToolManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[DebugUI] DevToolManager is not ready.");
                return;
            }

            DebugHandler.SetDebugEnabled(true);
            manager.UserAcceptedWarning = true;
            ShowImGuiField?.SetValue(manager, true);
            manager.panels.AddOrGetDevTool<TTool>();
            Debug.Log("[DebugUI] Native " + toolName + " DevTool opened.");
        }
    }
}
