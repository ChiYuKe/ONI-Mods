using Klei.AI;
using UnityEngine;
using EternalDecay.Content.Config;

using RunningOutOfTime.Content.Core;
using static RunningOutOfTime.Content.Patches.ChoreTypesPatch;

namespace RunningOutOfTime.Content.Components
{
    /// <summary>
    /// 挂载在“罐中脑”上的交互组件。
    /// 复制人通过“工作”来吸收大脑中的记忆与技能。
    /// </summary>
    public class AcceptInheritance : Workable
    {
        [MyCmpReq]
        public Assignable assignable;

        private Chore _chore;
        private GameObject _targetDuplicant;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            // 基础 Workable 设置
            this.synchronizeAnims = false;
            this.showProgressBar = true;
            this.resetProgressOnStop = true;
            this.attributeConverter = Db.Get().AttributeConverters.MachinerySpeed;

            // 动画设置
            this.overrideAnims = new[] { Assets.GetAnim("anim_interacts_hqbase_skill_upgrade_kanim") };
            this.workAnims = new HashedString[] { "upgrade" };

            // 绑定分配事件
            this.assignable.OnAssign += this.OnIdentityAssigned;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            this.SetWorkTime(10f); // 设定继承所需的交互时间
        }

        /// <summary>
        /// 当玩家在侧边栏将大脑分配给某个复制人时触发
        /// </summary>
        private void OnIdentityAssigned(IAssignableIdentity newIdentity)
        {
            this.CancelCurrentChore();

            if (newIdentity == null) return;

            // 获取实际的复制人 GameObject（处理 Proxy 代理对象）
            _targetDuplicant = ExtractTargetGameObject(newIdentity);

            if (_targetDuplicant != null)
            {
                this.CreateInheritanceChore();
            }
        }

        private GameObject ExtractTargetGameObject(IAssignableIdentity identity)
        {
            var soleOwner = identity.GetSoleOwner();
            if (soleOwner == null) return null;

            var proxy = soleOwner.GetComponent<MinionAssignablesProxy>();
            return proxy != null ? proxy.GetTargetGameObject() : soleOwner.gameObject;
        }

        private void CreateInheritanceChore()
        {
            if (_chore != null) return;

            // 创建工作任务
            _chore = new WorkChore<AcceptInheritance>(
                AddNewChorePatch.AcceptInheritance,
                this,
                null,
                true,
                o => CompleteChore(),
                null,
                null,
                true,
                null,
                false,
                false,
                null,
                false,
                true,
                false,
                PriorityScreen.PriorityClass.compulsory
            );
        }

        private void CancelCurrentChore()
        {
            if (_chore == null) return;
            _chore.Cancel("Assignment Changed");
            _chore = null;
        }

        private void CompleteChore()
        {
            _chore?.Cleanup();
            _chore = null;
        }

        protected override void OnCompleteWork(WorkerBase worker)
        {
            base.OnCompleteWork(worker);

            GameObject duplicant = worker.gameObject;
            GameObject brain = this.gameObject;

            // 执行核心遗产转移逻辑
            ExecuteInheritance(duplicant, brain);

            // UI 反馈
            SpawnSuccessFX(duplicant.transform);

            // 3. 标记复制人
            duplicant.AddTag(EDGameTags.IsLegacyAssigned);
            Util.KDestroyGameObject(brain);
        }

        private void ExecuteInheritance(GameObject duplicant, GameObject brain)
        {
            // 这里调用 InheritanceManager 的方法
            // 注意：因为大脑是 source，复制人是 target，逻辑与之前的 Spawn 相反
            // 我们需要确保 InheritanceManager 逻辑能够处理 复制人 <- 大脑
            //InheritanceManager.TransferAttributes(brain, duplicant);
            //InheritanceManager.TransferTraits(brain, duplicant);

            // 弹出信息面板展示继承结果
            // ShowMinionInfo.ShowInheritanceInfo(duplicant, brain);
        }

        private void SpawnSuccessFX(Transform targetTransform)
        {
            PopFXManager.Instance.SpawnFX(
                Assets.GetSprite("akisextratwitchevents_small_ring"),
                "核心数据同步完成",
                targetTransform,
                2f
            );
        }

        protected override void OnAbortWork(WorkerBase worker)
        {
            base.OnAbortWork(worker);
            // 可以在此处添加中断后的负面 Buff，模拟“精神冲击”
        }
    }
}