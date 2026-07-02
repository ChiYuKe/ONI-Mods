using System.Collections.Generic;
using FoodandFoodBuffTutorialCase.Effects;
using UnityEngine;

namespace FoodandFoodBuffTutorialCase.Items
{
    public class MyFirstFoodConfig : IEntityConfig
    {
        public const string ID = "MyFirstFood";
        private static readonly string[] AvailableDlcIds = { "", "EXPANSION1_ID" };

        public GameObject CreatePrefab()
        {
            GameObject go = EntityTemplates.CreateLooseEntity(
                ID,
                STRINGS.ITEMS.FOOD.MYFIRSTFOOD.NAME,
                STRINGS.ITEMS.FOOD.MYFIRSTFOOD.DESC,
                1f,
                false,
                Assets.GetAnim("my_first_item_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.6f,
                0.6f,
                true,
                0,
                SimHashes.Creature
            );

            EdiblesManager.FoodInfo foodInfo = new EdiblesManager.FoodInfo(
                ID,
                1000f * 1000f,
                1,
                255.15f,
                277.15f,
                4800f,
                true
            ).AddEffects(
                new List<string> { ModEffects.WELL_FED_EXAMPLE },
                AvailableDlcIds
            );

            return EntityTemplates.ExtendEntityToFood(go, foodInfo);
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
        public string[] GetDlcIds()
        {
            return AvailableDlcIds;
        }
    }
}
