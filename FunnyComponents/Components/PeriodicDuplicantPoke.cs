using System.Collections.Generic;
using Klei.AI;
using UnityEngine;

namespace FunnyComponents.Components
{
    [AddComponentMenu("KMonoBehaviour/scripts/PeriodicDuplicantPoke")]
    public sealed class PeriodicDuplicantPoke : KMonoBehaviour, ISim4000ms
    {
        private const float DamagePerPoke = 50f;
        private const float MinimumHealthToKeep = 1f;
        private const float RangeInCells = 5f;

        private MinionIdentity identity;
        private float secondsUntilNextPoke;
        private AttackChore activePlayFightChore;
        private GameObject activePlayFightTarget;
        private float activeTargetStartHealth;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            identity = GetComponent<MinionIdentity>();
            ResetTimer();
        }

        public void Sim4000ms(float dt)
        {
            secondsUntilNextPoke -= dt;
            if (secondsUntilNextPoke > 0f)
            {
                return;
            }

            TryStartAttackChore();
            ResetTimer();
        }

        private void TryStartAttackChore()
        {
            if (activePlayFightChore != null && activePlayFightChore.InProgress())
            {
                return;
            }

            GameObject target = FindNearbyTarget();
            if (target == null)
            {
                return;
            }

            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth == null || targetHealth.IsDefeated())
            {
                return;
            }

            activePlayFightTarget = target;
            activeTargetStartHealth = targetHealth.hitPoints;
            activePlayFightChore = new AttackChore(this, target);

            string attackerName = identity != null ? identity.GetProperName() : gameObject.GetProperName();
            string targetName = target.GetProperName();
            Debug.Log("[FunnyComponents] " + attackerName + " started a play fight with " + targetName + ".");
        }

        private void Update()
        {
            if (activePlayFightChore == null || activePlayFightTarget == null)
            {
                return;
            }

            Health targetHealth = activePlayFightTarget.GetComponent<Health>();
            if (targetHealth == null || !activePlayFightChore.IsValid())
            {
                ClearActivePlayFight();
                return;
            }

            float damageDone = activeTargetStartHealth - targetHealth.hitPoints;
            if (damageDone <= 0f)
            {
                return;
            }

            float allowedDamage = Mathf.Min(DamagePerPoke, activeTargetStartHealth - MinimumHealthToKeep);
            float excessDamage = damageDone - allowedDamage;
            if (excessDamage > 0f)
            {
                AmountInstance hp = targetHealth.GetAmountInstance;
                hp.ApplyDelta(excessDamage);
                damageDone = allowedDamage;
            }

            StopActivePlayFightAfterHit(damageDone);
        }

        private GameObject FindNearbyTarget()
        {
            List<GameObject> candidates = new List<GameObject>();
            Vector3 selfPosition = transform.GetPosition();
            int selfWorldId = gameObject.GetMyWorldId();
            float maxDistanceSquared = RangeInCells * RangeInCells;

            foreach (MinionIdentity otherIdentity in global::Components.LiveMinionIdentities.Items)
            {
                if (otherIdentity == null || otherIdentity.gameObject == gameObject)
                {
                    continue;
                }

                GameObject other = otherIdentity.gameObject;
                if (other == null || other.GetMyWorldId() != selfWorldId)
                {
                    continue;
                }

                KPrefabID prefabId = other.GetComponent<KPrefabID>();
                if (prefabId != null && (prefabId.HasTag(GameTags.Dead) || prefabId.HasTag(GameTags.Incapacitated)))
                {
                    continue;
                }

                Health health = other.GetComponent<Health>();
                if (health == null || health.IsDefeated() || health.hitPoints <= DamagePerPoke)
                {
                    continue;
                }

                Vector3 offset = other.transform.GetPosition() - selfPosition;
                if (offset.sqrMagnitude <= maxDistanceSquared)
                {
                    candidates.Add(other);
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private void StopActivePlayFightAfterHit(float damage)
        {
            ShowPokeText(activePlayFightTarget, damage);

            AttackChore choreToCancel = activePlayFightChore;
            ClearActivePlayFight();

            if (choreToCancel != null)
            {
                GameScheduler.Instance.Schedule("FunnyComponents_StopPlayFight", 0f, data =>
                {
                    AttackChore chore = data as AttackChore;
                    if (chore != null && chore.IsValid())
                    {
                        chore.Cancel("play fight landed one hit");
                    }
                }, choreToCancel);
            }
        }

        private void ClearActivePlayFight()
        {
            activePlayFightChore = null;
            activePlayFightTarget = null;
            activeTargetStartHealth = 0f;
        }

        private void ShowPokeText(GameObject target, float damage)
        {
            PopFXManager manager = PopFXManager.Instance;
            if (manager == null || !manager.Ready())
            {
                return;
            }

            manager.SpawnFX(
                manager.sprite_Negative,
                "-" + damage.ToString("0") + " HP",
                target.transform,
                new Vector3(0f, 1.8f, 0f),
                2f,
                true,
                false);
        }

        private void ResetTimer()
        {
            secondsUntilNextPoke = Random.Range(30f, 75f);
        }
    }
}
