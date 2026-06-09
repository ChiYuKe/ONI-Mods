using KModTool;
using MinionAge.Tool;
using OxygenConsumingPlant.Tool;
using STRINGS;
using System;
using System.Collections.Generic;

using UnityEngine;


namespace MinionAge.Core
{
    /// <summary>
    /// 可分配对象类，用于管理对象的分配、颜色、状态等。
    /// </summary>
    public class Ownable : Assignable, ISaveLoadable, IGameObjectEffectDescriptor, ISim200ms
    {
        // 颜色配置
        public bool tintWhenUnassigned = true;
        private Color unownedTint = Color.gray;
        private Color yellowTint = Color.yellow;
        private Color ownedTint = Color.white;

        private Assignable assignable;
        private Chore chore;

        // 移动相关状态
        private bool isMoving = false;
        private int targetCell;
        private IAssignableIdentity pendingAssignment;
        private GameObject targetGameObject;

        protected FetchChore fetchChore;





        // 定义所有可能的效果
        private static readonly List<string> DebuffEffects = new List<string>
        {
            "HeatWanderer",
            "CoolWanderer",
           
        };




        // 记录对象和时间
        public class ObjectTimeRecord
        {
            public GameObject GameObject { get; set; }
            public float Time { get; set; }
        }
        public static List<ObjectTimeRecord> objectTimeRecords = new List<ObjectTimeRecord>();
        private const float TimeThreshold = 600f;
        private static float nextRecordCleanupTime;

        // 继承对象列表
        private List<GameObject> inheritedObjects = new List<GameObject>();

        /// <summary>
        /// 分配对象给新的拥有者。
        /// </summary>
        /// <param name="new_assignee">新的拥有者</param>
        public override void Assign(IAssignableIdentity new_assignee)
        {
            if (new_assignee == assignee) return;

            // 处理MinionIdentity和StoredMinionIdentity的代理对象
            if (base.slot != null && new_assignee is MinionIdentity)
            {
                new_assignee = (new_assignee as MinionIdentity).assignableProxy.Get();
            }
            else if (base.slot != null && new_assignee is StoredMinionIdentity)
            {
                new_assignee = (new_assignee as StoredMinionIdentity).assignableProxy.Get();
            }

            // 处理MinionAssignablesProxy的分配
            if (new_assignee is MinionAssignablesProxy)
            {
                var assignableSlotInstance = new_assignee.GetSoleOwner().GetComponent<Ownables>().GetSlot(base.slot);
                if (assignableSlotInstance != null && assignableSlotInstance.assignable != null)
                {
                    assignableSlotInstance.assignable.Unassign();
                }
            }

            base.Assign(new_assignee);
        }

        /// <summary>
        /// 对象生成时的初始化逻辑。
        /// </summary>
        protected override void OnSpawn()
        {
            base.OnSpawn();
            UpdateTint();
            UpdateStatusString();


            base.OnAssign += OnNewAssignment;
            if (assignee != null) return;
            Debug.Log("OnAssign");

            var minionStorage = GetComponent<MinionStorage>();
            if (minionStorage == null) return;

            var storedMinionInfo = minionStorage.GetStoredMinionInfo();
            if (storedMinionInfo.Count > 0)
            {
                var serializedMinion = storedMinionInfo[0].serializedMinion;
                if (serializedMinion != null && serializedMinion.GetId() != -1)
                {
                    var storedMinionIdentity = serializedMinion.Get().GetComponent<StoredMinionIdentity>();
                    storedMinionIdentity.ValidateProxy();
                    Assign(storedMinionIdentity);
                }
            }
        }

        /// <summary>
        /// 更新对象的颜色。
        /// </summary>
        private void UpdateTint()
        {
            if (!tintWhenUnassigned) return;

            var animController = GetComponent<KAnimControllerBase>();
            if (animController != null && animController.HasBatchInstanceData)
            {
                animController.TintColour = (assignee == null) ? unownedTint : ownedTint;
                return;
            }

            var batchedAnimController = GetComponent<KBatchedAnimController>();
            if (batchedAnimController != null && batchedAnimController.HasBatchInstanceData)
            {
                batchedAnimController.TintColour = (assignee == null) ? unownedTint : ownedTint;
            }
        }

