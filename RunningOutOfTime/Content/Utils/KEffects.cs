using UnityEngine;
using Klei.AI;

namespace CykUtils
{
    public static class KEffects
    {
        /// <summary>
        /// 安全地为目标添加 Buff。
        /// </summary>
        public static void ApplyBuff(GameObject go, string effectId)
        {
            if (go == null) return;

            // 使用 AddOrGet 确保不会因为缺少组件而报错，或者直接 GetComponent
            Effects effects = go.GetComponent<Effects>();
            if (effects != null && !effects.HasEffect(effectId))
            {
                effects.Add(effectId, should_save: true);
            }
        }

        /// <summary>
        /// 安全地移除目标 Buff。
        /// </summary>
        public static void RemoveBuff(GameObject go, string effectId)
        {
            if (go == null) return;

            Effects effects = go.GetComponent<Effects>();
            if (effects != null && effects.HasEffect(effectId))
            {
                effects.Remove(effectId);
            }
        }

        /// <summary>
        /// 获取 Buff 剩余时间（秒）。
        /// 返回 -1 表示 Buff 不存在或永久存在。
        /// </summary>
        public static float GetRemainingTime(GameObject go, string effectId)
        {
            if (go == null) return -1f;

            Effects effects = go.GetComponent<Effects>();
            if (effects == null) return -1f;

            EffectInstance instance = effects.Get(effectId);
            return instance?.timeRemaining ?? -1f; // 使用 C# 语法糖简洁判空
        }

        /// <summary>
        /// 检查目标是否有特定的 Buff。
        /// </summary>
        public static bool HasBuff(GameObject go, string effectId)
        {
            if (go == null) return false;
            Effects effects = go.GetComponent<Effects>();
            return effects != null && effects.HasEffect(effectId);
        }
    }
}