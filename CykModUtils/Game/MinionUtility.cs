using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CykModUtils.Game
{
    /// <summary>
    /// 复制人查询和调试信息辅助方法。
    /// </summary>
    public static class MinionUtility
    {
        /// <summary>
        /// 获取当前场景中所有存活复制人的 GameObject。
        /// </summary>
        /// <returns>存活复制人对象列表。</returns>
        public static List<GameObject> GetLiveMinionGameObjects()
        {
            var result = new List<GameObject>();
            foreach (MinionIdentity identity in Components.LiveMinionIdentities.Items)
            {
                if (identity != null && identity.gameObject != null)
                {
                    result.Add(identity.gameObject);
                }
            }

            return result;
        }

        /// <summary>
        /// 尝试从对象上获取 MinionIdentity 组件。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="identity">找到的复制人身份组件。</param>
        /// <returns>组件存在时返回 true。</returns>
        public static bool TryGetIdentity(GameObject target, out MinionIdentity identity)
        {
            identity = target != null ? target.GetComponent<MinionIdentity>() : null;
            return identity != null;
        }

        /// <summary>
        /// 尝试从对象上获取 MinionResume 组件。
        /// </summary>
        /// <param name="target">目标对象。</param>
        /// <param name="resume">找到的复制人简历组件。</param>
        /// <returns>组件存在时返回 true。</returns>
        public static bool TryGetResume(GameObject target, out MinionResume resume)
        {
            resume = target != null ? target.GetComponent<MinionResume>() : null;
            return resume != null;
        }

        /// <summary>
        /// 构建复制人身份的简要调试文本。
        /// </summary>
        /// <param name="target">目标复制人对象。</param>
        /// <returns>可直接写入日志的多行文本。</returns>
        public static string BuildIdentitySummary(GameObject target)
        {
            if (!TryGetIdentity(target, out MinionIdentity identity))
            {
                return "MinionIdentity: null";
            }

            Vector3 position = identity.transform.position;
            var builder = new StringBuilder();
            builder.AppendLine("MinionIdentity:");
            builder.AppendLine("- Name: " + identity.name);
            builder.AppendLine("- ProperName: " + identity.GetProperName());
            builder.AppendLine("- Gender: " + identity.gender);
            builder.AppendLine("- Position: " + position);
            builder.AppendLine("- VoiceIdx: " + identity.voiceIdx);
            builder.AppendLine("- ArrivalTime: " + identity.arrivalTime);
            builder.AppendLine("- PersonalityResourceId: " + identity.personalityResourceId);
            return builder.ToString();
        }

        /// <summary>
        /// 构建复制人技能/职业简历的简要调试文本。
        /// </summary>
        /// <param name="target">目标复制人对象。</param>
        /// <returns>可直接写入日志的多行文本。</returns>
        public static string BuildResumeSummary(GameObject target)
        {
            if (!TryGetResume(target, out MinionResume resume))
            {
                return "MinionResume: null";
            }

            var builder = new StringBuilder();
            builder.AppendLine("MinionResume:");
            builder.AppendLine("- Identity: " + (resume.GetIdentity != null ? resume.GetIdentity.GetProperName() : "Unknown"));
            builder.AppendLine("- CurrentRole: " + resume.CurrentRole);
            builder.AppendLine("- TargetRole: " + resume.TargetRole);
            builder.AppendLine("- TotalExperienceGained: " + resume.TotalExperienceGained);
            builder.AppendLine("- TotalSkillPointsGained: " + resume.TotalSkillPointsGained);
            builder.AppendLine("- AvailableSkillpoints: " + resume.AvailableSkillpoints);
            return builder.ToString();
        }
    }
}