        /// <summary>
        /// 更新对象的状态字符串。
        /// </summary>
        private void UpdateStatusString()
        {
            var selectable = GetComponent<KSelectable>();
            if (selectable == null) return;

            var statusItem = (assignee == null) ? Db.Get().BuildingStatusItems.Unassigned :
                (assignee is MinionIdentity) ? Db.Get().BuildingStatusItems.AssignedTo :
                (!(assignee is Room)) ? Db.Get().BuildingStatusItems.AssignedTo : Db.Get().BuildingStatusItems.AssignedTo;

            selectable.SetStatusItem(Db.Get().StatusItemCategories.Ownable, statusItem, this);
        }

        /// <summary>
        /// 获取对象的描述符。
        /// </summary>
        /// <param name="go">游戏对象</param>
        /// <returns>描述符列表</returns>
        public List<Descriptor> GetDescriptors(GameObject go)
        {
            var descriptors = new List<Descriptor>();
            var descriptor = new Descriptor(UI.BUILDINGEFFECTS.ASSIGNEDDUPLICANT, UI.BUILDINGEFFECTS.TOOLTIPS.ASSIGNEDDUPLICANT, Descriptor.DescriptorType.Requirement);
            descriptors.Add(descriptor);
            return descriptors;
        }

        /// <summary>
        /// 处理新的分配逻辑。
        /// </summary>
        /// <param name="new_assignee">新的分配对象</param>
        private void OnNewAssignment(IAssignableIdentity new_assignee)
        {
            UpdateTint();
            UpdateStatusString();
            AssignInherited(new_assignee);
            Debug.Log("开始获取分配逻辑");
        }

        /// <summary>
        /// 处理继承分配逻辑，将当前对象的属性/状态转移到新分配对象
        /// </summary>
        /// <param name="newAssignee">新的分配对象</param>
        private void AssignInherited(IAssignableIdentity newAssignee)
        {
            // 1. 前置条件检查
            if (!CanProcessInheritance(assignee, out GameObject sourceObject))
            {
                return;
            }

            // 2. 设置待处理分配
            this.pendingAssignment = newAssignee;

            // 3. 处理代理对象并获取实际目标
            if (!TryGetActualTargetObject(sourceObject, out GameObject targetObject))
            {
                return;
            }

            // 4. 检查目标对象状态
            var prefabID = targetObject.GetComponent<KPrefabID>();

            // 4.1 可继承的情况（未分配且不是尸体）
            if (IsEligibleForInheritance(prefabID))
            {
                Debug.Log("开始执行分配逻辑");
                ProcessInheritanceMovement(targetObject);
            }
            // 4.2 不可继承的情况
            else
            {
                HandleNonEligibleCases(prefabID, targetObject, newAssignee);
            }
        }

        #region Helper Methods

        /// <summary>
        /// 检查是否可以处理继承逻辑
        /// </summary>
        private bool CanProcessInheritance(IAssignableIdentity currentAssignee, out GameObject sourceObject)
        {
            sourceObject = null;

            if (currentAssignee == null)
            {
                return false;
            }

            var soleOwner = currentAssignee.GetSoleOwner();
            if (soleOwner == null)
            {
                return false;
            }

            sourceObject = soleOwner.gameObject;
            return sourceObject != null;
        }

        /// <summary>
        /// 尝试获取实际目标对象（处理代理情况）
        /// </summary>
        private bool TryGetActualTargetObject(GameObject sourceObject, out GameObject targetObject)
        {
            targetObject = sourceObject;

            var proxy = sourceObject.GetComponent<MinionAssignablesProxy>();
            if (proxy != null)
            {
                targetObject = proxy.GetTargetGameObject();
                this.targetGameObject = targetObject;
            }

            return targetObject != null;
        }

