using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Klei.AI;
using UnityEngine;

namespace AutomaticHarvest
{
#pragma warning disable CS0649
    public class AutoPlantHarvester : KMonoBehaviour
    {
        private static readonly Tag[] StoredHarvestTags =
        {
            GameTags.Edible,
            GameTags.Seed,
            GameTags.Organics,
            GameTags.CookingIngredient,
            AutomaticHarvestTags.PlantFiber,
            AutomaticHarvestTags.Kelp,
        };

        private static readonly FieldInfo MaturityField = typeof(Growing).GetField("maturity", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo SpawnPlantFiberMethod = typeof(PlantFiberProducer).GetMethod("SpawnPlantFiber", BindingFlags.Instance | BindingFlags.NonPublic);

        public Vector3 storageFXOffset = Vector3.zero;

        [MyCmpGet]
        private RangeVisualizer visualizer;

        [MyCmpGet]
        private Storage storage;

        private readonly HashSet<GameObject> uniquePlants = new HashSet<GameObject>();

        private int rangeMinX;
        private int rangeMaxX;
        private int rangeMinY;
        private int rangeMaxY;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            InitializeHarvestRange();
        }

        public List<GameObject> ScanPlants(bool onlyHarvestable = true)
        {
            int originCell = Grid.PosToCell(transform.GetPosition());
            if (!Grid.IsValidCell(originCell))
            {
                return new List<GameObject>();
            }

            var cells = ListPool<int, AutoPlantHarvester>.Allocate();
            var plantEntries = ListPool<ScenePartitionerEntry, AutoPlantHarvester>.Allocate();
            var plants = new List<GameObject>();

            try
            {
                CollectOpenCellsInRange(originCell, cells);
                GatherUniquePlants(originCell, cells, plantEntries, plants, onlyHarvestable);
            }
            finally
            {
                cells.Recycle();
                plantEntries.Recycle();
            }

            return plants;
        }

        public void HarvestAndStorePlants(List<GameObject> plants)
        {
            if (plants == null || plants.Count == 0 || storage == null)
            {
                return;
            }

            WorkerBase fiberSkilledWorker = FindPlantFiberSkilledWorker();

            foreach (GameObject plant in plants)
            {
                if (plant == null)
                {
                    continue;
                }

                Harvestable harvestable = plant.GetComponent<Harvestable>();
                if (harvestable == null || !harvestable.CanBeHarvested)
                {
                    continue;
                }

                int harvestCell = Grid.PosToCell(plant.transform.GetPosition());

                try
                {
                    PlantFiberProducer fiberProducer = plant.GetComponent<PlantFiberProducer>();
                    harvestable.Harvest();

                    if (fiberSkilledWorker != null && fiberProducer != null)
                    {
                        SpawnPlantFiberMethod?.Invoke(fiberProducer, null);
                    }

                    GameScheduler.Instance.Schedule(
                        "DelayedStore" + plant.name,
                        0.1f,
                        AutoStoreItems,
                        harvestCell,
                        null);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AutomaticHarvest: failed to harvest {plant.name}\n{ex}");
                }
            }
        }

        public static GameObject SafeGetGameObject(object obj)
        {
            if (obj is GameObject gameObject)
            {
                return gameObject;
            }

            if (obj is Pickupable pickupable)
            {
                return pickupable.gameObject;
            }

            if (obj is KPrefabID prefabId)
            {
                return prefabId.gameObject;
            }

            return null;
        }

        [Obsolete("Debug helper for manually accelerating plant growth.")]
        public void AddGrowth(GameObject targetObject, float maturityDelta)
        {
            Growing growingComponent = targetObject.GetComponent<Growing>();
            if (growingComponent == null || MaturityField == null)
            {
                return;
            }

            if (MaturityField.GetValue(growingComponent) is AmountInstance maturity)
            {
                maturity.value = Mathf.Min(maturity.value + maturityDelta, maturity.GetMax());
            }
        }

        private void InitializeHarvestRange()
        {
            if (visualizer == null)
            {
                rangeMinX = -8;
                rangeMaxX = 8;
                rangeMinY = -3;
                rangeMaxY = 3;
                return;
            }

            rangeMinX = visualizer.RangeMin.x;
            rangeMaxX = visualizer.RangeMax.x;
            rangeMinY = visualizer.RangeMin.y;
            rangeMaxY = visualizer.RangeMax.y;
        }

        private void CollectOpenCellsInRange(int originCell, List<int> cells)
        {
            for (int y = rangeMinY; y <= rangeMaxY; y++)
            {
                for (int x = rangeMinX; x <= rangeMaxX; x++)
                {
                    int targetCell = Grid.OffsetCell(originCell, x, y);
                    if (Grid.IsValidCell(targetCell) && !Grid.Solid[targetCell])
                    {
                        cells.Add(targetCell);
                    }
                }
            }
        }

