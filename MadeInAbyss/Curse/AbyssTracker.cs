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

        /// <summary>本次上升是否已结算过诅咒（避免连续爬升重复触发）。</summary>
        [Serialize]
        private bool curseConsumedThisDive;

        private Klei.AI.Effects effects;
        private Health health;

        /// <summary>诊断用：上次打印深度的层阶。</summary>
        private int lastLoggedLayer = -1;

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
            float[] thresholds = AbyssStatics.GetLayerThresholds(worldId);
            if (thresholds == null)
                return;

            float y = transform.GetPosition().y;
            float depth = AbyssAnchors.GetDepthM(worldId, y);
            if (float.IsNaN(depth))
                return;

            UpdateWhistle(worldId, depth, config);

            if (config.EnableCurse)
                UpdateCurse(worldId, depth, thresholds, config);
        }

        // —— 上升负荷 ——

        private void UpdateCurse(int worldId, float depth, float[] thresholds, AbyssConfig config)
        {
            // 下潜到比之前更深的位置时重置结算标记：一段连续上升只触发一次诅咒。
            if (depth > maxDepthThisDive)
            {
                maxDepthThisDive = depth;
                curseConsumedThisDive = false;
            }

            // 诊断：首次进入某个层阶深度时打印一次。
            int depthLayer = AbyssStatics.GetLayerIndex(maxDepthThisDive, thresholds);
            if (depthLayer != lastLoggedLayer && depthLayer >= 0)
            {
                Debug.Log($"[MadeInAbyss] {gameObject.GetProperName()} 抵达深度 {Mathf.RoundToInt(maxDepthThisDive)}m（第 {depthLayer + 1} 层，已结算={curseConsumedThisDive}）");
                lastLoggedLayer = depthLayer;
            }
            if (depth < config.SurfaceResetM)
                lastLoggedLayer = -1;

            bool ascending = depth <= maxDepthThisDive - config.AscentTriggerM;
            int layerIndex = AbyssStatics.GetLayerIndex(maxDepthThisDive, thresholds);
            if (ascending && !curseConsumedThisDive && layerIndex >= 0)
            {
                curseConsumedThisDive = true;
                ApplyCurse(layerIndex);
            }

            if (depth < config.SurfaceResetM)
            {
                maxDepthThisDive = 0f;
                curseConsumedThisDive = false;
            }
        }

        private void ApplyCurse(int layerIndex)
        {
            if (effects == null)
                return;

            AbyssStatics.LayerDef layer = AbyssStatics.Layers[layerIndex];
            var curseEffect = Db.Get().effects.TryGet(layer.CurseEffectId);
            if (curseEffect == null)
                return;

            bool isFinalLayer = layerIndex == AbyssStatics.Layers.Length - 1;
            bool hasImmunity = effects.HasImmunityTo(curseEffect);

            // 深层边界上升（4→3、5→4、6→5）：弹药包挡下一次诅咒——
            // 扣 30 点伤害、包损坏掉落、减益不上身。
            if (layerIndex >= 3 && TryGetEquippedPouch(out Equippable deepPouch))
            {
                Debug.Log($"[MadeInAbyss] {gameObject.GetProperName()} 从第 {layerIndex + 1} 层上升，诅咒被弹药包挡下（{layerIndex + 1}→{layerIndex}）");
                ConsumeAmmoPouchCharge(deepPouch);
                DealCurseDamage(AbyssStatics.FinalLayerPouchBlockedDamage);
                return;
            }

            // 第 1~2 层：弹药包提供常驻免疫，不消耗、不减益。
            if (hasImmunity)
            {
                Debug.Log($"[MadeInAbyss] {gameObject.GetProperName()} 的 {curseEffect.Name} 被免疫挡下");
                return;
            }

            Debug.Log($"[MadeInAbyss] {gameObject.GetProperName()} 触发诅咒：层={layerIndex + 1} 深度={Mathf.RoundToInt(maxDepthThisDive)}m 免疫={hasImmunity}");

            effects.Add(layer.CurseEffectId, true);

            // 最终地：没有弹药包则受到 80 点伤害，幸存者立即被深渊重塑为生骸，伤害致死则正常死亡。
            if (isFinalLayer)
            {
                bool survived = DealCurseDamage(AbyssStatics.CurseInstantDamageHp[layerIndex]);
                if (survived && AbyssConfig.Instance.EnableNarehate)
                    TransformIntoNarehate();
                return;
            }

            // 其余层阶的诅咒在施放时造成一次性伤害；足以致死的伤害由深渊直接夺命（专属死亡方式）。
            if (layerIndex < AbyssStatics.CurseInstantDamageHp.Length)
                DealCurseDamage(AbyssStatics.CurseInstantDamageHp[layerIndex]);

            Notify(
                NotificationType.BadMinor,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_CURSE.NAME,
                $"{gameObject.GetProperName()} 从深渊第 {layerIndex + 1} 层「{layer.Name}」上升，受到了诅咒：{curseEffect.Name}");
        }

        /// <summary>
        /// 结算诅咒的一次性伤害；足以致死时以「深渊的诅咒」直接夺命。
        /// 返回复制人是否仍然存活。
        /// </summary>
        private bool DealCurseDamage(float amount)
        {
            if (health == null || health.State == Health.HealthState.Dead || amount <= 0f)
                return health != null && health.State != Health.HealthState.Dead;

            if (amount >= health.hitPoints)
            {
                DeathMonitor.Instance deathSmi = gameObject.GetSMI<DeathMonitor.Instance>();
                if (deathSmi != null && AbyssDeaths.Curse != null)
                {
                    deathSmi.Kill(AbyssDeaths.Curse);
                    return false;
                }
            }
            health.Damage(amount);
            return health.State != Health.HealthState.Dead && !gameObject.HasTag(GameTags.Dead);
        }

        /// <summary>弹药包卡槽上是否装着探窟弹药包。注意 Equipment 挂在复制人的代理对象上。</summary>
        private bool TryGetEquippedPouch(out Equippable pouch)
        {
            pouch = null;
            MinionIdentity identity = GetComponent<MinionIdentity>();
            Equipment equipment = identity != null && identity.assignableProxy != null
                ? identity.assignableProxy.Get().GetComponent<Equipment>()
                : GetComponent<Equipment>();
            EquipmentSlot pouchSlot = Db.Get().AssignableSlots.TryGet(AmmoPouch.SlotId) as EquipmentSlot;
            if (equipment == null || pouchSlot == null)
            {
                Debug.LogWarning("[MadeInAbyss] 查找弹药包失败：Equipment 组件或卡槽不存在");
                return false;
            }

            AssignableSlotInstance slotInstance = equipment.GetSlot(pouchSlot);
            Equippable equipped = slotInstance != null ? slotInstance.assignable as Equippable : null;
            if (equipped == null)
                return false;
            if (equipped.GetComponent<KPrefabID>().PrefabTag.Name != AmmoPouch.ItemId)
                return false;

            pouch = equipped;
            return true;
        }

        // —— 笛级 ——

        private void UpdateWhistle(int worldId, float depth, AbyssConfig config)
        {
            if (depth > maxDepthEver)
                maxDepthEver = depth;

            if (!config.EnableWhistle)
                return;

            int newRank = AbyssStatics.GetWhistleRank(worldId, maxDepthEver);
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
                    Debug.Log($"[MadeInAbyss] {dupeName} 化为生骸：{critterId}");
                }
            }
            else
            {
                Debug.LogWarning("[MadeInAbyss] 没有找到可用的生骸动物 prefab，本次只执行死亡结算");
            }

            Notify(
                NotificationType.Bad,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_NAREHATE.NAME,
                $"{dupeName} 从「最终地」上升，被深渊重塑了血肉——化为了一只生骸{(critterName != null ? $"（{critterName}）" : "")}。");

            DeathMonitor.Instance deathSmi = gameObject.GetSMI<DeathMonitor.Instance>();
            if (deathSmi != null && AbyssDeaths.Narehate != null)
                deathSmi.Kill(AbyssDeaths.Narehate);
            else
                Debug.LogWarning("[MadeInAbyss] 未找到死亡监视器，生骸化只完成了 spawn 部分");
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

        /// <summary>
        /// 弹药包抵挡诅咒后损坏：卸下并销毁原装备，原地掉落损坏的弹药包。
        /// </summary>
        private void ConsumeAmmoPouchCharge(Equippable pouch)
        {
            if (pouch == null)
                return;

            string dupeName = gameObject.GetProperName();
            Vector3 position = transform.GetPosition();

            pouch.Unassign();
            Util.KDestroyGameObject(pouch.gameObject);

            GameObject prefab = Assets.GetPrefab(AmmoPouchDamagedConfig.ID);
            if (prefab != null)
            {
                GameUtil.KInstantiate(prefab, position, Grid.SceneLayer.Ore, null, 0).SetActive(true);
                Debug.Log($"[MadeInAbyss] 弹药包抵挡诅咒后损坏并掉落（{dupeName}）");
            }
            else
            {
                Debug.LogWarning("[MadeInAbyss] 未注册的实体 Ammo_Pouch_Damaged，无法生成损坏的弹药包");
            }

            Notify(
                NotificationType.BadMinor,
                STRINGS.MISC.NOTIFICATIONS.ABYSS_POUCH_CONSUMED.NAME,
                $"{dupeName} 的探窟弹药包抵挡了一次上升负荷，随后损坏掉落在地，可以缝补修复。");
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