        /// <summary>
        /// 检查对象是否符合继承条件
        /// </summary>
        private bool IsEligibleForInheritance(KPrefabID prefabID)
        {
            return prefabID != null && !prefabID.HasTag(TUNINGS.Assigned) &&!prefabID.HasTag("Corpse");
        }

        /// <summary>
        /// 处理继承移动逻辑
        /// </summary>
        private void ProcessInheritanceMovement(GameObject targetObject)
        {
            var navigator = targetObject.GetComponent<Navigator>();
            if (navigator == null)
            {
                return;
            }

            this.targetCell = Grid.PosToCell(this.transform.position);
            var moveMonitor = navigator.GetSMI<MoveToLocationMonitor.Instance>();

            if (moveMonitor != null)
            {
                moveMonitor.MoveToLocation(this.targetCell);
                isMoving = true;
            }
        }

        /// <summary>
        /// 处理不符合继承条件的情况
        /// </summary>
        private void HandleNonEligibleCases(KPrefabID prefabID, GameObject targetObject, IAssignableIdentity newAssignee)
        {
            // 发送适当通知
            if (prefabID.HasTag(TUNINGS.Assigned))
            {
                NotifyHaveInherited(targetObject);
            }
            else if (prefabID.HasTag("Corpse"))
            {
                NotifyDead(targetObject);
            }

            // 处理代理对象的取消分配
            if (newAssignee is MinionAssignablesProxy proxy)
            {
                var slotInstance = proxy.GetSoleOwner()?.GetComponent<Ownables>()?.GetSlot(base.slot);
                slotInstance?.assignable?.Unassign();
            }
        }

        #endregion








        /// <summary>
        /// 取消分配对象。
        /// </summary>
        public override void Unassign()
        {
            if (targetGameObject != null)
            {
                KPrefabID prefabID = targetGameObject.GetComponent<KPrefabID>();
                if (prefabID != null && !prefabID.HasTag(TUNINGS.DieOfOldAge) && !prefabID.HasTag("Corpse"))
                {
                    // 获取 Navigator 组件并停止其移动
                    var navigator = targetGameObject.GetComponent<Navigator>();
                    if (navigator != null)
                    {
                        navigator.Stop();
                    }
                }
            }

            // 重置状态
            isMoving = false;
            targetGameObject = null;
            targetCell = -1;
            pendingAssignment = null;

            base.Unassign();
        }





