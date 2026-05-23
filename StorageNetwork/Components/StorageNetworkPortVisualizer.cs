using StorageNetwork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkPortVisualizer : KMonoBehaviour
    {
        public static readonly Color ConnectedInputColor = new Color(0.25f, 0.70f, 1f, 1f);
        public static readonly Color ConnectedOutputColor = new Color(1f, 0.82f, 0.20f, 1f);
        public static readonly Color DisconnectedColor = new Color(0.72f, 0.18f, 0.18f, 1f);

        private IStorageNetworkConnectable connector;
        private GameObject inputVisualizer;
        private GameObject outputVisualizer;
        private Image inputIcon;
        private Image outputIcon;
        private Sprite logicInputSprite;
        private Sprite logicOutputSprite;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            connector = GetComponent<IStorageNetworkConnectable>();
        }

        protected override void OnCleanUp()
        {
            DestroyVisualizer(inputVisualizer);
            DestroyVisualizer(outputVisualizer);
            base.OnCleanUp();
        }

        public void Draw()
        {
            if (connector == null)
            {
                connector = GetComponent<IStorageNetworkConnectable>();
            }

            if (connector == null)
            {
                return;
            }

            logicInputSprite = logicInputSprite ?? Assets.GetSprite("logicInput");
            logicOutputSprite = logicOutputSprite ?? Assets.GetSprite("logicOutput");

            DrawIcon(
                connector.InputCell,
                logicInputSprite,
                ref inputVisualizer,
                ref inputIcon,
                StorageNetworkRegistry.IsCableCell(connector.InputCell)
                    ? ConnectedInputColor
                    : DisconnectedColor);

            DrawIcon(
                connector.OutputCell,
                logicOutputSprite,
                ref outputVisualizer,
                ref outputIcon,
                StorageNetworkRegistry.IsCableCell(connector.OutputCell)
                    ? ConnectedOutputColor
                    : DisconnectedColor);
        }

        public void Hide()
        {
            SetActive(inputVisualizer, false);
            SetActive(outputVisualizer, false);
        }

        private static void DrawIcon(int cell, Sprite sprite, ref GameObject visualizer, ref Image icon, Color tint)
        {
            if (!Grid.IsValidCell(cell) || GameScreenManager.Instance == null || GameScreenManager.Instance.worldSpaceCanvas == null)
            {
                return;
            }

            if (visualizer == null)
            {
                visualizer = Util.KInstantiate(Assets.UIPrefabs.ResourceVisualizer, GameScreenManager.Instance.worldSpaceCanvas, null);
                visualizer.transform.SetAsFirstSibling();
                icon = visualizer.transform.GetChild(0).GetComponent<Image>();
                visualizer.transform.localScale = Vector3.one * 0.95f;
            }

            SetActive(visualizer, true);
            visualizer.GetComponent<Image>().enabled = true;
            icon.sprite = sprite;
            icon.raycastTarget = true;
            icon.color = tint;
            visualizer.transform.SetPosition(Grid.CellToPosCCC(cell, Grid.SceneLayer.Building));
        }

        private static void SetActive(GameObject visualizer, bool active)
        {
            if (visualizer != null && visualizer.activeInHierarchy != active)
            {
                visualizer.SetActive(active);
            }
        }

        private static void DestroyVisualizer(GameObject visualizer)
        {
            if (visualizer != null)
            {
                Util.KDestroyGameObject(visualizer);
            }
        }
    }
}
