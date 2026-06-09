

using Database;
using HarmonyLib;
using KModTool;
using PeterHan.PLib.UI;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using static STRINGS.CODEX.STORY_TRAITS.MORB_ROVER_MAKER;

namespace MinionAge_DLC
{
    internal class ModPatch
    {

        public static class Buildpatch
        {
            [HarmonyPatch(typeof(GeneratedBuildings), "LoadGeneratedBuildings")]
            public static class OilWellCap_1LoadGeneratedBuildings_Patch
            {

                public static void Prefix()
                {
                    ModUtil.AddBuildingToPlanScreen("Base", EnergyDispersionTableConifg.ID, "Tiles"); 
                    Db.Get().Techs.Get("HighTempForging").unlockedItemIDs.Add(EnergyDispersionTableConifg.ID);
                    KModStringUtils.Add_New_BuildStrings(EnergyDispersionTableConifg.ID, STRINGS.BUILDINGS.ENERGYDISPERSIONTABLECONIFG.NAME, STRINGS.BUILDINGS.ENERGYDISPERSIONTABLECONIFG.DESC, STRINGS.BUILDINGS.ENERGYDISPERSIONTABLECONIFG.EFFECT);
                }
            }
        }
    }
}
