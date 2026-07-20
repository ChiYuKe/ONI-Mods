using KSerialization;
using System.Collections.Generic;
using System.Linq;
using LogicNetwork.Runtime;
using UnityEngine;

namespace LogicNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class LogicNetworkEmitter : KMonoBehaviour, ISim200ms
    {
        public static readonly HashedString PORT_ID = "LogicNetworkEmitterOutput";
        public enum ChannelMode
        {
            SingleChannel = 0,
            FourChannel = 1
        }

        [Serialize]
        public int OutputModeValue;

        [Serialize]
        public int OutputSignalValue;

        [Serialize]
        public string RuntimeBlueprintJson;

        [Serialize]
        public string RuntimeLayoutJson;

        [MyCmpGet]
        private LogicPorts logicPorts = null;

        private readonly Dictionary<string, float> timerElapsedByNode = new Dictionary<string, float>();
        private readonly HashSet<string> timerPulseNodes = new HashSet<string>();
        private readonly Dictionary<string, int> cycleIndexByNode = new Dictionary<string, int>();
        private readonly Dictionary<string, float> runtimeEvalCache = new Dictionary<string, float>();
        private readonly Dictionary<string, float> runtimeStableOutputSnapshot = new Dictionary<string, float>();
        private readonly HashSet<string> runtimeEvalStack = new HashSet<string>();
        private readonly Dictionary<string, float> delayElapsedByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> latchStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> previousInputStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> counterValueByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, int> sequenceStepByNode = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> sequencePrevAdvanceByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> sequencePrevResetByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> musicStepByNode = new Dictionary<string, int>();
        private readonly Dictionary<string, float> musicStepStartedAtByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, bool> musicPrevResetByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> hysteresisStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> toggleStateByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> togglePrevInputByNode = new Dictionary<string, bool>();
        private readonly Dictionary<string, float> pulseShaperRemainingByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, float> previousNumberValueByNode = new Dictionary<string, float>();
        private readonly Dictionary<string, int> numberChangeFlagsByNode = new Dictionary<string, int>();
        private readonly HashSet<string> numberChangeUpdatedNodes = new HashSet<string>();
        private float runtimeEvalDt;
        private readonly LogicNetworkBlueprintCodec blueprintCodec = new LogicNetworkBlueprintCodec();
        private static readonly EventSystem.IntraObjectHandler<LogicNetworkEmitter> OnCopySettingsDelegate =
            new EventSystem.IntraObjectHandler<LogicNetworkEmitter>((component, data) => component.OnCopySettings(data));

        public ChannelMode OutputMode
        {
            get => (ChannelMode)Mathf.Clamp(OutputModeValue, 0, 1);
            set => SetOutputMode(value);
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
            LogicNetwork.UI.WebEditor.LogicNetworkPersistence.TryLoad(this);
            ClampOutputValue();
            EvaluateConditionOutput();
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

        public void LogicTick()
        {
        }

        public void Sim200ms(float dt)
        {
            LogicNetwork.UI.WebEditor.LogicNetworkWebEditor.ApplyPending(this);
            LogicNetwork.UI.WebEditor.LogicNetworkWebEditor.RefreshCachedStateIfActive(this);
            runtimeEvalDt = Mathf.Max(0f, dt);
            UpdateRuntimeTimers(dt);
            runtimeEvalCache.Clear();
            numberChangeUpdatedNodes.Clear();
            BuildRuntimeStableOutputSnapshot();
            EvaluateConditionOutput();

            // The editor renders node values from this cache. Evaluate every visual output as
            // well as the graph's final output so counters and monitors on diagnostic branches
            // keep advancing even when they are not wired to system:output.
            EvaluateRuntimeDisplayNodes();
            ApplyDeferredCounterResets();

            ForceLogicNetworkRefresh();

            LogicNetwork.UI.WebEditor.LogicNetworkWebEditor.RefreshRuntimeSignalsIfActive(this);
        }

        private void ForceLogicNetworkRefresh()
        {
            SendLogicSignal();
        }

        private void SendLogicSignal()
        {
            logicPorts?.SendSignal(PORT_ID, ClampOutputValue(OutputSignalValue));
        }

        public void EvaluateConditionOutput()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (TryEvaluateRuntimeOutput(blueprint, out int runtimeOutputValue))
            {
                SetSignalValue(runtimeOutputValue);
            }
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
                case "MusicSequencer":
                    result = EvaluateMusicSequencerNode(nodeId, a, b, node);
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
                    Debug.LogWarning($"LogicNetwork: Unknown module '{module}' for node '{nodeId}'");
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

        internal void ApplyWebEditorState(string runtimeBlueprintJson, int outputModeValue, string runtimeLayoutJson)
        {
            ApplyPersistedWebEditorState(runtimeBlueprintJson, outputModeValue, runtimeLayoutJson);
            LogicNetwork.UI.WebEditor.LogicNetworkPersistence.Save(this);
        }

        private float EvaluateMusicSequencerNode(string nodeId, float playValue, float resetValue, RuntimeBlueprintNode node)
        {
            List<float> notes = node?.Values;
            if (notes == null || notes.Count == 0) return 0f;
            if (!IsRuntimeTrue(playValue))
            {
                musicStepStartedAtByNode[nodeId] = Time.time;
                return 0f;
            }
            bool reset = IsRuntimeTrue(resetValue);
            musicPrevResetByNode.TryGetValue(nodeId, out bool previousReset);
            bool resetEdge = reset && !previousReset;
            musicPrevResetByNode[nodeId] = reset;
            if (!musicStepByNode.TryGetValue(nodeId, out int step) || resetEdge)
            {
                step = 0;
                musicStepByNode[nodeId] = 0;
                musicStepStartedAtByNode[nodeId] = Time.time;
            }
            if (!musicStepStartedAtByNode.TryGetValue(nodeId, out float startedAt))
            {
                startedAt = Time.time;
                musicStepStartedAtByNode[nodeId] = startedAt;
            }
            float bpm = Mathf.Clamp(node.Value, 20f, 400f);
            float beats = node.Durations != null && step < node.Durations.Count ? Mathf.Max(0.125f, node.Durations[step]) : 1f;
            float totalSeconds = Mathf.Max(0.4f, beats * 60f / bpm);
            float elapsed = Mathf.Max(0f, Time.time - startedAt);
            if (elapsed >= totalSeconds)
            {
                step++;
                if (step >= notes.Count)
                {
                    if (!node.Loop) return 0f;
                    step = 0;
                }
                musicStepByNode[nodeId] = step;
                musicStepStartedAtByNode[nodeId] = Time.time;
                elapsed = 0f;
                beats = node.Durations != null && step < node.Durations.Count ? Mathf.Max(0.125f, node.Durations[step]) : 1f;
                totalSeconds = Mathf.Max(0.4f, beats * 60f / bpm);
            }
            int note = Mathf.Clamp(Mathf.RoundToInt(notes[step]), 0, 12);
            float gap = Mathf.Clamp(node.GapSeconds, 0.2f, Mathf.Max(0.2f, totalSeconds - 0.2f));
            return note > 0 && elapsed < Mathf.Max(0f, totalSeconds - gap) ? note : 0f;
        }

        internal void ResetRuntimeStateForEditor()
        {
            ClearRuntimeNodeState();
            latchStateByNode.Clear();
            counterValueByNode.Clear();
            sequenceStepByNode.Clear();
            musicStepByNode.Clear();
            musicStepStartedAtByNode.Clear();
            musicPrevResetByNode.Clear();
            hysteresisStateByNode.Clear();
            toggleStateByNode.Clear();
            OutputSignalValue = 0;
            SendLogicSignal();
        }

        private void OnCopySettings(object data)
        {
            GameObject sourceObject = data as GameObject;
            LogicNetworkEmitter source = sourceObject != null
                ? sourceObject.GetComponent<LogicNetworkEmitter>()
                : null;
            if (source == null || source == this)
            {
                return;
            }

            LogicNetwork.UI.WebEditor.LogicNetworkWebEditor.ApplyPending(source);
            ApplyWebEditorState(
                source.RuntimeBlueprintJson,
                source.OutputModeValue,
                source.RuntimeLayoutJson);
        }

        internal void ApplyPersistedWebEditorState(string runtimeBlueprintJson, int outputModeValue, string runtimeLayoutJson)
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
            EvaluateConditionOutput();
            ForceLogicNetworkRefresh();
        }

        internal Dictionary<string, float> GetRuntimeEvalSnapshot()
        {
            return new Dictionary<string, float>(runtimeEvalCache);
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

            public List<float> Values { get; set; }

            public List<float> Durations { get; set; }

            public float GapSeconds { get; set; }

            public bool Loop { get; set; }

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
