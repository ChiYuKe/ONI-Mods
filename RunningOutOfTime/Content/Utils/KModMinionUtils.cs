using System.Collections.Generic;
using UnityEngine;



namespace CykUtils
{
    public static class KModMinionUtils
    {
        /// <summary>
        /// 获取所有场景中活跃的复制人对象（排除带有 Bionic 标签的）。
        /// </summary>
        /// <returns>包含所有符合条件复制人对象的列表。</returns>
        public static List<GameObject> GetAllMinionGameObjects()
        {
            List<GameObject> minionGameObjects = new List<GameObject>();

            // 遍历所有活跃的复制人身份对象
            foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities.Items)
            {
                if (minionIdentity != null)
                {
                    GameObject minionObject = minionIdentity.gameObject;
                    if (minionObject != null)
                    {
                        // 检查是否包含 Bionic 标签
                        KPrefabID prefabID = minionObject.GetComponent<KPrefabID>();
                        if (prefabID != null && prefabID.HasTag(GameTags.Minions.Models.Bionic))
                        {
                            continue; // 跳过仿生复制人
                        }

                        minionGameObjects.Add(minionObject); // 添加到列表中
                    }
                }
            }

            return minionGameObjects; // 返回完整的列表
        }


        // 封装信息打印的静态方法
        public static void PrintMinionIdentityInfo(GameObject obj)
        {
            MinionIdentity minionIdentity = obj.GetComponent<MinionIdentity>();

            if (minionIdentity == null)
            {
                Debug.Log("【KMod】MinionIdentity 组件为 null。");
                return;
            }

            // 打印属性信息
            Vector3 position = minionIdentity.transform.position;
            Debug.Log("复制人身份信息:");
            Debug.Log($"- 名字: {minionIdentity.name}");
            Debug.Log($"- 性别: {minionIdentity.gender}");
            Debug.Log($"- 当前位置: {position}");
            Debug.Log($"- 贴纸类型: {minionIdentity.stickerType}");
            Debug.Log($"- 生成时间时间: {minionIdentity.arrivalTime}");
            Debug.Log($"- 语音索引: {minionIdentity.voiceIdx}");
            Debug.Log($"- 语音 ID: {minionIdentity.voiceIdx}");
            Debug.Log($"- 性别字符串键: {minionIdentity.genderStringKey}");
            Debug.Log($"- 名字字符串键: {minionIdentity.nameStringKey}");
            Debug.Log($"- 个性资源 ID: {minionIdentity.personalityResourceId}");
            Debug.Log($"- 上次说话时间: {minionIdentity.timeLastSpoke}");
            Debug.Log($"- 是否加入身份列表: {minionIdentity.addToIdentityList}");

            // 打印组件信息

            if (minionIdentity.assignableProxy != null)
            {
                Debug.Log($"可分配代理: {minionIdentity.assignableProxy.Get().name}");
            }
            else
            {
                Debug.Log("可分配代理: 不存在");
            }

            // 打印拥有者信息
            Debug.Log("拥有者:");
            foreach (var owner in minionIdentity.GetOwners())
            {
                Debug.Log($"- {owner.name}");
            }
        }

        public static void PrintMinionResumeInfo(GameObject obj)
        {
            MinionResume minionresume = obj.GetComponent<MinionResume>();

            if (minionresume == null)
            {
                Debug.Log("【KMod】MinionResume 组件为 null。");
                return;
            }
            Debug.Log("复制人简历信息:");
            Debug.Log($"- 身份: {minionresume.GetIdentity?.GetProperName() ?? "未知"}");
            Debug.Log($"- 当前角色: {minionresume.CurrentRole}");
            Debug.Log($"- 目标角色: {minionresume.TargetRole}");
            Debug.Log($"- 当前帽子: {minionresume.CurrentHat}");
            Debug.Log($"- 目标帽子: {minionresume.TargetHat}");
            Debug.Log($"- 总经验值: {minionresume.TotalExperienceGained}");
            Debug.Log($"- 总技能点数: {minionresume.TotalSkillPointsGained}");
            Debug.Log($"- 已掌握技能: {minionresume.SkillsMastered}");
            Debug.Log($"- 可用技能点数: {minionresume.AvailableSkillpoints}");

            Debug.Log($"- 按角色 ID 掌握情况:");
            foreach (var kvp in minionresume.MasteryByRoleID)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }

