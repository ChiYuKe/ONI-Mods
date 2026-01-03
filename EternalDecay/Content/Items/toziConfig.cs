using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Klei.AI;
using UnityEngine;

namespace EternalDecay.Content.Items
{
    public class ToziConfig : IEntityConfig
    {
        public string[] GetDlcIds()
        {
            return null;
        }

        public GameObject CreatePrefab()
        {
            GameObject gameObject = EntityTemplates.CreateLooseEntity(
                "Ktozi",
                Configs.STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.TOZI.NAME,
                Configs.STRINGS.ITEMS.INDUSTRIAL_PRODUCTS.TOZI.DESC,
                5f,
                true,
                Assets.GetAnim("tozi_kanim"),
                "object",
                Grid.SceneLayer.SolidConduits,
                EntityTemplates.CollisionShape.RECTANGLE,
                0.6f,
                0.6f,
                true, 
                0,
                SimHashes.Creature,
                new List<Tag> { GameTags.IndustrialIngredient }
            );

            KBatchedAnimController animController = gameObject.GetComponent<KBatchedAnimController>();
            if (animController != null)
            {
                animController.animScale = 0.0015f;
            }


            return gameObject;
        }

        public void OnPrefabInit(GameObject inst)
        {

        }

        public void OnSpawn(GameObject inst)
        {


        }




    }
}
