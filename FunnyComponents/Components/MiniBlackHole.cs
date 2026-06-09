using System.Collections.Generic;
using UnityEngine;

namespace FunnyComponents.Components
{
    [AddComponentMenu("KMonoBehaviour/scripts/MiniBlackHole")]
    public sealed class MiniBlackHole : KMonoBehaviour, ISim4000ms
    {
        // 黑洞触发的最短间隔，单位是秒。
        private const float CooldownMin = 45f;
        // 黑洞触发的最长间隔，单位是秒。
        private const float CooldownMax = 90f;
        // 单次黑洞持续时间，单位是秒。
        private const float ActiveDuration = 5f;
        // 黑洞影响半径，单位是格子。
        private const int RadiusCells = 6;
        // 物品被吸向黑洞中心的力度。
        private const float PullStrength = 5.5f;
        // 生物被吸向黑洞中心的力度。
        private const float CreaturePullStrength = 3.2f;
        // 生物每帧最多被拖动的距离，避免瞬移太夸张。
        private const float MaxCreatureStepPerFrame = 0.18f;
        // 黑洞结束时弹飞物品的最小力度。
        private const float BurstStrengthMin = 20f;
        // 黑洞结束时弹飞物品的最大力度。
        private const float BurstStrengthMax =50f;
        // 黑洞结束时弹飞生物的最小力度。
        private const float CreatureBurstStrengthMin = 30f;
        // 黑洞结束时弹飞生物的最大力度。
        private const float CreatureBurstStrengthMax = 70f;

        // 当前被黑洞影响的物品列表。
        private readonly List<Pickupable> affectedItems = new List<Pickupable>();
        // 当前被黑洞影响的生物列表。
        private readonly List<GameObject> affectedCreatures = new List<GameObject>();

        // 距离下一次生成黑洞还剩多少秒。
        private float secondsUntilNextSingularity;
        // 当前黑洞还会持续多少秒。
        private float activeTimeRemaining;
        // 当前黑洞中心位置。
        private Vector3 singularityPosition;
        // 当前是否已经有黑洞正在生效。
        private bool active;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            ResetCooldown();
        }

        public void Sim4000ms(float dt)
        {
            if (active)
            {
                return;
            }

            secondsUntilNextSingularity -= dt;
            if (secondsUntilNextSingularity <= 0f)
            {
                BeginSingularity();
            }
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            activeTimeRemaining -= Time.deltaTime;
            PullItems(Time.deltaTime);
            PullCreatures(Time.deltaTime);

            if (activeTimeRemaining <= 0f)
            {
                EndSingularity();
            }
        }

        private void BeginSingularity()
        {
            singularityPosition = transform.GetPosition() + new Vector3(Random.Range(-1.5f, 1.5f), 1.2f, 0f);
            activeTimeRemaining = ActiveDuration;
            active = true;

            GatherPickupables();
            GatherCreatures();
            ShowSingularityText("mini black hole");
            SpawnPulseFx();
        }

        private void GatherPickupables()
        {
            affectedItems.Clear();

            int centerCell = Grid.PosToCell(singularityPosition);
            if (!Grid.IsValidCell(centerCell))
            {
                return;
            }

            int centerX;
            int centerY;
            Grid.CellToXY(centerCell, out centerX, out centerY);

            ListPool<ScenePartitionerEntry, MiniBlackHole>.PooledList entries = ListPool<ScenePartitionerEntry, MiniBlackHole>.Allocate();
            GameScenePartitioner.Instance.GatherEntries(
                centerX - RadiusCells,
                centerY - RadiusCells,
                RadiusCells * 2 + 1,
                RadiusCells * 2 + 1,
                GameScenePartitioner.Instance.pickupablesLayer,
                entries);

            float radiusSquared = RadiusCells * RadiusCells;
            foreach (ScenePartitionerEntry entry in entries)
            {
                Pickupable pickupable = entry.obj as Pickupable;
                if (!IsValidPickupable(pickupable))
                {
                    continue;
                }

                Vector3 offset = pickupable.transform.GetPosition() - singularityPosition;
                if (offset.sqrMagnitude <= radiusSquared && !affectedItems.Contains(pickupable))
                {
                    affectedItems.Add(pickupable);
                }
            }

            entries.Recycle();
        }

        private void GatherCreatures()
        {
            affectedCreatures.Clear();

            float radiusSquared = RadiusCells * RadiusCells;
            int myWorldId = Grid.WorldIdx[Grid.PosToCell(transform.GetPosition())];

            foreach (Brain brain in global::Components.Brains.Items)
            {
                if (brain == null || brain.gameObject == null)
                {
                    continue;
                }

                GameObject creature = brain.gameObject;
                if (!IsValidCreature(creature))
                {
                    continue;
                }

                int creatureCell = Grid.PosToCell(creature.transform.GetPosition());
                if (!Grid.IsValidCell(creatureCell) || Grid.WorldIdx[creatureCell] != myWorldId)
                {
                    continue;
                }

                Vector3 offset = creature.transform.GetPosition() - singularityPosition;
                if (offset.sqrMagnitude <= radiusSquared && !affectedCreatures.Contains(creature))
                {
                    affectedCreatures.Add(creature);
                }
            }
        }

        private bool IsValidPickupable(Pickupable pickupable)
        {
            if (pickupable == null || pickupable.gameObject == null || pickupable.gameObject == gameObject)
            {
                return false;
            }

            GameObject item = pickupable.gameObject;
            if (item.GetComponent<Navigator>() != null || item.GetComponent<MinionIdentity>() != null || item.GetComponent<CreatureBrain>() != null)
            {
                return false;
            }

            return item.GetComponent<PrimaryElement>() != null;
        }

