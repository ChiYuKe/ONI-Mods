using HarmonyLib;
using UnityEngine;

namespace MiniBox.BuildingConfig.StorageLocker
{
    internal class StorageLockerPatch
    {
        [HarmonyPatch(typeof(StorageLockerConfig), "DoPostConfigureComplete")]
        public class StorageLockerCapacityPatch
        {
            private static void Postfix(GameObject go)
            {
                go.AddOrGet<Storage>().capacityKg = ModSettings.Current.StorageLockerCapacityKg;
            }
        }
    }
}





