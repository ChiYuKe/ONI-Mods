
namespace MinionAge_DLC
{
    public class SpecializationTrigger : KMonoBehaviour
    {
        private KPrefabID prefabID;

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
                StartTimers();

            }

        }

        // 启动所有定时器 
        private void StartTimers()
        {
            InvokeRepeating(nameof(Sim2000ms), 2f, 2f);
            InvokeRepeating(nameof(Sim4000ms), 4f, 4f);
            InvokeRepeating(nameof(Sim6000ms), 6f, 6f);
            InvokeRepeating(nameof(Sim8000ms), 8f, 8f);
        }

        // 2秒定时器逻辑
        private void Sim2000ms()
        {
           
            // 添加具体逻辑
        }

        // 4秒定时器逻辑
        private void Sim4000ms()
        {
            // Debug.Log("当前对象为"+ gameObject.name);
            SpecializationHeatWanderer.TriggerScan(gameObject, 2); // 触发扫描，检测半径为 2
            SpecializationCoolWanderer.TriggerScan(gameObject, 2);
        }

        // 6秒定时器逻辑
        private void Sim6000ms()
        {
           
            // 添加具体逻辑
        }

        // 8秒定时器逻辑
        private void Sim8000ms()
        {
            SpecializationScorchingMetalSharer.TriggerScan(gameObject);
            // 添加具体逻辑
        }

        // 对象销毁时清理定时器
        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            CancelAllTimers(); // 取消所有定时器
        }

        // 取消所有定时器
        private void CancelAllTimers()
        {
            CancelInvoke(nameof(Sim2000ms));
            CancelInvoke(nameof(Sim4000ms));
            CancelInvoke(nameof(Sim6000ms));
            CancelInvoke(nameof(Sim8000ms));
        }
    }
}