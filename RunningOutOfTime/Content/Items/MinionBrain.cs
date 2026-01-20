using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CykUtils;
using RunningOutOfTime.Content.Core;
using UnityEngine;

namespace RunningOutOfTime.Content.Items
{
    public class MinionBrain : KMonoBehaviour
    {
        private bool notgenerated = true;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            KEffects.ApplyBuff(this.gameObject, ModEffects.STABLE_PERIOD);
            this.Subscribe((int)GameHashes.EffectRemoved, effectremoved); // buff移除时触发

        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            this.Unsubscribe((int)GameHashes.EffectRemoved, effectremoved);
        }



        private void effectremoved(object obj)
        {
            if (notgenerated)
            {
                Util.KDestroyGameObject(gameObject);
                GameUtil.KInstantiate(Assets.GetPrefab("KMinionBrainBadFx"), transform.position, Grid.SceneLayer.FXFront, null, 0).SetActive(true);

                GameObject prefab = Assets.GetPrefab(new Tag("KmodMiniBrainBad"));
                GameObject newMinion = GameUtil.KInstantiate(prefab, transform.position + new Vector3(0f, 1f, 0f), Grid.SceneLayer.Ore, null, 0);
                newMinion.SetActive(true);
            }

        }
    }
}
