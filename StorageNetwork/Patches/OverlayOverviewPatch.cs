using System;
using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Patches
{
    public static class OverlayOverviewPatch
    {
        [HarmonyPatch(typeof(OverlayScreen), "RegisterModes")]
        public static class OverlayScreenRegisterModesPatch
        {
            public static void Postfix(OverlayScreen __instance)
            {
                AccessTools.Method(typeof(OverlayScreen), "RegisterMode")
                    ?.Invoke(__instance, new object[] { new StorageNetworkOverviewMode() });
            }
        }

        [HarmonyPatch(typeof(OverlayMenu), "InitializeToggles")]
        public static class OverlayMenuInitializeTogglesPatch
        {
            public static void Postfix(OverlayMenu __instance)
            {
                try
                {
                    List<KIconToggleMenu.ToggleInfo> toggles = Traverse.Create(__instance)
                        .Field("overlayToggleInfos")
                        .GetValue<List<KIconToggleMenu.ToggleInfo>>();

                    if (toggles == null || toggles.Exists(toggle => toggle.text == STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON))
                    {
                        return;
                    }

                    Type toggleType = AccessTools.Inner(typeof(OverlayMenu), "OverlayToggleInfo");
                    object toggle = Activator.CreateInstance(
                        toggleType,
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON.ToString(),
                        "OverviewUI_consumables_icon",
                        StorageNetworkOverviewMode.ID,
                        string.Empty,
                        Action.NumActions,
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_TOOLTIP.ToString(),
                        STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON.ToString());

                    toggles.Insert(0, (KIconToggleMenu.ToggleInfo)toggle);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add overview overlay button: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(OverlayLegend), "OnSpawn")]
        public static class OverlayLegendOnSpawnPatch
        {
            public static void Postfix(OverlayLegend __instance)
            {
                try
                {
                    List<OverlayLegend.OverlayInfo> overlayInfoList = Traverse.Create(__instance)
                        .Field("overlayInfoList")
                        .GetValue<List<OverlayLegend.OverlayInfo>>();

                    if (overlayInfoList == null ||
                        overlayInfoList.Exists(info => info != null && info.mode == StorageNetworkOverviewMode.ID))
                    {
                        return;
                    }

                    overlayInfoList.Add(new OverlayLegend.OverlayInfo
                    {
                        name = STRINGS.UI.STORAGE_NETWORK.OVERVIEW_BUTTON.ToString(),
                        mode = StorageNetworkOverviewMode.ID,
                        infoUnits = new List<OverlayLegend.OverlayInfoUnit>(),
                        diagrams = new List<GameObject>(),
                        isProgrammaticallyPopulated = true
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add overview legend: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(SelectToolHoverTextCard), nameof(SelectToolHoverTextCard.UpdateHoverElements))]
        public static class SelectToolHoverTextCardUpdateHoverElementsPatch
        {
            public static bool Prefix(SelectToolHoverTextCard __instance)
            {
                if (OverlayScreen.Instance == null || OverlayScreen.Instance.mode != StorageNetworkOverviewMode.ID)
                {
                    return true;
                }

                int cell = Grid.PosToCell(Camera.main.ScreenToWorldPoint(KInputManager.GetMousePos()));
                if (!Grid.IsValidCell(cell))
                {
                    return true;
                }

                IStorageNetworkConnectable connectable = FindConnectableAtPortCell(cell, out bool isInput);
                if (connectable == null)
                {
                    return true;
                }

                DrawPortHover(__instance, connectable, cell, isInput);
                return false;
            }

            private static IStorageNetworkConnectable FindConnectableAtPortCell(int cell, out bool isInput)
            {
                foreach (StorageNetworkHub hub in StorageNetworkRegistry.RegisteredHubs)
                {
                    if (TryMatchPort(hub, cell, out isInput))
                    {
                        return hub;
                    }
                }

                foreach (StorageNetworkStorageConnector connector in StorageNetworkRegistry.RegisteredStorageConnectors)
                {
                    if (TryMatchPort(connector, cell, out isInput))
                    {
                        return connector;
                    }
                }

                isInput = false;
                return null;
            }

            private static bool TryMatchPort(IStorageNetworkConnectable connectable, int cell, out bool isInput)
            {
                isInput = false;
                if (connectable == null)
                {
                    return false;
                }

                if (connectable.InputCell == cell)
                {
                    isInput = true;
                    return true;
                }

                return connectable.OutputCell == cell;
            }

            private static void DrawPortHover(
                SelectToolHoverTextCard card,
                IStorageNetworkConnectable connectable,
                int cell,
                bool isInput)
            {
                if (card.iconDash == null)
                {
                    card.ConfigureHoverScreen();
                }

                bool connected = StorageNetworkRegistry.IsCableCell(cell);
                HoverTextDrawer drawer = HoverTextScreen.Instance.BeginDrawing();
                drawer.BeginShadowBar(false);
                drawer.DrawText(GetTitle(connectable, isInput), card.Styles_Title.Standard);
                drawer.NewLine(26);
                card.DrawLogicIcon(drawer, card.iconDash, connected ? card.Styles_LogicActive.Selected : card.Styles_LogicSignalInactive);
                card.DrawLogicText(
                    drawer,
                    connected
                        ? STRINGS.UI.STORAGE_NETWORK.PORT_CONNECTED.ToString()
                        : STRINGS.UI.STORAGE_NETWORK.PORT_DISCONNECTED.ToString(),
                    connected ? card.Styles_LogicActive.Selected : card.Styles_LogicSignalInactive);

                if (isInput && connectable.Storage != null)
                {
                    drawer.NewLine(26);
                    card.DrawLogicIcon(drawer, card.iconDash, connectable.CanShareStorage ? card.Styles_LogicStandby.Selected : card.Styles_LogicSignalInactive);
                    card.DrawLogicText(
                        drawer,
                        connectable.CanShareStorage
                            ? STRINGS.UI.STORAGE_NETWORK.PORT_STORAGE_AVAILABLE.ToString()
                            : STRINGS.UI.STORAGE_NETWORK.PORT_STORAGE_UNAVAILABLE.ToString(),
                        connectable.CanShareStorage ? card.Styles_LogicStandby.Selected : card.Styles_LogicSignalInactive);
                }

                drawer.EndShadowBar();
                drawer.EndDrawing();
            }

            private static string GetTitle(IStorageNetworkConnectable connectable, bool isInput)
            {
                string format = isInput
                    ? STRINGS.UI.STORAGE_NETWORK.PORT_INPUT_HOVER_FORMAT.ToString()
                    : STRINGS.UI.STORAGE_NETWORK.PORT_OUTPUT_HOVER_FORMAT.ToString();
                return format
                    .Replace("{Port}", (isInput
                        ? STRINGS.UI.STORAGE_NETWORK.LEGEND_INPUT_PORT
                        : STRINGS.UI.STORAGE_NETWORK.LEGEND_OUTPUT_PORT).ToString().ToUpper())
                    .Replace("{Name}", connectable.DisplayName.ToUpper());
            }
        }

        [HarmonyPatch(typeof(Database.BuildingStatusItems), MethodType.Constructor, typeof(ResourceSet))]
        public static class BuildingStatusItemsConstructorPatch
        {
            public static void Postfix(Database.BuildingStatusItems __instance)
            {
                ExtendUtilityOverlay(__instance.PendingDeconstruction);
                ExtendUtilityOverlay(__instance.PendingDemolition);
            }

            private static void ExtendUtilityOverlay(StatusItem statusItem)
            {
                if (statusItem == null)
                {
                    return;
                }

                Func<HashedString, object, bool> originalCallback = statusItem.conditionalOverlayCallback;
                statusItem.conditionalOverlayCallback = (mode, data) =>
                    (originalCallback != null && originalCallback(mode, data)) ||
                    ShouldShowStorageNetworkStatus(mode, data);
            }

            private static bool ShouldShowStorageNetworkStatus(HashedString mode, object data)
            {
                if (mode != StorageNetworkOverviewMode.ID)
                {
                    return false;
                }

                Transform transform = data as Transform;
                return transform != null && transform.GetComponent<StorageNetworkCable>() != null;
            }
        }
    }
}
