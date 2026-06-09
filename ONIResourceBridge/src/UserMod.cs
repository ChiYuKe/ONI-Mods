using HarmonyLib;
using KMod;
using UnityEngine;

namespace ONIResourceBridge
{
    public sealed class UserMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            MainThreadPump.EnsureCreated();
            ResourceBridgeServer.Start();
        }

        public override void OnAllModsLoaded(Harmony harmony, System.Collections.Generic.IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);
            MainThreadPump.EnsureCreated();
            ResourceBridgeServer.Start();
        }

        private sealed class MainThreadPump : MonoBehaviour
        {
            private static MainThreadPump instance;

            public static void EnsureCreated()
            {
                if (instance != null)
                {
                    return;
                }

                var host = new GameObject("ONIResourceBridge.MainThreadPump");
                Object.DontDestroyOnLoad(host);
                instance = host.AddComponent<MainThreadPump>();
            }

            private void Update()
            {
                ResourceBridgeServer.PumpMainThreadQueue();
            }
        }
    }
}
