using System;
using UnityEngine;

namespace LocalCanvas
{
    internal sealed class LocalCanvasDisplay : MonoBehaviour
    {
        private static readonly string[] CanvasIds = { "Canvas", "CanvasTall", "CanvasWide" };
        private static readonly HashedString CanvasSymbol = new HashedString("canvas");
        private const int OverridePriority = 1000;

        private SymbolOverrideController symbolOverrides;
        private BuildingComplete building;
        private string appliedStage;
        private KAnim.Build.Symbol appliedSourceSymbol;

        public static void TryAttach(Painting painting)
        {
            BuildingComplete building = painting?.GetComponent<BuildingComplete>();
            if (building == null || building.Def == null || Array.IndexOf(CanvasIds, building.Def.PrefabID) < 0)
            {
                return;
            }

            LocalCanvasDisplay display = building.gameObject.GetComponent<LocalCanvasDisplay>()
                ?? building.gameObject.AddComponent<LocalCanvasDisplay>();
            display.Initialize(building);
        }

        private void Initialize(BuildingComplete target)
        {
            if (building != null)
            {
                return;
            }

            building = target;
            symbolOverrides = building.GetComponent<SymbolOverrideController>();
            // Canvas instances can be rebuilt/rebatched when hovered or selected.
            // Keep the official per-instance override table reapplied so a batch
            // refresh cannot leave this instance displaying another canvas image.
            symbolOverrides.applySymbolOverridesEveryFrame = true;
            Refresh();
        }

        public void Refresh()
        {
            if (building == null || building.Def == null || symbolOverrides == null)
            {
                return;
            }

            Artable art = building.GetComponent<Artable>();
            if (art == null || !LocalCanvasRegistry.TryGetImage(art.CurrentStage, out LocalCanvasImageInfo image))
            {
                ClearOverride();
                return;
            }

            if (appliedStage == art.CurrentStage)
            {
                if (appliedSourceSymbol != null && HasExpectedOverride(appliedSourceSymbol))
                {
                    return;
                }
            }

            if (!image.TryGetSourceSymbol(out KAnim.Build.Symbol sourceSymbol))
            {
                Debug.LogWarning("[LocalCanvas] source KAnim symbol is unavailable for " + image.FilePath);
                ClearOverride();
                return;
            }

            symbolOverrides.RemoveSymbolOverride(CanvasSymbol, OverridePriority);
            symbolOverrides.AddSymbolOverride(CanvasSymbol, sourceSymbol, OverridePriority);
            appliedStage = art.CurrentStage;
            appliedSourceSymbol = sourceSymbol;
        }

        private void LateUpdate()
        {
            if (building == null || symbolOverrides == null)
            {
                return;
            }

            Artable art = building.GetComponent<Artable>();
            if (art == null || !LocalCanvasRegistry.TryGetImage(art.CurrentStage, out LocalCanvasImageInfo image))
            {
                if (appliedStage != null)
                {
                    ClearOverride();
                }
                return;
            }

            if (!image.TryGetSourceSymbol(out KAnim.Build.Symbol sourceSymbol))
            {
                return;
            }

            bool sameStageAndSource = appliedStage == art.CurrentStage && appliedSourceSymbol == sourceSymbol;
            bool hasExpectedOverride = HasExpectedOverride(sourceSymbol);
            if (!sameStageAndSource || !hasExpectedOverride)
            {
                Refresh();
            }
        }

        private bool HasExpectedOverride(KAnim.Build.Symbol sourceSymbol)
        {
            foreach (SymbolOverrideController.SymbolEntry entry in symbolOverrides.GetSymbolOverrides)
            {
                if (entry.targetSymbol == CanvasSymbol && entry.priority == OverridePriority && entry.sourceSymbol == sourceSymbol)
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearOverride()
        {
            if (symbolOverrides != null)
            {
                symbolOverrides.RemoveSymbolOverride(CanvasSymbol, OverridePriority);
            }

            appliedStage = null;
            appliedSourceSymbol = null;
        }

        private void OnDestroy()
        {
            ClearOverride();
        }
    }
}
