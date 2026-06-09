
namespace EternalDecay.Content.Comps.DebuffCom
{
    public class Trigger : KMonoBehaviour, ISim1000ms
    {
        private KPrefabID prefabID;
        private BreakStuff breakstuff; // 用于触发事件的实例
        private bool timersActive;
        private float elapsedSeconds;
        private int tickSeconds;

        // 当对象生成时初始化
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            prefabID = gameObject.GetComponent<KPrefabID>();
            breakstuff = new BreakStuff(0.5f); // 初始化 breakstuff，设置 80% 的触发概率


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
            while (elapsedSeconds >= 1f)
            {
                elapsedSeconds -= 1f;
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

        private void Sim2000ms()
        {
            AbyssophobiaDebuff.TriggerScan(gameObject);
            // breakstuff.TriggerScan(gameObject);
        }

        private void Sim4000ms()
        {
            SpecializationHeatWanderer.TriggerScan(gameObject, 2);
        }

        private void Sim6000ms()
        {
            SpecializationCoolWanderer.TriggerScan(gameObject, 2);
        }

        private void Sim8000ms()
        {
            SpecializationScorchingMetalSharer.TriggerScan(gameObject);
        }
    }
}
