using System.Collections.Generic;
using FoodandFoodBuffTutorialCase.Components;
using UnityEngine;

namespace FoodandFoodBuffTutorialCase.Items
{
    public class MyFirstItemConfig : IEntityConfig
    {
        public const string ID = "MyFirstItem";
        private static readonly string[] AvailableDlcIds = { "", "EXPANSION1_ID" };

        public GameObject CreatePrefab()
        {
            GameObject go = EntityTemplates.CreateLooseEntity(
                ID,
                STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.MYFIRSTITEM.NAME,
                STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.MYFIRSTITEM.DESC,
                5f,
                true,
                Assets.GetAnim("my_first_item_kanim"),
                "object",
                Grid.SceneLayer.Front,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.6f,
                0.6f,
                true,
                0,
                SimHashes.Creature,
                new List<Tag> { GameTags.IndustrialIngredient }
            );

            go.AddOrGet<KSelectable>();
            go.AddOrGet<MyFirstItem>();

            return go;
        }

        public void OnPrefabInit(GameObject inst) { }
        public void OnSpawn(GameObject inst) { }
        public string[] GetDlcIds()
        {
            return AvailableDlcIds;
        }
    }
}