            Debug.Log($"- 按技能 ID 掌握情况:");
            foreach (var kvp in minionresume.MasteryBySkillID)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }

            Debug.Log($"- 授予的技能 ID:");
            foreach (var skillId in minionresume.GrantedSkillIDs)
            {
                Debug.Log($"  - {skillId}");
            }

            Debug.Log($"- 按角色组适应度:");
            foreach (var kvp in minionresume.AptitudeByRoleGroup)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }

            Debug.Log($"- 按技能组适应度:");
            foreach (var kvp in minionresume.AptitudeBySkillGroup)
            {
                Debug.Log($"  - {kvp.Key}: {kvp.Value}");
            }

            Debug.Log($"- DEBUG 被动经验值: {minionresume.DEBUG_PassiveExperienceGained}");
            Debug.Log($"- DEBUG 主动经验值: {minionresume.DEBUG_ActiveExperienceGained}");
            Debug.Log($"- DEBUG 存活时间（秒）: {minionresume.DEBUG_SecondsAlive}");
        }


        public static void PrintHealthInfo(GameObject obj)
        {

            // 从 GameObject 获取 Health 组件
            Health health = obj.GetComponent<Health>();
            if (health == null)
            {
                Debug.Log("【KMod】Health 组件为 null。");
                return;
            }

            // 打印 Health 组件的信息
            Debug.Log("生命状态信息:");
            Debug.Log($"- 是否可以被使瘫痪: {(health.canBeIncapacitated ? "是" : "否")}");
            Debug.Log($"- 当前状态: {GetHealthStateDescription(health.State)}");
            Debug.Log($"- 当前生命值: {health.hitPoints}");
            Debug.Log($"- 最大生命值: {health.maxHitPoints}");
            Debug.Log($"- 生命值百分比: {health.percent() * 100}%");
        }

        private static string GetHealthStateDescription(Health.HealthState state)
        {
            switch (state)
            {
                case Health.HealthState.Perfect:
                    return "完美";
                case Health.HealthState.Alright:
                    return "良好";
                case Health.HealthState.Scuffed:
                    return "受损";
                case Health.HealthState.Injured:
                    return "受伤";
                case Health.HealthState.Critical:
                    return "危急";
                case Health.HealthState.Incapacitated:
                    return "瘫痪";
                case Health.HealthState.Dead:
                    return "死亡";
                case Health.HealthState.Invincible:
                    return "无敌";
                default:
                    return "未知";
            }
        }


        public static void PrintNavigatorInfo(GameObject obj)
        {
            Navigator navigator = obj.GetComponent<Navigator>();

            if (navigator == null)
            {
                Debug.Log("【KMod】Navigator 组件为 null。");
                return;
            }

            // 打印 Navigator 的基本信息
            Debug.Log($"Navigator 组件信息:");
            Debug.Log($"- 当前导航类型 (CurrentNavType): {navigator.CurrentNavType}");
            Debug.Log($"- 默认速度 (defaultSpeed): {navigator.defaultSpeed}");
            Debug.Log($"- 导航网格名称 (NavGridName): {navigator.NavGridName}");
            Debug.Log($"- 更新探测器 (updateProber): {navigator.updateProber}");
            // Debug.Log($"- 最大探测半径 (maxProbingRadius): {navigator.maxProbingRadius}");

            // 打印 distanceTravelledByNavType 字典的内容
            Debug.Log("distanceTravelledByNavType:");
            foreach (var entry in navigator.distanceTravelledByNavType)
            {
                Debug.Log($"  导航类型 (NavType): {entry.Key}, 距离 (Distance): {entry.Value}");
            }

            // 打印当前是否在移动状态
            Debug.Log($"- 是否移动中 (IsMoving): {navigator.IsMoving()}");

            // 打印当前的目标
            if (navigator.target != null)
            {
                Debug.Log($"- 目标 (Target): {navigator.target.name}");
            }
            else
            {
                Debug.Log("- 目标 (Target): 无");
            }

        }

        public static void PrintChoreDriverInfo(GameObject obj)
        {
            ChoreDriver choreDriver = obj.GetComponent<ChoreDriver>();

            if (choreDriver == null)
            {
                Debug.Log("【KMod】ChoreDriver 组件为 null。");
                return;
            }

            // 打印 ChoreDriver 的基本信息
            Debug.Log("ChoreDriver 组件信息:");

            // 打印当前 chore
            Chore currentChore = choreDriver.GetCurrentChore();
            if (currentChore != null)
            {
                Debug.Log($"- 当前 Chore: {currentChore.GetType().Name}");
                // 根据需要进一步打印 Chore 的详细信息
            }
            else
            {
                Debug.Log("- 当前 Chore: 无");
            }

            // 打印是否有 chore
            bool hasChore = choreDriver.HasChore();
            Debug.Log($"- 是否有 Chore: {hasChore}");


            // 打印 User 组件信息
            User user = choreDriver.GetComponent<User>();
            if (user != null)
            {
                Debug.Log($"- User 组件信息: {user.GetType().Name}");
                // 根据需要打印 User 组件的详细信息
            }
            else
            {
                Debug.Log("- User 组件: 无");
            }

            // 打印 ChoreDriver 状态机的信息
            ChoreDriver.StatesInstance statesInstance = choreDriver.GetComponent<ChoreDriver.StatesInstance>();
            if (statesInstance != null)
            {
                Debug.Log("ChoreDriver 状态机信息:");
                Debug.Log($"- masterProperName: {statesInstance.masterProperName}");
                Debug.Log($"- masterPrefabId: {statesInstance.masterPrefabId?.GetType().Name ?? "无"}");
                Debug.Log($"- navigator: {statesInstance.navigator?.GetType().Name ?? "无"}");
                Debug.Log($"- worker: {statesInstance.worker?.GetType().Name ?? "无"}");

                // 打印当前和下一个 chore
                Debug.Log($"- 当前 Chore: {statesInstance.GetCurrentChore()?.GetType().Name ?? "无"}");
                Debug.Log($"- 下一个 Chore: {statesInstance.GetNextChore()?.GetType().Name ?? "无"}");
            }
            else
            {
                Debug.Log("ChoreDriver 状态机信息: 状态实例为 null。");
            }
        }


        public static void PrintAllComponents(GameObject obj)
        {
            // 确保 GameObject 不为空
            if (obj == null)
            {
                Debug.Log("GameObject 为 null。");
                return;
            }

            // 获取所有组件
            Component[] components = obj.GetComponents<Component>();

            // 打印组件信息
            if (components.Length == 0)
            {
                Debug.Log("没有找到任何组件。");
                return;
            }

            Debug.Log("所有组件信息:");

            foreach (Component component in components)
            {
                if (component != null)
                {
                    Debug.Log($"- 组件类型: {component.GetType().Name}");

                    // 如果需要，可以进一步打印组件的字段信息
                    // 例如，使用反射打印所有字段
                    foreach (var field in component.GetType().GetFields())
                    {
                        object value = field.GetValue(component);
                        Debug.Log($"  - 字段: {field.Name}, 值: {value}");
                    }
                }
            }
        }
    }
}