        /// <summary>
        /// 每200ms调用一次的模拟逻辑。
        /// </summary>
        /// <param name="dt">时间间隔</param>
        public void Sim200ms(float dt)
        {
            if (isMoving)
            {
                if (this.targetGameObject == null) return;

                var navigator = this.targetGameObject.GetComponent<Navigator>();
                if (navigator == null)
                {
                    Debug.LogWarning("Navigator component not found, skipping.");
                    return;
                }

                if (Grid.PosToCell(navigator.gameObject.transform.position) == targetCell)
                {

                    // 继承成功概率判断
                    if (UnityEngine.Random.Range(0f, 1f) > TUNINGS.TIMERMANAGER.RANDOMDEBUFFTIMERMANAGER.INHERITANCESUCCESSPROBABILITY)
                    {
                        isMoving = false;  // 停止继续移动
                        
                        // 继承失败，发送通知
                        NotifyInheritanceFailure(this.targetGameObject);
                        this.targetGameObject.GetComponent<KPrefabID>().AddTag(TUNINGS.Assigned, true);


                        // this.targetGameObject.GetComponent<KPrefabID>().AddTag(TUNINGS.HeatWanderer, true);
                        //KModDeBuff.ApplyDebuff(this.targetGameObject, "HeatWanderer"); // 添加一个效果
                        //KModDeBuff.ApplyDebuff(this.targetGameObject, "CoolWanderer"); // 添加一个效果

                        // 判断有没有订阅mod扩展
                        bool ModDlc = Main.IsModLoaded("MinionAge _Dlc.Dev");
                        if (ModDlc) { KModDeBuff.ApplyDebuff(targetGameObject, GetRandomDebuff()); }
                        else 
                        {
                            KModDeBuff.ApplyDebuff(targetGameObject, "Immunerejection");
                            KModDeBuff.ApplyDebuff(targetGameObject, "EnergyBoost");
                            KModDeBuff.ApplyDebuff(targetGameObject, "HeatResistance");
                            KModDeBuff.ApplyDebuff(targetGameObject, "ColdResistance");
                            KModDeBuff.ApplyDebuff(targetGameObject, "EfficientWork");
                            KModDeBuff.ApplyDebuff(targetGameObject, "HappyMood");
                            KModDeBuff.ApplyDebuff(targetGameObject, "QuickRecovery");
                            KModDeBuff.ApplyDebuff(targetGameObject, "RadiationResistance");
                            KModDeBuff.ApplyDebuff(targetGameObject, "IronWill");
                            KModDeBuff.ApplyDebuff(targetGameObject, "LuminescenceKing"); 





                        }

                       
                        Destroy(this.gameObject, 1f);
                        return;  // 如果继承失败，不进行继承操作
                    }



                    GetAssignedObjectGameObject(this.targetGameObject, this.pendingAssignment);

                    var radiationItchEmote = Db.Get().Emotes.Minion.MorningStretch;
                    if (radiationItchEmote != null)
                    {
                        KModMiscellaneous.PerformEmoteActionOnSingleGameObject(this.targetGameObject, 10, radiationItchEmote, false);
                    }

                    if (Db.Get().Expressions.Sparkle != null)
                    {
                        KModMiscellaneous.SetExpression(this.targetGameObject, Db.Get().Expressions.Sparkle);
                    }

                    objectTimeRecords.Add(new ObjectTimeRecord
                    {
                        GameObject = this.targetGameObject,
                        Time = Time.time
                    });

                    isMoving = false;
                }
            }

        }

        public static void CleanupExpiredObjectTimeRecords()
        {
            if (Time.time < nextRecordCleanupTime)
            {
                return;
            }

            nextRecordCleanupTime = Time.time + 1f;
            for (int i = objectTimeRecords.Count - 1; i >= 0; i--)
            {
                var record = objectTimeRecords[i];
                if (record.GameObject == null) continue;

                if (Time.time - record.Time > TimeThreshold)
                {
                    if (Db.Get().Expressions.Sparkle != null)
                    {
                        KModMiscellaneous.SetExpression(record.GameObject, Db.Get().Expressions.Sparkle, true);
                    }
                    else
                    {
                        Debug.LogWarning("Db.Get().Expressions.Sparkle is null, skipping expression setting.");
                    }

                    objectTimeRecords.RemoveAt(i);
                }
            }
        }

        // 从效果列表中随机选择一个效果
        private string GetRandomDebuff()
        {
            if (DebuffEffects.Count == 0)
            {
                Debug.LogError("效果列表为空，无法随机选择效果！");
                return null;
            }

            // 随机选择一个索引
            int randomIndex = UnityEngine.Random.Range(0, DebuffEffects.Count);

            // 返回随机选择的效果
            return DebuffEffects[randomIndex];
        }

























        /// <summary>
        /// 获取分配对象的游戏对象并处理继承逻辑。
        /// </summary>
        /// <param name="newObject">新的游戏对象</param>
        /// <param name="assignables">分配对象</param>
        public void GetAssignedObjectGameObject(GameObject newObject, IAssignableIdentity assignables)
        {
            if (newObject == null)
            {
                Debug.LogError("传入的新对象为 null");
                return;
            }

            var prefabID = newObject.GetComponent<KPrefabID>();
            if (!prefabID.HasTag(TUNINGS.Assigned) && !prefabID.HasTag("Corpse"))
            {
                prefabID.AddTag(TUNINGS.Assigned, true);

                MinionDataRandomTransfer.RandomTransferTraits(this.gameObject, newObject);
                MinionDataRandomTransfer.RoundTransferAttributes(this.gameObject, newObject);
                MinionDataRandomTransfer.RandomTransferSkills(this.gameObject, newObject);

                NotifyFinishInherited(newObject);
                Destroy(this.gameObject, 2f);

                if (!inheritedObjects.Contains(newObject))
                {
                    inheritedObjects.Add(newObject);
                }
                else
                {
                    Debug.LogWarning("新对象已经存在于继承列表中，跳过添加");
                }
            }
            else
            {
                NotifyHaveInherited(newObject);
                if (assignables is MinionAssignablesProxy)
                {
                    var assignableSlotInstance = assignables.GetSoleOwner().GetComponent<Ownables>().GetSlot(base.slot);
                    if (assignableSlotInstance != null && assignableSlotInstance.assignable != null)
                    {
                        assignableSlotInstance.assignable.Unassign();
                    }
                }
            }
        }

