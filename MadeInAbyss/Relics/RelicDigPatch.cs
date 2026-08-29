using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace MadeInAbyss
{
    /// <summary>
    /// 深渊遗物：在足够深的地方挖透天然方块时，有小概率挖出游戏原版的遗物。
    /// 深度越深，能挖出的遗物品阶越高。
    /// </summary>
    public static class RelicDigPatch
    {
        /// <summary>已结算过掉落的格子，防止同一格在挖掘完成后的后续 tick 里重复结算。</summary>
        private static readonly HashSet<int> processedCells = new HashSet<int>();

        private static Dictionary<int, List<string>> artifactsByTier;
        private static bool poolBuilt;

        [HarmonyPatch(typeof(Diggable), "OnWorkTick")]
        public static class Diggable_OnWorkTick_Patch
        {
            public static void Postfix(Diggable __instance, WorkerBase worker, bool __result)
            {
                if (!__result)
                    return;
                TrySpawnRelic(Grid.PosToCell(__instance), worker);
            }
        }

        [HarmonyPatch(typeof(Diggable), "InstantlyFinish")]
        public static class Diggable_InstantlyFinish_Patch
        {
            public static void Postfix(Diggable __instance, WorkerBase worker, bool __result)
            {
                if (!__result)
                    return;
                TrySpawnRelic(Grid.PosToCell(__instance), worker);
            }
        }

        private static void TrySpawnRelic(int cell, WorkerBase worker)
        {
            AbyssConfig config = AbyssConfig.Instance;
            if (!config.EnableRelics)
                return;

            if (!Grid.IsValidCell(cell))
                return;

            if (!processedCells.Add(cell))
                return;
            if (processedCells.Count > 4096)
                processedCells.Clear();

            int worldId = (int)Grid.WorldIdx[cell];
            float depth = AbyssAnchors.GetDepthM(worldId, Grid.CellToPos(cell).y);
            if (float.IsNaN(depth) || depth < config.RelicMinDepthM)
                return;

            float chance = Mathf.Clamp(config.RelicChancePercent, 0f, 100f);
            if (UnityEngine.Random.value * 100f >= chance)
                return;

            string artifactId = PickArtifact(depth);
            if (string.IsNullOrEmpty(artifactId))
                return;

            GameObject prefab = Assets.GetPrefab(artifactId);
            if (prefab == null)
                return;

            GameObject artifact = GameUtil.KInstantiate(
                prefab,
                Grid.CellToPos(cell),
                Grid.SceneLayer.Ore,
                null,
                0);
            artifact.SetActive(true);

            Debug.Log($"[MadeInAbyss] 在深度 {Mathf.RoundToInt(depth)}m 挖出了遗物: {artifactId}");

            if (worker != null && worker.gameObject != null)
            {
                Notifier notifier = worker.gameObject.AddOrGet<Notifier>();
                string artifactName = artifact.GetProperName();
                string diggerName = worker.gameObject.GetProperName();
                notifier.Add(new Notification(
                    STRINGS.MISC.NOTIFICATIONS.ABYSS_RELIC_FOUND.NAME,
                    NotificationType.Good,
                    (list, data) => $"{diggerName} 在深渊中挖出了遗物：{artifactName}",
                    worker.gameObject), string.Empty);
            }
        }

        private struct TierWeight
        {
            public int tier;
            public float weight;

            public TierWeight(int tier, float weight)
            {
                this.tier = tier;
                this.weight = weight;
            }
        }

        /// <summary>
        /// 按深度分层加权挑选一件原版遗物：
        /// 40m 以下以低阶为主，越深越可能挖出高阶遗物。
        /// </summary>
        private static string PickArtifact(float depth)
        {
            BuildPool();
            if (artifactsByTier.Count == 0)
                return null;

            // (品阶下标, 权重) 按深度分三档。
            TierWeight[] weights;
            if (depth < 70f)
                weights = new[] { new TierWeight(0, 6f), new TierWeight(1, 4f) };
            else if (depth < 100f)
                weights = new[] { new TierWeight(1, 5f), new TierWeight(2, 3f), new TierWeight(3, 2f) };
            else
                weights = new[] { new TierWeight(2, 3f), new TierWeight(3, 4f), new TierWeight(4, 2f), new TierWeight(5, 1f) };

            float total = 0f;
            foreach (var w in weights)
                total += w.weight;

            float roll = UnityEngine.Random.value * total;
            int chosenTier = weights[weights.Length - 1].tier;
            foreach (var w in weights)
            {
                roll -= w.weight;
                if (roll <= 0f)
                {
                    chosenTier = w.tier;
                    break;
                }
            }

            while (chosenTier >= 0)
            {
                if (artifactsByTier.TryGetValue(chosenTier, out List<string> ids) && ids.Count > 0)
                    return ids[UnityEngine.Random.Range(0, ids.Count)];
                chosenTier--; // 该品阶没有可用遗物时退而求其次。
            }

            return null;
        }

        /// <summary>收集游戏原版遗物并按品阶（TIER0~TIER5）分组。</summary>
        private static void BuildPool()
        {
            if (poolBuilt)
                return;
            poolBuilt = true;

            artifactsByTier = new Dictionary<int, List<string>>();
            ArtifactTier[] tiers =
            {
                TUNING.DECOR.SPACEARTIFACT.TIER0,
                TUNING.DECOR.SPACEARTIFACT.TIER1,
                TUNING.DECOR.SPACEARTIFACT.TIER2,
                TUNING.DECOR.SPACEARTIFACT.TIER3,
                TUNING.DECOR.SPACEARTIFACT.TIER4,
                TUNING.DECOR.SPACEARTIFACT.TIER5,
            };

            HashSet<string> seen = new HashSet<string>();
            foreach (var pair in ArtifactConfig.artifactItems)
            {
                foreach (string id in pair.Value)
                {
                    if (!seen.Add(id))
                        continue;

                    GameObject prefab = Assets.GetPrefab(id);
                    SpaceArtifact artifact = prefab != null ? prefab.GetComponent<SpaceArtifact>() : null;
                    if (artifact == null)
                        continue;

                    int tierIndex = System.Array.IndexOf(tiers, artifact.GetArtifactTier());
                    if (tierIndex < 0)
                        continue;

                    if (!artifactsByTier.TryGetValue(tierIndex, out List<string> list))
                    {
                        list = new List<string>();
                        artifactsByTier[tierIndex] = list;
                    }
                    list.Add(id);
                }
            }
        }
    }
}
