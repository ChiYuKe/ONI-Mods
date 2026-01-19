using CykUtils;
using EternalDecay.Content.Config;
using HarmonyLib;
using Klei.AI;
using KSerialization;
using RunningOutOfTime.Content.Config;
using RunningOutOfTime.Content.Core;
using UnityEngine;

namespace RunningOutOfTime.Content.Components
{
    /// <summary>
    /// 复制人寿命监控组件：负责处理老龄化 Buff 注入及寿终正寝逻辑。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public class AgingMonitor : KMonoBehaviour, ISim4000ms
    {
        // --- 依赖注入 ---
        [MyCmpReq] private KPrefabID prefabID;
        [MyCmpGet] private Effects effects;

        // --- 存档持久化数据 ---
        [Serialize] public bool hasAppliedAgingEffects;
        [Serialize] private bool isLifeTerminated;


        // --- 核心逻辑入口 ---
        protected override void OnSpawn()
        {
            base.OnSpawn();
            AgingManager.Register(this); 
        }

        protected override void OnCleanUp()
        {
            AgingManager.Unregister(this); 
            base.OnCleanUp();
        }

        public void Sim4000ms(float dt)
        {
            if (isLifeTerminated) return;

            EvaluateBiologicalAge();
        }

        private void EvaluateBiologicalAge()
        {
            // 获取当前年龄（周期）
            AmountInstance ageInstance = Db.Get().Amounts.Get("AgeAttribute").Lookup(gameObject);
            if (ageInstance == null) return;

            float currentAge = ageInstance.value;
            float maxLifespan = TUNINGS.AGE.MINION_AGE_THRESHOLD;
            float senescencePoint = maxLifespan * TUNINGS.AGE.AGE_80PERCENT_THRESHOLD;

            // 死亡检查
            if (currentAge >= maxLifespan)
            {
                TerminateLife();
                
            }
            //进入衰老阶段 
            else if (currentAge >= senescencePoint && !hasAppliedAgingEffects)
            {
                ApplySenescence();
            }
        }

        /// <summary>
        /// 进入衰老阶段：注入 Buff、发送通知、播放特效。
        /// </summary>
        private void ApplySenescence()
        {
            hasAppliedAgingEffects = true;

            // 注入衰老 Buff
           //  KEffects.ApplyBuff(gameObject, "");

            // 全局 UI 通知
            EternalDecayMain.NotifyDeathApplied(gameObject);

        }

        /// <summary>
        /// 寿终正寝：处理死亡序列及遗物生成。
        /// </summary>
        private void TerminateLife()
        {
            isLifeTerminated = true;

            // 清理状态：移除衰老 Buff，准备死亡
            KEffects.RemoveBuff(gameObject,"");

            // 核心功能：生成新对象/数据转移
            InheritanceManager.TransferSoul(gameObject, transform.position);

            // 触发死亡序列
            var deathMonitor = this.GetSMI<DeathMonitor.Instance>();
            if (deathMonitor != null)
            {
                // 注入死亡标签：NoMourning 阻止基地压力爆发，DieOfOldAge 用于讣告分类
                prefabID.AddTag(EDGameTags.NoMourning);
                prefabID.AddTag(EDGameTags.DieOfOldAge);

                deathMonitor.Kill(Patches.DeathsPatch.KDeaths.Aging);
                AgingManager.Unregister(this);
            }
        }
    }
}