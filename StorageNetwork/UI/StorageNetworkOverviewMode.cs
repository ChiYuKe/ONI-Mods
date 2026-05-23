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

        private readonly HashSet<SaveLoadRoot> cableTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<SaveLoadRoot> objectTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<SaveLoadRoot> desiredTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<SaveLoadRoot> desiredPortTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<SaveLoadRoot> visiblePortTargets = new HashSet<SaveLoadRoot>();
        private readonly HashSet<int> connectedCableCells = new HashSet<int>();
        private readonly Color32 connectedCableColor = new Color32(75, 190, 92, 0);
        private readonly Color32 disconnectedCableColor = new Color32(220, 54, 54, 0);
        private readonly Color32 hubColor = new Color32(72, 128, 150, 0);
        private readonly Color32 storageColor = new Color32(150, 132, 82, 0);
        private readonly int cableTargetLayer;
        private readonly int objectTargetLayer;
        private readonly int cameraLayerMask;
        private readonly int selectionMask;
        private int cachedRegistryRevision = -1;

        public StorageNetworkOverviewMode()
        {
            cableTargetLayer = LayerMask.NameToLayer("MaskedOverlay");
            objectTargetLayer = LayerMask.NameToLayer("MaskedOverlayBG");
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
            cachedRegistryRevision = -1;
            RefreshCachedNetworkTargets();
        }

        public override void Disable()
        {
            ResetTargets(cableTargets);
            ResetTargets(objectTargets);
            HidePortIcons();
            cableTargets.Clear();
            objectTargets.Clear();
            desiredTargets.Clear();
            desiredPortTargets.Clear();
            visiblePortTargets.Clear();
            connectedCableCells.Clear();
            cachedRegistryRevision = -1;
            Camera.main.cullingMask &= ~cameraLayerMask;
            SelectTool.Instance.ClearLayerMask();
            GridCompositor.Instance.ToggleMinor(false);
        }

        public override void Update()
        {
            Vector2I visibleMin;
            Vector2I visibleMax;
            Grid.GetVisibleExtents(out visibleMin, out visibleMax);

            if (cachedRegistryRevision != StorageNetworkRegistry.Revision)
            {
                RefreshCachedNetworkTargets();
            }

            ClearStaleTargets();
            AddVisibleTargets(visibleMin, visibleMax);
            TintTargets();
            DrawPortIcons(visibleMin, visibleMax);
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
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_CABLE, STRINGS.UI.STORAGE_NETWORK.LEGEND_CONNECTED_CABLE_TOOLTIP, connectedCableColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED_CABLE, STRINGS.UI.STORAGE_NETWORK.LEGEND_DISCONNECTED_CABLE_TOOLTIP, disconnectedCableColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_HUB, STRINGS.UI.STORAGE_NETWORK.LEGEND_HUB_TOOLTIP, hubColor, null, null, true),
                new LegendEntry(STRINGS.UI.STORAGE_NETWORK.LEGEND_STORAGE, STRINGS.UI.STORAGE_NETWORK.LEGEND_STORAGE_TOOLTIP, storageColor, null, null, true)
            };
        }

        private void RefreshCachedNetworkTargets()
        {
            desiredTargets.Clear();
            desiredPortTargets.Clear();
            connectedCableCells.Clear();

            foreach (StorageNetworkCable cable in StorageNetworkRegistry.RegisteredCables)
            {
                AddRoot(cable);
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

                foreach (int cableCell in StorageNetworkRegistry.GetConnectedCableCells(hub))
                {
                    connectedCableCells.Add(cableCell);
                }

                foreach (StorageNetworkStorageInfo storageInfo in hub.ConnectedStorages)
                {
                    AddRoot(storageInfo.Storage);
                }
            }

            foreach (StorageNetworkStorageConnector connector in StorageNetworkRegistry.RegisteredStorageConnectors)
            {
                AddPortRoot(connector);
            }

            foreach (StorageNetworkHub hub in StorageNetworkRegistry.RegisteredHubs)
            {
                AddPortRoot(hub);
            }

            cachedRegistryRevision = StorageNetworkRegistry.Revision;
        }

        private void ClearStaleTargets()
        {
            ClearStaleTargets(cableTargets);
            ClearStaleTargets(objectTargets);
        }

        private void AddVisibleTargets(Vector2I visibleMin, Vector2I visibleMax)
        {
            foreach (SaveLoadRoot target in desiredTargets)
            {
                if (target.GetComponent<StorageNetworkCable>() != null)
                {
                    AddTargetIfVisible(target, visibleMin, visibleMax, cableTargets, cableTargetLayer, SetCableOverlayDepth, null);
                }
                else
                {
                    AddTargetIfVisible(target, visibleMin, visibleMax, objectTargets, objectTargetLayer, null, null);
                }
            }
        }

        private void TintTargets()
        {
            foreach (SaveLoadRoot target in cableTargets)
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

                StorageNetworkCable cable = target.GetComponent<StorageNetworkCable>();
                if (cable == null)
                {
                    continue;
                }

                controller.TintColour = IsCableConnected(cable)
                    ? connectedCableColor
                    : disconnectedCableColor;
            }

            foreach (SaveLoadRoot target in objectTargets)
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

                if (target.GetComponent<StorageNetworkHub>() != null)
                {
                    controller.TintColour = hubColor;
                }
                else
                {
                    controller.TintColour = storageColor;
                }
            }
        }

        private void ClearStaleTargets(HashSet<SaveLoadRoot> targets)
        {
            List<SaveLoadRoot> staleTargets = targets
                .Where(target => target == null || !desiredTargets.Contains(target))
                .ToList();

            foreach (SaveLoadRoot target in staleTargets)
            {
                HidePortIcon(target);
                ResetTarget(target);
                targets.Remove(target);
            }
        }

        private static void ResetTargets(HashSet<SaveLoadRoot> targets)
        {
            foreach (SaveLoadRoot target in targets)
            {
                ResetTarget(target);
            }
        }

        private static void ResetTarget(SaveLoadRoot target)
        {
            if (target == null)
            {
                return;
            }

            KBatchedAnimController controller = target.GetComponent<KBatchedAnimController>();
            if (controller != null)
            {
                OverlayModes.Mode.ResetDisplayValues(controller);
            }

            Vector3 position = target.transform.GetPosition();
            position.z = OverlayModes.Mode.GetDefaultDepth(target);
            target.transform.SetPosition(position);
            if (controller != null)
            {
                controller.enabled = false;
                controller.enabled = true;
            }
        }

        private static void SetCableOverlayDepth(SaveLoadRoot target)
        {
            Vector3 position = target.transform.GetPosition();
            position.z = Grid.GetLayerZ(Grid.SceneLayer.Building) - 0.1f;
            target.transform.SetPosition(position);

            KBatchedAnimController controller = target.GetComponent<KBatchedAnimController>();
            if (controller != null)
            {
                controller.enabled = false;
                controller.enabled = true;
            }
        }

        private bool IsCableConnected(StorageNetworkCable cable)
        {
            return cable != null && connectedCableCells.Contains(cable.Cell);
        }

        private void AddRoot(KMonoBehaviour component)
        {
            SaveLoadRoot root = component != null ? component.GetComponent<SaveLoadRoot>() : null;
            if (root != null)
            {
                desiredTargets.Add(root);
            }
        }

        private void AddPortRoot(KMonoBehaviour component)
        {
            SaveLoadRoot root = component != null ? component.GetComponent<SaveLoadRoot>() : null;
            if (root != null)
            {
                desiredPortTargets.Add(root);
            }
        }

        private void DrawPortIcons(Vector2I visibleMin, Vector2I visibleMax)
        {
            foreach (SaveLoadRoot target in visiblePortTargets
                .Where(target => target == null || !desiredPortTargets.Contains(target) || !IsVisible(target, visibleMin, visibleMax))
                .ToList())
            {
                HidePortIcon(target);
                visiblePortTargets.Remove(target);
            }

            foreach (SaveLoadRoot target in desiredPortTargets)
            {
                if (IsVisible(target, visibleMin, visibleMax))
                {
                    DrawPortIcon(target);
                    visiblePortTargets.Add(target);
                }
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

        private static bool IsVisible(SaveLoadRoot target, Vector2I visibleMin, Vector2I visibleMax)
        {
            if (target == null)
            {
                return false;
            }

            int cell = Grid.PosToCell(target);
            if (!Grid.IsValidCell(cell))
            {
                return false;
            }

            Vector2I xy = Grid.CellToXY(cell);
            return xy.x >= visibleMin.x && xy.x <= visibleMax.x &&
                xy.y >= visibleMin.y && xy.y <= visibleMax.y;
        }

        private static void HidePortIcon(SaveLoadRoot target)
        {
            if (target == null)
            {
                return;
            }

            StorageNetworkPortVisualizer visualizer = target.GetComponent<StorageNetworkPortVisualizer>();
            if (visualizer != null)
            {
                visualizer.Hide();
            }
        }
    }
}
