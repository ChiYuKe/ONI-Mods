using KSerialization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StorageNetwork.Core;
using StorageNetwork.LogicDiy.Runtime;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkLogicDiy : KMonoBehaviour, ISim200ms
    {
        public static readonly HashedString PORT_ID = "StorageNetworkLogicDiyOutput";
        private const int StartupRefreshTicks = 600;

        public enum ChannelMode
        {
            SingleChannel = 0,
            FourChannel = 1
        }

        public enum SourceMode
        {
            Fixed = 0,
            MaterialCondition = 1
        }

        public enum ComparisonMode
        {
            GreaterOrEqual = 0,
            LessThan = 1
        }

        [Serialize]
        public int OutputModeValue;

        [Serialize]
        public int OutputSignalValue;

        [Serialize]
        public int SourceModeValue;

        [Serialize]
        public string ConditionItemKey;

        [Serialize]
        public float ConditionThresholdKg = 100f;

        [Serialize]
        public int ConditionComparisonValue;

        [Serialize]
        public int ConditionOutputChannel;

        [Serialize]
        public string RuntimeBlueprintJson;

        [Serialize]
        public string RuntimeLayoutJson;

        [MyCmpGet]
        private LogicPorts logicPorts = null;

        private int startupRefreshTicks;
        private bool forceInventorySnapshot;
        private readonly Dictionary<string, float> timerElapsedByNode = new Dictionary<string, float>();
        private readonly HashSet<string> timerPulseNodes = new HashSet<string>();
        private readonly Dictionary<string, int> cycleIndexByNode = new Dictionary<string, int>();
        private readonly Dictionary<string, float> runtimeEvalCache = new Dictionary<string, float>();
        private readonly Dictionary<string, float> runtimeStableOutputSnapshot = new Dictionary<string, float>();
        private readonly HashSet<string> runtimeEvalStack = new HashSet<string>();
        private readonly Dictionary<string, float> delayElapsedByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> latchStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> previousInputStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> previousMaterialAmountByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, float> counterValueByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, int> sequenceStepByNode = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> sequencePrevAdvanceByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> sequencePrevResetByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> hysteresisStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> toggleStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> togglePrevInputByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> pulseShaperRemainingByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, float> previousNumberValueByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, int> numberChangeFlagsByNode = new Dictionary<string, int>();
        private readonly HashSet<string> numberChangeUpdatedNodes = new HashSet<string>();
        private float runtimeEvalDt;
        private readonly LogicDiyBlueprintCodec blueprintCodec = new LogicDiyBlueprintCodec();
        private static readonly Dictionary<System.Type, PropertyInfo> switchLikeOutputPropertyByType = new Dictionary<System.Type, PropertyInfo>();
        private static readonly EventSystem.IntraObjectHandler<StorageNetworkLogicDiy> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkLogicDiy>((component, data) => component.OnCopySettings(data));

        public ChannelMode OutputMode
        {
            get => (ChannelMode)Mathf.Clamp(OutputModeValue, 0, 1);
            set => SetOutputMode(value);
        }

        public SourceMode OutputSourceMode
        {
            get => (SourceMode)Mathf.Clamp(SourceModeValue, 0, 1);
            set => SetSourceMode(value);
        }

        public ComparisonMode ConditionComparison
        {
            get => (ComparisonMode)Mathf.Clamp(ConditionComparisonValue, 0, 1);
            set => ConditionComparisonValue = Mathf.Clamp((int)value, 0, 1);
        }

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            logicPorts = logicPorts ?? GetComponent<LogicPorts>() ?? gameObject.AddOrGet<LogicPorts>();
            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyPersistence.TryLoad(this);
            ConditionThresholdKg = Mathf.Max(0f, ConditionThresholdKg);
            ConditionOutputChannel = Mathf.Clamp(ConditionOutputChannel, 0, 3);
            EnsureRuntimeBlueprintForLegacyState();
            ClampOutputValue();
            startupRefreshTicks = StartupRefreshTicks;
            EvaluateWithForcedSnapshot();
            ForceLogicNetworkRefresh();
        }

        protected override void OnCleanUp()
        {
            logicPorts?.SendSignal(PORT_ID, 0);
            base.OnCleanUp();
        }

        public void SetSignal(bool active)
        {
            SetSignalValue(active ? 1 : 0);
        }

        public void SetSignalValue(int value)
        {
            int previousValue = OutputSignalValue;
            OutputSignalValue = ClampOutputValue(value);
            if (previousValue != OutputSignalValue)
            {
                SendLogicSignal();
            }
        }

        public void SetOutputMode(ChannelMode mode)
        {
            int previousValue = OutputSignalValue;
            OutputModeValue = Mathf.Clamp((int)mode, 0, 1);
            ConditionOutputChannel = OutputMode == ChannelMode.FourChannel ? Mathf.Clamp(ConditionOutputChannel, 0, 3) : 0;
            ClampOutputValue();
            if (previousValue != OutputSignalValue)
            {
                SendLogicSignal();
            }
            else
            {
                SendLogicSignal();
            }
        }

        public void SetSourceMode(SourceMode mode)
        {
            SourceModeValue = Mathf.Clamp((int)mode, 0, 1);
            EvaluateConditionOutput();
        }

        public void SetConditionItem(string itemKey)
        {
            ConditionItemKey = itemKey ?? string.Empty;
            EvaluateConditionOutput();
        }

        public void SetConditionThreshold(float thresholdKg)
        {
            ConditionThresholdKg = Mathf.Max(0f, thresholdKg);
            EvaluateConditionOutput();
        }

        public void SetConditionComparison(ComparisonMode comparison)
        {
            ConditionComparison = comparison;
            EvaluateConditionOutput();
        }

        public void SetConditionOutputChannel(int channel)
        {
            ConditionOutputChannel = OutputMode == ChannelMode.FourChannel ? Mathf.Clamp(channel, 0, 3) : 0;
            EvaluateConditionOutput();
        }

        public void LogicTick()
        {
        }

        public void Sim200ms(float dt)
        {
            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyWebEditor.ApplyPending(this);
            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyWebEditor.RefreshCachedStateIfActive(this);
            runtimeEvalDt = Mathf.Max(0f, dt);
            UpdateRuntimeTimers(dt);
            runtimeEvalCache.Clear();
            numberChangeUpdatedNodes.Clear();
            BuildRuntimeStableOutputSnapshot();
            if (startupRefreshTicks > 0)
            {
                EvaluateWithForcedSnapshot();
            }
            else
            {
                EvaluateConditionOutput();
            }

            // The editor renders node values from this cache. Evaluate every visual output as
            // well as the graph's final output so counters and monitors on diagnostic branches
            // keep advancing even when they are not wired to system:output.
            EvaluateRuntimeDisplayNodes();
            ApplyDeferredCounterResets();

            if (startupRefreshTicks > 0)
            {
                if (!IsMaterialNetworkOfflineDuringStartup())
                {
                    startupRefreshTicks--;
                }

                ForceLogicNetworkRefresh();
            }
            else
            {
                ForceLogicNetworkRefresh();
            }

            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyWebEditor.RefreshRuntimeSignalsIfActive(this);
        }

        private void ForceLogicNetworkRefresh()
        {
            SendLogicSignal();
        }

        private void SendLogicSignal()
        {
            logicPorts?.SendSignal(PORT_ID, ClampOutputValue(OutputSignalValue));
        }

        private void EvaluateWithForcedSnapshot()
        {
            try
            {
                forceInventorySnapshot = true;
                EvaluateConditionOutput();
            }
            finally
            {
                forceInventorySnapshot = false;
            }
        }

        public void EvaluateConditionOutput()
        {
            EnsureRuntimeBlueprintForLegacyState();
            if (ShouldDeferMaterialEvaluation())
            {
                return;
            }

            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (TryEvaluateRuntimeOutput(blueprint, out int runtimeOutputValue))
            {
                SetSignalValue(runtimeOutputValue);
                return;
            }

            if (OutputSourceMode != SourceMode.MaterialCondition || string.IsNullOrEmpty(ConditionItemKey))
            {
                return;
            }

            float amountKg = GetRuntimeCompareInputKg();
            bool conditionMet = ConditionComparison == ComparisonMode.GreaterOrEqual
                ? amountKg >= ConditionThresholdKg
                : amountKg < ConditionThresholdKg;

            int newValue = BuildConditionOutputValue(conditionMet);
            SetSignalValue(newValue);
        }

        private bool ShouldDeferMaterialEvaluation()
        {
            if (startupRefreshTicks <= 0 || !UsesMaterialInput())
            {
                return false;
            }

            return IsMaterialNetworkOfflineDuringStartup();
        }

        private bool IsMaterialNetworkOfflineDuringStartup()
        {
            if (startupRefreshTicks <= 0 || !UsesMaterialInput())
            {
                return false;
            }

            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, force: true);
            return snapshot == null || !snapshot.NetworkOnline;
        }

        private void EnsureRuntimeBlueprintForLegacyState()
        {
            if (!string.IsNullOrEmpty(RuntimeBlueprintJson) ||
                OutputSourceMode != SourceMode.MaterialCondition ||
                string.IsNullOrEmpty(ConditionItemKey))
            {
                return;
            }

            string comparisonModule = ConditionComparison == ComparisonMode.LessThan ? "LessThan" : "GreaterThan";
            int outputPort = OutputMode == ChannelMode.FourChannel ? Mathf.Clamp(ConditionOutputChannel, 0, 3) : 0;
            RuntimeBlueprint blueprint = new RuntimeBlueprint
            {
                Nodes = new List<RuntimeBlueprintNode>
                {
                    new RuntimeBlueprintNode { Id = "system:material", Module = "MaterialCondition" },
                    new RuntimeBlueprintNode { Id = "system:compare", Module = "Variable" },
                    new RuntimeBlueprintNode { Id = "system:legacy_condition", Module = comparisonModule },
                    new RuntimeBlueprintNode { Id = "system:output", Module = "Output" }
                },
                Connections = new List<RuntimeBlueprintConnection>
                {
                    new RuntimeBlueprintConnection { FromNodeId = "system:material", FromPortIndex = 0, ToNodeId = "system:legacy_condition", ToPortIndex = 0 },
                    new RuntimeBlueprintConnection { FromNodeId = "system:compare", FromPortIndex = 0, ToNodeId = "system:legacy_condition", ToPortIndex = 1 },
                    new RuntimeBlueprintConnection { FromNodeId = "system:legacy_condition", FromPortIndex = 0, ToNodeId = "system:output", ToPortIndex = outputPort }
                }
            };

            RuntimeBlueprintJson = Newtonsoft.Json.JsonConvert.SerializeObject(blueprint);
        }

        private bool UsesMaterialInput()
        {
            if (OutputSourceMode == SourceMode.MaterialCondition)
            {
                return true;
            }

            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null)
            {
                return false;
            }

            foreach (RuntimeBlueprintNode node in blueprint.Nodes)
            {
                if (node != null && (node.Id == "system:material" ||
                    LogicDiyNodeRegistry.UsesMaterialInput(node.Module)))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryEvaluateRuntimeOutput(RuntimeBlueprint blueprint, out int outputValue)
        {
            outputValue = 0;
            if (blueprint == null)
            {
                return false;
            }

            Dictionary<string, RuntimeBlueprintNode> nodes = BuildRuntimeNodeMap(blueprint);
            if (UsesFourChannelRuntimeOutput(blueprint))
            {
                bool hasAnyInput = false;
                for (int portIndex = 0; portIndex < 4; portIndex++)
                {
                    RuntimeBlueprintConnection input = FindRuntimeInput(blueprint, "system:output", portIndex);
                    if (input == null)
                    {
                        continue;
                    }

                    hasAnyInput = true;
                    float portValue = EvaluateRuntimeNumber(blueprint, nodes, input.FromNodeId, input.FromPortIndex, 0);
                    if (IsRuntimeTrue(portValue))
                    {
                        outputValue |= 1 << portIndex;
                    }
                }

                if (!hasAnyInput)
                {
                    outputValue = 0;
                }

                return true;
            }

            RuntimeBlueprintConnection outputInput = FindRuntimeInput(blueprint, "system:output", 0);
            if (outputInput == null)
            {
                return true;
            }

            float value = EvaluateRuntimeNumber(blueprint, nodes, outputInput.FromNodeId, outputInput.FromPortIndex, 0);
            outputValue = IsRuntimeTrue(value) ? 1 : 0;
            outputValue = ClampOutputValue(outputValue);
            return true;
        }

        private void EvaluateRuntimeDisplayNodes()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null)
            {
                return;
            }

            Dictionary<string, RuntimeBlueprintNode> nodes = BuildRuntimeNodeMap(blueprint);
            foreach (RuntimeBlueprintNode node in blueprint.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id) || node.Module == "Output" ||
                    node.Module == "Group")
                {
                    continue;
                }

                int outputCount = node.Module == "Cycle4" || node.Module == "Split4" ? 4 :
                    node.Module == "NumberChanged" ? 3 :
                    node.Module == "Sequence" ? 2 : 1;
                for (int outputPort = 0; outputPort < outputCount; outputPort++)
                {
                    EvaluateRuntimeNumber(blueprint, nodes, node.Id, outputPort, 0);
                }
            }
        }

        // A Reset expression can legitimately feed back from a counter through a comparator:
        // Counter -> Equal -> Counter.Reset.  Resolve it after all node values for this tick
        // have been committed, so the comparator reads the current count rather than a value
        // captured while the counter was still being evaluated.
        private void ApplyDeferredCounterResets()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null)
            {
                return;
            }

            Dictionary<string, RuntimeBlueprintNode> nodes = BuildRuntimeNodeMap(blueprint);
            foreach (RuntimeBlueprintNode node in blueprint.Nodes)
            {
                if (node == null || node.Module != "Counter" || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                RuntimeBlueprintConnection resetInput = FindRuntimeInput(blueprint, node.Id, 1);
                if (resetInput == null)
                {
                    continue;
                }

                // The Reset source may have been evaluated before this counter published its
                // final value. Recompute that source against the committed counter cache.
                runtimeEvalCache.Remove(resetInput.FromNodeId + ":" + resetInput.FromPortIndex);
                float resetValue = EvaluateRuntimeNumber(blueprint, nodes, resetInput.FromNodeId, resetInput.FromPortIndex, 0);
                if (!IsRuntimeTrue(resetValue))
                {
                    continue;
                }

                counterValueByNode[node.Id] = 0f;
                runtimeEvalCache[node.Id + ":0"] = 0f;
            }
        }

        private bool UsesFourChannelRuntimeOutput(RuntimeBlueprint blueprint)
        {
            if (OutputMode == ChannelMode.FourChannel)
            {
                return true;
            }

            if (blueprint?.Connections == null)
            {
                return false;
            }

            foreach (RuntimeBlueprintConnection connection in blueprint.Connections)
            {
                if (connection != null && connection.ToNodeId == "system:output" && connection.ToPortIndex > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool UsesFourChannelRuntimeOutput()
        {
            return UsesFourChannelRuntimeOutput(TryGetRuntimeBlueprint());
        }

        private float GetRuntimeCompareInputKg()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint == null)
            {
                return GetNetworkItemAmountKg(ConditionItemKey);
            }

            RuntimeBlueprintConnection input = FindRuntimeInput(blueprint, "system:compare", 0);
            if (input == null)
            {
                return GetNetworkItemAmountKg(ConditionItemKey);
            }

            Dictionary<string, RuntimeBlueprintNode> nodes = BuildRuntimeNodeMap(blueprint);
            return EvaluateRuntimeNumber(blueprint, nodes, input.FromNodeId, input.FromPortIndex, 0);
        }

        private RuntimeBlueprint TryGetRuntimeBlueprint()
        {
            return blueprintCodec.Parse(RuntimeBlueprintJson);
        }

        private static Dictionary<string, RuntimeBlueprintNode> BuildRuntimeNodeMap(RuntimeBlueprint blueprint)
        {
            Dictionary<string, RuntimeBlueprintNode> nodes = new Dictionary<string, RuntimeBlueprintNode>();
            if (blueprint?.Nodes == null)
            {
                return nodes;
            }

            foreach (RuntimeBlueprintNode node in blueprint.Nodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.Id))
                {
                    nodes[node.Id] = node;
                }
            }

            return nodes;
        }

        private float EvaluateRuntimeNumber(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int outputPortIndex, int depth)
        {
            if (blueprint == null || nodes == null || string.IsNullOrEmpty(nodeId) || depth > 32)
            {
                return 0f;
            }

            if (nodeId == "system:material")
            {
                return GetNetworkItemAmountKg(ConditionItemKey);
            }

            if (nodeId == "system:fixed")
            {
                return OutputSignalValue;
            }

            if (!nodes.TryGetValue(nodeId, out RuntimeBlueprintNode node) || node == null)
            {
                return 0f;
            }

            string cacheKey = nodeId + ":" + outputPortIndex;
            string module = node.Module ?? string.Empty;
            if (runtimeEvalStack.Contains(cacheKey))
            {
                // Stateful nodes may publish an in-progress candidate for feedback paths
                // (for example: Counter -> Equal -> Counter.Reset).
                if (runtimeEvalCache.TryGetValue(cacheKey, out float inProgressValue))
                {
                    return inProgressValue;
                }
                return GetRuntimeStableOutput(nodeId, outputPortIndex, node, module);
            }

            if (runtimeEvalCache.TryGetValue(cacheKey, out float cached))
            {
                return cached;
            }

            runtimeEvalStack.Add(cacheKey);
            try
            {
            if (module == "Counter")
            {
                float counterResult = EvaluateCounterNode(blueprint, nodes, nodeId, depth + 1);
                runtimeEvalCache[cacheKey] = counterResult;
                return counterResult;
            }

            float a = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 0, depth + 1);
            float b = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 1, depth + 1);
            float result;

            switch (module)
            {
                case "Add":
                    result = EvaluateRuntimeInputValues(blueprint, nodes, nodeId, node.InputCount, depth + 1, 2)
                        .Aggregate(0f, (sum, value) => sum + value);
                    break;
                case "Subtract":
                    result = EvaluateRuntimeFold(blueprint, nodes, nodeId, node.InputCount, depth + 1, 2, (current, value) => current - value);
                    break;
                case "Multiply":
                    result = EvaluateRuntimeInputValues(blueprint, nodes, nodeId, node.InputCount, depth + 1, 2)
                        .Aggregate(1f, (product, value) => product * value);
                    break;
                case "Divide":
                    result = EvaluateRuntimeFold(blueprint, nodes, nodeId, node.InputCount, depth + 1, 2, (current, value) => Mathf.Abs(value) < 0.0001f ? 0f : current / value);
                    break;
                case "Negate":
                    result = -a;
                    break;
                case "Min":
                    result = Mathf.Min(a, b);
                    break;
                case "Max":
                    result = Mathf.Max(a, b);
                    break;
                case "Clamp":
                    float clampMax = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 2, depth + 1);
                    result = Mathf.Clamp(a, Mathf.Min(b, clampMax), Mathf.Max(b, clampMax));
                    break;
                case "Modulo":
                    result = Mathf.Abs(b) < 0.0001f ? 0f : a % b;
                    break;
                case "GreaterThan":
                    result = a > b ? 1f : 0f;
                    break;
                case "Equal":
                    result = Mathf.Approximately(a, b) ? 1f : 0f;
                    break;
                case "LessThan":
                    result = a < b ? 1f : 0f;
                    break;
                case "Range":
                    float c = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 2, depth + 1);
                    result = a >= Mathf.Min(b, c) && a <= Mathf.Max(b, c) ? 1f : 0f;
                    break;
                case "Variable":
                    result = ConditionThresholdKg;
                    break;
                case "Constant":
                    result = node.Value;
                    break;
                case "TestSignal":
                    result = node.Value > 0.5f ? 1f : 0f;
                    break;
                case "BoolTrue":
                    result = 1f;
                    break;
                case "BoolFalse":
                    result = 0f;
                    break;
                case "BoolAnd":
                    result = IsRuntimeTrue(a) && IsRuntimeTrue(b) ? 1f : 0f;
                    break;
                case "BoolNand":
                    result = IsRuntimeTrue(a) && IsRuntimeTrue(b) ? 0f : 1f;
                    break;
                case "BoolOr":
                    result = IsRuntimeTrue(a) || IsRuntimeTrue(b) ? 1f : 0f;
                    break;
                case "BoolNor":
                    result = IsRuntimeTrue(a) || IsRuntimeTrue(b) ? 0f : 1f;
                    break;
                case "BoolXor":
                    result = IsRuntimeTrue(a) != IsRuntimeTrue(b) ? 1f : 0f;
                    break;
                case "BoolNot":
                    result = IsRuntimeTrue(a) ? 0f : 1f;
                    break;
                case "Selector":
                    result = IsRuntimeTrue(a)
                        ? b
                        : EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 2, depth + 1);
                    break;
                case "Sequence":
                    result = EvaluateSequenceNode(nodeId, outputPortIndex, a, b);
                    break;
                case "Delay":
                    result = EvaluateDelayNode(nodeId, a, node.IntervalSeconds);
                    break;
                case "Latch":
                    result = EvaluateLatchNode(nodeId, a, b);
                    break;
                case "EdgePulse":
                    result = EvaluateEdgePulseNode(nodeId, a);
                    break;
                case "Hysteresis":
                    result = EvaluateHysteresisNode(nodeId, a, node);
                    break;
                case "Toggle":
                    result = EvaluateToggleNode(nodeId, a);
                    break;
                case "PulseShaper":
                    result = EvaluatePulseShaperNode(nodeId, a, node);
                    break;
                case "NumberChanged":
                    result = EvaluateNumberChangedNode(nodeId, a, outputPortIndex);
                    break;
                case "MapRange":
                    result = EvaluateMapRangeNode(nodeId, a, node);
                    break;
                case "Counter":
                    result = EvaluateCounterNode(blueprint, nodes, nodeId, depth + 1);
                    break;
                case "RandomChance":
                    result = UnityEngine.Random.value * 100f < Mathf.Clamp(node.Value, 0f, 100f) ? 1f : 0f;
                    break;
                case "TimerPulse":
                    result = timerPulseNodes.Contains(nodeId) ? 1f : 0f;
                    break;
                case "Cycle4":
                    result = cycleIndexByNode.TryGetValue(nodeId, out int activeIndex) && activeIndex == Mathf.Clamp(outputPortIndex, 0, 3) ? 1f : 0f;
                    break;
                case "MaterialCondition":
                    result = GetNetworkItemAmountKg(ConditionItemKey);
                    break;
                case "MaterialLow":
                    result = !string.IsNullOrEmpty(ConditionItemKey) && GetNetworkItemAmountKg(ConditionItemKey) < Mathf.Max(0f, node.Value) ? 1f : 0f;
                    break;
                case "MaterialHigh":
                    result = !string.IsNullOrEmpty(ConditionItemKey) && GetNetworkItemAmountKg(ConditionItemKey) >= Mathf.Max(0f, node.Value) ? 1f : 0f;
                    break;
                case "MaterialChanged":
                    result = EvaluateMaterialChangedNode(nodeId);
                    break;
                case "InventoryPercent":
                    result = GetNetworkFillPercent();
                    break;
                case "InventoryStored":
                    result = GetNetworkStoredKg();
                    break;
                case "InventoryRemaining":
                    result = GetNetworkRemainingKg();
                    break;
                case "InventoryCapacity":
                    result = GetNetworkCapacityKg();
                    break;
                case "PowerPercent":
                    result = GetNetworkPowerPercent();
                    break;
                case "PowerStored":
                    result = GetNetworkPowerStoredJoules();
                    break;
                case "PowerCapacity":
                    result = GetNetworkPowerCapacityJoules();
                    break;
                case "PowerRemaining":
                    result = GetNetworkPowerRemainingJoules();
                    break;
                case "BuildingStatus":
                    result = GetBuildingStatusSignal(node.SelectedBuildingInstanceId);
                    break;
                case "BuildingSignal":
                    result = GetBuildingOutputSignal(node.SelectedBuildingInstanceId);
                    break;
                case "Output":
                    result = a;
                    break;
                case "Split4":
                    result = EvaluateSplit4Node(outputPortIndex, a);
                    break;
                case "Merge4":
                    result = EvaluateMerge4Node(blueprint, nodes, nodeId, depth);
                    break;
                case "Select":
                    int portCount = node?.Value > 1f ? Mathf.FloorToInt(node.Value) : 6;
                    int selIndex = Mathf.Clamp(Mathf.FloorToInt(a), 0, portCount - 1);
                    result = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, selIndex + 1, depth + 1);
                    break;
                case "PixelScreen":
                    result = EvaluatePixelScreenNode(blueprint, nodes, nodeId, depth);
                    break;
                default:
                    Debug.LogWarning($"StorageNetwork LogicDiy: Unknown module '{module}' for node '{nodeId}'");
                    result = 0f;
                    break;
            }

            runtimeEvalCache[cacheKey] = result;
            return result;
            }
            finally
            {
                runtimeEvalStack.Remove(cacheKey);
            }
        }

        private float GetRuntimeStableOutput(string nodeId, int outputPortIndex, RuntimeBlueprintNode node, string module)
        {
            string cacheKey = nodeId + ":" + outputPortIndex;
            if (runtimeStableOutputSnapshot.TryGetValue(cacheKey, out float snapshotted))
            {
                return snapshotted;
            }

            switch (module)
            {
                case "Counter":
                    return counterValueByNode.TryGetValue(nodeId, out float count) ? count : 0f;
                case "Sequence":
                    int step = sequenceStepByNode.TryGetValue(nodeId, out int storedStep) ? storedStep : 0;
                    if (outputPortIndex == 1)
                    {
                        return step;
                    }

                    List<float> values = node?.Values;
                    if (values == null || values.Count == 0)
                    {
                        return 0f;
                    }

                    return Mathf.Max(0f, Mathf.Min(15f, Mathf.Floor(values[Mathf.Clamp(step, 0, values.Count - 1)])));
                case "Latch":
                    return latchStateByNode.TryGetValue(nodeId, out bool latch) && latch ? 1f : 0f;
                case "Toggle":
                    return toggleStateByNode.TryGetValue(nodeId, out bool toggle) && toggle ? 1f : 0f;
                case "Hysteresis":
                    return hysteresisStateByNode.TryGetValue(nodeId, out bool hysteresis) && hysteresis ? 1f : 0f;
                case "PulseShaper":
                    return pulseShaperRemainingByNode.TryGetValue(nodeId, out float remaining) && remaining > 0f ? 1f : 0f;
                default:
                    return 0f;
            }
        }

        private void BuildRuntimeStableOutputSnapshot()
        {
            runtimeStableOutputSnapshot.Clear();
            foreach (KeyValuePair<string, float> item in counterValueByNode)
            {
                runtimeStableOutputSnapshot[item.Key + ":0"] = item.Value;
            }

            foreach (KeyValuePair<string, bool> item in latchStateByNode)
            {
                runtimeStableOutputSnapshot[item.Key + ":0"] = item.Value ? 1f : 0f;
            }

            foreach (KeyValuePair<string, bool> item in toggleStateByNode)
            {
                runtimeStableOutputSnapshot[item.Key + ":0"] = item.Value ? 1f : 0f;
            }

            foreach (KeyValuePair<string, bool> item in hysteresisStateByNode)
            {
                runtimeStableOutputSnapshot[item.Key + ":0"] = item.Value ? 1f : 0f;
            }

            foreach (KeyValuePair<string, float> item in pulseShaperRemainingByNode)
            {
                runtimeStableOutputSnapshot[item.Key + ":0"] = item.Value > 0f ? 1f : 0f;
            }
        }

        private float EvaluateRuntimeInputNumber(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int portIndex, int depth)
        {
            RuntimeBlueprintConnection input = FindRuntimeInput(blueprint, nodeId, portIndex);
            if (input == null)
            {
                RuntimeBlueprintNode currentNode = nodes != null && nodes.TryGetValue(nodeId, out RuntimeBlueprintNode found) ? found : null;
                return currentNode?.InputValues != null && portIndex >= 0 && portIndex < currentNode.InputValues.Count ? currentNode.InputValues[portIndex] : 0f;
            }

            return EvaluateRuntimeNumber(blueprint, nodes, input.FromNodeId, input.FromPortIndex, depth);
        }

        private List<float> EvaluateRuntimeInputValues(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int inputCount, int depth, int minimumCount)
        {
            int count = Mathf.Clamp(inputCount > 0 ? inputCount : minimumCount, minimumCount, 10);
            List<float> values = new List<float>(count);
            for (int i = 0; i < count; i++)
            {
                RuntimeBlueprintConnection input = FindRuntimeInput(blueprint, nodeId, i);
                if (input == null)
                {
                    RuntimeBlueprintNode currentNode = nodes != null && nodes.TryGetValue(nodeId, out RuntimeBlueprintNode found) ? found : null;
                    values.Add(currentNode?.InputValues != null && i < currentNode.InputValues.Count ? currentNode.InputValues[i] : 0f);
                    continue;
                }

                values.Add(EvaluateRuntimeNumber(blueprint, nodes, input.FromNodeId, input.FromPortIndex, depth + 1));
            }

            return values;
        }

        private float EvaluateRuntimeFold(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int inputCount, int depth, int minimumCount, System.Func<float, float, float> fold)
        {
            List<float> values = EvaluateRuntimeInputValues(blueprint, nodes, nodeId, inputCount, depth + 1, minimumCount);
            float result = values.Count > 0 ? values[0] : 0f;
            for (int i = 1; i < values.Count; i++)
            {
                result = fold(result, values[i]);
            }

            return result;
        }

        private float EvaluateDelayNode(string nodeId, float inputValue, float intervalSeconds)
        {
            if (!IsRuntimeTrue(inputValue))
            {
                delayElapsedByNode[nodeId] = 0f;
                return 0f;
            }

            float interval = Mathf.Max(0.2f, intervalSeconds > 0f ? intervalSeconds : 5f);
            delayElapsedByNode.TryGetValue(nodeId, out float elapsed);
            elapsed += runtimeEvalDt;
            delayElapsedByNode[nodeId] = elapsed;
            return elapsed >= interval ? 1f : 0f;
        }

        private float EvaluateLatchNode(string nodeId, float setValue, float resetValue)
        {
            latchStateByNode.TryGetValue(nodeId, out bool latched);
            if (IsRuntimeTrue(resetValue))
            {
                latched = false;
            }
            else if (IsRuntimeTrue(setValue))
            {
                latched = true;
            }

            latchStateByNode[nodeId] = latched;
            return latched ? 1f : 0f;
        }

        private float EvaluateEdgePulseNode(string nodeId, float inputValue)
        {
            bool current = IsRuntimeTrue(inputValue);
            previousInputStateByNode.TryGetValue(nodeId, out bool previous);
            previousInputStateByNode[nodeId] = current;
            return current && !previous ? 1f : 0f;
        }

        private float EvaluateHysteresisNode(string nodeId, float inputValue, RuntimeBlueprintNode node)
        {
            float upper = node?.Upper ?? 500f;
            float lower = node?.Lower ?? 200f;
            if (upper < lower)
            {
                float temp = upper;
                upper = lower;
                lower = temp;
            }

            hysteresisStateByNode.TryGetValue(nodeId, out bool currentState);
            if (inputValue >= upper)
            {
                hysteresisStateByNode[nodeId] = true;
                return 1f;
            }

            if (inputValue <= lower)
            {
                hysteresisStateByNode[nodeId] = false;
                return 0f;
            }

            if (!hysteresisStateByNode.ContainsKey(nodeId))
            {
                hysteresisStateByNode[nodeId] = inputValue > lower;
            }

            return hysteresisStateByNode[nodeId] ? 1f : 0f;
        }

        private float EvaluateToggleNode(string nodeId, float inputValue)
        {
            bool current = IsRuntimeTrue(inputValue);
            togglePrevInputByNode.TryGetValue(nodeId, out bool previous);
            togglePrevInputByNode[nodeId] = current;
            if (current && !previous)
            {
                toggleStateByNode.TryGetValue(nodeId, out bool currentState);
                toggleStateByNode[nodeId] = !currentState;
            }

            toggleStateByNode.TryGetValue(nodeId, out bool state);
            return state ? 1f : 0f;
        }

        private float EvaluatePulseShaperNode(string nodeId, float inputValue, RuntimeBlueprintNode node)
        {
            float holdSeconds = Mathf.Max(0.1f, node?.IntervalSeconds ?? 1f);
            bool inputActive = IsRuntimeTrue(inputValue);
            pulseShaperRemainingByNode.TryGetValue(nodeId, out float remaining);
            if (inputActive)
            {
                pulseShaperRemainingByNode[nodeId] = holdSeconds;
                return 1f;
            }

            if (remaining > 0f)
            {
                remaining -= runtimeEvalDt;
                if (remaining > 0f)
                {
                    pulseShaperRemainingByNode[nodeId] = remaining;
                    return 1f;
                }
            }

            pulseShaperRemainingByNode[nodeId] = 0f;
            return 0f;
        }

        private float EvaluateNumberChangedNode(string nodeId, float inputValue, int outputPortIndex)
        {
            if (!numberChangeUpdatedNodes.Contains(nodeId))
            {
                previousNumberValueByNode.TryGetValue(nodeId, out float previousValue);
                int flags = 0;
                if (previousNumberValueByNode.ContainsKey(nodeId))
                {
                    if (inputValue > previousValue + 0.0001f) flags |= 1;
                    else if (inputValue < previousValue - 0.0001f) flags |= 2;
                    if (flags != 0) flags |= 4;
                }

                previousNumberValueByNode[nodeId] = inputValue;
                numberChangeFlagsByNode[nodeId] = flags;
                numberChangeUpdatedNodes.Add(nodeId);
            }

            return numberChangeFlagsByNode.TryGetValue(nodeId, out int flagsForOutput) &&
                   outputPortIndex >= 0 && outputPortIndex < 3 && (flagsForOutput & (1 << outputPortIndex)) != 0 ? 1f : 0f;
        }

        private float EvaluateMapRangeNode(string nodeId, float inputValue, RuntimeBlueprintNode node)
        {
            float inMin = node?.InMin ?? 0f;
            float inMax = node?.InMax ?? 100f;
            float outMin = node?.OutMin ?? 0f;
            float outMax = node?.OutMax ?? 100f;
            float range = inMax - inMin;
            if (Mathf.Abs(range) < 0.0001f)
            {
                return outMin;
            }

            float t = Mathf.Clamp01((inputValue - inMin) / range);
            return outMin + t * (outMax - outMin);
        }

        private float EvaluateCounterNode(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int depth)
        {
            float pulseValue = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 0, depth + 1);
            bool current = IsRuntimeTrue(pulseValue);
            previousInputStateByNode.TryGetValue(nodeId, out bool previous);
            counterValueByNode.TryGetValue(nodeId, out float count);
            float candidateCount = current && !previous ? count + 1f : count;
            // Let a Reset expression that feeds back through this counter compare against
            // the value this tick is about to publish, rather than the previous tick.
            runtimeEvalCache[nodeId + ":0"] = candidateCount;
            bool resetActive = IsRuntimeTrue(EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 1, depth + 1));

            previousInputStateByNode[nodeId] = current;
            if (resetActive)
            {
                counterValueByNode[nodeId] = 0f;
                return 0f;
            }

            counterValueByNode[nodeId] = candidateCount;
            return candidateCount;
        }

        private float EvaluateSequenceNode(string nodeId, int outputPortIndex, float advanceValue, float resetValue)
        {
            bool advanceActive = IsRuntimeTrue(advanceValue);
            sequencePrevAdvanceByNode.TryGetValue(nodeId, out bool prevAdvance);
            bool advanceEdge = advanceActive && !prevAdvance;
            sequencePrevAdvanceByNode[nodeId] = advanceActive;

            bool resetActive = IsRuntimeTrue(resetValue);
            sequencePrevResetByNode.TryGetValue(nodeId, out bool prevReset);
            bool resetEdge = resetActive && !prevReset;
            sequencePrevResetByNode[nodeId] = resetActive;

            if (!sequenceStepByNode.TryGetValue(nodeId, out int step))
            {
                step = 0;
            }

            if (resetEdge)
            {
                step = 0;
            }
            else if (advanceEdge)
            {
                step++;
            }

            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            Dictionary<string, RuntimeBlueprintNode> nodes = BuildRuntimeNodeMap(blueprint);
            int valuesLength = 1;
            if (nodes.TryGetValue(nodeId, out RuntimeBlueprintNode node) && node?.Values != null && node.Values.Count > 0)
            {
                valuesLength = node.Values.Count;
            }

            step = step % Mathf.Max(1, valuesLength);
            sequenceStepByNode[nodeId] = step;

            if (outputPortIndex == 1)
            {
                return step;
            }

            float stepValue = 0f;
            if (nodes.TryGetValue(nodeId, out RuntimeBlueprintNode valueNode) && valueNode?.Values != null && valueNode.Values.Count > 0)
            {
                int clampedStep = Mathf.Clamp(step, 0, valueNode.Values.Count - 1);
                stepValue = Mathf.Clamp(valueNode.Values[clampedStep], 0f, 15f);
            }

            return stepValue;
        }

        private static float EvaluateSplit4Node(int outputPortIndex, float inputValue)
        {
            int rawValue = Mathf.Clamp(Mathf.FloorToInt(inputValue), 0, 15);
            int bit = outputPortIndex switch
            {
                0 => rawValue & 1,
                1 => (rawValue >> 1) & 1,
                2 => (rawValue >> 2) & 1,
                3 => (rawValue >> 3) & 1,
                _ => 0
            };
            return bit;
        }

        private float EvaluateMerge4Node(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int depth)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                float inputVal = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, i, depth + 1);
                if (IsRuntimeTrue(inputVal))
                {
                    value |= 1 << i;
                }
            }
            return value;
        }

        private float EvaluatePixelScreenNode(RuntimeBlueprint blueprint, Dictionary<string, RuntimeBlueprintNode> nodes, string nodeId, int depth)
        {
            float a = EvaluateRuntimeInputNumber(blueprint, nodes, nodeId, 0, depth + 1);
            return Mathf.Clamp(Mathf.FloorToInt(a), 0, 15);
        }

        private float EvaluateMaterialChangedNode(string nodeId)
        {
            if (string.IsNullOrEmpty(ConditionItemKey))
            {
                previousMaterialAmountByNode.Remove(nodeId);
                return 0f;
            }

            float amount = GetNetworkItemAmountKg(ConditionItemKey);
            bool hadPrevious = previousMaterialAmountByNode.TryGetValue(nodeId, out float previous);
            previousMaterialAmountByNode[nodeId] = amount;
            return hadPrevious && !Mathf.Approximately(previous, amount) ? 1f : 0f;
        }

        private float GetNetworkFillPercent()
        {
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, force: forceInventorySnapshot);
            if (snapshot == null || !snapshot.NetworkOnline || snapshot.TotalCapacityKg <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(snapshot.TotalStoredKg / snapshot.TotalCapacityKg) * 100f;
        }

        private float GetNetworkStoredKg()
        {
            StorageSceneSnapshot snapshot = GetCurrentStorageSnapshot();
            return snapshot == null || !snapshot.NetworkOnline ? 0f : Mathf.Max(0f, snapshot.TotalStoredKg);
        }

        private float GetNetworkRemainingKg()
        {
            StorageSceneSnapshot snapshot = GetCurrentStorageSnapshot();
            return snapshot == null || !snapshot.NetworkOnline ? 0f : Mathf.Max(0f, snapshot.TotalCapacityKg - snapshot.TotalStoredKg);
        }

        private float GetNetworkCapacityKg()
        {
            StorageSceneSnapshot snapshot = GetCurrentStorageSnapshot();
            return snapshot == null || !snapshot.NetworkOnline ? 0f : Mathf.Max(0f, snapshot.TotalCapacityKg);
        }

        private StorageSceneSnapshot GetCurrentStorageSnapshot()
        {
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            return StorageSceneCollector.CollectForWorld(worldId, force: forceInventorySnapshot);
        }

        private float GetNetworkPowerPercent()
        {
            StorageNetworkPowerSnapshot snapshot = GetCurrentPowerSnapshot();
            return !snapshot.NetworkOnline || snapshot.CapacityJoules <= 0f
                ? 0f
                : Mathf.Clamp01(snapshot.StoredJoules / snapshot.CapacityJoules) * 100f;
        }

        private float GetNetworkPowerStoredJoules()
        {
            StorageNetworkPowerSnapshot snapshot = GetCurrentPowerSnapshot();
            return snapshot.NetworkOnline ? Mathf.Max(0f, snapshot.StoredJoules) : 0f;
        }

        private float GetNetworkPowerCapacityJoules()
        {
            StorageNetworkPowerSnapshot snapshot = GetCurrentPowerSnapshot();
            return snapshot.NetworkOnline ? Mathf.Max(0f, snapshot.CapacityJoules) : 0f;
        }

        private float GetNetworkPowerRemainingJoules()
        {
            StorageNetworkPowerSnapshot snapshot = GetCurrentPowerSnapshot();
            return snapshot.NetworkOnline ? Mathf.Max(0f, snapshot.AvailableCapacityJoules) : 0f;
        }

        private StorageNetworkPowerSnapshot GetCurrentPowerSnapshot()
        {
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            return StorageNetworkPowerService.GetAutomationSnapshot(worldId);
        }

        private float GetBuildingStatusSignal(int selectedBuildingInstanceId)
        {
            if (selectedBuildingInstanceId == KPrefabID.InvalidInstanceID || selectedBuildingInstanceId <= 0)
            {
                return 0f;
            }

            if (!StorageNetworkBuildingRegistry.TryGetBuilding(selectedBuildingInstanceId, out GameObject target))
            {
                return 0f;
            }

            Operational operational = target.GetComponent<Operational>();
            return operational == null || operational.IsOperational ? 1f : 0f;
        }

        private float GetBuildingOutputSignal(int selectedBuildingInstanceId)
        {
            if (selectedBuildingInstanceId == KPrefabID.InvalidInstanceID || selectedBuildingInstanceId <= 0)
            {
                return 0f;
            }

            if (!StorageNetworkBuildingRegistry.TryGetLogicOutputBuilding(selectedBuildingInstanceId, out GameObject target) ||
                target == gameObject)
            {
                return 0f;
            }

            return ReadBuildingOutputSignal(target);
        }

        private static int ReadBuildingOutputSignal(GameObject target)
        {
            LogicPorts ports = target != null ? target.GetComponent<LogicPorts>() : null;
            if (ports?.outputPortInfo == null || ports.outputPortInfo.Length == 0)
            {
                return TryReadSwitchLikeOutput(target, out int switchValue) ? switchValue : 0;
            }

            if (ports.outputPortInfo.Length == 1)
            {
                int value = Mathf.Max(0, ports.GetOutputValue(ports.outputPortInfo[0].id));
                return value > 0 || !TryReadSwitchLikeOutput(target, out int switchValue) ? value : switchValue;
            }

            int signal = 0;
            for (int index = 0; index < ports.outputPortInfo.Length && index < 4; index++)
            {
                int value = ports.GetOutputValue(ports.outputPortInfo[index].id);
                if (value > 1)
                {
                    signal |= Mathf.Clamp(value, 0, 15);
                }
                else if (value > 0)
                {
                    signal |= 1 << index;
                }
            }

            return signal > 0 || !TryReadSwitchLikeOutput(target, out int fallbackValue)
                ? Mathf.Clamp(signal, 0, 15)
                : fallbackValue;
        }

        private static bool TryReadSwitchLikeOutput(GameObject target, out int value)
        {
            value = 0;
            if (target == null)
            {
                return false;
            }

            Component[] components = target.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                System.Type componentType = component.GetType();
                if (!switchLikeOutputPropertyByType.TryGetValue(componentType, out PropertyInfo property))
                {
                    property = componentType.GetProperty("IsSwitchedOn", BindingFlags.Instance | BindingFlags.Public);
                    if (property != null && property.PropertyType != typeof(bool))
                    {
                        property = null;
                    }

                    switchLikeOutputPropertyByType[componentType] = property;
                }

                if (property == null || property.PropertyType != typeof(bool))
                {
                    continue;
                }

                try
                {
                    value = (bool)property.GetValue(component, null) ? 1 : 0;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private void UpdateRuntimeTimers(float dt)
        {
            timerPulseNodes.Clear();

            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null)
            {
                timerElapsedByNode.Clear();
                cycleIndexByNode.Clear();
                return;
            }

            HashSet<string> activeTimerIds = new HashSet<string>();
            HashSet<string> activeCycleIds = new HashSet<string>();
            foreach (RuntimeBlueprintNode node in blueprint.Nodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id))
                {
                    continue;
                }

                float interval = Mathf.Max(0.2f, node.IntervalSeconds > 0f ? node.IntervalSeconds : 5f);
                if (node.Module == "TimerPulse")
                {
                    activeTimerIds.Add(node.Id);
                    timerElapsedByNode.TryGetValue(node.Id, out float elapsed);
                    elapsed += Mathf.Max(0f, dt);

                    if (elapsed >= interval)
                    {
                        timerPulseNodes.Add(node.Id);
                        elapsed %= interval;
                    }

                    timerElapsedByNode[node.Id] = elapsed;
                    continue;
                }

                if (node.Module == "Cycle4")
                {
                    activeCycleIds.Add(node.Id);
                    timerElapsedByNode.TryGetValue(node.Id, out float elapsed);
                    elapsed += Mathf.Max(0f, dt);

                    if (!cycleIndexByNode.ContainsKey(node.Id))
                    {
                        cycleIndexByNode[node.Id] = 0;
                    }

                    while (elapsed >= interval)
                    {
                        cycleIndexByNode[node.Id] = (cycleIndexByNode[node.Id] + 1) % 4;
                        elapsed -= interval;
                    }

                    timerElapsedByNode[node.Id] = elapsed;
                }
            }

            List<string> staleIds = new List<string>();
            foreach (string id in timerElapsedByNode.Keys)
            {
                if (!activeTimerIds.Contains(id) && !activeCycleIds.Contains(id))
                {
                    staleIds.Add(id);
                }
            }

            foreach (string id in staleIds)
            {
                timerElapsedByNode.Remove(id);
                cycleIndexByNode.Remove(id);
            }
        }

        private static RuntimeBlueprintConnection FindRuntimeInput(RuntimeBlueprint blueprint, string nodeId, int portIndex)
        {
            if (blueprint?.Connections == null || string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            // Old layouts may contain more than one wire targeting the same input port.
            // The editor treats the most recently created wire as the active one, so resolve
            // from the end to match what the canvas displays.
            for (int index = blueprint.Connections.Count - 1; index >= 0; index--)
            {
                RuntimeBlueprintConnection connection = blueprint.Connections[index];
                if (connection != null && connection.ToNodeId == nodeId && connection.ToPortIndex == portIndex)
                {
                    return connection;
                }
            }

            return null;
        }

        private static bool IsRuntimeTrue(float value)
        {
            return Mathf.Abs(value) > 0.0001f;
        }

        private int BuildConditionOutputValue(bool conditionMet)
        {
            if (!conditionMet)
            {
                return 0;
            }

            if (OutputMode == ChannelMode.FourChannel)
            {
                return 1 << Mathf.Clamp(ConditionOutputChannel, 0, 3);
            }

            return 1;
        }

        public float GetConditionAmountKg()
        {
            return string.IsNullOrEmpty(ConditionItemKey) ? 0f : GetNetworkItemAmountKg(ConditionItemKey);
        }

        private float GetNetworkItemAmountKg(string itemKey)
        {
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, force: forceInventorySnapshot);
            if (snapshot?.Storages == null || !snapshot.NetworkOnline)
            {
                return 0f;
            }

            float amount = 0f;
            foreach (StorageInfo info in snapshot.Storages)
            {
                if (info?.StoredItems == null)
                {
                    continue;
                }

                foreach (GameObject item in info.StoredItems)
                {
                    if (item != null && StorageItemUtility.GetStoredItemKey(item) == itemKey)
                    {
                        amount += StorageItemUtility.GetMass(item);
                    }
                }
            }

            return amount;
        }

        private void ClampOutputValue()
        {
            OutputSignalValue = ClampOutputValue(OutputSignalValue);
        }

        private int ClampOutputValue(int value)
        {
            return UsesFourChannelRuntimeOutput()
                ? Mathf.Clamp(value, 0, 15)
                : Mathf.Clamp(value, 0, 1);
        }

        internal void ApplyWebEditorState(string runtimeBlueprintJson, int outputModeValue, int sourceModeValue, float thresholdKg, string conditionItemKey, string runtimeLayoutJson)
        {
            ApplyPersistedWebEditorState(runtimeBlueprintJson, outputModeValue, sourceModeValue, thresholdKg, conditionItemKey, runtimeLayoutJson);
            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyPersistence.Save(this);
        }

        internal void ResetRuntimeStateForEditor()
        {
            ClearRuntimeNodeState();
            latchStateByNode.Clear();
            counterValueByNode.Clear();
            sequenceStepByNode.Clear();
            hysteresisStateByNode.Clear();
            toggleStateByNode.Clear();
            OutputSignalValue = 0;
            SendLogicSignal();
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            StorageNetworkLogicDiy source = sourceObject != null
                ? sourceObject.GetComponent<StorageNetworkLogicDiy>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyWebEditor.ApplyPending(source);
            ApplyWebEditorState(
                source.RuntimeBlueprintJson,
                source.OutputModeValue,
                source.SourceModeValue,
                source.ConditionThresholdKg,
                source.ConditionItemKey,
                source.RuntimeLayoutJson);
        }

        internal void ApplyPersistedWebEditorState(string runtimeBlueprintJson, int outputModeValue, int sourceModeValue, float thresholdKg, string conditionItemKey, string runtimeLayoutJson)
        {
            RuntimeBlueprintJson = runtimeBlueprintJson ?? string.Empty;
            blueprintCodec.Invalidate();
            RuntimeLayoutJson = runtimeLayoutJson ?? string.Empty;
            ClearRuntimeNodeState();
            OutputModeValue = Mathf.Clamp(outputModeValue, 0, 1);
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (UsesFourChannelRuntimeOutput(blueprint))
            {
                OutputModeValue = (int)ChannelMode.FourChannel;
            }
            SourceModeValue = Mathf.Clamp(sourceModeValue, 0, 1);
            ConditionThresholdKg = Mathf.Max(0f, thresholdKg);
            ConditionItemKey = conditionItemKey ?? ConditionItemKey ?? string.Empty;
            ConditionOutputChannel = OutputMode == ChannelMode.FourChannel ? Mathf.Clamp(ConditionOutputChannel, 0, 3) : 0;
            EvaluateWithForcedSnapshot();
            ForceLogicNetworkRefresh();
        }

        internal float GetSelectedMaterialAmountKgForWebEditor()
        {
            return string.IsNullOrEmpty(ConditionItemKey) ? 0f : GetNetworkItemAmountKg(ConditionItemKey);
        }

        internal Dictionary<string, float> GetRuntimeEvalSnapshot()
        {
            return new Dictionary<string, float>(runtimeEvalCache);
        }

        internal WebEditorNetworkMetrics GetWebEditorNetworkMetrics()
        {
            StorageSceneSnapshot storage = GetCurrentStorageSnapshot();
            StorageNetworkPowerSnapshot power = GetCurrentPowerSnapshot();
            return new WebEditorNetworkMetrics
            {
                TotalStoredKg = storage == null || !storage.NetworkOnline ? 0f : storage.TotalStoredKg,
                TotalCapacityKg = storage == null || !storage.NetworkOnline ? 0f : storage.TotalCapacityKg,
                PowerStoredJoules = power.NetworkOnline ? power.StoredJoules : 0f,
                PowerCapacityJoules = power.NetworkOnline ? power.CapacityJoules : 0f,
                PowerRemainingJoules = power.NetworkOnline ? power.AvailableCapacityJoules : 0f,
                PowerJoulesLostPerCycle = power.NetworkOnline ? power.JoulesLostPerCycle : 0f
            };
        }

        internal List<WebEditorMaterialOption> GetWebEditorMaterialOptions()
        {
            Dictionary<string, WebEditorMaterialAccumulator> totals = new Dictionary<string, WebEditorMaterialAccumulator>();
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId, force: forceInventorySnapshot);
            if (snapshot?.Storages == null || !snapshot.NetworkOnline)
            {
                return new List<WebEditorMaterialOption>();
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

                    float mass = StorageItemUtility.GetMass(item);
                    if (!totals.TryGetValue(key, out WebEditorMaterialAccumulator total))
                    {
                        total = new WebEditorMaterialAccumulator
                        {
                            Key = key,
                            Name = StripWebEditorRichText(StorageNetworkStorageDisplay.GetStoredItemName(item))
                        };
                        totals.Add(key, total);
                    }

                    total.MassKg += mass;
                }
            }

            List<WebEditorMaterialOption> options = new List<WebEditorMaterialOption>();
            foreach (WebEditorMaterialAccumulator total in totals.Values)
            {
                options.Add(new WebEditorMaterialOption
                {
                    Key = total.Key,
                    Name = total.Name,
                    MassKg = total.MassKg,
                    Selected = total.Key == ConditionItemKey
                });
            }

            options.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.CurrentCulture));
            return options;
        }

        internal List<WebEditorBuildingOption> GetWebEditorBuildingOptions()
        {
            List<WebEditorBuildingOption> options = new List<WebEditorBuildingOption>();
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            List<GameObject> buildings = StorageNetworkBuildingRegistry.GetBuildingsForWorld(worldId);
            if (buildings.Count == 0)
            {
                StorageNetworkBuildingRegistry.RebuildFromScene();
                buildings = StorageNetworkBuildingRegistry.GetBuildingsForWorld(worldId);
            }

            foreach (GameObject target in buildings)
            {
                if (target == null)
                {
                    continue;
                }

                KPrefabID prefabId = target.GetComponent<KPrefabID>();
                if (prefabId == null || prefabId.InstanceID == KPrefabID.InvalidInstanceID)
                {
                    continue;
                }

                bool hasLogicOutput = StorageNetworkBuildingRegistry.IsLogicOutputBuilding(target);
                if (!IsStorageNetworkModBuilding(target, prefabId) && !hasLogicOutput)
                {
                    continue;
                }

                Operational operational = target.GetComponent<Operational>();
                options.Add(new WebEditorBuildingOption
                {
                    InstanceId = prefabId.InstanceID,
                    Name = StripWebEditorRichText(target.GetProperName()),
                    Operational = operational == null || operational.IsOperational,
                    HasLogicOutput = hasLogicOutput,
                    SignalValue = hasLogicOutput ? ReadBuildingOutputSignal(target) : 0
                });
            }

            options.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.CurrentCulture));
            return options;
        }

        private static bool IsStorageNetworkModBuilding(GameObject target, KPrefabID prefabId)
        {
            if (target == null || prefabId == null)
            {
                return false;
            }

            return target.GetComponent<StorageNetworkSceneMember>() != null ||
                   target.GetComponent<StorageNetworkEnrollment>() != null ||
                   target.GetComponent<StorageNetworkLogicDiy>() != null ||
                   prefabId.HasTag(StorageSceneTags.ModStorage) ||
                   prefabId.HasTag(StorageSceneTags.CategoryModStorage);
        }

        private void ClearRuntimeNodeState()
        {
            // Eval caches — always safe to clear (rebuilt each tick).
            runtimeEvalCache.Clear();
            runtimeStableOutputSnapshot.Clear();

            // Transient timing state — rebuilt each tick.
            timerElapsedByNode.Clear();
            timerPulseNodes.Clear();
            cycleIndexByNode.Clear();

            // Edge-detection & previous-value tracking — reset to avoid stale edges.
            previousInputStateByNode.Clear();
            previousMaterialAmountByNode.Clear();
            togglePrevInputByNode.Clear();
            sequencePrevAdvanceByNode.Clear();
            sequencePrevResetByNode.Clear();
            previousNumberValueByNode.Clear();
            numberChangeFlagsByNode.Clear();
            numberChangeUpdatedNodes.Clear();

            // Transient duration-based state — reset to avoid stale durations.
            delayElapsedByNode.Clear();
            pulseShaperRemainingByNode.Clear();

            // Stateful memory is intentionally preserved here. Loading/saving layout
            // and other non-topology edits must not reset a running circuit. Explicit
            // topology/parameter edits call ResetRuntimeStateForEditor instead.
        }

        internal sealed class WebEditorMaterialOption
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public float MassKg { get; set; }
            public bool Selected { get; set; }
        }

        internal sealed class WebEditorBuildingOption
        {
            public int InstanceId { get; set; }
            public string Name { get; set; }
            public bool Operational { get; set; }
            public bool HasLogicOutput { get; set; }
            public int SignalValue { get; set; }
        }

        internal sealed class WebEditorNetworkMetrics
        {
            public float TotalStoredKg { get; set; }
            public float TotalCapacityKg { get; set; }
            public float PowerStoredJoules { get; set; }
            public float PowerCapacityJoules { get; set; }
            public float PowerRemainingJoules { get; set; }
            public float PowerJoulesLostPerCycle { get; set; }
        }

        private sealed class WebEditorMaterialAccumulator
        {
            public string Key;
            public string Name;
            public float MassKg;
        }

        private static string StripWebEditorRichText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string text = value;
            int guard = 0;
            while (guard++ < 12)
            {
                int open = text.IndexOf("<link=", System.StringComparison.OrdinalIgnoreCase);
                if (open < 0)
                {
                    break;
                }

                int close = text.IndexOf('>', open);
                if (close < 0)
                {
                    break;
                }

                text = text.Remove(open, close - open + 1);
            }

            return text.Replace("</link>", string.Empty);
        }

        public sealed class RuntimeBlueprint
        {
            public List<RuntimeBlueprintNode> Nodes { get; set; } = new List<RuntimeBlueprintNode>();

            public List<RuntimeBlueprintConnection> Connections { get; set; } = new List<RuntimeBlueprintConnection>();
        }

        public sealed class RuntimeBlueprintNode
        {
            public string Id { get; set; }

            public string Module { get; set; }

            public float IntervalSeconds { get; set; }

            public float Value { get; set; }

            public int InputCount { get; set; }

            public List<float> InputValues { get; set; }

            public int SelectedBuildingInstanceId { get; set; }

            public List<float> Values { get; set; }

            public float Upper { get; set; }

            public float Lower { get; set; }

            public float InMin { get; set; }

            public float InMax { get; set; }

            public float OutMin { get; set; }

            public float OutMax { get; set; }
        }

        public sealed class RuntimeBlueprintConnection
        {
            public string FromNodeId { get; set; }

            public int FromPortIndex { get; set; }

            public string ToNodeId { get; set; }

            public int ToPortIndex { get; set; }
        }
    }
}
