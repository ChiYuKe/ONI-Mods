using HarmonyLib;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 给所有复制人（含 DLC 生体人）挂上深渊状态组件。
    /// BasePrefabInit 在所有复制人模板创建时调用，组件随模板被所有实例继承。
    /// </summary>
    [HarmonyPatch(typeof(BaseMinionConfig), nameof(BaseMinionConfig.BasePrefabInit))]
    public static class BaseMinionConfig_BasePrefabInit_Patch
    {
        public static void Postfix(GameObject go)
        {
            go.AddOrGet<AbyssTracker>();
        }
    }
}
