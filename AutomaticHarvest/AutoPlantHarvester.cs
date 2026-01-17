using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Klei.AI;
using UnityEngine;
using CykUtils;
using static STRINGS.CREATURES.STATS;
using System.Linq;
using static STRINGS.UI.USERMENUACTIONS.MANUAL_PUMP_DELIVERY;
using static STRINGS.UI.STARMAP;
using System;
using Satsuma.Drawing;
using KSerialization;
using static STRINGS.DUPLICANTS.PERSONALITIES;
using AutomaticHarvest;


public class AutoPlantHarvester : KMonoBehaviour
{
   
    public float growthSpeedMultiplier = 0.5f; // 生长加速倍率
    public List<Tag> allowedTags = new List<Tag>();
    public Vector3 storageFXOffset = Vector3.zero;

    private int xmin, xmax, ymin, ymax;




    [MyCmpGet]
    private RangeVisualizer visualizer; // 可视化组件，用于判断遮挡
    [MyCmpGet]
    private Storage storage;
    [MyCmpGet]
    private LogicOperationalController operational;

    [MyCmpGet]
    private AutomaticHarvestK automaticHarvestK;

   




    private HashSet<GameObject> uniquePlants = new HashSet<GameObject>(); // 去重植物对象集合
    // 是否启用调试日志（默认为关闭）
    public bool enableDebugLogs = false;




    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();

