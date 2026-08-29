using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 复制人身上的深渊状态组件：
    /// 记录下潜深度、施加上升负荷诅咒、晋升笛级、授予生骸化。
    /// 数据通过 [Serialize] 随存档持久化。
    /// </summary>
    public class AbyssTracker : KMonoBehaviour, ISim1000ms
    {
        /// <summary>本次下潜抵达的最深深度（米）。</summary>
        [Serialize]
        private float maxDepthThisDive;

        /// <summary>历史最深探索深度（米），决定笛级。</summary>
        [Serialize]
        private float maxDepthEver;

        /// <summary>当前笛级序号（0 = 无，1 = 红笛 … 5 = 白笛）。</summary>
        [Serialize]
        private int whistleRank;

        /// <summary>是否已从最终地诅咒中幸存并化为生骸。</summary>
        [Serialize]
        private bool isNarehate;

        /// <summary>是否正在经历最终地诅咒（结束后结算生骸化）。</summary>
        private bool narehatePending;

        private Klei.AI.Effects effects;
        private Health health;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            effects = GetComponent<Klei.AI.Effects>();
            health = GetComponent<Health>();
            // 兼容旧存档中可能越界的笛级序号。
            whistleRank = Mathf.Clamp(whistleRank, 0, AbyssStatics.Whistles.Length - 1);
            ReapplyPersistentState();
        }

        /// <summary>
        /// 读取存档后重新施加笛级与生骸的永久效果（永久效果本身不随存档保存，这里兜底）。
        /// </summary>
        private void ReapplyPersistentState()
        {
            if (effects == null)
                return;

            if (whistleRank >= 2)
            {
                string effectId = AbyssStatics.Whistles[whistleRank].EffectId;
                if (!string.IsNullOrEmpty(effectId) && !effects.HasEffect(effectId))
                    effects.Add(effectId, true);
            }
        }

        public void Sim1000ms(float dt)
        {
            AbyssConfig config = AbyssConfig.Instance;
            if (!config.EnableCurse && !config.EnableWhistle && !config.EnableNarehate)
                return;

            if (GetComponent<MinionIdentity>() == null)
                return;

            // 经由 DeathMonitor 死亡的复制人（如被诅咒夺命）Health.State 不会变为 Dead，
            // 因此还要检查死亡/濒死标签。
            if (health != null && health.State == Health.HealthState.Dead)
                return;
            if (gameObject.HasTag(GameTags.Dead) || gameObject.HasTag(GameTags.Dying))
                return;

            int worldId = this.GetMyWorldId();
            float y = transform.GetPosition().y;
            float depth = AbyssAnchors.GetDepthM(worldId, y);
            if (float.IsNaN(depth))
                return;

            UpdateWhistle(depth, config);

            if (config.EnableCurse)
                UpdateCurse(depth, config);

            if (config.EnableNarehate && narehatePending)
                UpdateNarehate();
        }

        // —— 上升负荷 ——

        private void UpdateCurse(float depth, AbyssConfig config)
        {
            if (depth > maxDepthThisDive)
                maxDepthThisDive = depth;

            // 下潜更深后再次上升：本次上升只结算一次（结算后把锚点重置为当前深度）。
            bool ascending = depth <= maxDepthThisDive - config.AscentTriggerM;
            int layerIndex = AbyssStatics.GetLayerIndex(maxDepthThisDive);
            if (ascending && layerIndex >= 0)
            {
                maxDepthThisDive = depth;
                ApplyCurse(layerIndex);
            }

            if (depth < config.SurfaceResetM)
                maxDepthThisDive = 0f;
        }

        private void ApplyCurse(int layerIndex)
        {
            if (effects == null)
                return;

            AbyssStatics.LayerDef layer = AbyssStatics.Layers[layerIndex];
            var curseEffect = Db.Get().effects.TryGet(layer.CurseEffectId);
            if (curseEffect == null)
                return;

            // 已化为生骸的复制人不受诅咒影响。
            if (isNarehate)
                return;
            if (effects.HasImmunityTo(curseEffect))
                return;

            effects.Add(layer.CurseEffectId, true);

            // 深层诅咒在施放时造成一次性伤害；足以致死的伤害由深渊直接夺命（专属死亡方式）。
            if (layerIndex < AbyssStatics.CurseInstantDamageHp.Length)
            {
                float instantDamage = AbyssStatics.CurseInstantDamageHp[layerIndex];
                if (instantDamage > 0f && health != null && health.State != Health.HealthState.Dead)
                {
                    DeathMonitor.Instance deathSmi = gameObject.GetSMI<DeathMonitor.Instance>();
                    if (instantDamage >= health.hitPoints && deathSmi != null && AbyssDeaths.Curse != null)
                        deathSmi.Kill(AbyssDeaths.Curse);
                    else
                        health.Damage(instantDamage);
                }
            }

            bool isFinalLayer = layerIndex == AbyssStatics.Layers.Length - 1;
            if (isFinalLayer)
            {
                if (AbyssConfig.Instance.EnableNarehate)
                    narehatePending = true;
                Notify(
                    NotificationType.Bad,
                    STRINGS.MISC.NOTIFICATIONS.ABYSS_CURSE_FINAL.NAME,
                    string.Format(STRINGS.MISC.NOTIFICATIONS.ABYSS_CURSE_FINAL.TOOLTIP, gameObject.GetProperName()));
            }

            Notify(
                NotificationType.BadMinor,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_CURSE.NAME,
                $"{gameObject.GetProperName()} 从深渊第 {layerIndex + 1} 层「{layer.Name}」上升，受到了诅咒：{curseEffect.Name}");
        }

        // —— 笛级 ——

        private void UpdateWhistle(float depth, AbyssConfig config)
        {
            if (depth > maxDepthEver)
                maxDepthEver = depth;

            if (!config.EnableWhistle)
                return;

            int newRank = AbyssStatics.GetWhistleRank(maxDepthEver);
            newRank = Mathf.Clamp(newRank, 0, AbyssStatics.Whistles.Length - 1);
            if (newRank <= whistleRank)
                return;

            whistleRank = newRank;
            AbyssStatics.WhistleDef whistle = AbyssStatics.Whistles[whistleRank];

            // 替换旧的笛级效果。
            for (int i = 2; i < AbyssStatics.Whistles.Length; i++)
            {
                string oldEffect = AbyssStatics.Whistles[i].EffectId;
                if (!string.IsNullOrEmpty(oldEffect) && oldEffect != whistle.EffectId && effects.HasEffect(oldEffect))
                    effects.Remove(oldEffect);
            }

            if (!string.IsNullOrEmpty(whistle.EffectId) && !effects.HasEffect(whistle.EffectId))
                effects.Add(whistle.EffectId, true);

            Notify(
                NotificationType.Good,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_WHISTLE_PROMOTE.NAME,
                $"{gameObject.GetProperName()} 的探窟深度达到 {Mathf.RoundToInt(maxDepthEver)} 米，晋升为{whistle.Name}探窟家！");
        }

        // —— 生骸化 ——

        private void UpdateNarehate()
        {
            if (effects == null)
                return;

            string finalCurse = AbyssStatics.CurseEffectIds[AbyssStatics.CurseEffectIds.Length - 1];
            if (effects.HasEffect(finalCurse))
                return;

            // 从最终地诅咒的持续影响中存活下来了，但深渊重塑了他们的血肉。
            narehatePending = false;
            isNarehate = true;
            TransformIntoNarehate();
        }

        private void TransformIntoNarehate()
        {
            string dupeName = gameObject.GetProperName();
            string critterId = PickNarehateCritter();
            string critterName = null;

            if (critterId != null)
            {
                GameObject prefab = Assets.GetPrefab(critterId);
                if (prefab != null)
                {
                    GameObject critter = GameUtil.KInstantiate(prefab, transform.GetPosition(), Grid.SceneLayer.Creatures, null, 0);
                    critter.SetActive(true);

                    // 生骸保留复制人的名字。
                    KSelectable selectable = critter.GetComponent<KSelectable>();
                    if (selectable != null)
                    {
                        critterName = $"生骸·{dupeName}";
                        selectable.SetName(critterName);
                    }
                }
            }

            Notify(
                NotificationType.Bad,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_NAREHATE.NAME,
                $"{dupeName} 从「最终地」的诅咒中幸存了下来，但深渊重塑了他们的血肉——化为了一只生骸{(critterName != null ? $"（{critterName}）" : "")}。");

            DeathMonitor.Instance deathSmi = gameObject.GetSMI<DeathMonitor.Instance>();
            if (deathSmi != null && AbyssDeaths.Narehate != null)
                deathSmi.Kill(AbyssDeaths.Narehate);
            else
                Debug.LogWarning("[MadeInAbyss] 未找到死亡监视器，生骸化只完成了.spawn 部分");
        }

        private static string PickNarehateCritter()
        {
            string[] ids = AbyssStatics.NarehateCritterIds;
            if (ids == null || ids.Length == 0)
                return null;

            int start = UnityEngine.Random.Range(0, ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                string id = ids[(start + i) % ids.Length];
                if (Assets.GetPrefab(id) != null)
                    return id;
            }
            return null;
        }

        private void Notify(NotificationType type, string title, string body)
        {
            Notifier notifier = gameObject.AddOrGet<Notifier>();
            notifier.Add(new Notification(
                title,
                type,
                (list, data) => body,
                gameObject), string.Empty);
        }
    }
}
