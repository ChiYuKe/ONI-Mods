using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CykUtils;
using HarmonyLib;
using KMod;
using UnityEngine;

namespace AutomaticHarvest
{
    public class KModPatch
    {


        public static class Buildpatch
        {
            [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
            public static class ThermalBlock_F_1LoadGeneratedBuildings_Patch
            {

                public static void Prefix()
                {
                    ModUtil.AddBuildingToPlanScreen("Conveyance", AutomaticHarvestConfig.ID);  // 添加到建筑菜单 ，"Base"是基础菜单，"Tiles"是子菜单 具体可定位到 TUNING.BUILDING.PLANORDER 查看其结构
                    Db.Get().Techs.Get("SmartStorage").unlockedItemIDs.Add(AutomaticHarvestConfig.ID); // 使其可研究
                    KModStringUtils.Add_New_BuildStrings(AutomaticHarvestConfig.ID, STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.NAME, STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.DESC,STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.EFFECT);
                }
            }
        }


        [HarmonyPatch(typeof(Localization), "Initialize")]

        private class Translate_Initialize_Patch
        {
            public static void Postfix()
            {
                Loc.Translate(typeof(STRINGS), true);
            }
        }




        [HarmonyPatch(typeof(RangeVisualizerEffect), "OnPostRender")]
        public static class RangeVisualizerEffectPatch
        {
            public static void Prefix(RangeVisualizerEffect __instance)
            {
                // 1. 确定目标 GameObject
                GameObject targetGo = null;

                // 检查是否有选中的对象
                if (SelectTool.Instance != null && SelectTool.Instance.selected != null)
                {
                    targetGo = SelectTool.Instance.selected.gameObject;
                }
                // 如果没有选中对象，检查是否有建造预览对象
                else if (BuildTool.Instance != null && BuildTool.Instance.visualizer != null)
                {
                    targetGo = BuildTool.Instance.visualizer;
                }

                // 2. 定义您要检查的目标 Tag
                Tag myTargetTag = "AutomaticHarvest";

                // 默认颜色
                Color newColor = new Color(0f, 1f, 0.8f, 1f); 

                // 3. 检查目标对象是否存在且拥有 KPrefabID 组件
                if (targetGo != null && targetGo.TryGetComponent<KPrefabID>(out var kPrefabID))
                {
                    if (kPrefabID.HasTag(myTargetTag))
                    {
                      
                        newColor = new Color(0.1f, 1f, 0f, 1f); 
                    }
                    // else 保持默认色
                }

                // 4. 设置 __instance.highlightColor，并在 Material 上更新颜色
                __instance.highlightColor = newColor;

                // 设置 Material 颜色
                FieldInfo materialField = typeof(RangeVisualizerEffect).GetField("material", BindingFlags.Instance | BindingFlags.NonPublic);
                if (materialField != null)
                {
                    Material material = (Material)materialField.GetValue(__instance);
                    if (material != null)
                    {
                        // 确保在 OnPostRender 实际渲染前，Material 上的颜色已更新
                        material.SetColor("_HighlightColor", __instance.highlightColor);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlantFiberConfig), "CreatePrefab")]
        public static class Patch_PlantFiberConfig_CreatePrefab
        {
            public static void Postfix(GameObject __result)
            {
                // 1. 获取 KPrefabID 组件
                KPrefabID prefabID = __result.GetComponent<KPrefabID>();

                if (prefabID != null)
                {
                    prefabID.AddTag(new Tag("PlantFiber_"), false);
                }
            }
        }







        // ------------------ 用户菜单及使能控制 ------------------

        //private static readonly EventSystem.IntraObjectHandler<AutomaticHarvestK> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<AutomaticHarvestK>(delegate (AutomaticHarvestK component, object data)
        //{
        //    component.OnRefreshUserMenu(data);
        //});

        //private void OnRefreshUserMenu(object data)
        //{
        //    // 清空按钮
        //    KIconButtonMenu.ButtonInfo emptyButton = new KIconButtonMenu.ButtonInfo("action_empty_contents", "清空内容", delegate
        //    {
        //        // 确保 DropAll 调用参数正确
        //        this.storage.DropAll(true,  false, transform.GetPosition(), true, null);
        //    }, global::Action.NumActions, null, null, null, "清空建筑内部的容物", true);


        //    KIconButtonMenu.ButtonInfo toggleButton;

        //    if (this.isEnabled)
        //    {
        //        toggleButton = new KIconButtonMenu.ButtonInfo(
        //            "action_building_disabled",
        //            "禁用自动收割",
        //            new System.Action(this.DisableHarvester),
        //            global::Action.NumActions,
        //            null, null, null,
        //            "点击禁用，停止自动扫描和收割。",
        //            true
        //        );
        //    }
        //    else
        //    {
        //        toggleButton = new KIconButtonMenu.ButtonInfo(
        //            "action_harvest",
        //            "启用自动收割",
        //            new System.Action(this.EnableHarvester),
        //            global::Action.NumActions,
        //            null, null, null,
        //            "点击启用，自动扫描和收割植物。",
        //            true
        //        );
        //    }

        //    Game.Instance.userMenu.AddButton(base.gameObject, toggleButton, 0.5f);
        //    Game.Instance.userMenu.AddButton(base.gameObject, emptyButton, 1f);
        //}

        //private void EnableHarvester()
        //{
        //    this.isEnabled = true;
        //    reservoir.RefreshHstatusLight(HstatusLight);
        //    // 触发事件以通知状态机切换状态
        //    Trigger((int)GameHashes.RefreshUserMenu, null);
        //}

        //private void DisableHarvester()
        //{
        //    this.isEnabled = false;
        //    reservoir.RefreshHstatusLight(HstatusLight);
        //    // 触发事件以通知状态机切换状态
        //    Trigger((int)GameHashes.RefreshUserMenu, null);
        //}



    }
}