        allowedTags.Add(GameTags.Edible);
        allowedTags.Add(GameTags.Seed);
        allowedTags.Add(GameTags.Organics);
        allowedTags.Add(GameTags.CookingIngredient);
        allowedTags.Add("PlantFiber_");


    }










    // 当建筑生成（spawn）时触发
    protected override void OnSpawn()
    {
        base.OnSpawn();
       


        // 读取可视化组件的范围
        if (visualizer != null)
        {
            xmin = visualizer.RangeMin.x;
            ymin = visualizer.RangeMin.y;
            xmax = visualizer.RangeMax.x;
            ymax = visualizer.RangeMax.y;
        }
        else  // 兜底，防止忘记挂 RangeVisualizer
        {
            xmin = -8; xmax = 8;
            ymin = -3; ymax = 3;
        }



        // 每 5 秒自动调用一次扫描与收获逻辑
       //  InvokeRepeating(nameof(ScanAndHarvestPlants), 1f, scanInterval);
    }







    /// <summary>
    /// 扫描范围内的植物，返回植物的列表（根据条件返回可收获或所有植物）。
    /// </summary>
    /// <param name="onlyHarvestable">如果为 true，则返回可收获的植物；如果为 false，则返回所有植物。</param>
    /// <returns>植物的 GameObject 列表</returns>
    public List<GameObject> ScanPlants(bool onlyHarvestable = true)
    {


        // 获取当前建筑所在的格子索引
        int currentCell = Grid.PosToCell(transform.GetPosition());
        if (!Grid.IsValidCell(currentCell))
            return new List<GameObject>();

        // 分配临时列表资源（使用对象池以减少 GC）
        var cells = ListPool<int, AutoPlantHarvester>.Allocate();
        var plantEntries = ListPool<ScenePartitionerEntry, AutoPlantHarvester>.Allocate();
        var allPlants = new List<GameObject>(); // 存储最终植物列表

        try
        {
            // 1. 计算检测格子范围内的所有非固体格子
            for (int dy = ymin; dy <= ymax; dy++)
            {
                for (int dx = xmin; dx <= xmax; dx++)
                {
                    int testCell = Grid.OffsetCell(currentCell, dx, dy);
                    if (Grid.IsValidCell(testCell) && !Grid.Solid[testCell])
                        cells.Add(testCell);
                }
            }

            // 2. 清空去重集合，收集植物条目
            uniquePlants.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                int targetCell = cells[i];

                // 检查视野遮挡
                if (LineOfSightUtils.IsLineOfSightBlocked(currentCell, targetCell, visualizer))
                    continue;

                Grid.CellToXY(targetCell, out int cellX, out int cellY);
                GameScenePartitioner.Instance.GatherEntries(cellX, cellY, 1, 1, GameScenePartitioner.Instance.plants, plantEntries);
            }

            // 3. 遍历所有找到的植物条目，去重并检查是否满足条件
            foreach (var entry in plantEntries)
            {
                GameObject plantGO = SafeGetGameObject(entry.obj);
                if (plantGO == null) continue;

                // 去重
                if (!uniquePlants.Add(plantGO)) continue;

                Harvestable harvestable = plantGO.GetComponent<Harvestable>();

                if (harvestable != null)
                {
                    // 执行生长加速
                    // Addvalue(plantGO, growthSpeedMultiplier);

                    PreventHarvestTask(plantGO);

                    if (onlyHarvestable)
                    {
                        // 只返回可收获的植物
                        if (harvestable.CanBeHarvested)
                        {
                            allPlants.Add(plantGO);
                        }
                    }
                    else
                    {
                        // 返回所有植物
                        allPlants.Add(plantGO);
                    }
                }
            }
        }
        finally
        {
            cells.Recycle();
            plantEntries.Recycle();
        }

        return allPlants;
    }



    /// <summary>
    /// 对传入的已成熟植物列表执行收获和自动存储逻辑。
    /// </summary>
    public void HarvestAndStorePlants(List<GameObject> plants)
    {
        if (plants == null || plants.Count == 0 || storage == null)
        {
            return;
        }

        WorkerBase skilledWorker = null;
        foreach (var minionGO in GetAllMinionGameObjects())
        {
            var resume = minionGO.GetComponent<MinionResume>();
            if (resume != null && resume.HasPerk(Db.Get().SkillPerks.CanSalvagePlantFiber))
            {
                skilledWorker = minionGO.GetComponent<WorkerBase>();
                break; 
            }
        }

        foreach (GameObject plantGO in plants)
        {
            if (plantGO == null)
                continue;

            Harvestable harvestable = plantGO.GetComponent<Harvestable>();

            if (harvestable != null && harvestable.CanBeHarvested)
            {
                // 记录植物所在的格子，用于延迟存储
                int harvestCell = Grid.PosToCell(plantGO.transform.GetPosition());

                try
                {
                    var fiberProducer = plantGO.GetComponent<PlantFiberProducer>();
                    harvestable.Harvest(); // 执行收获逻辑，掉落物生成
                    if (skilledWorker != null)
                    {
                        if (fiberProducer != null)
                        {
                            // 直接反射调用私有方法 SpawnPlantFiber
                            var spawnMethod = typeof(PlantFiberProducer).GetMethod("SpawnPlantFiber",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                            if (spawnMethod != null)
                            {
                                spawnMethod.Invoke(fiberProducer, null);
                              
                            }
                        }
                    }
                    // 延迟执行 AutoStoreItems，将 harvestCell 作为 data 参数传入 
                    GameScheduler.Instance.Schedule(
                        "DelayedStore" + plantGO.name,
                        0.1f, // 延迟 0.1s 确保掉落物已生成
                        (System.Action<object>)AutoStoreItems,
                        harvestCell,
                        null
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ 收获失败: {plantGO.name}\n{ex}");
                }
            }
        }
    }





    /// <summary>
    /// 自动扫描指定格子并存储物品 (用于延时调用)
    /// </summary>
    private void AutoStoreItems(object data) 
    {
        // 将传入的 data 参数转换回 int 类型的格子索引
        if (!(data is int harvestCell) || !Grid.IsValidCell(harvestCell))
            return;

        if (storage == null)
            return;

        Grid.CellToXY(harvestCell, out int cellX, out int cellY);

        var pickupEntries = ListPool<ScenePartitionerEntry, AutoPlantHarvester>.Allocate();

        // 搜索 3x3 区域
        GameScenePartitioner.Instance.GatherEntries(
            cellX - 1, cellY - 1,
            3, 3,
            GameScenePartitioner.Instance.pickupablesLayer,
            pickupEntries
        );

        // 你可以从某个地方获取建筑位置 (pointA)
        Vector3 buildingPosition = gameObject.transform.position; // 假设建筑位置是 (10, 0, 10)
        // 将植物位置（harvestCell）转换为世界坐标 (pointB)
        Vector3 plantPosition = Grid.CellToPos(harvestCell);

        Game.Instance.SpawnFX(SpawnFXHashes.MeteorImpactMetal, plantPosition, 0f);


        foreach (var entry in pickupEntries)
        {
            GameObject itemGO = SafeGetGameObject(entry.obj);
            Pickupable pickupable = itemGO != null ? itemGO.GetComponent<Pickupable>() : null;
            KPrefabID kPrefabID = itemGO != null ? itemGO.GetComponent<KPrefabID>() : null;

            if (pickupable != null && pickupable.gameObject != null)
            {
                //检查 KPrefabID 是否包含 allowedTags 列表中的任意一个 Tag
                bool hasTargetTag = allowedTags.Any(tag => kPrefabID.HasTag(tag));

                // 只有当物品具有目标 Tag 之一时，才尝试存储
                if (!hasTargetTag) continue;

                // 尝试存储
                if (storage.Store(pickupable.gameObject, true))
                {
                    PopFXManager.Instance.SpawnFX(
                        Def.GetUISprite(pickupable.gameObject, "ui", false).first, 
                        PopFXManager.Instance.sprite_Plus,
                        pickupable.GetProperName() + " " +  GameUtil.GetFormattedMass(
                            pickupable.TotalAmount,
                            GameUtil.TimeSlice.None, 
                            GameUtil.MetricMassFormat.UseThreshold, 
                            true, "{0:0.#}"),
                        pickupable.transform, 
                        this.storageFXOffset + new Vector3(1,0,0), 
                        0.2f, true, false, false);


                }
            }
        }

        pickupEntries.Recycle();
    }


    /// <summary>
    /// 获取所有场景中活跃的复制人对象。
    /// </summary>
    /// <returns>包含所有复制人对象的列表。</returns>
    public static List<GameObject> GetAllMinionGameObjects()
    {
        List<GameObject> minionGameObjects = new List<GameObject>();

        // 遍历所有活跃的复制人身份对象
        foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities.Items)
        {
            if (minionIdentity != null)
            {
                GameObject minionObject = minionIdentity.gameObject; // 直接获取 GameObject
                if (minionObject != null)
                {
                    minionGameObjects.Add(minionObject); // 添加到列表中
                }
            }
        }

        return minionGameObjects; // 返回完整的列表
    }




    /// <summary>
    /// 阻止植物向复制人发布收获任务
    /// </summary>
    private void PreventHarvestTask(GameObject plantGO)
    {
        // 获取植物的 Harvestable 组件
        Harvestable harvestable = plantGO.GetComponent<Harvestable>();

        if (harvestable != null)
        {
            // 如果植物已有收获任务，强制取消
            if (harvestable.HasChore())
            {
                try
                {
                    harvestable.ForceCancelHarvest(); // 强制取消收获任务
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"❌ 取消收获任务失败: {plantGO.name}\n{ex}");
                }
            }

            // 关闭自动收获标记，防止复制人自动收获
            if (harvestable.harvestDesignatable != null)
            {
                harvestable.harvestDesignatable.SetHarvestWhenReady(false);  // 禁用自动收获标记
                harvestable.harvestDesignatable.MarkedForHarvest = false;     // 清除标记
            }
        }
    }

    /// <summary>
    /// 获取 GameObject（包括 Pickupable 和 KPrefabID 对象）
    /// </summary>
    public static GameObject SafeGetGameObject(object obj)
    {
        if (obj == null)
            return null;

        // 直接是 GameObject
        GameObject go = obj as GameObject;
        if (go != null)
            return go;

        // 是 Pickupable
        Pickupable pickupable = obj as Pickupable;
        if (pickupable != null)
            return pickupable.gameObject;

        // 是 KPrefabID
        KPrefabID kprefabID = obj as KPrefabID;
        if (kprefabID != null)
            return kprefabID.gameObject;

        // 其他情况
        return null;
    }

    /// <summary>
    /// 加速植物生长
    /// </summary>
    /// 
    [Obsolete("用于内部测试，当然你想开就直说，直接用就得了兄弟")]
    public void Addvalue(GameObject targetObject, float V)
    {
        Growing growingComponent = targetObject.GetComponent<Growing>();
        if (growingComponent == null)
        {
            Debug.Log("目标对象没有 Growing 组件！");
            return;
        }

        FieldInfo maturityField = typeof(Growing).GetField("maturity", BindingFlags.NonPublic | BindingFlags.Instance);
        if (maturityField != null)
        {
            var maturityInstance = maturityField.GetValue(growingComponent) as AmountInstance;
            if (maturityInstance != null)
            {
                // 修改成熟度值来加速生长
                maturityInstance.value += V;

                // 确保成熟度不会超过最大值
                if (maturityInstance.value > maturityInstance.GetMax())
                {
                    maturityInstance.value = maturityInstance.GetMax();
                }
            }
        }
    }









    //private static readonly HashedString[] PreAnims = new HashedString[] { "grid_pre", "grid_loop" };
    //private static readonly HashedString PostAnim = "grid_pst";
    //// 动画延迟和持续时间
    //private static readonly float DistanceDelay = 0.25f;
    //private static readonly float Duration = 3f;

    //// 绘制声波效果
    //private static void DrawVisualEffect(int centerCell, HashSet<int> cells)
    //{
    //    // 播放声波声音
    //    // SoundEvent.PlayOneShot(GlobalResources.Instance().AcousticDisturbanceSound, Grid.CellToPos(centerCell), 1f);

    //    // 播放声波动画
    //    foreach (int cell in cells)
    //    {
    //        int gridDistance = GetGridDistance(cell, centerCell);
    //        GameScheduler.Instance.Schedule("radialgrid_pre", DistanceDelay * (float)gridDistance, new Action<object>(SpawnEffect), cell, null);
    //    }
    //}

    //// 生成声波动画
    //private static void SpawnEffect(object data)
    //{
    //    int cell = (int)data;
    //    KBatchedAnimController animController = FXHelpers.CreateEffect("radialgrid_kanim", Grid.CellToPosCCC(cell, Grid.SceneLayer.FXFront), null, false, Grid.SceneLayer.FXFront, false);
    //    // 设置动画颜色为红色 (RGBA: 255, 0, 0, 255)
    //    animController.TintColour = new Color32(255, 0, 0, 200);
    //    animController.destroyOnAnimComplete = false;
    //    animController.Play(PreAnims, KAnim.PlayMode.Loop);
    //    GameScheduler.Instance.Schedule("radialgrid_loop", Duration, new Action<object>(DestroyEffect), animController, null);
    //}

    //// 销毁声波动画
    //private static void DestroyEffect(object data)
    //{
    //    KBatchedAnimController animController = (KBatchedAnimController)data;
    //    animController.destroyOnAnimComplete = true;
    //    animController.Play(PostAnim, KAnim.PlayMode.Once, 1f, 0f);
    //}

    //// 计算两个单元格之间的网格距离
    //private static int GetGridDistance(int cell, int centerCell)
    //{
    //    Vector2I cellPos = Grid.CellToXY(cell);
    //    Vector2I centerPos = Grid.CellToXY(centerCell);
    //    return Math.Abs(cellPos.x - centerPos.x) + Math.Abs(cellPos.y - centerPos.y);
    //}


}
