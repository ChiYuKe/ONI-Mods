using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkOverviewMode : OverlayModes.Mode
    {
        public static readonly HashedString ID = "StorageNetworkOverview";
        private static StorageNetworkHub focusHub;

        private readonly HashSet<SaveLoadRoot> layerTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<SaveLoadRoot> desiredTargets = new HashSet<SaveLoadRoot>();
        private readonly Color32 cableColor = new Color32(118, 86, 150, 0);
        private readonly Color32 hubColor = new Color32(72, 128, 150, 0);
        private readonly Color32 storageColor = new Color32(150, 132, 82, 0);
        private readonly int targetLayer;
        private readonly int cameraLayerMask;
        private readonly int selectionMask;

        public StorageNetworkOverviewMode()
        {
            targetLayer = LayerMask.NameToLayer("MaskedOverlay");
            cameraLayerMask = LayerMask.GetMask("MaskedOverlay", "MaskedOverlayBG");
            selectionMask = cameraLayerMask;
        }

        public static void Show(StorageNetworkHub hub)
        {
            focusHub = hub;
            OverlayScreen.Instance?.ToggleOverlay(ID, true);
        }

        public override HashedString ViewMode()
        {
            return ID;
        }

        public override string GetSoundName()
        {
            return "HUD_Click";
        }

        public override void Enable()
        {
            Camera.main.cullingMask |= cameraLayerMask;
            SelectTool.Instance.SetLayerMask(selectionMask);
            GridCompositor.Instance.ToggleMinor(true);
        }

        public override void Disable()
        {
            OverlayModes.Mode.ResetDisplayValues<SaveLoadRoot>(layerTargets);
            HidePortIcons();
            layerTargets.Clear();
            desiredTargets.Clear();
            Camera.main.cullingMask &= ~cameraLayerMask;
            SelectTool.Instance.ClearLayerMask();
            GridCompositor.Instance.ToggleMinor(false);
        }

        public override void Update()
        {
            Vector2I visibleMin;
            Vector2I visibleMax;
            Grid.GetVisibleExtents(out visibleMin, out visibleMax);

            RebuildDesiredTargets();
            ClearStaleTargets();
            AddVisibleTargets(visibleMin, visibleMax);
            TintTargets();
            DrawPortIcons();
        }

        public override List<LegendEntry> GetCustomLegendData()
        {
            return new List<LegendEntry>
            {
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_INPUT_PORT, STRINGS.UI.STORAGE_NETWORK.LEGEND_INPUT_PORT_TOOLTIP, Color.white, null, Assets.GetSprite("logicInput"), true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_OUTPUT_PORT, STRINGS.UI.STORAGE_NETWORK.LEGEND_OUTPUT_PORT_TOOLTIP, Color.white, null, Assets.GetSprite("logicOutput"), true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_INPUT, STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_INPUT_TOOLTIP, StorageNetworkPortVisualizer.ConnectedInputColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_OUTPUT, STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_OUTPUT_TOOLTIP, StorageNetworkPortVisualizer.ConnectedOutputColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED, STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED_TOOLTIP, StorageNetworkPortVisualizer.DisconnectedColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CABLE, STRINGS.UI.STORAGE_NETWORK.LEGEND_CABLE_TOOLTIP, cableColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_HUB, STRINGS.UI.STORAGE_NETWORK.LEGEND_HUB_TOOLTIP, hubColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_STORAGE, STRINGS.UI.STORAGE_NETWORK.LEGEND_STORAGE_TOOLTIP, storageColor, null, null, true)
            };
        }

        private void RebuildDesiredTargets()
        {
            desiredTargets.Clear();

            foreach (StorageNetworkCable cable in StorageNetworkRegistry.RegisteredCables)
            {
                if (focusHub == null)
                {
                    AddRoot(cable);
                }
            }

            IEnumerable<StorageNetworkHub> hubs = focusHub != null
                ? new[] { focusHub }
                : StorageNetworkRegistry.RegisteredHubs;

            foreach (StorageNetworkHub hub in hubs)
            {
                if (hub == null)
                {
                    continue;
                }

                hub.RefreshNetwork();
                AddRoot(hub);

                foreach (StorageNetworkCable cable in StorageNetworkRegistry.GetConnectedCables(hub))
                {
                    AddRoot(cable);
                }

                foreach (StorageNetworkStorageInfo storageInfo in hub.ConnectedStorages)
                {
                    AddRoot(storageInfo.Storage);
                }
            }
        }

        private void ClearStaleTargets()
        {
            List<SaveLoadRoot> staleTargets = layerTargets
                .Where(target => target == null || !desiredTargets.Contains(target))
                .ToList();

            foreach (SaveLoadRoot target in staleTargets)
            {
                if (target != null)
                {
                    KBatchedAnimController controller = target.GetComponent<KBatchedAnimController>();
                    if (controller != null)
                    {
                        OverlayModes.Mode.ResetDisplayValues(controller);
                    }
                }

                layerTargets.Remove(target);
            }
        }

        private void AddVisibleTargets(Vector2I visibleMin, Vector2I visibleMax)
        {
            foreach (SaveLoadRoot target in desiredTargets)
            {
                AddTargetIfVisible(target, visibleMin, visibleMax, layerTargets, targetLayer, null, null);
            }
        }

        private void TintTargets()
        {
            foreach (SaveLoadRoot target in layerTargets)
            {
                if (target == null)
                {
                    continue;
                }

                KBatchedAnimController controller = target.GetComponent<KBatchedAnimController>();
                if (controller == null)
                {
                    continue;
                }

                if (target.GetComponent<StorageNetworkCable>() != null)
                {
                    controller.TintColour = cableColor;
                }
                else if (target.GetComponent<StorageNetworkHub>() != null)
                {
                    controller.TintColour = hubColor;
                }
                else
                {
                    controller.TintColour = storageColor;
                }
            }
        }

        private void AddRoot(KMonoBehaviour component)
        {
            SaveLoadRoot root = component != null ? component.GetComponent<SaveLoadRoot>() : null;
            if (root != null)
            {
                desiredTargets.Add(root);
            }
        }

        private void DrawPortIcons()
        {
            foreach (StorageNetworkStorageConnector connector in StorageNetworkRegistry.RegisteredStorageConnectors)
            {
                DrawPortIcon(connector);
            }

            foreach (StorageNetworkHub hub in StorageNetworkRegistry.RegisteredHubs)
            {
                DrawPortIcon(hub);
            }
        }

        private void HidePortIcons()
        {
            foreach (StorageNetworkStorageConnector connector in StorageNetworkRegistry.RegisteredStorageConnectors)
            {
                if (connector == null)
                {
                    continue;
                }

                StorageNetworkPortVisualizer visualizer = connector.GetComponent<StorageNetworkPortVisualizer>();
                if (visualizer != null)
                {
                    visualizer.Hide();
                }
            }

            foreach (StorageNetworkHub hub in StorageNetworkRegistry.RegisteredHubs)
            {
                if (hub == null)
                {
                    continue;
                }

                StorageNetworkPortVisualizer visualizer = hub.GetComponent<StorageNetworkPortVisualizer>();
                if (visualizer != null)
                {
                    visualizer.Hide();
                }
            }
        }

        private void DrawPortIcon(KMonoBehaviour connectable)
        {
            if (connectable == null)
            {
                return;
            }

            StorageNetworkPortVisualizer visualizer = connectable.GetComponent<StorageNetworkPortVisualizer>();
            if (visualizer == null)
            {
                return;
            }

            SaveLoadRoot root = connectable.GetComponent<SaveLoadRoot>();
            if (focusHub == null || (root != null && desiredTargets.Contains(root)))
            {
                visualizer.Draw();
            }
            else
            {
                visualizer.Hide();
            }
        }
    }
}
