using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CykUtils;
using EternalDecay.Content.Comps.KUI;
using EternalDecay.Content.Configs;
using Klei.AI;
using UnityEngine;

namespace EternalDecay.Content.Core
{
    public class ShowMinionInfo
    {

        public static void ShowInheritanceInfo(GameObject workerminion, GameObject gameObject)
        {
            var oldAttributes = workerminion.GetComponent<AttributeLevels>();
            var newAttributes = gameObject.GetComponent<AttributeLevels>();

            if (oldAttributes == null)
            {
                Debug.LogWarning("旧角色未找到 AttributeLevels 组件。");
                return;
            }

            HashSet<string> filteredAttributes = new HashSet<string>
            {
                "Toggle","LifeSupport","Immunity","FarmTinker","PowerTinker"
            }; // 要过滤掉的属性ID列表


            List<(string attrName, int oldLevel, int newLevel)> attrList = new();
            List<(string attrName, int oldLevel, int newLevel)> skillList = new();
            List<(string attrName, int oldLevel, int newLevel)> traitList = new();

            foreach (var oldAttrLevel in oldAttributes)
            {
                var attribute = oldAttrLevel.attribute.Attribute;
                if (filteredAttributes.Contains(attribute.Id))
                    continue;

                string attrName = attribute.Name;
                int oldLevel = oldAttrLevel.GetLevel();

                var newAttrLevel = newAttributes.GetAttributeLevel(attribute.Id);
                int newLevel = oldLevel;
                if (newAttrLevel != null)
                {
                    newLevel += newAttrLevel.GetLevel();
                    if (newLevel > TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.ATTRIBUTEMAXLEVEL) 
                    {
                        newLevel = TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.TRANSFER.ATTRIBUTEMAXLEVEL;
                    }
                        
                }

                attrList.Add((attrName, oldLevel, newLevel));
            }

            // 创建并显示面板
            var screenGO = new GameObject("InheritanceInfo");
            var rectTransform = screenGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300f, 500f);




            KScreen kscreen = screenGO.AddComponent<InheritanceInformation>();
            GameObject panel = InheritanceInformation.Createpanel(attrList, skillList,traitList);

            panel.transform.SetParent(kscreen.transform, false);

            screenGO.transform.SetParent(GameObject.Find("ScreenSpaceOverlayCanvas").transform, false);
            kscreen.Activate();
        }

    }
}
