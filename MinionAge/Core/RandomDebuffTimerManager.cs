using Klei.AI;
using KModTool;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MinionAge.Core
{
    public static class RandomDebuffTimerManager
    {
        // 复制人对象相关
        private static readonly HashSet<GameObject> cachedMinionGameObjects = new HashSet<GameObject>(); // 缓存的复制人对象集合
        private static readonly HashSet<GameObject> processedMinions = new HashSet<GameObject>(); // 记录是否已经应用了衰老效果
        private static readonly HashSet<GameObject> deadMinions = new HashSet<GameObject>(); // 已死亡的复制人集合
        private static int cachedMinionCount = 0; // 缓存的复制人数量

        // 复制人年龄相关
        public static float MinionAgeThreshold = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.MINIONAGETHRESHOLD; // 复制人年龄阈值（单位：周期）
        private static readonly float AgeThreshold = MinionAgeThreshold * 600f; // 年龄阈值（秒）
        private static readonly float Age80PercentThreshold = AgeThreshold * TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.AGE80PERCENTTHRESHOLD; // 年龄80%阈值
        public static readonly float DebuffTimeThreshold = AgeThreshold - Age80PercentThreshold; // 衰老效果阈值

        // 时间间隔控制
        private static float cacheUpdateInterval = 1f; // 缓存更新间隔（秒）
        private static float debuffCheckInterval = 5f; // Debuff检查间隔（秒）
        private static float dataSyncInterval = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TIMERINTERVAL; // 数据同步间隔（秒）

        private static float lastCacheUpdateTime = 0f; // 上次缓存更新时间
        private static float lastDebuffCheckTime = 0f; // 上次Debuff检查时间
        private static float lastDataSyncTime = 0f; // 上次数据同步时间

        // 初始化定时器
        public static void InitializeTimer()
        {
            lastCacheUpdateTime = Time.time;
            lastDebuffCheckTime = Time.time;
            lastDataSyncTime = Time.time;
        }

        // 每帧调用更新逻辑
        public static void Update()
        {
            float currentTime = Time.time;

            // 更新缓存
            if (currentTime >= lastCacheUpdateTime + cacheUpdateInterval)
            {
                Ownable.CleanupExpiredObjectTimeRecords();
                UpdateMinionCache();
                lastCacheUpdateTime = currentTime;
            }

            // 检查Debuff
            if (currentTime >= lastDebuffCheckTime + debuffCheckInterval)
            {
                ProcessMinionObjects();
                lastDebuffCheckTime = currentTime;
            }

            // 同步数据
            if (currentTime >= lastDataSyncTime + dataSyncInterval)
            {
                SynchronizeMinionData();
                lastDataSyncTime = currentTime;
            }
        }

        // 更新复制人对象缓存
        private static void UpdateMinionCache()
        {
            List<GameObject> currentMinions = KModMinionUtils.GetAllMinionGameObjects();
            if (currentMinions == null) return;

            // 移除无效或已死亡的复制人
            cachedMinionGameObjects.RemoveWhere(obj => obj == null || !currentMinions.Contains(obj) || deadMinions.Contains(obj));

            // 添加新的复制人
            foreach (var minion in currentMinions)
            {
                if (minion != null && !deadMinions.Contains(minion))
                {
                    cachedMinionGameObjects.Add(minion);
                }
            }

            cachedMinionCount = cachedMinionGameObjects.Count;
        }

        // 处理缓存中的复制人对象
        private static void ProcessMinionObjects()
        {
            foreach (GameObject minion in cachedMinionGameObjects.ToList())
            {
                if (minion == null || deadMinions.Contains(minion)) continue;

                var ageInstance = Db.Get().Amounts.Age.Lookup(minion);
                if (ageInstance == null) continue;

                float currentAgeInSeconds = ageInstance.value * 600;

                // 处理死亡
                if (currentAgeInSeconds >= AgeThreshold)
                {
                    HandleDeath(minion);
                }
                // 处理衰老
                else if (currentAgeInSeconds >= Age80PercentThreshold)
                {
                    if (!processedMinions.Contains(minion))
                    {
                        // 应用衰老效果
                        KModMiscellaneous.SetExpression(minion, Db.Get().Expressions.Tired);
                        KModDeBuff.ApplyDebuff(minion, "shuailao");
                        KModDeBuff.NotifyDeathApplied(minion); // 通知衰老效果已应用

                        processedMinions.Add(minion); // 标记为已处理
                        Debug.Log($"应用衰老效果：minion (InstanceID: {minion.GetComponent<KPrefabID>().InstanceID})，年龄 = {currentAgeInSeconds} 秒");
                    }
                }
                else if (currentAgeInSeconds < Age80PercentThreshold && processedMinions.Contains(minion))
                {
                    RemoveEffect(minion, "shuailao");
                    // 如果年龄低于阈值且已经应用过衰老效果，则移除标记
                    processedMinions.Remove(minion);
                    Debug.Log($"移除衰老标记：minion (InstanceID: {minion.GetComponent<KPrefabID>().InstanceID})，年龄 = {currentAgeInSeconds} 秒");
                }
            }
        }



        // 处理复制人对象的死亡
        private static void HandleDeath(GameObject minion)
        {
            if (minion == null) return;

            var deathMonitor = minion.GetSMI<DeathMonitor.Instance>();
            if (deathMonitor != null)
            {
                minion.AddOrGet<KPrefabID>().AddTag(TUNINGS.NoMourning, true);
                minion.AddOrGet<KPrefabID>().AddTag(TUNINGS.DieOfOldAge, true);
                deathMonitor.Kill(AddNewDeathPatch.customDeath);

                // 更新缓存和状态
                cachedMinionGameObjects.Remove(minion);
                deadMinions.Add(minion);

                // 延迟生成新对象并销毁旧对象
                KModDelayedActionExecutor.Instance.ExecuteAfterDelay(2f, () =>
                {
                    if (minion != null)
                    {
                        MinionDataTransfer.GenerateNewObject(minion, minion.transform.position);
                        // UnityEngine.Object.Destroy(minion);
                    }
                });
            }
            else
            {
                Debug.LogWarning("DeathMonitor.Instance 为空，无法执行死亡操作");
            }
        }

        // 同步复制人数据
        private static void SynchronizeMinionData()
        {
            try
            {
                MinionDataSaver.SynchronizeAllMinionData(cachedMinionGameObjects);
            }
            catch (Exception ex)
            {
                Debug.LogError($"数据同步失败: {ex.Message}");
            }
        }

        public static void RemoveEffect(GameObject gameObject, string effect_id)
        {
            // 获取 Effects 组件
            Effects effectsComponent = gameObject.GetComponent<Effects>();

            // 如果没有找到 Effects 组件，返回
            if (effectsComponent == null)
            {
                return;
            }

            // 使用 effect_id 移除效果
            effectsComponent.Remove(effect_id);
        }


        // 移除无效的 GameObject
        private static void RemoveInvalidGameObjects()
        {
            foreach (var minion in cachedMinionGameObjects.ToList())
            {
                if (minion == null)
                {
                    cachedMinionGameObjects.Remove(minion);
#if DEBUG
                    Debug.LogWarning($"发现无效的 GameObject，已从缓存中移除。");
#endif
                }
            }

            if (cachedMinionGameObjects.Count < cachedMinionCount)
            {
#if DEBUG
                Debug.LogError($"发现无效的 GameObject 在缓存中，当前缓存数量: {cachedMinionGameObjects.Count}");
#endif
                cachedMinionCount = cachedMinionGameObjects.Count;
            }
        }
    }
}
