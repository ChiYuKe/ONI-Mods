using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using StorageNetwork.UI;

namespace StorageNetwork.Patches
{
    public static class StorageNetworkHotkeyPatch
    {
        public const global::Action Action = global::Action.ManageDiagnostics;

        [HarmonyPatch(typeof(Global), nameof(Global.GenerateDefaultBindings))]
        public static class GlobalGenerateDefaultBindingsPatch
        {
            public static void Postfix(ref BindingEntry[] __result)
            {
                if (__result == null || __result.Any(entry => entry.mAction == Action))
                {
                    return;
                }

                List<BindingEntry> bindings = new List<BindingEntry>(__result)
                {
                    new BindingEntry("Management", GamepadButton.NumButtons, KKeyCode.Q, Modifier.None, Action, true, false)
                };
                __result = bindings.ToArray();
            }
        }

        public static bool TryConsume(KButtonEvent e)
        {
            if (e == null || StorageNetworkPanel.IsTextInputFocused() || !e.TryConsume(Action))
            {
                return false;
            }

            KMonoBehaviour.PlaySound(GlobalAssets.GetSound("HUD_Click", false));
            StorageNetworkPanel.Toggle();
            return true;
        }
    }
}
