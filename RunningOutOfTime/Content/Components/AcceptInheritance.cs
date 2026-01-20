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

        private KBatchedAnimController _animController;

        private Chore _chore;
        private GameObject _targetDuplicant;

        private Coroutine moveRoutine;
        private Vector3 originalPosition;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();

            // 提前获取动画控制器
            _animController = GetComponent<KBatchedAnimController>();

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
            this.SetWorkTime(9f); // 设定继承所需的交互时间
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



        protected override void OnStartWork(WorkerBase worker)
        {
            base.OnStartWork(worker);
            if (moveRoutine != null) StopCoroutine(moveRoutine);

            // 启动协程：目标是 worker 的头顶
            moveRoutine = StartCoroutine(AnimateToWorkerHead(worker));
        }

        protected override void OnStopWork(WorkerBase worker)
        {
            base.OnStopWork(worker);
            if (moveRoutine != null) StopCoroutine(moveRoutine);

            // 停止工作时，平滑回到原位（Offset 归零）
            moveRoutine = StartCoroutine(ReturnToOrigin());
        }

        private System.Collections.IEnumerator AnimateToWorkerHead(WorkerBase worker)
        {
            if (_animController == null || worker == null) yield break;

            float elapsed = 0f;
            float moveDuration = 1.5f; // 移动到头顶的时间
            Vector2 startOffset = _animController.Offset;

            while (true)
            {
                //实时计算目标位置（以防复制人移动或动画偏移）
                Vector3 brainPos = transform.position;
                Vector3 workerHeadPos = worker.transform.position + new Vector3(0, 1.8f, 0);
                Vector2 targetBaseOffset = new Vector2(workerHeadPos.x - brainPos.x, workerHeadPos.y - brainPos.y);

                elapsed += Time.deltaTime;
                float moveT = Mathf.Clamp01(elapsed / moveDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, moveT);

                // 计算浮动
                // 使用一个浮动权重，在移动过程中权重为 0，移动完成后权重逐渐变为 1
                // 这样 Sin 效果就是从“静止”平滑过渡到“摆动”
                float hoverWeight = moveT; // 也可以用 Mathf.Max(0, (elapsed - moveDuration) / 1.0f) 来更晚触发浮动
                float hoverRange = 0.15f;
                float hoverSpeed = 2.0f;
                float sinValue = Mathf.Sin(Time.time * hoverSpeed) * hoverRange * hoverWeight;

                // 混合位置
                // 基础位置从 start 移动到 targetBase
                Vector2 currentBase = Vector2.Lerp(startOffset, targetBaseOffset, smoothT);

                // 应用最终偏移（基础位置 + 浮动）
                _animController.Offset = new Vector2(currentBase.x, currentBase.y + sinValue);

                yield return null;
            }
        }

        private System.Collections.IEnumerator ReturnToOrigin()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Vector2 currentOffset = _animController.Offset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _animController.Offset = Vector2.Lerp(currentOffset, Vector2.zero, elapsed / duration);
                yield return null;
            }
            _animController.Offset = Vector2.zero;
            moveRoutine = null;
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