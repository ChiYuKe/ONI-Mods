using System.Collections.Generic;
using Klei.AI;
using RunningOutOfTime.Content.Config;
// using RunningOutOfTime.Content.UI;
using UnityEngine;

namespace RunningOutOfTime.Content.Core
{
    public class ShowMinionInfo
    {
        public static void ShowInheritanceInfo(GameObject targetMinion, GameObject sourceBrain)
        {
            if (targetMinion == null || sourceBrain == null) return;

            // 1. 提取属性数据 (返回类型: List<(string name, int oldLv, int newLv)>)
            var attrList = GetAttributeComparison(targetMinion, sourceBrain);

            // 2. 提取技能数据 (必须与 UI 端的参数类型完全一致)
            // 注意：如果 UI 端要求的是 int，这里也要传 int。为了演示，我先传示例数据。
            var skillList = new List<(string name, int oldVal, int newVal)>();

            //提取特质数据
            var traitList = new List<(string name, int oldVal, int newVal)>();

            // 4. 唤起 UI
            //InheritanceReportScreen.ScreenInstance?.Close();
            //InheritanceReportScreen.Createpanel(attrList, skillList, traitList);
        }

        private static List<(string name, int oldLv, int newLv)> GetAttributeComparison(GameObject target, GameObject source)
        {
            var targetAttrs = target.GetComponent<AttributeLevels>();
            var sourceAttrs = source.GetComponent<AttributeLevels>();
            var results = new List<(string, int, int)>();

            if (targetAttrs == null || sourceAttrs == null) return results;

            // 过滤列表
            var filter = new HashSet<string> { "Toggle", "LifeSupport", "Immunity", "FarmTinker", "PowerTinker" };
            int maxLevel = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.ATTRIBUTEMAXLEVEL;

            foreach (AttributeLevel targetLevel in targetAttrs)
            {
                var attr = targetLevel.attribute.Attribute;
                if (filter.Contains(attr.Id)) continue;

                int oldVal = targetLevel.GetLevel();
                int sourceVal = sourceAttrs.GetAttributeLevel(attr.Id)?.GetLevel() ?? 0;
                int newVal = Mathf.Min(maxLevel, oldVal + sourceVal);

                results.Add((attr.Name, oldVal, newVal));
            }
            return results;
        }
    }
}