        /// <summary>
        /// 通知对象已经继承。
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        private static void NotifyHaveInherited(GameObject gameObject)
        {
            var notifier = gameObject.AddOrGet<Notifier>();
            var notification = new Notification(
                STRINGS.MISC.NOTIFICATIONS.NOINHERITED.REPEATED_INHERITANCE,
                NotificationType.Bad,
                (notificationList, data) => notificationList.ReduceMessages(false),
                STRINGS.MISC.NOTIFICATIONS.NOINHERITED.REPEATED_INHERITANCE_DESC + "/t• " + gameObject.GetProperName(),
                true, 0f, null, null, null, true, false, false
            );
            notifier.Add(notification, "");
        }

        /// <summary>
        /// 通知对象已经死亡。
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        private static void NotifyDead(GameObject gameObject)
        {
            var notifier = gameObject.AddOrGet<Notifier>();
            var notification = new Notification(
                STRINGS.MISC.NOTIFICATIONS.NOINHERITED.DEATH, // 使用一个新的死亡通知字符串
                NotificationType.Bad, // 死亡是一个负面事件，所以类型是 Bad
                (notificationList, data) => notificationList.ReduceMessages(false),
                STRINGS.MISC.NOTIFICATIONS.NOINHERITED.DEATH_DESC + "/t• " + gameObject.GetProperName(), // 死亡的描述，包含对象的名称
                true, 0f, null, null, null, true, false, false
            );
            notifier.Add(notification, "");
        }



        /// <summary>
        /// 通知对象继承完成。
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        private static void NotifyFinishInherited(GameObject gameObject)
        {
            var notifier = gameObject.AddOrGet<Notifier>();
            var notification = new Notification(
                STRINGS.MISC.NOTIFICATIONS.YESINHERITED.NAME,
                NotificationType.MessageImportant,
                (notificationList, data) => notificationList.ReduceMessages(false),
                "/t• " + gameObject.GetProperName(),
                true, 0f, null, null, null, true, false, false
            );
            notifier.Add(notification, "");
        }


        /// <summary>
        /// 继承失败时发送通知
        /// </summary>
        /// <param name="gameObject">游戏对象</param>
        private static void NotifyInheritanceFailure(GameObject gameObject)
        {
            var notifier = gameObject.AddOrGet<Notifier>();
            var notification = new Notification(
                STRINGS.MISC.NOTIFICATIONS.INHERITANCE_FAILED.NAME,  // 你可以自定义通知名称
                NotificationType.Bad,
                (notificationList, data) => notificationList.ReduceMessages(false),
                STRINGS.MISC.NOTIFICATIONS.INHERITANCE_FAILED.DESCRIPTION + "/t• " + gameObject.GetProperName(),  // 你可以自定义通知描述
                true, 0f, null, null, null, true, false, false
            );
            notifier.Add(notification, "");
        }

        /// <summary>
        /// 更新UI以反映分配的小人。
        /// </summary>
        /// <param name="minion">小人对象</param>
        private void UpdateUIForAssignedMinion(GameObject minion)
        {
            // 在这里添加代码来更新 UI，例如从 UI 中移除小人相关的信息
        }
    }
}
