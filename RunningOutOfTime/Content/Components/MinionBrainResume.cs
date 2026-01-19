using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSerialization;
using TUNING;
using UnityEngine;

namespace RunningOutOfTime.Content.Components
{
    public class MinionBrainResume : KMonoBehaviour, ISaveLoadable
    {
        // 存储技能 ID 和其掌握状态的字典
        [Serialize]
        public Dictionary<string, bool> MasteryBySkillID = new Dictionary<string, bool>();

        // 存放总经验值
        [Serialize]
        private float totalExperienceGained;

        [Serialize]
        public List<string> GrantedSkillIDs = new List<string>();

        [Serialize]
        public int TotalSkillPointsGained => CalculateTotalSkillPointsGained(TotalExperienceGained);





        [Serialize]
        public int AvailableSkillpoints => TotalSkillPointsGained - SkillsMastered + ((GrantedSkillIDs != null) ? GrantedSkillIDs.Count : 0);

        public int SkillsMastered
        {
            get
            {
                int num = 0;
                foreach (KeyValuePair<string, bool> item in MasteryBySkillID)
                {
                    if (item.Value)
                    {
                        num++;
                    }
                }

                return num;
            }
        }



        public static int CalculateTotalSkillPointsGained(float experience)
        {
            return Mathf.FloorToInt(Mathf.Pow(experience / (float)SKILLS.TARGET_SKILLS_CYCLE / 600f, 1f / SKILLS.EXPERIENCE_LEVEL_POWER) * (float)SKILLS.TARGET_SKILLS_EARNED);
        }



        // 获取和设置总经验值
        public float TotalExperienceGained
        {
            get => totalExperienceGained;
            set => totalExperienceGained = value;
        }

        // 添加技能方法
        public void AddSkill(string skillId, bool isMastered)
        {
            MasteryBySkillID[skillId] = isMastered;
        }

        // 获取技能掌握状态
        public bool IsSkillMastered(string skillId)
        {
            return MasteryBySkillID.TryGetValue(skillId, out bool isMastered) && isMastered;
        }

        // 清除所有技能
        public void ClearSkills()
        {
            MasteryBySkillID.Clear();
        }
    }
}
