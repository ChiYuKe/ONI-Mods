using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using StorageNetwork.Buildings;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class AddTagPatches
    {
        // 存储箱
        [HarmonyPatch(typeof(StorageLockerConfig), nameof(StorageLockerConfig.ConfigureBuildingTemplate))]
        public static class StorageLockerConfigPatch
        {
            public static void Postfix(GameObject go)
            {
                go.AddTag(StorageNetworkTags.NetworkConnectable);
            }
        }

        // DropAllWorkable 组件常用于可清空内容物的建筑。所以我狠狠地给这种建筑打tag就对了
        [HarmonyPatch(typeof(DropAllWorkable), "OnPrefabInit")]
        public static class DropAllWorkablePatch
        {
            public static void Postfix(DropAllWorkable __instance)
            {
                if (__instance == null || __instance.gameObject == null)
                {
                    return;
                }

                KPrefabID prefabId = __instance.GetComponent<KPrefabID>();
                if (prefabId == null)
                {
                    return;
                }

                prefabId.AddTag(StorageNetworkTags.NetworkConnectable, false);
            }
        }
    }
}
