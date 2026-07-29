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
                return;
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
        }

        private void ClearOverride()
        {
            if (symbolOverrides != null)
            {
                symbolOverrides.RemoveSymbolOverride(CanvasSymbol, OverridePriority);
            }

            appliedStage = null;
        }

        private void OnDestroy()
        {
            ClearOverride();
        }
    }
}
