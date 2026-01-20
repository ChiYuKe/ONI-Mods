
using CykUtils;
using Klei.AI;
using RunningOutOfTime.Content.Components;
using RunningOutOfTime.Content.Config;
using System.Collections.Generic;
using System.Linq;
using TUNING;
using UnityEngine;

namespace RunningOutOfTime.Content.Core
{
    /// <summary>
    /// 负责处理复制人遗产继承的核心逻辑：将生命数据从旧躯壳转移到新的载体（大脑）。
    /// </summary>
    internal static class InheritanceManager
    {
        private const float BRAIN_SPAWN_OFFSET_Y = 1.0f;
        private static readonly Tag BRAIN_PREFAB_TAG = new Tag("KmodMiniBrain");

        public static void TransferSoul(GameObject sourceMinion, Vector3 position)
        {
            if (sourceMinion == null) return;

            GameObject brainInstance = SpawnLegacyCarrier(position);
            if (brainInstance == null) return;

            // 2. 执行核心转移任务
            ExecuteTransferSequence(sourceMinion, brainInstance);

            // 3. 视觉与身份命名
            FinalizeIdentity(sourceMinion, brainInstance);
        }

        private static GameObject SpawnLegacyCarrier(Vector3 position)
        {
            GameObject prefab = Assets.GetPrefab(BRAIN_PREFAB_TAG);
            if (prefab == null)
            {
                LogUtil.LogError($"[EternalDecay] 关键预制件丢失: {BRAIN_PREFAB_TAG}");
                return null;
            }

            Vector3 spawnPos = position + new Vector3(0f, BRAIN_SPAWN_OFFSET_Y, 0f);
            GameObject instance = GameUtil.KInstantiate(prefab, spawnPos, Grid.SceneLayer.Ore);

            instance.SetActive(true);
            return instance;
        }

        private static void ExecuteTransferSequence(GameObject source, GameObject target)
        {
            // 封装转移上下文，方便后续扩展（如加入随机损耗逻辑）
            TransferTraits(source, target);
            TransferExperience(source, target);
            TransferAttributes(source, target);
        }

        /// <summary>
        /// 转移特质：过滤掉基础特质，并遵循最大数量限制。
        /// </summary>
        private static void TransferTraits(GameObject source, GameObject target)
        {
            var sourceTraits = source.GetComponent<Traits>();
            var targetTraits = target.GetComponent<Traits>();

            if (sourceTraits == null || targetTraits == null) return;

            int limit = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.TRAITSMAXAMOUNT;

            // 使用 LINQ 提高可读性：过滤基础特质、重复特质
            var validTraits = sourceTraits.TraitList
                .Where(t => t.Id != "MinionBaseTrait" && !targetTraits.HasTrait(t))
                .Take(limit);

            foreach (var trait in validTraits)
            {
                targetTraits.Add(trait);
            }
        }

        /// <summary>
        /// 转移经验与技能：计算技能点对应的补偿经验。
        /// </summary>
        private static void TransferExperience(GameObject source, GameObject target)
        {
            var sourceResume = source.GetComponent<MinionResume>();
            var targetResume = target.GetComponent<MinionBrainResume>();

            if (sourceResume == null || targetResume == null) return;

            int skillsToTransfer = 0;
            int skillLimit = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.SKILLMAXAMOUNT;

            // 转移已掌握的技能
            foreach (var skillId in sourceResume.MasteryBySkillID.Keys)
            {
                if (skillsToTransfer >= skillLimit) break;

                if (sourceResume.HasMasteredSkill(skillId) && !targetResume.MasteryBySkillID.ContainsKey(skillId))
                {
                    targetResume.MasteryBySkillID.Add(skillId, true);
                    skillsToTransfer++;
                }
            }

            // 注入补偿经验值
            float compensationXP = CalculateCompensationExperience(skillsToTransfer);
            targetResume.TotalExperienceGained += sourceResume.TotalExperienceGained + compensationXP;
        }

        /// <summary>
        /// 转移属性：合并等级并处理溢出上限。
        /// </summary>
        private static void TransferAttributes(GameObject source, GameObject target)
        {
            var sourceLevels = source.GetComponent<AttributeLevels>();
            var targetLevels = target.GetComponent<AttributeLevels>();

            if (sourceLevels == null || targetLevels == null) return;

            int maxLevelLimit = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.ATTRIBUTEMAXLEVEL;

            foreach (AttributeLevel sourceLevel in sourceLevels)
            {
                if (sourceLevel?.attribute?.Attribute == null) continue;

                string attrId = sourceLevel.attribute.Attribute.Id;

                AttributeLevel targetLevel = targetLevels.GetAttributeLevel(attrId);

                if (targetLevel != null)
                {
                    // 计算等级（取原等级与配置上限的最小值）
                    int oldLevel = sourceLevel.GetLevel();
                    int newLevel = Mathf.Min(maxLevelLimit, oldLevel);

                    targetLevels.SetLevel(attrId, newLevel);
                    targetLevels.SetExperience(attrId, sourceLevel.experience);

                    // Debug.Log($"[Inheritance] 转移属性 {attrId}: 等级 {newLevel}, 经验 {sourceLevel.experience}");
                }
            }
        }

        private static float CalculateCompensationExperience(int skillCount)
        {
            // 利用游戏原生公式：XP = (n / target)^power * cycle * 600
            float progress = skillCount / (float)SKILLS.TARGET_SKILLS_EARNED;
            return Mathf.Pow(progress, SKILLS.EXPERIENCE_LEVEL_POWER) * SKILLS.TARGET_SKILLS_CYCLE * 600f;
        }

        private static void FinalizeIdentity(GameObject source, GameObject target)
        {
            string oldName = source.GetComponent<KSelectable>().GetName();
            var nameable = target.AddOrGet<UserNameable>();

            // 使用格式化字符串增强可读性
            nameable.SetName($"{oldName}{Config.STRINGS.MISC.NEWMINIONNAME.NAME}");
        }
    }
}