        private void GatherUniquePlants(
            int originCell,
            List<int> cells,
            List<ScenePartitionerEntry> plantEntries,
            List<GameObject> plants,
            bool onlyHarvestable)
        {
            uniquePlants.Clear();

            for (int i = 0; i < cells.Count; i++)
            {
                int targetCell = cells[i];
                if (global::LineOfSightUtils.IsLineOfSightBlocked(originCell, targetCell, visualizer))
                {
                    continue;
                }

                Grid.CellToXY(targetCell, out int cellX, out int cellY);
                GameScenePartitioner.Instance.GatherEntries(cellX, cellY, 1, 1, GameScenePartitioner.Instance.plants, plantEntries);
            }

            foreach (ScenePartitionerEntry entry in plantEntries)
            {
                GameObject plant = SafeGetGameObject(entry.obj);
                if (plant == null || !uniquePlants.Add(plant))
                {
                    continue;
                }

                Harvestable harvestable = plant.GetComponent<Harvestable>();
                if (harvestable == null)
                {
                    continue;
                }

                PreventHarvestTask(plant, harvestable);
                if (!onlyHarvestable || harvestable.CanBeHarvested)
                {
                    plants.Add(plant);
                }
            }
        }

        private void AutoStoreItems(object data)
        {
            if (!(data is int harvestCell) || !Grid.IsValidCell(harvestCell) || storage == null)
            {
                return;
            }

            Grid.CellToXY(harvestCell, out int cellX, out int cellY);
            var pickupEntries = ListPool<ScenePartitionerEntry, AutoPlantHarvester>.Allocate();

            try
            {
                GameScenePartitioner.Instance.GatherEntries(
                    cellX - 1,
                    cellY - 1,
                    3,
                    3,
                    GameScenePartitioner.Instance.pickupablesLayer,
                    pickupEntries);

                Vector3 plantPosition = Grid.CellToPos(harvestCell);
                Game.Instance.SpawnFX(SpawnFXHashes.MeteorImpactMetal, plantPosition, 0f);

                foreach (ScenePartitionerEntry entry in pickupEntries)
                {
                    TryStorePickup(entry);
                }
            }
            finally
            {
                pickupEntries.Recycle();
            }
        }

        private void TryStorePickup(ScenePartitionerEntry entry)
        {
            GameObject item = SafeGetGameObject(entry.obj);
            Pickupable pickupable = item != null ? item.GetComponent<Pickupable>() : null;
            KPrefabID prefabId = item != null ? item.GetComponent<KPrefabID>() : null;

            if (pickupable == null || prefabId == null || !StoredHarvestTags.Any(prefabId.HasTag))
            {
                return;
            }

            if (!storage.Store(pickupable.gameObject, true))
            {
                return;
            }

            PopFXManager.Instance.SpawnFX(
                Def.GetUISprite(pickupable.gameObject, "ui", false).first,
                PopFXManager.Instance.sprite_Plus,
                pickupable.GetProperName() + " " + GameUtil.GetFormattedMass(
                    pickupable.TotalAmount,
                    GameUtil.TimeSlice.None,
                    GameUtil.MetricMassFormat.UseThreshold,
                    true,
                    "{0:0.#}"),
                pickupable.transform,
                storageFXOffset + new Vector3(1, 0, 0),
                0.2f,
                true,
                false,
                false);
        }

        private static WorkerBase FindPlantFiberSkilledWorker()
        {
            foreach (MinionIdentity minionIdentity in Components.LiveMinionIdentities.Items)
            {
                if (minionIdentity == null)
                {
                    continue;
                }

                MinionResume resume = minionIdentity.GetComponent<MinionResume>();
                if (resume != null && resume.HasPerk(Db.Get().SkillPerks.CanSalvagePlantFiber))
                {
                    return minionIdentity.GetComponent<WorkerBase>();
                }
            }

            return null;
        }

        private static void PreventHarvestTask(GameObject plant, Harvestable harvestable)
        {
            if (harvestable.HasChore())
            {
                try
                {
                    harvestable.ForceCancelHarvest();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AutomaticHarvest: failed to cancel harvest chore for {plant.name}\n{ex}");
                }
            }

            if (harvestable.harvestDesignatable != null)
            {
                harvestable.harvestDesignatable.SetHarvestWhenReady(false);
                harvestable.harvestDesignatable.MarkedForHarvest = false;
            }
        }
    }
#pragma warning restore CS0649
}
