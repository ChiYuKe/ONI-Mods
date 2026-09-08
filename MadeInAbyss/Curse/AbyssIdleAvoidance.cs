using HarmonyLib;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 闲逛避让：复制人无事闲逛时，选址不会选择深渊深层。
    /// 只影响闲逛选址（IdleCellQuery 远距离选址 / IdleStates.MoveCellQuery 近距离挪动），
    /// 任务寻路完全不受影响——玩家明确指派的下潜工作照常执行。
    /// 闲逛拿不到目标格时复制人会在原地闲逛（原版 IdleChore 对此安全），不会卡死。
    /// </summary>
    /// <remarks>
    /// 避让深度按是否装备探窟弹药包区分：
    /// - 有弹药包：可闲逛到第 1~2 层（弹药包常驻免疫浅层负荷），第 3 层及更深避让
    ///   （避让层阶 = AbyssConfig.IdleAvoidMinLayerIndex，默认 2）。
    /// - 无弹药包：连第 1~2 层也避让，只在表面对流层闲逛（避让层阶 = 0）。
    ///
    /// 实现要点（避免破坏原版查询语义）：
    /// - IdleCellQuery.IsMatch 返回的是“搜索终止”信号（cost 超限即停），targetCell 是副作用。
    ///   深层格跳过原方法、不写入 targetCell，同时复刻 cost 终止条件。
    /// - MoveCellQuery.IsMatch 每匹配一格就递减 maxIterations，减到 0 才终止。
    ///   跳过原方法时必须手动递减并复刻返回值，否则迭代计数失效、搜索失控。
    /// - PathFinder.Run → FindPaths 在 Navigator.RunQuery 调用栈内主线程同步执行，
    ///   因此 RunQuery 补丁算出的有效避让层阶在 IsMatch 回调期间是安全、确定的。
    /// </remarks>
    public static class AbyssIdleAvoidance
    {
        /// <summary>
        /// 当前查询的有效避让层阶（<see cref="AbyssStatics.GetLayerIndex"/> 返回值下限）。
        /// 由 <see cref="Navigator.RunQuery"/> 补丁按查询发起者（复制人）是否装备弹药包计算，
        /// 与两个闲逛查询的 IsMatch 主线程同步使用；负值表示本次不启用避让。
        /// </summary>
        private static int effectiveAvoidLayerIndex = -1;

        /// <summary>层阈值单槽缓存：一次闲逛查询内的所有格子都在同一世界，避免逐格分配数组。</summary>
        private static int cachedWorldId = -1;
        private static float[] cachedThresholds;

        private static float[] GetLayerThresholdsCached(int worldId)
        {
            if (cachedWorldId != worldId)
            {
                cachedWorldId = worldId;
                cachedThresholds = AbyssStatics.GetLayerThresholds(worldId);
            }
            return cachedThresholds;
        }

        /// <summary>闲逛选址是否把某格视为"深层"（应避让）。</summary>
        public static bool IsDeepIdleCell(int cell)
        {
            if (effectiveAvoidLayerIndex < 0)
                return false; // 本次查询不启用避让

            if (!Grid.IsValidCell(cell))
                return false;

            int worldId = Grid.WorldIdx[cell];
            Grid.CellToXY(cell, out _, out int y);
            float depth = AbyssAnchors.GetDepthM(worldId, y);
            if (float.IsNaN(depth))
                return false;

            int layer = AbyssStatics.GetLayerIndex(depth, GetLayerThresholdsCached(worldId));
            return layer >= effectiveAvoidLayerIndex;
        }

        /// <summary>查询发起者（复制人）是否装备了探窟弹药包。</summary>
        private static bool HasAmmoPouch(GameObject dupe)
        {
            if (dupe == null)
                return false;

            MinionIdentity identity = dupe.GetComponent<MinionIdentity>();
            Equipment equipment = identity != null && identity.assignableProxy != null
                ? identity.assignableProxy.Get().GetComponent<Equipment>()
                : dupe.GetComponent<Equipment>();
            EquipmentSlot pouchSlot = Db.Get().AssignableSlots.TryGet(AmmoPouch.SlotId) as EquipmentSlot;
            if (equipment == null || pouchSlot == null)
                return false;

            AssignableSlotInstance slotInstance = equipment.GetSlot(pouchSlot);
            Equippable equipped = slotInstance != null ? slotInstance.assignable as Equippable : null;
            if (equipped == null)
                return false;
            return equipped.GetComponent<KPrefabID>().PrefabTag.Name == AmmoPouch.ItemId;
        }

        /// <summary>
        /// 在所有寻路查询的共同入口计算本次查询的有效避让层阶。
        /// 仅对复制人（MinionIdentity）发起的两个闲逛查询计算——
        /// 小动物等其余实体与小动物闲逛查询保持原版行为，也不给全局寻路加开销。
        /// </summary>
        [HarmonyPatch(typeof(Navigator), "RunQuery")]
        public static class Navigator_RunQuery_Patch
        {
            public static void Prefix(Navigator __instance, PathFinderQuery query)
            {
                if (!(query is IdleCellQuery) && !(query is IdleStates.MoveCellQuery))
                    return;
                if (__instance.GetComponent<MinionIdentity>() == null)
                {
                    effectiveAvoidLayerIndex = -1;
                    return;
                }

                AbyssConfig config = AbyssConfig.Instance;
                if (config == null || config.IdleAvoidMinLayerIndex < 0)
                {
                    effectiveAvoidLayerIndex = -1;
                    return;
                }

                bool hasPouch = HasAmmoPouch(__instance.gameObject);
                // 无弹药包：连第 1 层（下标 0）也避让，只在地表闲逛。
                effectiveAvoidLayerIndex = hasPouch ? config.IdleAvoidMinLayerIndex : 0;
            }

            public static void Postfix()
            {
                effectiveAvoidLayerIndex = -1;
            }
        }

        /// <summary>远距离闲逛选址：深层格子不作为闲逛目标。</summary>
        [HarmonyPatch(typeof(IdleCellQuery), "IsMatch")]
        public static class IdleCellQuery_IsMatch_Patch
        {
            public static bool Prefix(IdleCellQuery __instance, int cell, int parent_cell, int cost, ref bool __result)
            {
                if (!IsDeepIdleCell(cell))
                    return true; // 浅层：走原逻辑

                // 深层：不接受为目标，并复刻原方法的搜索终止条件（cost 超限即停）。
                __result = cost > Traverse.Create(__instance).Field("maxCost").GetValue<int>();
                return false;
            }
        }

        /// <summary>近距离随机挪动：也不跨入深层。</summary>
        [HarmonyPatch(typeof(IdleStates.MoveCellQuery), "IsMatch")]
        public static class MoveCellQuery_IsMatch_Patch
        {
            public static bool Prefix(IdleStates.MoveCellQuery __instance, int cell, int parent_cell, int cost, ref bool __result)
            {
                if (!IsDeepIdleCell(cell))
                    return true; // 浅层：走原逻辑

                // 深层：不设为目标格，但保留原方法的迭代计数与终止语义——
                // 否则 maxIterations 永不递减，搜索会越过 5~25 次限制无限扩展（复制人乱走）。
                int remaining = Traverse.Create(__instance).Field("maxIterations").GetValue<int>() - 1;
                Traverse.Create(__instance).Field("maxIterations").SetValue(remaining);
                __result = remaining <= 0;
                return false;
            }
        }
    }
}
