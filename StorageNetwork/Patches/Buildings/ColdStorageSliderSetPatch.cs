using HarmonyLib;
using StorageNetwork.Components;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class ColdStorageSliderSetPatch
    {
        [HarmonyPatch(typeof(SliderSet), nameof(SliderSet.SetTarget))]
        public static class SliderSetSetTargetPatch
        {
            public static void Postfix(SliderSet __instance, ISliderControl target)
            {
                if (!(target is StorageNetworkColdStorageCooling) || __instance?.numberInput == null)
                {
                    return;
                }

                __instance.numberInput.field.characterLimit = 4;
                RectTransform rect = __instance.numberInput.GetComponent<RectTransform>();
                if (rect != null)
                {
                    Vector2 size = rect.sizeDelta;
                    size.x = 64f;
                    rect.sizeDelta = size;
                }
            }
        }
    }
}
