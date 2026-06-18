using UnityEngine;

namespace TestMod
{
    public static class UiPrefabToolkitHost
    {
        public static Transform GetDefaultParent()
        {
            if (GameScreenManager.Instance?.ssOverlayCanvas != null)
            {
                return GameScreenManager.Instance.ssOverlayCanvas.transform;
            }

            if (FrontEndManager.Instance != null)
            {
                return FrontEndManager.Instance.gameObject.transform;
            }

            return null;
        }
    }
}
