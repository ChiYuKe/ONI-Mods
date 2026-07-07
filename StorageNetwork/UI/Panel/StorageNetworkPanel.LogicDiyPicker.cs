using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        public static void ShowLogicDiyOutputModePicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            ShowLogicDiySettingsPanel(logic);
        }

        private static void ShowLogicDiySettingsPanel(StorageNetworkLogicDiy logic)
        {
            CloseStandaloneOutputFilterPicker();
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkLogicDiySettings");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            standaloneOutputFilterPickerRoot = root;
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.04f);
            blocker.raycastTarget = false;

            GameObject panel = CreatePlainImage("LogicDiyPanel", root.transform, new Color(0.17f, 0.19f, 0.22f, 0.98f));
            standaloneOutputFilterPickerWindow = panel;
            panel.AddComponent<ScrollWheelBlocker>();
            panel.AddComponent<StandaloneRightClickCloseHandler>();
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1680f, 980f);
            panelRect.anchoredPosition = Vector2.zero;
            StorageNetworkWindowDrag.TryApplyLayout("logicDiySettingsPanel", panelRect, new Vector2(1120f, 720f), new Vector2(1920f, 1120f));

            GameObject header = CreatePlainImage("Header", panel.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 8f, 8f, 8f, 34f);
            header.AddComponent<StorageNetworkWindowDrag>().Configure(panelRect, "logicDiySettingsPanel");
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 3, 3);
            headerLayout.spacing = 8f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = true;

            TextMeshProUGUI title = CreateText("Title", header.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONFIG_PANEL_TITLE), 12, TextAlignmentOptions.MidlineLeft);
            title.color = new Color(0.96f, 0.94f, 0.86f, 1f);
            title.fontStyle = FontStyles.Bold;
            title.raycastTarget = false;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateCloseIconButton("Close", header.transform, CloseStandaloneOutputFilterPickerAction);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 24f;
            closeLayout.preferredHeight = 22f;

            GameObject viewport = CreatePlainImage("Viewport", panel.transform, new Color(0.83f, 0.82f, 0.76f, 1f));
            SetStretch(viewport.GetComponent<RectTransform>(), 8f, 8f, 8f, 48f);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<ScrollWheelBlocker>();

            CreateLogicDiyBlueprintWorkspace(viewport.transform, logic);
        }

        private enum LogicDiyBlueprintModule
        {
            FixedOutput,
            MaterialCondition,
            SingleChannel,
            FourChannel,
            Lua,
            Timer
        }

        private static void CreateLogicDiyBlueprintWorkspace(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject workspace = new GameObject("BlueprintWorkspace");
            workspace.transform.SetParent(parent, false);
            RectTransform workspaceRect = workspace.AddComponent<RectTransform>();
            workspaceRect.anchorMin = Vector2.zero;
            workspaceRect.anchorMax = Vector2.one;
            workspaceRect.offsetMin = new Vector2(8f, 8f);
            workspaceRect.offsetMax = new Vector2(-8f, -8f);

            GameObject palette = CreatePlainImage("ModulePalette", workspace.transform, new Color(0.68f, 0.68f, 0.61f, 1f));
            RectTransform paletteRect = palette.GetComponent<RectTransform>();
            paletteRect.anchorMin = new Vector2(0f, 0f);
            paletteRect.anchorMax = new Vector2(0f, 1f);
            paletteRect.offsetMin = Vector2.zero;
            paletteRect.offsetMax = new Vector2(290f, 0f);

            VerticalLayoutGroup paletteLayout = palette.AddComponent<VerticalLayoutGroup>();
            paletteLayout.padding = new RectOffset(10, 10, 10, 10);
            paletteLayout.spacing = 8f;
            paletteLayout.childControlWidth = true;
            paletteLayout.childControlHeight = true;
            paletteLayout.childForceExpandWidth = true;
            paletteLayout.childForceExpandHeight = false;

            TextMeshProUGUI paletteTitle = CreateText("PaletteTitle", palette.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MODULE_LIBRARY), 12, TextAlignmentOptions.MidlineLeft);
            paletteTitle.color = new Color(0.20f, 0.22f, 0.23f, 1f);
            paletteTitle.fontStyle = FontStyles.Bold;
            paletteTitle.raycastTarget = false;
            paletteTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_DESC), LogicDiyBlueprintModule.FixedOutput, logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.Fixed);
            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION_DESC), LogicDiyBlueprintModule.MaterialCondition, logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition);
            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP), LogicDiyBlueprintModule.SingleChannel, logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.SingleChannel);
            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_TOOLTIP), LogicDiyBlueprintModule.FourChannel, logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel);
            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_LUA_MODULE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_LUA_MODULE_DESC), LogicDiyBlueprintModule.Lua, false);
            CreateLogicDiyBlueprintPaletteCard(palette.transform, logic, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_TIMER_MODULE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_TIMER_MODULE_DESC), LogicDiyBlueprintModule.Timer, false);

            GameObject canvasViewport = CreatePlainImage("BlueprintCanvasViewport", workspace.transform, new Color(0.76f, 0.75f, 0.69f, 1f));
            RectTransform canvasViewportRect = canvasViewport.GetComponent<RectTransform>();
            canvasViewportRect.anchorMin = new Vector2(0f, 0f);
            canvasViewportRect.anchorMax = Vector2.one;
            canvasViewportRect.offsetMin = new Vector2(300f, 0f);
            canvasViewportRect.offsetMax = Vector2.zero;
            canvasViewport.AddComponent<RectMask2D>();
            canvasViewport.AddComponent<ScrollWheelBlocker>();

            GameObject canvas = new GameObject("BlueprintCanvas");
            canvas.transform.SetParent(canvasViewport.transform, false);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null)
            {
                canvasRect = canvas.AddComponent<RectTransform>();
            }

            canvasRect.anchorMin = new Vector2(0f, 1f);
            canvasRect.anchorMax = new Vector2(0f, 1f);
            canvasRect.pivot = new Vector2(0f, 1f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1560f, 920f);
            canvas.AddComponent<LogicDiyBlueprintCanvas>();

            canvasViewport.AddComponent<StorageNetworkPanZoom>().Configure(canvasViewportRect, canvasRect);

            TextMeshProUGUI hint = CreateText("CanvasHint", canvas.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_BLUEPRINT_HINT), 11, TextAlignmentOptions.TopLeft);
            hint.color = new Color(0.36f, 0.38f, 0.36f, 1f);
            hint.textWrappingMode = TextWrappingModes.Normal;
            hint.raycastTarget = false;
            RectTransform hintRect = hint.rectTransform();
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.offsetMin = new Vector2(12f, -58f);
            hintRect.offsetMax = new Vector2(-12f, -12f);

            CreateLogicDiyBlueprintNodes(canvas.transform, logic);
        }

        private static void CreateLogicDiyBlueprintPaletteCard(Transform parent, StorageNetworkLogicDiy logic, string title, string detail, LogicDiyBlueprintModule module, bool selected)
        {
            GameObject row = CreateLogicDiyButtonRow(parent, 58f);
            Image hitArea = row.AddComponent<Image>();
            hitArea.color = Color.clear;
            CreateLogicDiyChoiceButton(row.transform, title, detail, selected, () => ApplyLogicDiyBlueprintModule(logic, module));
            row.AddComponent<LogicDiyPaletteDragSource>().Configure(logic, module, title, detail);
        }

        private static void CreateLogicDiyBlueprintNodes(Transform canvas, StorageNetworkLogicDiy logic)
        {
            if (logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition)
            {
                GameObject material = CreateLogicDiyBlueprintNode(canvas, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_MATERIAL), new Vector2(42f, -86f), new Vector2(240f, 104f), false, true);
                ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
                string selectedName = selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE);
                CreateLogicDiyWideButton(material.transform, selectedName, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_CURRENT), GameUtil.GetFormattedMass(selected.MassKg)), !string.IsNullOrEmpty(logic.ConditionItemKey), () => ShowLogicDiyMaterialPicker(logic));

                GameObject compare = CreateLogicDiyBlueprintNode(canvas, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_COMPARE), new Vector2(340f, -110f), new Vector2(282f, 196f), true, true);
                GameObject compareRow = CreateLogicDiyButtonRow(compare.transform, 38f);
                CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE), ">=", logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual));
                CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT), "<", logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.LessThan, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.LessThan));
                CreateLogicDiyThresholdControl(compare.transform, logic);

                GameObject output = CreateLogicDiyBlueprintOutputNode(canvas, logic, new Vector2(688f, -110f));
                DrawLogicDiyConnector(canvas, material, compare);
                DrawLogicDiyConnector(canvas, compare, output);
            }
            else
            {
                GameObject fixedNode = CreateLogicDiyBlueprintNode(canvas, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_FIXED), new Vector2(78f, -110f), new Vector2(300f, logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 250f : 112f), false, true);
                if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
                {
                    GridLayoutGroup grid = CreateLogicDiyGrid(fixedNode.transform, 4, 38f, 16);
                    for (int value = 0; value <= 15; value++)
                    {
                        int capturedValue = value;
                        CreateLogicDiyChoiceButton(grid.transform, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_TITLE), value), ToFourBitBinary(value), logic.OutputSignalValue == value, () => SetLogicDiyOutputValue(logic, capturedValue));
                    }
                }
                else
                {
                    GameObject row = CreateLogicDiyButtonRow(fixedNode.transform, 42f);
                    CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF_DESC), logic.OutputSignalValue == 0, () => SetLogicDiyOutputValue(logic, 0));
                    CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON_DESC), logic.OutputSignalValue != 0, () => SetLogicDiyOutputValue(logic, 1));
                }

                GameObject output = CreateLogicDiyBlueprintOutputNode(canvas, logic, new Vector2(520f, -110f));
                DrawLogicDiyConnector(canvas, fixedNode, output);
            }
        }

        private static GameObject CreateLogicDiyBlueprintOutputNode(Transform canvas, StorageNetworkLogicDiy logic, Vector2 position)
        {
            GameObject output = CreateLogicDiyBlueprintNode(canvas, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_OUTPUT), position, new Vector2(240f, logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 112f : 86f), true, false);
            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                GameObject row = CreateLogicDiyButtonRow(output.transform, 42f);
                for (int channel = 0; channel < 4; channel++)
                {
                    int capturedChannel = channel;
                    CreateLogicDiyChoiceButton(row.transform, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), channel), string.Format("{0}", 1 << channel), logic.ConditionOutputChannel == channel, () => SetLogicDiyConditionChannel(logic, capturedChannel));
                }
            }
            else
            {
                CreateLogicDiyWideButton(output.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_DESC), true, null);
            }

            return output;
        }

        private static GameObject CreateLogicDiyBlueprintNode(Transform canvas, string title, Vector2 position, Vector2 size, bool inputPort, bool outputPort)
        {
            GameObject node = CreatePlainImage("BlueprintNode", canvas, new Color(0.17f, 0.19f, 0.25f, 1f));
            RectTransform rect = node.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            node.AddComponent<LogicDiyBlueprintNodeDrag>();

            VerticalLayoutGroup layout = node.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 9, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI titleText = CreateText("NodeTitle", node.transform, title, 11, TextAlignmentOptions.MidlineLeft);
            titleText.color = Color.white;
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            if (inputPort)
            {
                CreateLogicDiyEndpoint(node.transform, false);
            }

            if (outputPort)
            {
                CreateLogicDiyEndpoint(node.transform, true);
            }

            return node;
        }

        private static void CreateLogicDiyEndpoint(Transform node, bool output)
        {
            GameObject endpoint = CreatePlainImage(output ? "OutputEndpoint" : "InputEndpoint", node, output ? new Color(0.75f, 0.32f, 0.54f, 1f) : new Color(0.32f, 0.47f, 0.68f, 1f));
            RectTransform rect = endpoint.GetComponent<RectTransform>();
            LayoutElement layout = endpoint.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            rect.anchorMin = output ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rect.anchorMax = output ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = output ? new Vector2(8f, 0f) : new Vector2(-8f, 0f);
            rect.sizeDelta = new Vector2(14f, 14f);
            endpoint.transform.SetAsLastSibling();
            endpoint.AddComponent<LogicDiyEndpointHandle>().IsOutput = output;
        }

        private static void DrawLogicDiyConnector(Transform canvas, GameObject from, GameObject to)
        {
            GameObject line = CreatePlainImage("BlueprintConnection", canvas, new Color(0.46f, 0.47f, 0.42f, 1f));
            Image image = line.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            RectTransform fromRect = FindLogicDiyEndpoint(from, true) ?? from.GetComponent<RectTransform>();
            RectTransform toRect = FindLogicDiyEndpoint(to, false) ?? to.GetComponent<RectTransform>();
            line.AddComponent<LogicDiyConnectionLine>().Configure(canvas as RectTransform, fromRect, toRect);
            line.transform.SetAsFirstSibling();
        }

        private static RectTransform FindLogicDiyEndpoint(GameObject node, bool output)
        {
            if (node == null)
            {
                return null;
            }

            LogicDiyEndpointHandle[] endpoints = node.GetComponentsInChildren<LogicDiyEndpointHandle>(true);
            foreach (LogicDiyEndpointHandle endpoint in endpoints)
            {
                if (endpoint != null && endpoint.IsOutput == output)
                {
                    return endpoint.GetComponent<RectTransform>();
                }
            }

            return null;
        }

        private static void ApplyLogicDiyBlueprintModule(StorageNetworkLogicDiy logic, LogicDiyBlueprintModule module)
        {
            if (logic == null)
            {
                return;
            }

            switch (module)
            {
                case LogicDiyBlueprintModule.FixedOutput:
                    logic.SetSourceMode(StorageNetworkLogicDiy.SourceMode.Fixed);
                    break;
                case LogicDiyBlueprintModule.MaterialCondition:
                    logic.SetSourceMode(StorageNetworkLogicDiy.SourceMode.MaterialCondition);
                    break;
                case LogicDiyBlueprintModule.SingleChannel:
                    logic.SetOutputMode(StorageNetworkLogicDiy.ChannelMode.SingleChannel);
                    break;
                case LogicDiyBlueprintModule.FourChannel:
                    logic.SetOutputMode(StorageNetworkLogicDiy.ChannelMode.FourChannel);
                    break;
            }

            ShowLogicDiyOutputModePicker(logic);
        }

        private static void ApplyLogicDiyBlueprintModuleWithoutRefresh(StorageNetworkLogicDiy logic, LogicDiyBlueprintModule module)
        {
            if (logic == null)
            {
                return;
            }

            switch (module)
            {
                case LogicDiyBlueprintModule.FixedOutput:
                    logic.SetSourceMode(StorageNetworkLogicDiy.SourceMode.Fixed);
                    break;
                case LogicDiyBlueprintModule.MaterialCondition:
                    logic.SetSourceMode(StorageNetworkLogicDiy.SourceMode.MaterialCondition);
                    break;
                case LogicDiyBlueprintModule.SingleChannel:
                    logic.SetOutputMode(StorageNetworkLogicDiy.ChannelMode.SingleChannel);
                    break;
                case LogicDiyBlueprintModule.FourChannel:
                    logic.SetOutputMode(StorageNetworkLogicDiy.ChannelMode.FourChannel);
                    break;
            }
        }

        private static void CreateLogicDiyDroppedBlueprintModule(Transform canvas, string title, string detail, Vector2 position)
        {
            GameObject node = CreateLogicDiyBlueprintNode(canvas, title, position, new Vector2(248f, 96f), true, true);
            TextMeshProUGUI detailText = CreateText("ModuleDetail", node.transform, detail, 9, TextAlignmentOptions.TopLeft);
            detailText.color = new Color(0.82f, 0.84f, 0.83f, 1f);
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.raycastTarget = false;
            detailText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        }

        private static void CreateLogicDiyModuleColumn(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject column = CreateLogicDiyColumn(parent, "ModuleColumn", Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MODULE_LIBRARY), 218f);
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_DESC), logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.Fixed, () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.Fixed));
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION_DESC), logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition, () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.MaterialCondition));
            CreateLogicDiySectionLabel(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE));
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.SingleChannel, () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.SingleChannel));
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_TOOLTIP), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel, () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.FourChannel));
            CreateLogicDiySectionLabel(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FUTURE_MODULES));
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_LUA_MODULE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_LUA_MODULE_DESC), false, null);
            CreateLogicDiyPaletteButton(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_TIMER_MODULE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_TIMER_MODULE_DESC), false, null);
        }

        private static void CreateLogicDiyBlueprintColumn(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject column = CreateLogicDiyColumn(parent, "BlueprintColumn", Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONNECTION_GRAPH), 0f);
            column.GetComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject summary = CreatePlainImage("GraphSummary", column.transform, new Color(0.78f, 0.77f, 0.70f, 1f));
            summary.AddComponent<LayoutElement>().preferredHeight = 54f;
            TextMeshProUGUI summaryText = CreateText("Summary", summary.transform, BuildLogicDiyCurrentValueText(logic), 11, TextAlignmentOptions.MidlineLeft);
            summaryText.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            summaryText.textWrappingMode = TextWrappingModes.Normal;
            summaryText.raycastTarget = false;
            SetStretch(summaryText.rectTransform(), 10f, 6f, 6f, 6f);

            if (logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition)
            {
                CreateLogicDiyMaterialNode(column.transform, logic);
                CreateLogicDiyConnector(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONNECT_TO_COMPARE));
                CreateLogicDiyCompareNode(column.transform, logic);
                CreateLogicDiyConnector(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONNECT_TO_OUTPUT));
                CreateLogicDiyOutputNode(column.transform, logic);
            }
            else
            {
                CreateLogicDiyFixedNode(column.transform, logic);
                CreateLogicDiyConnector(column.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CONNECT_TO_OUTPUT));
                CreateLogicDiyOutputNode(column.transform, logic);
            }
        }

        private static GameObject CreateLogicDiyColumn(Transform parent, string name, string title, float width)
        {
            GameObject column = CreatePlainImage(name, parent, new Color(0.68f, 0.68f, 0.61f, 1f));
            LayoutElement layout = column.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                layout.preferredWidth = width;
                layout.minWidth = width;
                layout.flexibleWidth = 0f;
            }

            VerticalLayoutGroup group = column.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 8, 10);
            group.spacing = 8f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            TextMeshProUGUI titleText = CreateText("ColumnTitle", column.transform, title, 11, TextAlignmentOptions.MidlineLeft);
            titleText.color = new Color(0.20f, 0.22f, 0.23f, 1f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            return column;
        }

        private static void CreateLogicDiyPaletteButton(Transform parent, string title, string detail, bool selected, System.Action onClick)
        {
            GameObject row = CreateLogicDiyButtonRow(parent, 48f);
            CreateLogicDiyChoiceButton(row.transform, title, detail, selected, onClick);
        }

        private static void CreateLogicDiyMaterialNode(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject node = CreateLogicDiyNode(parent, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_MATERIAL), 92f);
            ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
            string selectedName = selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE);
            CreateLogicDiyWideButton(node.transform, selectedName, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_CURRENT), GameUtil.GetFormattedMass(selected.MassKg)), !string.IsNullOrEmpty(logic.ConditionItemKey), () => ShowLogicDiyMaterialPicker(logic));
        }

        private static void CreateLogicDiyCompareNode(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject node = CreateLogicDiyNode(parent, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_COMPARE), 168f);
            GameObject compareRow = CreateLogicDiyButtonRow(node.transform, 42f);
            CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE), ">=", logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual));
            CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT), "<", logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.LessThan, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.LessThan));
            CreateLogicDiyThresholdControl(node.transform, logic);
        }

        private static void CreateLogicDiyFixedNode(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject node = CreateLogicDiyNode(parent, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_FIXED), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 236f : 92f);
            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                GridLayoutGroup grid = CreateLogicDiyGrid(node.transform, 4, 40f, 16);
                for (int value = 0; value <= 15; value++)
                {
                    int capturedValue = value;
                    CreateLogicDiyChoiceButton(grid.transform, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_TITLE), value), ToFourBitBinary(value), logic.OutputSignalValue == value, () => SetLogicDiyOutputValue(logic, capturedValue));
                }
            }
            else
            {
                GameObject row = CreateLogicDiyButtonRow(node.transform, 42f);
                CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF_DESC), logic.OutputSignalValue == 0, () => SetLogicDiyOutputValue(logic, 0));
                CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON_DESC), logic.OutputSignalValue != 0, () => SetLogicDiyOutputValue(logic, 1));
            }
        }

        private static void CreateLogicDiyOutputNode(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject node = CreateLogicDiyNode(parent, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_NODE_OUTPUT), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 92f : 64f);
            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                GameObject row = CreateLogicDiyButtonRow(node.transform, 42f);
                for (int channel = 0; channel < 4; channel++)
                {
                    int capturedChannel = channel;
                    CreateLogicDiyChoiceButton(row.transform, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), channel), string.Format("{0}", 1 << channel), logic.ConditionOutputChannel == channel, () => SetLogicDiyConditionChannel(logic, capturedChannel));
                }
            }
            else
            {
                CreateLogicDiyWideButton(node.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_DESC), true, null);
            }
        }

        private static GameObject CreateLogicDiyNode(Transform parent, string title, float preferredHeight)
        {
            GameObject node = CreatePlainImage("BlueprintNode", parent, new Color(0.76f, 0.75f, 0.69f, 1f));
            node.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
            VerticalLayoutGroup group = node.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 7, 9);
            group.spacing = 6f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            TextMeshProUGUI titleText = CreateText("NodeTitle", node.transform, title, 10, TextAlignmentOptions.MidlineLeft);
            titleText.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            return node;
        }

        private static void CreateLogicDiyConnector(Transform parent, string label)
        {
            TextMeshProUGUI connector = CreateText("Connector", parent, "      > " + label, 10, TextAlignmentOptions.MidlineLeft);
            connector.color = new Color(0.42f, 0.45f, 0.43f, 1f);
            connector.fontStyle = FontStyles.Bold;
            connector.raycastTarget = false;
            connector.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private static void CreateLogicDiySummaryCard(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject card = CreateLogicDiyCard(parent, "SummaryCard", Loc.Get(Loc.UI.STORAGE_NETWORK.PRODUCTION_STATUS_TITLE), 76f);
            TextMeshProUGUI text = CreateText("Summary", card.transform, BuildLogicDiyCurrentValueText(logic), 11, TextAlignmentOptions.TopLeft);
            text.color = new Color(0.22f, 0.24f, 0.23f, 1f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        }

        private static void CreateLogicDiyModeCard(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject card = CreateLogicDiyCard(parent, "ModeCard", Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_MODE), 122f);
            GameObject sourceRow = CreateLogicDiyLabeledButtonRow(card.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_SOURCE), 42f);
            CreateLogicDiyChoiceButton(sourceRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_DESC), logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.Fixed, () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.Fixed));
            CreateLogicDiyChoiceButton(sourceRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION_DESC), logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition, () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.MaterialCondition));

            GameObject channelRow = CreateLogicDiyLabeledButtonRow(card.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE), 42f);
            CreateLogicDiyChoiceButton(channelRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.SingleChannel, () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.SingleChannel));
            CreateLogicDiyChoiceButton(channelRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_TOOLTIP), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel, () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.FourChannel));
        }

        private static void CreateLogicDiyFixedOutputCard(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject card = CreateLogicDiyCard(parent, "FixedOutputCard", Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_FIXED_VALUE), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 230f : 92f);
            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                GridLayoutGroup grid = CreateLogicDiyGrid(card.transform, 4, 44f, 16);
                for (int value = 0; value <= 15; value++)
                {
                    int capturedValue = value;
                    CreateLogicDiyChoiceButton(
                        grid.transform,
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_TITLE), value),
                        ToFourBitBinary(value),
                        logic.OutputSignalValue == value,
                        () => SetLogicDiyOutputValue(logic, capturedValue));
                }
            }
            else
            {
                GameObject row = CreateLogicDiyButtonRow(card.transform, 44f);
                CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF_DESC), logic.OutputSignalValue == 0, () => SetLogicDiyOutputValue(logic, 0));
                CreateLogicDiyChoiceButton(row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON_DESC), logic.OutputSignalValue != 0, () => SetLogicDiyOutputValue(logic, 1));
            }
        }

        private static void CreateLogicDiyConditionCard(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject card = CreateLogicDiyCard(parent, "ConditionCard", Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION), logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 330f : 274f);
            ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
            string selectedName = selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE);
            CreateLogicDiyLabeledWideButtonRow(
                card.transform,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_MATERIAL),
                selectedName,
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_CURRENT), GameUtil.GetFormattedMass(selected.MassKg)),
                !string.IsNullOrEmpty(logic.ConditionItemKey),
                () => ShowLogicDiyMaterialPicker(logic));

            GameObject compareRow = CreateLogicDiyLabeledButtonRow(card.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_COMPARE), 44f);
            CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE_DESC), logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual));
            CreateLogicDiyChoiceButton(compareRow.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT), Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT_DESC), logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.LessThan, () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.LessThan));

            CreateLogicDiyThresholdControl(card.transform, logic);

            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                GameObject portRow = CreateLogicDiyLabeledButtonRow(card.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_OUTPUT_PORT), 42f);
                for (int channel = 0; channel < 4; channel++)
                {
                    int capturedChannel = channel;
                    CreateLogicDiyChoiceButton(portRow.transform, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), channel), string.Format("{0}", 1 << channel), logic.ConditionOutputChannel == channel, () => SetLogicDiyConditionChannel(logic, capturedChannel));
                }
            }
            else
            {
                CreateLogicDiyLabeledWideButtonRow(
                    card.transform,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_OUTPUT_PORT),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_DESC),
                    true,
                    null);
            }
        }

        private static void SetLogicDiyThresholdAndRefresh(StorageNetworkLogicDiy logic, float threshold)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetConditionThreshold(threshold);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void CreateLogicDiyThresholdControl(Transform parent, StorageNetworkLogicDiy logic)
        {
            GameObject row = new GameObject("ThresholdControl");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = 72f;

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            TextMeshProUGUI label = CreateText("ThresholdLabel", row.transform, Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_THRESHOLD), 10, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.23f, 0.25f, 0.25f, 1f);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 82f;
            labelLayout.minWidth = 82f;

            GameObject block = new GameObject("ThresholdControls");
            block.transform.SetParent(row.transform, false);
            block.AddComponent<RectTransform>();
            block.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup blockLayout = block.AddComponent<VerticalLayoutGroup>();
            blockLayout.spacing = 6f;
            blockLayout.childControlWidth = true;
            blockLayout.childControlHeight = true;
            blockLayout.childForceExpandWidth = true;
            blockLayout.childForceExpandHeight = false;

            GameObject inputRow = new GameObject("ThresholdInputRow");
            inputRow.transform.SetParent(block.transform, false);
            inputRow.AddComponent<RectTransform>();
            inputRow.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup inputLayout = inputRow.AddComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 6f;
            inputLayout.childAlignment = TextAnchor.MiddleLeft;
            inputLayout.childControlWidth = true;
            inputLayout.childControlHeight = true;
            inputLayout.childForceExpandWidth = false;
            inputLayout.childForceExpandHeight = false;

            KInputTextField input = StorageNetworkInputBuilder.CreateKNumberInput(
                inputRow.transform,
                "ThresholdInput",
                FormatLogicDiyThresholdInput(logic.ConditionThresholdKg),
                100f,
                24f,
                12,
                TextAlignmentOptions.MidlineRight,
                Color.white,
                new Color(0.08f, 0.09f, 0.10f, 1f),
                new Vector2(6f, 2f),
                true);
            StorageNetworkNumberInputField numberInput = input.gameObject.AddComponent<StorageNetworkNumberInputField>();
            numberInput.Configure(input, 0f, 1000000f, false);
            numberInput.SetAmount(Mathf.Max(0f, logic.ConditionThresholdKg));
            numberInput.onEndEdit += () => SetLogicDiyThresholdAndRefresh(logic, numberInput.currentValue);
            input.onEndEdit.AddListener(value =>
            {
                if (TryParseLogicDiyThreshold(value, out float parsed))
                {
                    SetLogicDiyThresholdAndRefresh(logic, parsed);
                }
            });

            TextMeshProUGUI unit = CreateText("ThresholdUnit", inputRow.transform, "kg", 11, TextAlignmentOptions.MidlineLeft);
            unit.color = new Color(0.32f, 0.34f, 0.33f, 1f);
            unit.raycastTarget = false;
            unit.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;

            GameObject spacer = new GameObject("ThresholdSpacer");
            spacer.transform.SetParent(inputRow.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

            CreateLogicDiyStepButton(inputRow.transform, "▼", () => SetLogicDiyThresholdAndRefresh(logic, Mathf.Max(0f, logic.ConditionThresholdKg - GetLogicDiyThresholdStep(logic.ConditionThresholdKg))));
            CreateLogicDiyStepButton(inputRow.transform, "▲", () => SetLogicDiyThresholdAndRefresh(logic, logic.ConditionThresholdKg + GetLogicDiyThresholdStep(logic.ConditionThresholdKg)));

            GameObject presetRow = CreateLogicDiyButtonRow(block.transform, 28f);
            CreateLogicDiyChoiceButton(presetRow.transform, "100kg", string.Empty, Mathf.Approximately(logic.ConditionThresholdKg, 100f), () => SetLogicDiyThresholdAndRefresh(logic, 100f));
            CreateLogicDiyChoiceButton(presetRow.transform, "500kg", string.Empty, Mathf.Approximately(logic.ConditionThresholdKg, 500f), () => SetLogicDiyThresholdAndRefresh(logic, 500f));
            CreateLogicDiyChoiceButton(presetRow.transform, "1t", string.Empty, Mathf.Approximately(logic.ConditionThresholdKg, 1000f), () => SetLogicDiyThresholdAndRefresh(logic, 1000f));
        }

        private static void CreateLogicDiyStepButton(Transform parent, string text, System.Action onClick)
        {
            GameObject buttonObject = new GameObject("ThresholdStepButton");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 28f;
            layout.minWidth = 28f;
            layout.preferredHeight = 24f;
            layout.minHeight = 24f;

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = KleiBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            TextMeshProUGUI label = CreateText("Text", buttonObject.transform, text, 13, TextAlignmentOptions.Center);
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static float GetLogicDiyThresholdStep(float currentThreshold)
        {
            return currentThreshold >= 1000f ? 100f : 10f;
        }

        private static string FormatLogicDiyThresholdInput(float value)
        {
            return Mathf.Max(0f, value).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryParseLogicDiyThreshold(string value, out float amount)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out amount) ||
                   float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out amount);
        }

        private static GameObject CreateLogicDiyCard(Transform parent, string name, string title, float preferredHeight)
        {
            GameObject card = CreatePlainImage(name, parent, new Color(0.68f, 0.68f, 0.61f, 1f));
            LayoutElement layout = card.AddComponent<LayoutElement>();
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;

            VerticalLayoutGroup group = card.AddComponent<VerticalLayoutGroup>();
            group.padding = new RectOffset(10, 10, 8, 10);
            group.spacing = 6f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;

            TextMeshProUGUI titleText = CreateText("CardTitle", card.transform, title, 11, TextAlignmentOptions.MidlineLeft);
            titleText.color = new Color(0.20f, 0.22f, 0.23f, 1f);
            titleText.fontStyle = FontStyles.Bold;
            titleText.raycastTarget = false;
            titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
            return card;
        }

        private static GameObject CreateLogicDiyButtonRow(Transform parent, float height)
        {
            GameObject row = new GameObject("ButtonRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            LayoutElement layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = height;
            HorizontalLayoutGroup group = row.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 6f;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            return row;
        }

        private static GameObject CreateLogicDiyLabeledButtonRow(Transform parent, string label, float height)
        {
            GameObject row = new GameObject("LabeledButtonRow");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = height;

            HorizontalLayoutGroup rowGroup = row.AddComponent<HorizontalLayoutGroup>();
            rowGroup.spacing = 8f;
            rowGroup.childControlWidth = true;
            rowGroup.childControlHeight = true;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childForceExpandHeight = true;

            TextMeshProUGUI labelText = CreateText("RowLabel", row.transform, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.23f, 0.25f, 0.25f, 1f);
            labelText.fontStyle = FontStyles.Bold;
            labelText.raycastTarget = false;
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 82f;
            labelLayout.minWidth = 82f;

            GameObject buttons = CreateLogicDiyButtonRow(row.transform, height);
            buttons.GetComponent<LayoutElement>().flexibleWidth = 1f;
            return buttons;
        }

        private static void CreateLogicDiySectionLabel(Transform parent, string label)
        {
            TextMeshProUGUI labelText = CreateText("SectionLabel", parent, label, 10, TextAlignmentOptions.MidlineLeft);
            labelText.color = new Color(0.23f, 0.25f, 0.25f, 1f);
            labelText.fontStyle = FontStyles.Bold;
            labelText.raycastTarget = false;
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        }

        private static GridLayoutGroup CreateLogicDiyGrid(Transform parent, int columns, float cellHeight, int itemCount)
        {
            GameObject gridObject = new GameObject("ButtonGrid");
            gridObject.transform.SetParent(parent, false);
            RectTransform rect = gridObject.AddComponent<RectTransform>();
            LayoutElement layout = gridObject.AddComponent<LayoutElement>();
            layout.preferredHeight = Mathf.CeilToInt(Mathf.Max(1, itemCount) / (float)Mathf.Max(1, columns)) * cellHeight +
                Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(1, itemCount) / (float)Mathf.Max(1, columns)) - 1) * 6f;
            GridLayoutGroup grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = new Vector2(6f, 6f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.cellSize = new Vector2(132f, cellHeight);
            rect.sizeDelta = new Vector2(0f, layout.preferredHeight);
            return grid;
        }

        private static void CreateLogicDiyWideButton(Transform parent, string title, string detail, bool selected, System.Action onClick)
        {
            GameObject row = CreateLogicDiyButtonRow(parent, 44f);
            CreateLogicDiyChoiceButton(row.transform, title, detail, selected, onClick);
        }

        private static void CreateLogicDiyLabeledWideButtonRow(Transform parent, string label, string title, string detail, bool selected, System.Action onClick)
        {
            GameObject buttons = CreateLogicDiyLabeledButtonRow(parent, label, 64f);
            CreateLogicDiyChoiceButton(buttons.transform, title, detail, selected, onClick);
        }

        private static void CreateLogicDiyChoiceButton(Transform parent, string title, string detail, bool selected, System.Action onClick)
        {
            GameObject buttonObject = new GameObject("ChoiceButton");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();
            buttonObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            KImage background = buttonObject.AddComponent<KImage>();
            background.type = Image.Type.Sliced;
            ApplyThinButtonSprite(background);
            background.colorStyleSetting = selected ? KleiPinkStyle() : KleiBlueStyle();
            background.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = background;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            if (onClick != null)
            {
                button.onClick += () => onClick();
            }

            TextMeshProUGUI titleText = CreateText("Title", buttonObject.transform, title, 10, TextAlignmentOptions.MidlineLeft);
            titleText.color = new Color(0.95f, 0.96f, 0.98f, 1f);
            titleText.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            titleText.textWrappingMode = TextWrappingModes.NoWrap;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.raycastTarget = false;
            RectTransform titleRect = titleText.rectTransform();
            titleRect.anchorMin = string.IsNullOrEmpty(detail) ? Vector2.zero : new Vector2(0f, 0.46f);
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(8f, string.IsNullOrEmpty(detail) ? 0f : 0f);
            titleRect.offsetMax = new Vector2(-8f, string.IsNullOrEmpty(detail) ? 0f : -4f);

            if (!string.IsNullOrEmpty(detail))
            {
                TextMeshProUGUI detailText = CreateText("Detail", buttonObject.transform, detail, 8, TextAlignmentOptions.MidlineLeft);
                detailText.color = selected ? new Color(0.88f, 0.84f, 0.78f, 1f) : new Color(0.70f, 0.73f, 0.78f, 1f);
                detailText.textWrappingMode = TextWrappingModes.NoWrap;
                detailText.overflowMode = TextOverflowModes.Ellipsis;
                detailText.raycastTarget = false;
                RectTransform detailRect = detailText.rectTransform();
                detailRect.anchorMin = Vector2.zero;
                detailRect.anchorMax = new Vector2(1f, 0.48f);
                detailRect.offsetMin = new Vector2(8f, 3f);
                detailRect.offsetMax = new Vector2(-8f, -1f);
            }
        }

        private static List<ProductionPickerOption> BuildLogicDiyOptions(StorageNetworkLogicDiy logic)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_SOURCE),
                    GetLogicDiySourceModeName(logic.OutputSourceMode),
                    false,
                    () => ShowLogicDiySourcePicker(logic),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_SOURCE_TOOLTIP)),
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE),
                    GetLogicDiyModeName(logic.OutputMode),
                    false,
                    () => ShowLogicDiyChannelModePicker(logic),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE_TOOLTIP))
            };

            if (logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition)
            {
                ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
                string selectedName = selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE);
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_MATERIAL),
                    string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_VALUE), selectedName, GameUtil.GetFormattedMass(selected.MassKg)),
                    false,
                    () => ShowLogicDiyMaterialPicker(logic),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_TOOLTIP)));
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_COMPARE),
                    GetLogicDiyComparisonName(logic.ConditionComparison),
                    false,
                    () => ShowLogicDiyComparisonPicker(logic),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_COMPARE_TOOLTIP)));
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_THRESHOLD),
                    GameUtil.GetFormattedMass(Mathf.Max(0f, logic.ConditionThresholdKg)),
                    false,
                    () => ShowLogicDiyThresholdPicker(logic),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_TOOLTIP)));
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_OUTPUT_PORT),
                    GetLogicDiyOutputChannelName(logic),
                    false,
                    logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? () => ShowLogicDiyConditionChannelPicker(logic) : null,
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_TOOLTIP)));
                return options;
            }

            options.Add(new ProductionPickerOption(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_FIXED_VALUE),
                BuildLogicDiyFixedValueSummary(logic),
                false,
                () => ShowLogicDiyFixedValuePicker(logic),
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_TOOLTIP)));
            return options;
        }

        private static void AddLogicDiyConditionOptions(List<ProductionPickerOption> options, StorageNetworkLogicDiy logic)
        {
            ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
            string selectedName = selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE);
            options.Add(new ProductionPickerOption(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL),
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_VALUE), selectedName, GameUtil.GetFormattedMass(selected.MassKg)),
                !string.IsNullOrEmpty(logic.ConditionItemKey),
                () => ShowLogicDiyMaterialPicker(logic),
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_TOOLTIP)));

            options.Add(new ProductionPickerOption(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE_DESC),
                logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual,
                () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual),
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE_TOOLTIP)));
            options.Add(new ProductionPickerOption(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT_DESC),
                logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.LessThan,
                () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.LessThan),
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT_TOOLTIP)));

            options.Add(new ProductionPickerOption(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD),
                GameUtil.GetFormattedMass(Mathf.Max(0f, logic.ConditionThresholdKg)),
                false,
                () => ShowLogicDiyThresholdPicker(logic),
                null,
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_TOOLTIP)));

            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                for (int channel = 0; channel < 4; channel++)
                {
                    int capturedChannel = channel;
                    options.Add(new ProductionPickerOption(
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), channel),
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_DESC), 1 << channel),
                        logic.ConditionOutputChannel == channel,
                        () => SetLogicDiyConditionChannel(logic, capturedChannel),
                        null,
                        Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_TOOLTIP)));
                }
            }
            else
            {
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_DESC),
                    true,
                    null,
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE_TOOLTIP)));
            }
        }

        private static void SetLogicDiyOutputMode(StorageNetworkLogicDiy logic, StorageNetworkLogicDiy.ChannelMode mode)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetOutputMode(mode);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void SetLogicDiySourceMode(StorageNetworkLogicDiy logic, StorageNetworkLogicDiy.SourceMode mode)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetSourceMode(mode);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void SetLogicDiyOutputValue(StorageNetworkLogicDiy logic, int value)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetSignalValue(value);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void SetLogicDiyComparison(StorageNetworkLogicDiy logic, StorageNetworkLogicDiy.ComparisonMode comparison)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetConditionComparison(comparison);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void SetLogicDiyConditionChannel(StorageNetworkLogicDiy logic, int channel)
        {
            if (logic == null)
            {
                CloseStandaloneOutputFilterPicker();
                return;
            }

            logic.SetConditionOutputChannel(channel);
            ShowLogicDiyOutputModePicker(logic);
        }

        private static void ShowLogicDiySourcePicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_DESC),
                    logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.Fixed,
                    () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.Fixed),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_TOOLTIP)),
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION_DESC),
                    logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition,
                    () => SetLogicDiySourceMode(logic, StorageNetworkLogicDiy.SourceMode.MaterialCondition),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION_TOOLTIP))
            };

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_SOURCE),
                options,
                GetLogicDiySourceModeName(logic.OutputSourceMode),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_SOURCE_TOOLTIP),
                false,
                "logicDiySourcePicker",
                430f,
                360f);
        }

        private static void ShowLogicDiyChannelModePicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            StorageNetworkLogicDiy.ChannelMode currentMode = logic.OutputMode;
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_DESC),
                    currentMode == StorageNetworkLogicDiy.ChannelMode.SingleChannel,
                    () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.SingleChannel),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL_TOOLTIP)),
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_DESC),
                    currentMode == StorageNetworkLogicDiy.ChannelMode.FourChannel,
                    () => SetLogicDiyOutputMode(logic, StorageNetworkLogicDiy.ChannelMode.FourChannel),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL_TOOLTIP))
            };

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE),
                options,
                GetLogicDiyModeName(logic.OutputMode),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_CHANNEL_MODE_TOOLTIP),
                false,
                "logicDiyChannelModePicker",
                430f,
                360f);
        }

        private static void ShowLogicDiyFixedValuePicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>();
            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                for (int value = 0; value <= 15; value++)
                {
                    int capturedValue = value;
                    options.Add(new ProductionPickerOption(
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_TITLE), capturedValue),
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_BINARY), ToFourBitBinary(capturedValue)),
                        logic.OutputSignalValue == capturedValue,
                        () => SetLogicDiyOutputValue(logic, capturedValue),
                        null,
                        string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_VALUE_TOOLTIP), capturedValue, ToFourBitBinary(capturedValue))));
                }
            }
            else
            {
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF_DESC),
                    logic.OutputSignalValue == 0,
                    () => SetLogicDiyOutputValue(logic, 0),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF_TOOLTIP)));
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON_DESC),
                    logic.OutputSignalValue != 0,
                    () => SetLogicDiyOutputValue(logic, 1),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON_TOOLTIP)));
            }

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_FIXED_VALUE),
                options,
                BuildLogicDiyFixedValueSummary(logic),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED_TOOLTIP),
                false,
                "logicDiyFixedValuePicker",
                logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 430f : 360f,
                logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel ? 620f : 360f);
        }

        private static void ShowLogicDiyComparisonPicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE_DESC),
                    logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual,
                    () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE_TOOLTIP)),
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT_DESC),
                    logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.LessThan,
                    () => SetLogicDiyComparison(logic, StorageNetworkLogicDiy.ComparisonMode.LessThan),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT_TOOLTIP))
            };

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_COMPARE),
                options,
                GetLogicDiyComparisonName(logic.ConditionComparison),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_COMPARE_TOOLTIP),
                false,
                "logicDiyComparisonPicker",
                430f,
                360f);
        }

        private static void ShowLogicDiyConditionChannelPicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>();
            for (int channel = 0; channel < 4; channel++)
            {
                int capturedChannel = channel;
                options.Add(new ProductionPickerOption(
                    string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), channel),
                    string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_DESC), 1 << channel),
                    logic.ConditionOutputChannel == channel,
                    () => SetLogicDiyConditionChannel(logic, capturedChannel),
                    null,
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_TOOLTIP)));
            }

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SETTING_OUTPUT_PORT),
                options,
                GetLogicDiyOutputChannelName(logic),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_TOOLTIP),
                false,
                "logicDiyConditionChannelPicker",
                430f,
                420f);
        }

        private static void ShowLogicDiyMaterialPicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            List<ProductionPickerOption> options = new List<ProductionPickerOption>();
            foreach (ItemTotal total in GetLogicDiyItemTotals(logic))
            {
                ItemTotal captured = total;
                options.Add(new ProductionPickerOption(
                    captured.Name,
                    GameUtil.GetFormattedMass(captured.MassKg),
                    captured.Key == logic.ConditionItemKey,
                    () =>
                    {
                        logic.SetConditionItem(captured.Key);
                        ShowLogicDiyOutputModePicker(logic);
                    },
                    null,
                    captured.Key));
            }

            if (options.Count == 0)
            {
                options.Add(new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE_DESC),
                    false,
                    () => ShowLogicDiyOutputModePicker(logic)));
            }

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_PICKER_TITLE),
                options,
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_PICKER_COUNT), options.Count),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_PICKER_HINT),
                true,
                "logicDiyMaterialPicker",
                540f,
                620f);
        }

        private static void ShowLogicDiyThresholdPicker(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return;
            }

            float[] thresholds = new[] { 1f, 10f, 100f, 1000f, 10000f, 100000f };
            List<ProductionPickerOption> options = new List<ProductionPickerOption>();
            if (logic.ConditionThresholdKg > 0f && !thresholds.Any(value => Mathf.Approximately(value, logic.ConditionThresholdKg)))
            {
                options.Add(BuildThresholdOption(logic, logic.ConditionThresholdKg));
            }

            foreach (float threshold in thresholds)
            {
                options.Add(BuildThresholdOption(logic, threshold));
            }

            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_PICKER_TITLE),
                options,
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_CURRENT), GameUtil.GetFormattedMass(logic.ConditionThresholdKg)),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_PICKER_HINT),
                false,
                "logicDiyThresholdPicker",
                430f,
                520f);
        }

        private static ProductionPickerOption BuildThresholdOption(StorageNetworkLogicDiy logic, float threshold)
        {
            float capturedThreshold = threshold;
            return new ProductionPickerOption(
                GameUtil.GetFormattedMass(capturedThreshold),
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_THRESHOLD_OPTION_DESC),
                Mathf.Approximately(logic.ConditionThresholdKg, capturedThreshold),
                () =>
                {
                    logic.SetConditionThreshold(capturedThreshold);
                    ShowLogicDiyOutputModePicker(logic);
                });
        }

        private static string BuildLogicDiyCurrentValueText(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return string.Empty;
            }

            if (logic.OutputSourceMode == StorageNetworkLogicDiy.SourceMode.MaterialCondition)
            {
                ItemTotal selected = GetLogicDiySelectedItemTotal(logic);
                string compare = logic.ConditionComparison == StorageNetworkLogicDiy.ComparisonMode.GreaterOrEqual ? ">=" : "<";
                string channel = logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), logic.ConditionOutputChannel)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE);
                return string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_CONDITION),
                    selected.Name ?? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_MATERIAL_NONE),
                    GameUtil.GetFormattedMass(selected.MassKg),
                    compare,
                    GameUtil.GetFormattedMass(logic.ConditionThresholdKg),
                    channel);
            }

            if (logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                return string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_VALUE_FOUR),
                    logic.OutputSignalValue,
                    ToFourBitBinary(logic.OutputSignalValue));
            }

            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_VALUE_SINGLE),
                logic.OutputSignalValue != 0
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF));
        }

        private static string GetLogicDiyModeName(StorageNetworkLogicDiy.ChannelMode mode)
        {
            return mode == StorageNetworkLogicDiy.ChannelMode.FourChannel
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_FOUR_CHANNEL)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SINGLE_CHANNEL);
        }

        private static string GetLogicDiySourceModeName(StorageNetworkLogicDiy.SourceMode mode)
        {
            return mode == StorageNetworkLogicDiy.SourceMode.MaterialCondition
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_CONDITION)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_SOURCE_FIXED);
        }

        private static string GetLogicDiyComparisonName(StorageNetworkLogicDiy.ComparisonMode comparison)
        {
            return comparison == StorageNetworkLogicDiy.ComparisonMode.LessThan
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_LT)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_COMPARE_GE);
        }

        private static string BuildLogicDiyFixedValueSummary(StorageNetworkLogicDiy logic)
        {
            if (logic == null)
            {
                return string.Empty;
            }

            return logic.OutputMode == StorageNetworkLogicDiy.ChannelMode.FourChannel
                ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_VALUE_FOUR), logic.OutputSignalValue, ToFourBitBinary(logic.OutputSignalValue))
                : string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_CURRENT_VALUE_SINGLE),
                    logic.OutputSignalValue != 0
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_ON)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_OFF));
        }

        private static string GetLogicDiyOutputChannelName(StorageNetworkLogicDiy logic)
        {
            if (logic == null || logic.OutputMode != StorageNetworkLogicDiy.ChannelMode.FourChannel)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL_SINGLE);
            }

            return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.LOGIC_DIY_OUTPUT_CHANNEL), logic.ConditionOutputChannel);
        }

        private static string ToFourBitBinary(int value)
        {
            return System.Convert.ToString(Mathf.Clamp(value, 0, 15), 2).PadLeft(4, '0');
        }

        private static List<ItemTotal> GetLogicDiyItemTotals(StorageNetworkLogicDiy logic)
        {
            Dictionary<string, LogicDiyItemTotalAccumulator> totals = new Dictionary<string, LogicDiyItemTotalAccumulator>();
            int worldId = logic != null && logic.gameObject != null ? logic.gameObject.GetMyWorldId() : -1;
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId);
            if (snapshot?.Storages == null || !snapshot.NetworkOnline)
            {
                return new List<ItemTotal>();
            }

            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.StoredItems == null)
                {
                    continue;
                }

                foreach (GameObject item in info.StoredItems)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    string key = StorageItemUtility.GetStoredItemKey(item);
                    if (string.IsNullOrEmpty(key))
                    {
                        continue;
                    }

                    if (!totals.TryGetValue(key, out LogicDiyItemTotalAccumulator total))
                    {
                        total = new LogicDiyItemTotalAccumulator(key, StorageNetworkStorageDisplay.GetStoredItemName(item));
                        totals.Add(key, total);
                    }

                    total.MassKg += StorageItemUtility.GetMass(item);
                }
            }

            return totals.Values
                .Select(total => new ItemTotal(total.Key, total.Name, total.MassKg))
                .OrderBy(total => total.Name)
                .ToList();
        }

        private static ItemTotal GetLogicDiySelectedItemTotal(StorageNetworkLogicDiy logic)
        {
            if (logic == null || string.IsNullOrEmpty(logic.ConditionItemKey))
            {
                return default;
            }

            foreach (ItemTotal total in GetLogicDiyItemTotals(logic))
            {
                if (total.Key == logic.ConditionItemKey)
                {
                    return total;
                }
            }

            return new ItemTotal(logic.ConditionItemKey, logic.ConditionItemKey, logic.GetConditionAmountKg());
        }

        private sealed class LogicDiyItemTotalAccumulator
        {
            public LogicDiyItemTotalAccumulator(string key, string name)
            {
                Key = key;
                Name = name;
            }

            public string Key { get; }

            public string Name { get; }

            public float MassKg { get; set; }
        }

        private readonly struct ItemTotal
        {
            public ItemTotal(string key, string name, float massKg)
            {
                Key = key;
                Name = name;
                MassKg = massKg;
            }

            public string Key { get; }

            public string Name { get; }

            public float MassKg { get; }
        }

        private sealed class LogicDiyBlueprintCanvas : MonoBehaviour
        {
        }

        private sealed class LogicDiyPaletteDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private StorageNetworkLogicDiy logic;
            private LogicDiyBlueprintModule module;
            private string title;
            private string detail;
            private GameObject ghost;

            public void Configure(StorageNetworkLogicDiy targetLogic, LogicDiyBlueprintModule targetModule, string targetTitle, string targetDetail)
            {
                logic = targetLogic;
                module = targetModule;
                title = targetTitle;
                detail = targetDetail;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                TryCreateDragGhost();
                MoveGhost(eventData);
            }

            private void TryCreateDragGhost()
            {
                try
                {
                    Transform ghostParent = transform != null ? transform.root : null;
                    if (ghostParent == null)
                    {
                        return;
                    }

                    ghost = new GameObject("ModuleDragGhost");
                    ghost.transform.SetParent(ghostParent, false);
                    RectTransform rect = ghost.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(160f, 42f);
                    Image image = ghost.AddComponent<Image>();
                    image.color = new Color(0.75f, 0.32f, 0.54f, 0.78f);
                    image.raycastTarget = false;
                    CanvasGroup canvasGroup = ghost.AddComponent<CanvasGroup>();
                    canvasGroup.blocksRaycasts = false;

                    TextMeshProUGUI label = CreateText("Label", ghost.transform, title ?? string.Empty, 10, TextAlignmentOptions.Center);
                    label.color = Color.white;
                    label.fontStyle = FontStyles.Bold;
                    RectTransform labelRect = label.rectTransform();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = new Vector2(6f, 2f);
                    labelRect.offsetMax = new Vector2(-6f, -2f);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"StorageNetwork LogicDiy drag preview failed: {ex.Message}");
                    Object.Destroy(ghost);
                    ghost = null;
                }
            }

            public void OnDrag(PointerEventData eventData)
            {
                MoveGhost(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                Object.Destroy(ghost);
                ghost = null;

                LogicDiyBlueprintCanvas[] canvases = Object.FindObjectsByType<LogicDiyBlueprintCanvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (LogicDiyBlueprintCanvas canvas in canvases)
                {
                    RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                    if (canvasRect != null && RectTransformUtility.RectangleContainsScreenPoint(canvasRect, eventData.position, eventData.pressEventCamera))
                    {
                        ApplyLogicDiyBlueprintModuleWithoutRefresh(logic, module);
                        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPosition))
                        {
                            Vector2 topLeftPosition = localPosition;
                            CreateLogicDiyDroppedBlueprintModule(canvas.transform, title, detail, topLeftPosition);
                        }

                        return;
                    }
                }
            }

            private void MoveGhost(PointerEventData eventData)
            {
                if (ghost == null)
                {
                    return;
                }

                RectTransform rect = ghost.GetComponent<RectTransform>();
                RectTransform parentRect = rect.parent as RectTransform;
                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPosition))
                {
                    rect.anchoredPosition = localPosition;
                }
            }
        }

        private sealed class LogicDiyBlueprintNodeDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            private RectTransform rect;
            private Vector2 startPosition;
            private Vector2 startPointer;

            public void OnBeginDrag(PointerEventData eventData)
            {
                rect = GetComponent<RectTransform>();
                startPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
                RectTransform parentRect = rect != null ? rect.parent as RectTransform : null;
                if (parentRect != null)
                {
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out startPointer);
                }
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (rect == null)
                {
                    return;
                }

                RectTransform parentRect = rect.parent as RectTransform;
                if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 pointer))
                {
                    rect.anchoredPosition = startPosition + (pointer - startPointer);
                }
            }
        }

        private sealed class LogicDiyConnectionLine : MonoBehaviour, IPointerClickHandler
        {
            private RectTransform canvas;
            private RectTransform from;
            private RectTransform to;
            private RectTransform line;

            public void Configure(RectTransform targetCanvas, RectTransform fromEndpoint, RectTransform toEndpoint)
            {
                canvas = targetCanvas;
                from = fromEndpoint;
                to = toEndpoint;
                line = GetComponent<RectTransform>();
                UpdateLine();
            }

            private void LateUpdate()
            {
                UpdateLine();
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                {
                    Object.Destroy(gameObject);
                    eventData.Use();
                }
            }

            private void UpdateLine()
            {
                if (canvas == null || from == null || to == null)
                {
                    return;
                }

                Vector2 start = EndpointToCanvasLocal(canvas, from);
                Vector2 end = EndpointToCanvasLocal(canvas, to);
                PositionLine(line, start, end);
                transform.SetAsFirstSibling();
            }

            private static Vector2 EndpointToCanvasLocal(RectTransform canvasRect, RectTransform endpoint)
            {
                Vector3 world = endpoint.TransformPoint(endpoint.rect.center);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    RectTransformUtility.WorldToScreenPoint(null, world),
                    null,
                    out Vector2 local);
                return local;
            }

            private static void PositionLine(RectTransform rect, Vector2 start, Vector2 end)
            {
                if (rect == null)
                {
                    return;
                }

                Vector2 middle = (start + end) * 0.5f;
                float length = Mathf.Max(8f, Vector2.Distance(start, end));
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = middle;
                rect.sizeDelta = new Vector2(length, 6f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            }
        }

        private sealed class LogicDiyEndpointHandle : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            private static LogicDiyEndpointHandle pending;

            public bool IsOutput { get; set; }
            private GameObject previewLine;
            private RectTransform canvasRect;

            public void OnPointerClick(PointerEventData eventData)
            {
                if (pending == null)
                {
                    pending = this;
                    return;
                }

                if (pending != this && pending.IsOutput != IsOutput)
                {
                    Transform canvas = GetComponentInParent<LogicDiyBlueprintCanvas>()?.transform;
                    if (canvas != null)
                    {
                        DrawConnection(canvas, pending.GetComponent<RectTransform>(), GetComponent<RectTransform>());
                    }
                }

                pending = null;
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                Transform canvas = GetComponentInParent<LogicDiyBlueprintCanvas>()?.transform;
                canvasRect = canvas as RectTransform;
                if (canvasRect == null)
                {
                    return;
                }

                previewLine = new GameObject("ConnectionPreview");
                previewLine.transform.SetParent(canvas, false);
                Image image = previewLine.AddComponent<Image>();
                image.color = new Color(0.75f, 0.32f, 0.54f, 0.72f);
                image.raycastTarget = false;
                UpdatePreview(eventData);
            }

            public void OnDrag(PointerEventData eventData)
            {
                UpdatePreview(eventData);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                LogicDiyEndpointHandle target = FindEndpointUnderPointer(eventData);
                if (target != null && target != this && target.IsOutput != IsOutput)
                {
                    Transform canvas = GetComponentInParent<LogicDiyBlueprintCanvas>()?.transform;
                    if (canvas != null)
                    {
                        DrawConnection(canvas, GetComponent<RectTransform>(), target.GetComponent<RectTransform>());
                    }
                }

                Object.Destroy(previewLine);
                previewLine = null;
                canvasRect = null;
            }

            private void UpdatePreview(PointerEventData eventData)
            {
                if (previewLine == null || canvasRect == null)
                {
                    return;
                }

                RectTransform endpointRect = GetComponent<RectTransform>();
                Vector3 startWorld = endpointRect.TransformPoint(endpointRect.rect.center);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(null, startWorld), null, out Vector2 start);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 end);
                PositionLine(previewLine.GetComponent<RectTransform>(), start, end);
                previewLine.transform.SetAsFirstSibling();
            }

            private static LogicDiyEndpointHandle FindEndpointUnderPointer(PointerEventData eventData)
            {
                if (UnityEngine.EventSystems.EventSystem.current == null)
                {
                    return null;
                }

                List<RaycastResult> results = new List<RaycastResult>();
                UnityEngine.EventSystems.EventSystem.current.RaycastAll(eventData, results);
                foreach (RaycastResult result in results)
                {
                    LogicDiyEndpointHandle endpoint = result.gameObject != null ? result.gameObject.GetComponent<LogicDiyEndpointHandle>() : null;
                    if (endpoint != null)
                    {
                        return endpoint;
                    }
                }

                return null;
            }

            private static void DrawConnection(Transform canvas, RectTransform a, RectTransform b)
            {
                if (a == null || b == null)
                {
                    return;
                }

                Vector3 aWorld = a.TransformPoint(a.rect.center);
                Vector3 bWorld = b.TransformPoint(b.rect.center);
                RectTransform canvasRect = canvas as RectTransform;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(null, aWorld), null, out Vector2 start);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, RectTransformUtility.WorldToScreenPoint(null, bWorld), null, out Vector2 end);

                GameObject line = new GameObject("UserConnection");
                line.transform.SetParent(canvas, false);
                Image image = line.AddComponent<Image>();
                image.color = new Color(0.75f, 0.32f, 0.54f, 1f);
                image.raycastTarget = true;
                line.AddComponent<LogicDiyConnectionLine>().Configure(canvas as RectTransform, a, b);
                line.transform.SetAsFirstSibling();
            }

            private static void PositionLine(RectTransform rect, Vector2 start, Vector2 end)
            {
                if (rect == null)
                {
                    return;
                }

                Vector2 middle = (start + end) * 0.5f;
                float length = Vector2.Distance(start, end);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = middle;
                rect.sizeDelta = new Vector2(length, 3f);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
            }
        }
    }
}
