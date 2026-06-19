using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KSerialization;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkOrderProductionCenterFabricator : ComplexFabricator
    {
        private static readonly FieldInfo RecipeListField = AccessTools.Field(typeof(ComplexFabricator), "recipe_list");
        private static readonly FieldInfo RecipeQueueCountsField = AccessTools.Field(typeof(ComplexFabricator), "recipeQueueCounts");
        private static readonly FieldInfo OpenOrderCountsField = AccessTools.Field(typeof(ComplexFabricator), "openOrderCounts");
        private static readonly FieldInfo NextOrderIdxField = AccessTools.Field(typeof(ComplexFabricator), "nextOrderIdx");
        private static readonly FieldInfo WorkingOrderIdxField = AccessTools.Field(typeof(ComplexFabricator), "workingOrderIdx");
        private static readonly FieldInfo OrderProgressField = AccessTools.Field(typeof(ComplexFabricator), "orderProgress");
        private static readonly FieldInfo QueueDirtyField = AccessTools.Field(typeof(ComplexFabricator), "queueDirty");
        private static readonly FieldInfo HasOpenOrdersField = AccessTools.Field(typeof(ComplexFabricator), "hasOpenOrders");
        private static readonly FieldInfo HeatedTemperatureField = AccessTools.Field(typeof(ComplexFabricator), "heatedTemperature");

        [Serialize]
        private List<CoreState> cores = new List<CoreState>();

        [MyCmpGet]
        private StorageNetworkOrderProductionCenter center = null;

        private readonly ProgressBar[] worldProgressBars = new ProgressBar[3];

        public int ActiveCoreCount => center != null
            ? Mathf.Clamp(center.DiskSlots.Count(slot => slot != null && slot.HasDisk), 0, 3)
            : 0;

        public IReadOnlyList<CoreState> Cores
        {
            get
            {
                EnsureCores();
                return cores;
            }
        }

        public IEnumerable<CoreState> ActiveCores
        {
            get
            {
                EnsureCores();
                return cores.Take(ActiveCoreCount);
            }
        }

        public bool HasParallelWorkingOrder
        {
            get
            {
                EnsureCores();
                return ActiveCores.Any(core => core.IsWorking);
            }
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureCores();
            EnsureSafeOutputTemperature();
            SyncVanillaCurrentOrder();
            RefreshWorldProgressBars();
        }

        protected override void OnCleanUp()
        {
            ClearWorldProgressBars();
            base.OnCleanUp();
        }

        public override void Sim1000ms(float dt)
        {
            ComplexRecipe[] recipes = RecipeListField?.GetValue(this) as ComplexRecipe[];
            if (recipes == null || recipes.Length == 0)
            {
                QueueDirtyField?.SetValue(this, false);
                HasOpenOrdersField?.SetValue(this, false);
                return;
            }

            ClampOrderIndex(NextOrderIdxField, recipes.Length);
            ClampOrderIndex(WorkingOrderIdxField, recipes.Length);
            QueueDirtyField?.SetValue(this, false);
            HasOpenOrdersField?.SetValue(this, HasAnyQueuedRecipe());
            SyncVanillaCurrentOrder();
            RefreshWorldProgressBars();
        }

        public void SetEngravedRecipeIds(IEnumerable<string> recipeIds)
        {
            ComplexRecipe[] recipes = (recipeIds ?? Enumerable.Empty<string>())
                .Select(id => ComplexRecipeManager.Get().GetRecipe(id))
                .Where(recipe => recipe != null && Game.IsCorrectDlcActiveForCurrentSave(recipe))
                .OrderBy(recipe => recipe.GetUIName(false))
                .ToArray();
            RecipeListField?.SetValue(this, recipes);

            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(this) as Dictionary<string, int>;
            if (queueCounts != null)
            {
                HashSet<string> validRecipeIds = new HashSet<string>(recipes.Select(recipe => recipe.id));
                foreach (string queuedRecipeId in queueCounts.Keys.ToList())
                {
                    if (!validRecipeIds.Contains(queuedRecipeId))
                    {
                        queueCounts.Remove(queuedRecipeId);
                    }
                }

                foreach (ComplexRecipe recipe in recipes)
                {
                    if (!queueCounts.ContainsKey(recipe.id))
                    {
                        queueCounts.Add(recipe.id, 0);
                    }
                }
            }

            EnsureOpenOrderCountsMatchRecipes();

            ClampOrderIndex(NextOrderIdxField, recipes.Length);
            ClampOrderIndex(WorkingOrderIdxField, recipes.Length);
            HasOpenOrdersField?.SetValue(this, queueCounts != null && queueCounts.Values.Any(count => count != 0));
            QueueDirtyField?.SetValue(this, true);
            StopInvalidCores();
            SyncVanillaCurrentOrder();
            RefreshWorldProgressBars();
        }

        public void SetOrderCenterRecipeQueueCount(ComplexRecipe recipe, int count)
        {
            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(this) as Dictionary<string, int>;
            if (recipe == null || queueCounts == null)
            {
                return;
            }

            queueCounts[recipe.id] = count;
            EnsureOpenOrderCountsMatchRecipes();
            HasOpenOrdersField?.SetValue(this, queueCounts.Values.Any(value => value != 0));
            QueueDirtyField?.SetValue(this, true);
            SyncVanillaCurrentOrder();
            Trigger(1721324763, this);
        }

        public void TickParallelCores(float dt)
        {
            EnsureCores();
            if (operational != null && !operational.IsOperational)
            {
                return;
            }

            int activeCoreCount = ActiveCoreCount;
            for (int i = activeCoreCount; i < cores.Count; i++)
            {
                StopCore(cores[i]);
            }

            for (int i = 0; i < activeCoreCount; i++)
            {
                CoreState core = cores[i];
                if (!core.IsWorking)
                {
                    TryStartCore(core);
                }
            }

            bool completedAny = false;
            for (int i = 0; i < activeCoreCount; i++)
            {
                CoreState core = cores[i];
                if (!core.IsWorking)
                {
                    continue;
                }

                ComplexRecipe recipe = GetRecipe(core.RecipeId);
                if (recipe == null)
                {
                    StopCore(core);
                    continue;
                }

                core.Progress += ComputeWorkProgress(dt, recipe);
                if (core.Progress >= 1f)
                {
                    CompleteCore(core, recipe);
                    completedAny = true;
                }
            }

            if (completedAny)
            {
                QueueDirtyField?.SetValue(this, true);
            }

            SyncVanillaCurrentOrder();
            RefreshWorldProgressBars();
        }

        public float GetProgressForRecipe(ComplexRecipe recipe)
        {
            if (recipe == null)
            {
                return 0f;
            }

            EnsureCores();
            return ActiveCores
                .Where(core => core.IsWorking && core.RecipeId == recipe.id)
                .Select(core => Mathf.Clamp01(core.Progress))
                .DefaultIfEmpty(0f)
                .Max();
        }

        public int GetWorkingCountForRecipe(ComplexRecipe recipe)
        {
            if (recipe == null)
            {
                return 0;
            }

            EnsureCores();
            return ActiveCores.Count(core => core.IsWorking && core.RecipeId == recipe.id);
        }

        private void TryStartCore(CoreState core)
        {
            ComplexRecipe recipe = FindNextStartableRecipe();
            if (recipe == null)
            {
                return;
            }

            TransferRecipeIngredientsForBuild(recipe);
            DecrementQueueCount(recipe);
            core.RecipeId = recipe.id;
            core.Progress = 0f;
            Trigger(2023536846, recipe);
        }

        private ComplexRecipe FindNextStartableRecipe()
        {
            ComplexRecipe[] recipes = RecipeListField?.GetValue(this) as ComplexRecipe[];
            if (recipes == null || recipes.Length == 0)
            {
                return null;
            }

            string activeRecipeId = ActiveCores.FirstOrDefault(core => core.IsWorking)?.RecipeId;
            if (!string.IsNullOrEmpty(activeRecipeId))
            {
                ComplexRecipe activeRecipe = GetRecipe(activeRecipeId);
                return activeRecipe != null && GetQueueCount(activeRecipe) != 0 && HasIngredients(activeRecipe, inStorage)
                    ? activeRecipe
                    : null;
            }

            int startIndex = Mathf.Clamp(GetIntField(NextOrderIdxField, this, 0), 0, recipes.Length - 1);
            for (int offset = 0; offset < recipes.Length; offset++)
            {
                int index = (startIndex + offset) % recipes.Length;
                ComplexRecipe recipe = recipes[index];
                if (recipe != null && GetQueueCount(recipe) != 0 && HasIngredients(recipe, inStorage))
                {
                    NextOrderIdxField?.SetValue(this, (index + 1) % recipes.Length);
                    return recipe;
                }
            }

            return null;
        }

        private void CompleteCore(CoreState core, ComplexRecipe recipe)
        {
            SanitizeBuildStorageTemperatures(recipe);
            EnsureSafeOutputTemperature();
            SpawnOrderProduct(recipe);
            core.Clear();
            Trigger(1355439576, recipe);
        }

        private void StopInvalidCores()
        {
            EnsureCores();
            foreach (CoreState core in cores)
            {
                if (!core.IsWorking || GetRecipe(core.RecipeId) != null)
                {
                    continue;
                }

                StopCore(core);
            }
        }

        private void StopCore(CoreState core)
        {
            if (core == null || !core.IsWorking)
            {
                return;
            }

            core.Clear();
        }

        private void TransferRecipeIngredientsForBuild(ComplexRecipe recipe)
        {
            if (recipe?.ingredients == null || inStorage == null || buildStorage == null)
            {
                return;
            }

            foreach (ComplexRecipe.RecipeElement ingredient in recipe.ingredients)
            {
                float remaining = ingredient.amount;
                while (remaining > 0f && inStorage.GetAmountAvailable(ingredient.material) > 0f)
                {
                    float before = buildStorage.GetAmountAvailable(ingredient.material);
                    inStorage.TransferUnitMass(buildStorage, ingredient.material, remaining, false, false, true);
                    float moved = buildStorage.GetAmountAvailable(ingredient.material) - before;
                    if (moved <= 0f)
                    {
                        break;
                    }

                    remaining -= moved;
                }

                if (remaining > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    Debug.LogWarningFormat(gameObject, "Parallel order center ran out of {0} but still needed {1} more.", ingredient.material, remaining);
                }
            }

            SanitizeBuildStorageTemperatures(recipe);
        }

        private void SanitizeBuildStorageTemperatures(ComplexRecipe recipe)
        {
            if (buildStorage?.items == null)
            {
                return;
            }

            float fallbackTemperature = GetSafeOutputTemperature();
            foreach (GameObject item in buildStorage.items)
            {
                PrimaryElement primaryElement = item != null ? item.GetComponent<PrimaryElement>() : null;
                if (primaryElement == null)
                {
                    continue;
                }

                if (!IsValidOutputTemperature(primaryElement.Temperature))
                {
                    primaryElement.Temperature = fallbackTemperature;
                }
            }
        }

        internal float GetSafeOutputTemperature()
        {
            PrimaryElement primaryElement = GetComponent<PrimaryElement>();
            if (primaryElement != null && IsValidOutputTemperature(primaryElement.Temperature))
            {
                return primaryElement.Temperature;
            }

            return 293.15f;
        }

        internal void EnsureSafeOutputTemperature()
        {
            if (HeatedTemperatureField == null)
            {
                return;
            }

            float heatedTemperature = (float)HeatedTemperatureField.GetValue(this);
            if (!IsValidOutputTemperature(heatedTemperature))
            {
                HeatedTemperatureField.SetValue(this, GetSafeOutputTemperature());
            }
        }

        internal static bool IsValidOutputTemperature(float temperature)
        {
            return temperature > 0.1f && !float.IsNaN(temperature) && !float.IsInfinity(temperature);
        }

        private void DecrementQueueCount(ComplexRecipe recipe)
        {
            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(this) as Dictionary<string, int>;
            if (recipe == null || queueCounts == null || !queueCounts.TryGetValue(recipe.id, out int count))
            {
                return;
            }

            if (count == ComplexFabricator.QUEUE_INFINITE)
            {
                return;
            }

            queueCounts[recipe.id] = Mathf.Max(0, count - 1);
            HasOpenOrdersField?.SetValue(this, queueCounts.Values.Any(value => value != 0));
            QueueDirtyField?.SetValue(this, true);
        }

        private int GetQueueCount(ComplexRecipe recipe)
        {
            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(this) as Dictionary<string, int>;
            if (recipe == null || queueCounts == null || !queueCounts.TryGetValue(recipe.id, out int count))
            {
                return 0;
            }

            return count;
        }

        private void SyncVanillaCurrentOrder()
        {
            EnsureCores();
            ComplexRecipe[] recipes = RecipeListField?.GetValue(this) as ComplexRecipe[];
            CoreState firstWorkingCore = ActiveCores.FirstOrDefault(core => core.IsWorking);
            if (firstWorkingCore == null || recipes == null)
            {
                WorkingOrderIdxField?.SetValue(this, -1);
                OrderProgressField?.SetValue(this, 0f);
                return;
            }

            int index = System.Array.FindIndex(recipes, recipe => recipe != null && recipe.id == firstWorkingCore.RecipeId);
            WorkingOrderIdxField?.SetValue(this, index);
            OrderProgressField?.SetValue(this, Mathf.Clamp01(firstWorkingCore.Progress));
        }

        private void RefreshWorldProgressBars()
        {
            EnsureCores();
            for (int i = 0; i < worldProgressBars.Length; i++)
            {
                bool shouldShow = i < ActiveCoreCount && cores[i].IsWorking;
                if (shouldShow && worldProgressBars[i] == null)
                {
                    int coreIndex = i;
                    worldProgressBars[i] = ProgressBar.CreateProgressBar(gameObject, () => GetCoreProgress(coreIndex), GetWorldProgressOffset(coreIndex));
                    worldProgressBars[i].autoHide = false;
                }

                if (worldProgressBars[i] != null)
                {
                    worldProgressBars[i].SetVisibility(shouldShow);
                }
            }
        }

        private void ClearWorldProgressBars()
        {
            for (int i = 0; i < worldProgressBars.Length; i++)
            {
                if (worldProgressBars[i] != null)
                {
                    worldProgressBars[i].gameObject.DeleteObject();
                    worldProgressBars[i] = null;
                }
            }
        }

        private float GetCoreProgress(int coreIndex)
        {
            EnsureCores();
            return coreIndex >= 0 && coreIndex < cores.Count && cores[coreIndex].IsWorking
                ? Mathf.Clamp01(cores[coreIndex].Progress)
                : 0f;
        }

        private static Vector3 GetWorldProgressOffset(int coreIndex)
        {
            return new Vector3(0f, 0.1f - coreIndex * 0.22f, 0f);
        }

        private void EnsureCores()
        {
            if (cores == null)
            {
                cores = new List<CoreState>();
            }

            while (cores.Count < 3)
            {
                cores.Add(new CoreState());
            }

            if (cores.Count > 3)
            {
                cores = cores.Take(3).ToList();
            }
        }

        private void EnsureOpenOrderCountsMatchRecipes()
        {
            ComplexRecipe[] recipes = RecipeListField?.GetValue(this) as ComplexRecipe[];
            List<int> openOrderCounts = OpenOrderCountsField?.GetValue(this) as List<int>;
            if (recipes == null || openOrderCounts == null)
            {
                return;
            }

            while (openOrderCounts.Count > recipes.Length)
            {
                openOrderCounts.RemoveAt(openOrderCounts.Count - 1);
            }

            while (openOrderCounts.Count < recipes.Length)
            {
                openOrderCounts.Add(0);
            }
        }

        private void ClampOrderIndex(FieldInfo field, int recipeCount)
        {
            if (field == null)
            {
                return;
            }

            int index = (int)field.GetValue(this);
            if (index < 0)
            {
                return;
            }

            field.SetValue(this, recipeCount > 0 ? Mathf.Clamp(index, 0, recipeCount - 1) : 0);
        }

        private bool HasAnyQueuedRecipe()
        {
            Dictionary<string, int> queueCounts = RecipeQueueCountsField?.GetValue(this) as Dictionary<string, int>;
            return queueCounts != null && queueCounts.Values.Any(count => count != 0);
        }

        private static int GetIntField(FieldInfo field, object instance, int fallback)
        {
            return field != null ? (int)field.GetValue(instance) : fallback;
        }

        [SerializationConfig(MemberSerialization.OptIn)]
        public sealed class CoreState
        {
            [Serialize]
            public string RecipeId;

            [Serialize]
            public float Progress;

            public bool IsWorking => !string.IsNullOrEmpty(RecipeId);

            public void Clear()
            {
                RecipeId = null;
                Progress = 0f;
            }
        }
    }
}
