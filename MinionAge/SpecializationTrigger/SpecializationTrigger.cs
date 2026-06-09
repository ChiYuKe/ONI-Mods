using DebuffRoulette;
using UnityEngine;

namespace MinionAge.SpecializationTrigger
{
    public class SpecializationTrigger : KMonoBehaviour, ISim1000ms
    {
        private KPrefabID prefabID;
        private bool timersActive;
        private float elapsedSeconds;

        // 当对象生成时初始化
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            prefabID = gameObject.GetComponent<KPrefabID>();

        }

        // 当对象生成时启动定时器
        protected override void OnSpawn()
        {
            base.OnSpawn();
           
            if (!prefabID.HasTag(GameTags.Minions.Models.Bionic))
            {
                // Debug.Log("当前初始化对象为" + gameObject.name);
                timersActive = true;

            }

        }

        public void Sim1000ms(float dt)
        {
            if (!timersActive)
            {
                return;
            }

            elapsedSeconds += dt;
            int wholeSeconds = Mathf.FloorToInt(elapsedSeconds);
            if (wholeSeconds <= 0)
            {
                return;
            }

            elapsedSeconds -= wholeSeconds;
            for (int i = 0; i < wholeSeconds; i++)
            {
                Sim1000msTick();
            }
        }

        private void Sim1000msTick()
        {
            tickSeconds++;
            if (tickSeconds % 2 == 0)
            {
                Sim2000ms();
            }

            if (tickSeconds % 4 == 0)
            {
                Sim4000ms();
            }

            if (tickSeconds % 6 == 0)
            {
                Sim6000ms();
            }

            if (tickSeconds % 8 == 0)
            {
                Sim8000ms();
                tickSeconds = 0;
            }
        }

        private int tickSeconds;

        private void Sim2000ms()
        {
        }

        private void Sim4000ms()
        {
            SpecializationHeatWanderer.TriggerScan(gameObject, 2);
            SpecializationCoolWanderer.TriggerScan(gameObject, 2);
        }

        private void Sim6000ms()
        {
        }

        private void Sim8000ms()
        {
        }
    }
}