        private bool IsValidCreature(GameObject creature)
        {
            if (creature == null || creature == gameObject || !creature.activeInHierarchy)
            {
                return false;
            }

            if (creature.GetComponent<MinionIdentity>() != null || creature.GetComponent<CreatureBrain>() == null)
            {
                return false;
            }

            KPrefabID prefabId = creature.GetComponent<KPrefabID>();
            if (prefabId != null && (prefabId.HasTag(GameTags.Dead) || prefabId.HasTag(GameTags.Creatures.Bagged)))
            {
                return false;
            }

            return creature.GetComponent<Navigator>() != null;
        }

        private void PullItems(float dt)
        {
            for (int i = affectedItems.Count - 1; i >= 0; i--)
            {
                Pickupable pickupable = affectedItems[i];
                if (!IsValidPickupable(pickupable))
                {
                    affectedItems.RemoveAt(i);
                    continue;
                }

                Transform itemTransform = pickupable.transform;
                Vector3 current = itemTransform.GetPosition();
                Vector3 toCenter = singularityPosition - current;
                float distance = Mathf.Max(toCenter.magnitude, 0.1f);
                Vector3 step = toCenter.normalized * (PullStrength / distance) * dt;

                if (step.magnitude > toCenter.magnitude)
                {
                    step = toCenter;
                }

                Vector3 next = current + step;
                next.z = current.z;
                itemTransform.SetPosition(next);
            }
        }

        private void PullCreatures(float dt)
        {
            for (int i = affectedCreatures.Count - 1; i >= 0; i--)
            {
                GameObject creature = affectedCreatures[i];
                if (!IsValidCreature(creature))
                {
                    affectedCreatures.RemoveAt(i);
                    continue;
                }

                Transform creatureTransform = creature.transform;
                Vector3 current = creatureTransform.GetPosition();
                Vector3 toCenter = singularityPosition - current;
                float distance = Mathf.Max(toCenter.magnitude, 0.1f);
                Vector3 step = toCenter.normalized * (CreaturePullStrength / distance) * dt;

                if (step.magnitude > MaxCreatureStepPerFrame)
                {
                    step = step.normalized * MaxCreatureStepPerFrame;
                }

                if (step.magnitude > toCenter.magnitude)
                {
                    step = toCenter;
                }

                Vector3 next = current + step;
                next.z = current.z;
                creatureTransform.SetPosition(next);
            }
        }

        private void EndSingularity()
        {
            for (int i = affectedItems.Count - 1; i >= 0; i--)
            {
                Pickupable pickupable = affectedItems[i];
                if (!IsValidPickupable(pickupable))
                {
                    continue;
                }

                GameObject item = pickupable.gameObject;
                Vector2 direction = item.transform.GetPosition() - singularityPosition;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Random.insideUnitCircle.normalized;
                }

                direction = (direction.normalized + new Vector2(0f, 0.45f)).normalized;
                Vector2 velocity = direction * Random.Range(BurstStrengthMin, BurstStrengthMax);

                if (GameComps.Fallers.Has(item))
                {
                    GameComps.Fallers.Remove(item);
                }

                if (GameComps.Gravities.Has(item))
                {
                    GameComps.Gravities.Remove(item);
                }

                GameComps.Fallers.Add(item, velocity);
            }

            for (int i = affectedCreatures.Count - 1; i >= 0; i--)
            {
                GameObject creature = affectedCreatures[i];
                if (!IsValidCreature(creature))
                {
                    continue;
                }

                Vector2 direction = creature.transform.GetPosition() - singularityPosition;
                if (direction.sqrMagnitude < 0.01f)
                {
                    direction = Random.insideUnitCircle.normalized;
                }

                direction = (direction.normalized + new Vector2(0f, 0.35f)).normalized;
                Vector2 velocity = direction * Random.Range(CreatureBurstStrengthMin, CreatureBurstStrengthMax);

                if (GameComps.Fallers.Has(creature))
                {
                    GameComps.Fallers.Remove(creature);
                }

                if (GameComps.Gravities.Has(creature))
                {
                    GameComps.Gravities.Remove(creature);
                }

                GameComps.Fallers.Add(creature, velocity);
            }

            affectedItems.Clear();
            affectedCreatures.Clear();
            active = false;
            ResetCooldown();
            ShowSingularityText("pop");
        }

        private void SpawnPulseFx()
        {
            int cell = Grid.PosToCell(singularityPosition);
            if (!Grid.IsValidCell(cell))
            {
                return;
            }

            KBatchedAnimController fx = FXHelpers.CreateEffect("radialgrid_kanim", Grid.CellToPosCCC(cell, Grid.SceneLayer.FXFront), null, false, Grid.SceneLayer.FXFront, false);
            if (fx == null)
            {
                return;
            }

            GameScheduler.Instance.Schedule("FunnyComponents_DestroyMiniBlackHoleFx", ActiveDuration, data =>
            {
                KBatchedAnimController controller = data as KBatchedAnimController;
                if (controller != null)
                {
                    Util.KDestroyGameObject(controller.gameObject);
                }
            }, fx);
        }

        private void ShowSingularityText(string text)
        {
            PopFXManager manager = PopFXManager.Instance;
            if (manager == null || !manager.Ready())
            {
                return;
            }

            manager.SpawnFX(
                manager.sprite_Research,
                text,
                null,
                singularityPosition + new Vector3(0f, 0.5f, 0f),
                1.5f,
                false,
                false);
        }

        private void ResetCooldown()
        {
            secondsUntilNextSingularity = Random.Range(CooldownMin, CooldownMax);
        }
    }
}
