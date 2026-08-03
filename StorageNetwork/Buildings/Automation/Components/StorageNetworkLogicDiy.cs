using KSerialization;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using StorageNetwork.Core;
using StorageNetwork.LogicDiy.Runtime;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;
using System;

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
        // All graph state below is indexed by the integer node index assigned when a blueprint
        // is compiled.  Sim200ms never hashes node ids or constructs port keys/temporary sets.
        private CompiledRuntimeNode[] compiledNodes = Array.Empty<CompiledRuntimeNode>();
        private Dictionary<string, int> compiledNodeIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
        private CompiledRuntimeInput[] compiledOutputInputs = CreateDisconnectedInputs(4);
        private CompiledRuntimeInput compiledCompareInput = CompiledRuntimeInput.Disconnected;
        private int[] compiledTimerNodeIndices = Array.Empty<int>();
        private int[] compiledDisplayNodeIndices = Array.Empty<int>();
        private int[] compiledCounterNodeIndices = Array.Empty<int>();
        private int[] compiledRemoteNodeIndices = Array.Empty<int>();
        private int[] compiledForwardingNodeIndices = Array.Empty<int>();
        private float[] timerElapsedByNode = Array.Empty<float>();
        private bool[] timerPulseByNode = Array.Empty<bool>();
        private int[] cycleIndexByNode = Array.Empty<int>();
        private float[] delayElapsedByNode = Array.Empty<float>();
        private bool[] latchStateByNode = Array.Empty<bool>();
        private bool[] previousInputStateByNode = Array.Empty<bool>();
        private float[] previousMaterialAmountByNode = Array.Empty<float>();
        private bool[] previousMaterialAmountKnownByNode = Array.Empty<bool>();
        private float[] counterValueByNode = Array.Empty<float>();
        private int[] sequenceStepByNode = Array.Empty<int>();
        private bool[] sequencePrevAdvanceByNode = Array.Empty<bool>();
        private bool[] sequencePrevResetByNode = Array.Empty<bool>();
        private int[] musicStepByNode = Array.Empty<int>();
        private bool[] musicStepKnownByNode = Array.Empty<bool>();
        private float[] musicStepStartedAtByNode = Array.Empty<float>();
        private bool[] musicStepStartedKnownByNode = Array.Empty<bool>();
        private bool[] musicPrevResetByNode = Array.Empty<bool>();
        private bool[] hysteresisStateByNode = Array.Empty<bool>();
        private bool[] hysteresisStateKnownByNode = Array.Empty<bool>();
        private bool[] toggleStateByNode = Array.Empty<bool>();
        private bool[] togglePrevInputByNode = Array.Empty<bool>();
        private float[] pulseShaperRemainingByNode = Array.Empty<float>();
        private float[] previousNumberValueByNode = Array.Empty<float>();
        private bool[] previousNumberValueKnownByNode = Array.Empty<bool>();
        private byte[] numberChangeFlagsByNode = Array.Empty<byte>();
        private int[] numberChangeUpdatedGenerationByNode = Array.Empty<int>();
        private int[] remotePixelScreenTargetByNode = Array.Empty<int>();
        private int[] networkSignalOutputTargetByNode = Array.Empty<int>();
        private readonly HashSet<int> signalForwardActiveTargets = new HashSet<int>();
        private readonly List<int> signalForwardRemovalBuffer = new List<int>();
        private readonly List<Component> switchLikeComponentBuffer = new List<Component>(8);
        private readonly LogicValueChanged pixelPackLogicChangeBuffer = new LogicValueChanged();
        private readonly object[] pixelPackLogicInvokeArguments = new object[1];
        private float[] runtimeEvalValues = Array.Empty<float>();
        private float[] runtimeStableOutputSnapshot = Array.Empty<float>();
        private int[] runtimeEvalGenerationByOutput = Array.Empty<int>();
        private byte[] runtimeEvalStateByOutput = Array.Empty<byte>();
        private int runtimeEvalGeneration;
        private bool runtimeEvaluationPassPrepared;
        private readonly Dictionary<int, Guid> signalForwardStatusHandleByTarget = new Dictionary<int, Guid>();
        private float runtimeEvalDt;
        private Tag compiledConditionItemTag = Tag.Invalid;
        private bool compiledConditionItemTagValid;
        private readonly LogicDiyBlueprintCodec blueprintCodec = new LogicDiyBlueprintCodec();
        private RuntimeBlueprint compiledBlueprint;
        private bool compiledUsesMaterialInput;
        private bool compiledUsesFourChannelOutput;
        private int compiledBlueprintVersion;
        private int remoteBindingsBlueprintVersion = -1;
        private int signalStatusBlueprintVersion = -1;
        private int lastSentSignalValue = int.MinValue;
        private static readonly Dictionary<System.Type, Func<Component, bool>> switchLikeOutputGetterByType =
            new Dictionary<System.Type, Func<Component, bool>>();
        private static readonly MethodInfo pixelPackLogicValueChangedMethod = typeof(PixelPack).GetMethod(
            "OnLogicValueChanged",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Action<PixelPack, object> pixelPackLogicValueChanged = CreatePixelPackLogicValueChangedAccessor();
        private static readonly AccessTools.FieldRef<PixelPack, int> pixelPackLogicValue =
            CreatePixelPackLogicValueAccessor();
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
            simRenderLoadBalance = true;
            Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            logicPorts = logicPorts ?? GetComponent<LogicPorts>() ?? gameObject.AddOrGet<LogicPorts>();
            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyPersistence.TryLoad(this);
            CompileConditionItemTag();
            ConditionThresholdKg = Mathf.Max(0f, ConditionThresholdKg);
            ConditionOutputChannel = Mathf.Clamp(ConditionOutputChannel, 0, 3);
            EnsureRuntimeBlueprintForLegacyState();
            ClampOutputValue();
            startupRefreshTicks = StartupRefreshTicks;
            EvaluateWithForcedSnapshot();
            SendLogicSignal();
        }

        protected override void OnCleanUp()
        {
            SendLogicSignal(0, true);
            RemoveAllSignalForwardStatuses();
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
        }

        public void SetSourceMode(SourceMode mode)
        {
            SourceModeValue = Mathf.Clamp((int)mode, 0, 1);
            EvaluateConditionOutput();
        }

        public void SetConditionItem(string itemKey)
        {
            ConditionItemKey = itemKey ?? string.Empty;
            CompileConditionItemTag();
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
            BeginRuntimeEvaluationPass();
            BuildRuntimeStableOutputSnapshot();
            runtimeEvaluationPassPrepared = true;
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
            UpdateRemotePixelScreens();
            UpdateNetworkSignalOutputStatus();
            ApplyDeferredCounterResets();

            if (startupRefreshTicks > 0)
            {
                if (!IsMaterialNetworkOfflineDuringStartup())
                {
                    startupRefreshTicks--;
                }
            }

            StorageNetwork.UI.WebEditor.StorageNetworkLogicDiyWebEditor.RefreshRuntimeSignalsIfActive(this);
            runtimeEvaluationPassPrepared = false;
        }

        private void SendLogicSignal()
        {
            SendLogicSignal(OutputSignalValue, false);
        }

        private void SendLogicSignal(int value, bool force)
        {
            int clamped = ClampOutputValue(value);
            if (!force && lastSentSignalValue == clamped)
            {
                return;
            }

            lastSentSignalValue = clamped;
            logicPorts?.SendSignal(PORT_ID, clamped);
        }

        private void EvaluateWithForcedSnapshot()
        {
            EvaluateConditionOutput();
        }

        public void EvaluateConditionOutput()
        {
            EnsureRuntimeBlueprintForLegacyState();
            if (ShouldDeferMaterialEvaluation())
            {
                return;
            }

            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (!runtimeEvaluationPassPrepared)
            {
                BeginRuntimeEvaluationPass();
                BuildRuntimeStableOutputSnapshot();
            }
            if (TryEvaluateRuntimeOutput(blueprint, out int runtimeOutputValue))
            {
                SetSignalValue(runtimeOutputValue);
                return;
            }

            if (OutputSourceMode != SourceMode.MaterialCondition ||
                !compiledConditionItemTagValid)
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
            return !StorageSceneRegistry.HasOnlineCoreInWorld(worldId);
        }

        private void EnsureRuntimeBlueprintForLegacyState()
        {
            if (!string.IsNullOrEmpty(RuntimeBlueprintJson) ||
                OutputSourceMode != SourceMode.MaterialCondition ||
                !compiledConditionItemTagValid)
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
            EnsureCompiledBlueprint(blueprint);
            return compiledUsesMaterialInput;
        }

        private bool TryEvaluateRuntimeOutput(RuntimeBlueprint blueprint, out int outputValue)
        {
            outputValue = 0;
            if (blueprint == null)
            {
                return false;
            }

            EnsureCompiledBlueprint(blueprint);
            if (UsesFourChannelRuntimeOutput(blueprint))
            {
                bool hasAnyInput = false;
                for (int portIndex = 0; portIndex < 4; portIndex++)
                {
                    CompiledRuntimeInput input = compiledOutputInputs[portIndex];
                    if (!input.IsConnected)
                    {
                        continue;
                    }

                    hasAnyInput = true;
                    float portValue = EvaluateRuntimeNumber(input.SourceNodeIndex, input.SourcePortIndex, 0);
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

            CompiledRuntimeInput outputInput = compiledOutputInputs[0];
            if (!outputInput.IsConnected)
            {
                return true;
            }

            float value = EvaluateRuntimeNumber(outputInput.SourceNodeIndex, outputInput.SourcePortIndex, 0);
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

            for (int index = 0; index < compiledDisplayNodeIndices.Length; index++)
            {
                int nodeIndex = compiledDisplayNodeIndices[index];
                int outputCount = compiledNodes[nodeIndex].DisplayOutputCount;
                for (int outputPort = 0; outputPort < outputCount; outputPort++)
                {
                    EvaluateRuntimeNumber(nodeIndex, outputPort, 0);
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

            for (int index = 0; index < compiledCounterNodeIndices.Length; index++)
            {
                int nodeIndex = compiledCounterNodeIndices[index];
                CompiledRuntimeInput resetInput = GetRuntimeInput(nodeIndex, 1);
                if (!resetInput.IsConnected)
                {
                    continue;
                }

                // The Reset source may have been evaluated before this counter published its
                // final value. Recompute that source against the committed counter cache.
                InvalidateRuntimeOutput(resetInput.SourceNodeIndex, resetInput.SourcePortIndex);
                float resetValue = EvaluateRuntimeNumber(resetInput.SourceNodeIndex, resetInput.SourcePortIndex, 0);
                if (!IsRuntimeTrue(resetValue))
                {
                    continue;
                }

                counterValueByNode[nodeIndex] = 0f;
                SetRuntimeOutput(nodeIndex, 0, 0f);
            }
        }

        private bool UsesFourChannelRuntimeOutput(RuntimeBlueprint blueprint)
        {
            if (OutputMode == ChannelMode.FourChannel)
            {
                return true;
            }

            EnsureCompiledBlueprint(blueprint);
            return compiledUsesFourChannelOutput;
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
                return GetNetworkItemAmountKg();
            }

            CompiledRuntimeInput input = compiledCompareInput;
            if (!input.IsConnected)
            {
                return GetNetworkItemAmountKg();
            }

            return EvaluateRuntimeNumber(input.SourceNodeIndex, input.SourcePortIndex, 0);
        }

        private RuntimeBlueprint TryGetRuntimeBlueprint()
        {
            RuntimeBlueprint blueprint = blueprintCodec.Parse(RuntimeBlueprintJson);
            EnsureCompiledBlueprint(blueprint);
            return blueprint;
        }

        private void EnsureCompiledBlueprint(RuntimeBlueprint blueprint)
        {
            if (ReferenceEquals(compiledBlueprint, blueprint))
            {
                return;
            }

            CompiledRuntimeNode[] oldNodes = compiledNodes;
            Dictionary<string, int> oldNodeIndexById = compiledNodeIndexById;
            bool[] oldLatchState = latchStateByNode;
            float[] oldCounterValue = counterValueByNode;
            int[] oldSequenceStep = sequenceStepByNode;
            int[] oldMusicStep = musicStepByNode;
            bool[] oldMusicStepKnown = musicStepKnownByNode;
            float[] oldMusicStartedAt = musicStepStartedAtByNode;
            bool[] oldMusicStartedKnown = musicStepStartedKnownByNode;
            bool[] oldMusicPrevReset = musicPrevResetByNode;
            bool[] oldHysteresisState = hysteresisStateByNode;
            bool[] oldHysteresisKnown = hysteresisStateKnownByNode;
            bool[] oldToggleState = toggleStateByNode;
            float[] oldPulseRemaining = pulseShaperRemainingByNode;
            int[] oldRemoteTargets = remotePixelScreenTargetByNode;
            int[] oldForwardTargets = networkSignalOutputTargetByNode;

            compiledBlueprint = blueprint;
            compiledBlueprintVersion++;
            compiledUsesMaterialInput = false;
            compiledUsesFourChannelOutput = false;
            compiledOutputInputs = CreateDisconnectedInputs(4);
            compiledCompareInput = CompiledRuntimeInput.Disconnected;

            // Keep the last integer state arrays available for migration if an editor payload is
            // temporarily malformed. The null blueprint is still treated as inactive this tick.
            if (blueprint == null)
            {
                compiledTimerNodeIndices = Array.Empty<int>();
                compiledDisplayNodeIndices = Array.Empty<int>();
                compiledCounterNodeIndices = Array.Empty<int>();
                compiledRemoteNodeIndices = Array.Empty<int>();
                compiledForwardingNodeIndices = Array.Empty<int>();
                remoteBindingsBlueprintVersion = -1;
                signalStatusBlueprintVersion = -1;
                return;
            }

            List<CompiledRuntimeNode> nodeBuffer = new List<CompiledRuntimeNode>(blueprint?.Nodes?.Count ?? 0);
            Dictionary<string, int> newNodeIndexById = new Dictionary<string, int>(StringComparer.Ordinal);
            if (blueprint?.Nodes != null)
            {
                foreach (RuntimeBlueprintNode node in blueprint.Nodes)
                {
                    if (node == null || string.IsNullOrEmpty(node.Id))
                    {
                        continue;
                    }

                    RuntimeModuleKind module = node.Id == "system:material"
                        ? RuntimeModuleKind.SystemMaterial
                        : node.Id == "system:fixed"
                            ? RuntimeModuleKind.SystemFixed
                            : GetRuntimeModuleKind(node.Module);
                    if (module == RuntimeModuleKind.Unknown && !string.IsNullOrEmpty(node.Module))
                    {
                        Debug.LogWarning($"StorageNetwork LogicDiy: Unknown module '{node.Module}' for node '{node.Id}'");
                    }
                    int nodeIndex = nodeBuffer.Count;
                    nodeBuffer.Add(new CompiledRuntimeNode
                    {
                        Id = node.Id,
                        BlueprintNode = node,
                        Module = module,
                        DisplayOutputCount = GetDisplayOutputCount(module),
                        OutputCount = GetDisplayOutputCount(module),
                        RequiredInputCount = GetRequiredInputCount(module, node)
                    });
                    // Deliberately overwrite in blueprint order: this is the node equivalent
                    // of the editor's existing last-wins behaviour for duplicate ids.
                    newNodeIndexById[node.Id] = nodeIndex;
                    if (node.Id == "system:material" || IsMaterialRuntimeModule(module))
                    {
                        compiledUsesMaterialInput = true;
                    }
                }
            }

            if (blueprint?.Connections != null)
            {
                foreach (RuntimeBlueprintConnection connection in blueprint.Connections)
                {
                    if (connection == null || string.IsNullOrEmpty(connection.ToNodeId))
                    {
                        continue;
                    }

                    if (newNodeIndexById.TryGetValue(connection.ToNodeId, out int targetIndex) &&
                        connection.ToPortIndex >= 0 && connection.ToPortIndex < 64)
                    {
                        CompiledRuntimeNode target = nodeBuffer[targetIndex];
                        target.RequiredInputCount = Math.Max(target.RequiredInputCount, connection.ToPortIndex + 1);
                    }

                    if (newNodeIndexById.TryGetValue(connection.FromNodeId ?? string.Empty, out int sourceIndex) &&
                        connection.FromPortIndex >= 0 && connection.FromPortIndex < 64)
                    {
                        CompiledRuntimeNode source = nodeBuffer[sourceIndex];
                        source.OutputCount = Math.Max(source.OutputCount, connection.FromPortIndex + 1);
                    }
                }
            }

            compiledNodes = nodeBuffer.ToArray();
            compiledNodeIndexById = newNodeIndexById;
            int totalOutputCount = 0;
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                CompiledRuntimeNode node = compiledNodes[nodeIndex];
                node.OutputOffset = totalOutputCount;
                node.OutputCount = Math.Max(1, node.OutputCount);
                totalOutputCount += node.OutputCount;
                node.Inputs = CreateDisconnectedInputs(node.RequiredInputCount);
                node.OutputCacheKeys = new string[node.OutputCount];
                for (int outputPort = 0; outputPort < node.OutputCount; outputPort++)
                {
                    node.OutputCacheKeys[outputPort] = node.Id + ":" + outputPort;
                }
            }

            if (blueprint?.Connections != null)
            {
                // Assign in creation order so a later wire overwrites an older wire targeting
                // the same port, matching the editor's existing "last wire wins" rule.
                foreach (RuntimeBlueprintConnection connection in blueprint.Connections)
                {
                    if (connection == null || string.IsNullOrEmpty(connection.ToNodeId) || connection.ToPortIndex < 0)
                    {
                        continue;
                    }

                    CompiledRuntimeInput input = new CompiledRuntimeInput(
                        ResolveCompiledSourceNode(connection.FromNodeId),
                        connection.FromPortIndex,
                        true);
                    if (connection.ToNodeId == "system:output")
                    {
                        if (connection.ToPortIndex < compiledOutputInputs.Length)
                        {
                            compiledOutputInputs[connection.ToPortIndex] = input;
                        }
                        if (connection.ToPortIndex > 0)
                        {
                            compiledUsesFourChannelOutput = true;
                        }
                        continue;
                    }

                    if (connection.ToNodeId == "system:compare" && connection.ToPortIndex == 0)
                    {
                        compiledCompareInput = input;
                    }

                    if (compiledNodeIndexById.TryGetValue(connection.ToNodeId, out int targetIndex))
                    {
                        CompiledRuntimeInput[] inputs = compiledNodes[targetIndex].Inputs;
                        if (connection.ToPortIndex < inputs.Length)
                        {
                            inputs[connection.ToPortIndex] = input;
                        }
                    }
                }
            }

            compiledTimerNodeIndices = BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind.Timer);
            compiledDisplayNodeIndices = BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind.Display);
            compiledCounterNodeIndices = BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind.Counter);
            compiledRemoteNodeIndices = BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind.Remote);
            compiledForwardingNodeIndices = BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind.Forwarding);

            timerElapsedByNode = new float[compiledNodes.Length];
            timerPulseByNode = new bool[compiledNodes.Length];
            cycleIndexByNode = new int[compiledNodes.Length];
            delayElapsedByNode = new float[compiledNodes.Length];
            latchStateByNode = new bool[compiledNodes.Length];
            previousInputStateByNode = new bool[compiledNodes.Length];
            previousMaterialAmountByNode = new float[compiledNodes.Length];
            previousMaterialAmountKnownByNode = new bool[compiledNodes.Length];
            counterValueByNode = new float[compiledNodes.Length];
            sequenceStepByNode = new int[compiledNodes.Length];
            sequencePrevAdvanceByNode = new bool[compiledNodes.Length];
            sequencePrevResetByNode = new bool[compiledNodes.Length];
            musicStepByNode = new int[compiledNodes.Length];
            musicStepKnownByNode = new bool[compiledNodes.Length];
            musicStepStartedAtByNode = new float[compiledNodes.Length];
            musicStepStartedKnownByNode = new bool[compiledNodes.Length];
            musicPrevResetByNode = new bool[compiledNodes.Length];
            hysteresisStateByNode = new bool[compiledNodes.Length];
            hysteresisStateKnownByNode = new bool[compiledNodes.Length];
            toggleStateByNode = new bool[compiledNodes.Length];
            togglePrevInputByNode = new bool[compiledNodes.Length];
            pulseShaperRemainingByNode = new float[compiledNodes.Length];
            previousNumberValueByNode = new float[compiledNodes.Length];
            previousNumberValueKnownByNode = new bool[compiledNodes.Length];
            numberChangeFlagsByNode = new byte[compiledNodes.Length];
            numberChangeUpdatedGenerationByNode = new int[compiledNodes.Length];
            remotePixelScreenTargetByNode = new int[compiledNodes.Length];
            networkSignalOutputTargetByNode = new int[compiledNodes.Length];
            runtimeEvalValues = new float[totalOutputCount];
            runtimeStableOutputSnapshot = new float[totalOutputCount];
            runtimeEvalGenerationByOutput = new int[totalOutputCount];
            runtimeEvalStateByOutput = new byte[totalOutputCount];
            runtimeEvalGeneration = 0;

            // Stateful nodes survive non-topology editor updates when their stable id remains.
            // Migration occurs only while compiling, never on the simulation hot path.
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                if (!oldNodeIndexById.TryGetValue(compiledNodes[nodeIndex].Id, out int oldIndex) ||
                    oldIndex < 0 || oldIndex >= oldNodes.Length)
                {
                    continue;
                }

                CopyArrayValue(oldLatchState, oldIndex, latchStateByNode, nodeIndex);
                CopyArrayValue(oldCounterValue, oldIndex, counterValueByNode, nodeIndex);
                CopyArrayValue(oldSequenceStep, oldIndex, sequenceStepByNode, nodeIndex);
                CopyArrayValue(oldMusicStep, oldIndex, musicStepByNode, nodeIndex);
                CopyArrayValue(oldMusicStepKnown, oldIndex, musicStepKnownByNode, nodeIndex);
                CopyArrayValue(oldMusicStartedAt, oldIndex, musicStepStartedAtByNode, nodeIndex);
                CopyArrayValue(oldMusicStartedKnown, oldIndex, musicStepStartedKnownByNode, nodeIndex);
                CopyArrayValue(oldMusicPrevReset, oldIndex, musicPrevResetByNode, nodeIndex);
                CopyArrayValue(oldHysteresisState, oldIndex, hysteresisStateByNode, nodeIndex);
                CopyArrayValue(oldHysteresisKnown, oldIndex, hysteresisStateKnownByNode, nodeIndex);
                CopyArrayValue(oldToggleState, oldIndex, toggleStateByNode, nodeIndex);
                CopyArrayValue(oldPulseRemaining, oldIndex, pulseShaperRemainingByNode, nodeIndex);
                CopyArrayValue(oldRemoteTargets, oldIndex, remotePixelScreenTargetByNode, nodeIndex);
                CopyArrayValue(oldForwardTargets, oldIndex, networkSignalOutputTargetByNode, nodeIndex);
            }

            PrewarmCompiledBuildingSignalAccessors();
            ClearRemovedRemotePixelScreenTargets(oldNodes, oldRemoteTargets);
            remoteBindingsBlueprintVersion = -1;
            signalStatusBlueprintVersion = -1;
        }

        private float EvaluateRuntimeNumber(int nodeIndex, int outputPortIndex, int depth)
        {
            if (depth > 32)
            {
                return 0f;
            }

            if (nodeIndex == MaterialSourceNodeIndex)
            {
                return GetNetworkItemAmountKg();
            }

            if (nodeIndex == FixedSourceNodeIndex)
            {
                return OutputSignalValue;
            }

            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length)
            {
                return 0f;
            }

            CompiledRuntimeNode compiledNode = compiledNodes[nodeIndex];
            int outputSlot = GetRuntimeOutputSlot(compiledNode, outputPortIndex);
            if (outputSlot < 0)
            {
                return 0f;
            }

            if (runtimeEvalGenerationByOutput[outputSlot] == runtimeEvalGeneration)
            {
                byte state = runtimeEvalStateByOutput[outputSlot];
                if (state == RuntimeEvalStateEvaluating)
                {
                    return runtimeStableOutputSnapshot[outputSlot];
                }
                if (state == RuntimeEvalStatePublished)
                {
                    // Stateful nodes may publish an in-progress candidate for feedback paths
                    // (for example: Counter -> Equal -> Counter.Reset), including zero.
                    return runtimeEvalValues[outputSlot];
                }

                return runtimeEvalValues[outputSlot];
            }

            runtimeEvalGenerationByOutput[outputSlot] = runtimeEvalGeneration;
            runtimeEvalStateByOutput[outputSlot] = RuntimeEvalStateEvaluating;
            RuntimeBlueprintNode node = compiledNode.BlueprintNode;
            RuntimeModuleKind module = compiledNode.Module;
            float result;
            if (module == RuntimeModuleKind.SystemMaterial)
            {
                result = GetNetworkItemAmountKg(node);
                CompleteRuntimeOutput(outputSlot, result);
                return result;
            }
            if (module == RuntimeModuleKind.SystemFixed)
            {
                result = OutputSignalValue;
                CompleteRuntimeOutput(outputSlot, result);
                return result;
            }
            if (module == RuntimeModuleKind.Counter)
            {
                result = EvaluateCounterNode(nodeIndex, depth + 1);
                CompleteRuntimeOutput(outputSlot, result);
                return result;
            }

            // Preserve the original evaluator's eager first-two-input semantics. Some stateful
            // upstream nodes rely on being advanced even when the selected module ignores them.
            float a = EvaluateRuntimeInputNumber(nodeIndex, 0, depth + 1);
            float b = EvaluateRuntimeInputNumber(nodeIndex, 1, depth + 1);
            switch (module)
            {
                case RuntimeModuleKind.Add:
                    result = EvaluateRuntimeAggregate(nodeIndex, node.InputCount, depth + 2, 2, RuntimeAggregateOperation.Add);
                    break;
                case RuntimeModuleKind.Subtract:
                    result = EvaluateRuntimeAggregate(nodeIndex, node.InputCount, depth + 3, 2, RuntimeAggregateOperation.Subtract);
                    break;
                case RuntimeModuleKind.Multiply:
                    result = EvaluateRuntimeAggregate(nodeIndex, node.InputCount, depth + 2, 2, RuntimeAggregateOperation.Multiply);
                    break;
                case RuntimeModuleKind.Divide:
                    result = EvaluateRuntimeAggregate(nodeIndex, node.InputCount, depth + 3, 2, RuntimeAggregateOperation.Divide);
                    break;
                case RuntimeModuleKind.Negate: result = -a; break;
                case RuntimeModuleKind.Min: result = Mathf.Min(a, b); break;
                case RuntimeModuleKind.Max: result = Mathf.Max(a, b); break;
                case RuntimeModuleKind.Clamp:
                    float clampMax = EvaluateRuntimeInputNumber(nodeIndex, 2, depth + 1);
                    result = Mathf.Clamp(a, Mathf.Min(b, clampMax), Mathf.Max(b, clampMax));
                    break;
                case RuntimeModuleKind.Modulo: result = Mathf.Abs(b) < 0.0001f ? 0f : a % b; break;
                case RuntimeModuleKind.GreaterThan: result = a > b ? 1f : 0f; break;
                case RuntimeModuleKind.Equal: result = Mathf.Approximately(a, b) ? 1f : 0f; break;
                case RuntimeModuleKind.LessThan: result = a < b ? 1f : 0f; break;
                case RuntimeModuleKind.Range:
                    float c = EvaluateRuntimeInputNumber(nodeIndex, 2, depth + 1);
                    result = a >= Mathf.Min(b, c) && a <= Mathf.Max(b, c) ? 1f : 0f;
                    break;
                case RuntimeModuleKind.Variable: result = ConditionThresholdKg; break;
                case RuntimeModuleKind.Constant: result = node.Value; break;
                case RuntimeModuleKind.TestSignal: result = node.Value > 0.5f ? 1f : 0f; break;
                case RuntimeModuleKind.BoolTrue: result = 1f; break;
                case RuntimeModuleKind.BoolFalse: result = 0f; break;
                case RuntimeModuleKind.BoolAnd: result = IsRuntimeTrue(a) && IsRuntimeTrue(b) ? 1f : 0f; break;
                case RuntimeModuleKind.BoolNand: result = IsRuntimeTrue(a) && IsRuntimeTrue(b) ? 0f : 1f; break;
                case RuntimeModuleKind.BoolOr: result = IsRuntimeTrue(a) || IsRuntimeTrue(b) ? 1f : 0f; break;
                case RuntimeModuleKind.BoolNor: result = IsRuntimeTrue(a) || IsRuntimeTrue(b) ? 0f : 1f; break;
                case RuntimeModuleKind.BoolXor: result = IsRuntimeTrue(a) != IsRuntimeTrue(b) ? 1f : 0f; break;
                case RuntimeModuleKind.BoolNot: result = IsRuntimeTrue(a) ? 0f : 1f; break;
                case RuntimeModuleKind.Selector:
                    result = IsRuntimeTrue(a) ? b : EvaluateRuntimeInputNumber(nodeIndex, 2, depth + 1);
                    break;
                case RuntimeModuleKind.Sequence: result = EvaluateSequenceNode(nodeIndex, outputPortIndex, a, b, node); break;
                case RuntimeModuleKind.MusicSequencer: result = EvaluateMusicSequencerNode(nodeIndex, a, b, node); break;
                case RuntimeModuleKind.Delay: result = EvaluateDelayNode(nodeIndex, a, node.IntervalSeconds); break;
                case RuntimeModuleKind.Latch: result = EvaluateLatchNode(nodeIndex, a, b); break;
                case RuntimeModuleKind.EdgePulse: result = EvaluateEdgePulseNode(nodeIndex, a); break;
                case RuntimeModuleKind.Hysteresis: result = EvaluateHysteresisNode(nodeIndex, a, node); break;
                case RuntimeModuleKind.Toggle: result = EvaluateToggleNode(nodeIndex, a); break;
                case RuntimeModuleKind.PulseShaper: result = EvaluatePulseShaperNode(nodeIndex, a, node); break;
                case RuntimeModuleKind.NumberChanged: result = EvaluateNumberChangedNode(nodeIndex, a, outputPortIndex); break;
                case RuntimeModuleKind.MapRange: result = EvaluateMapRangeNode(a, node); break;
                case RuntimeModuleKind.RandomChance:
                    result = UnityEngine.Random.value * 100f < Mathf.Clamp(node.Value, 0f, 100f) ? 1f : 0f;
                    break;
                case RuntimeModuleKind.TimerPulse: result = timerPulseByNode[nodeIndex] ? 1f : 0f; break;
                case RuntimeModuleKind.Cycle4:
                    result = cycleIndexByNode[nodeIndex] == Mathf.Clamp(outputPortIndex, 0, 3) ? 1f : 0f;
                    break;
                case RuntimeModuleKind.MaterialCondition: result = GetNetworkItemAmountKg(node); break;
                case RuntimeModuleKind.MaterialLow:
                    result = HasMaterialSelection(node) && GetNetworkItemAmountKg(node) < Mathf.Max(0f, node.Value) ? 1f : 0f;
                    break;
                case RuntimeModuleKind.MaterialHigh:
                    result = HasMaterialSelection(node) && GetNetworkItemAmountKg(node) >= Mathf.Max(0f, node.Value) ? 1f : 0f;
                    break;
                case RuntimeModuleKind.MaterialChanged: result = EvaluateMaterialChangedNode(nodeIndex); break;
                case RuntimeModuleKind.InventoryPercent: result = GetNetworkFillPercent(); break;
                case RuntimeModuleKind.InventoryStored: result = GetNetworkStoredKg(); break;
                case RuntimeModuleKind.InventoryRemaining: result = GetNetworkRemainingKg(); break;
                case RuntimeModuleKind.InventoryCapacity: result = GetNetworkCapacityKg(); break;
                case RuntimeModuleKind.PowerPercent: result = GetNetworkPowerPercent(); break;
                case RuntimeModuleKind.PowerStored: result = GetNetworkPowerStoredJoules(); break;
                case RuntimeModuleKind.PowerCapacity: result = GetNetworkPowerCapacityJoules(); break;
                case RuntimeModuleKind.PowerRemaining: result = GetNetworkPowerRemainingJoules(); break;
                case RuntimeModuleKind.BuildingStatus: result = GetBuildingStatusSignal(node.SelectedBuildingInstanceId); break;
                case RuntimeModuleKind.BuildingSignal:
                case RuntimeModuleKind.NetworkSignalOutput:
                    result = GetBuildingOutputSignal(node.SelectedBuildingInstanceId);
                    break;
                case RuntimeModuleKind.Output: result = a; break;
                case RuntimeModuleKind.Split4: result = EvaluateSplit4Node(outputPortIndex, a); break;
                case RuntimeModuleKind.Merge4: result = EvaluateMerge4Node(nodeIndex, depth); break;
                case RuntimeModuleKind.Select:
                    int portCount = node.Value > 1f ? Mathf.FloorToInt(node.Value) : 6;
                    int selectedIndex = Mathf.Clamp(Mathf.FloorToInt(a), 0, portCount - 1);
                    result = EvaluateRuntimeInputNumber(nodeIndex, selectedIndex + 1, depth + 1);
                    break;
                case RuntimeModuleKind.PixelScreen: result = EvaluatePixelScreenNode(nodeIndex, depth); break;
                default: result = 0f; break;
            }

            CompleteRuntimeOutput(outputSlot, result);
            return result;
        }

        private void BuildRuntimeStableOutputSnapshot()
        {
            Array.Clear(runtimeStableOutputSnapshot, 0, runtimeStableOutputSnapshot.Length);
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                CompiledRuntimeNode node = compiledNodes[nodeIndex];
                int slot = node.OutputOffset;
                switch (node.Module)
                {
                    case RuntimeModuleKind.Counter:
                        FillRuntimeStableOutputs(node, counterValueByNode[nodeIndex]);
                        break;
                    case RuntimeModuleKind.Sequence:
                        int step = sequenceStepByNode[nodeIndex];
                        List<float> values = node.BlueprintNode.Values;
                        float sequenceValue = 0f;
                        if (values != null && values.Count > 0)
                        {
                            sequenceValue = Mathf.Clamp(
                                Mathf.Floor(values[Mathf.Clamp(step, 0, values.Count - 1)]), 0f, 15f);
                        }
                        FillRuntimeStableOutputs(node, sequenceValue);
                        if (node.OutputCount > 1)
                        {
                            runtimeStableOutputSnapshot[slot + 1] = step;
                        }
                        break;
                    case RuntimeModuleKind.Latch:
                        FillRuntimeStableOutputs(node, latchStateByNode[nodeIndex] ? 1f : 0f);
                        break;
                    case RuntimeModuleKind.Toggle:
                        FillRuntimeStableOutputs(node, toggleStateByNode[nodeIndex] ? 1f : 0f);
                        break;
                    case RuntimeModuleKind.Hysteresis:
                        FillRuntimeStableOutputs(node, hysteresisStateByNode[nodeIndex] ? 1f : 0f);
                        break;
                    case RuntimeModuleKind.PulseShaper:
                        FillRuntimeStableOutputs(node, pulseShaperRemainingByNode[nodeIndex] > 0f ? 1f : 0f);
                        break;
                }
            }
        }

        private void FillRuntimeStableOutputs(CompiledRuntimeNode node, float value)
        {
            int end = node.OutputOffset + node.OutputCount;
            for (int slot = node.OutputOffset; slot < end; slot++)
            {
                runtimeStableOutputSnapshot[slot] = value;
            }
        }

        private float EvaluateRuntimeInputNumber(int nodeIndex, int portIndex, int depth)
        {
            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length || portIndex < 0)
            {
                return 0f;
            }

            CompiledRuntimeNode node = compiledNodes[nodeIndex];
            CompiledRuntimeInput input = GetRuntimeInput(nodeIndex, portIndex);
            if (!input.IsConnected)
            {
                List<float> inputValues = node.BlueprintNode.InputValues;
                return inputValues != null && portIndex < inputValues.Count ? inputValues[portIndex] : 0f;
            }

            return EvaluateRuntimeNumber(input.SourceNodeIndex, input.SourcePortIndex, depth);
        }

        private float EvaluateRuntimeAggregate(
            int nodeIndex,
            int inputCount,
            int inputDepth,
            int minimumCount,
            RuntimeAggregateOperation operation)
        {
            int count = Mathf.Clamp(inputCount > 0 ? inputCount : minimumCount, minimumCount, 10);
            float result;
            int startIndex;
            switch (operation)
            {
                case RuntimeAggregateOperation.Add:
                    result = 0f;
                    startIndex = 0;
                    break;
                case RuntimeAggregateOperation.Multiply:
                    result = 1f;
                    startIndex = 0;
                    break;
                default:
                    result = EvaluateRuntimeInputNumber(nodeIndex, 0, inputDepth);
                    startIndex = 1;
                    break;
            }

            for (int i = startIndex; i < count; i++)
            {
                float value = EvaluateRuntimeInputNumber(nodeIndex, i, inputDepth);
                switch (operation)
                {
                    case RuntimeAggregateOperation.Add:
                        result += value;
                        break;
                    case RuntimeAggregateOperation.Subtract:
                        result -= value;
                        break;
                    case RuntimeAggregateOperation.Multiply:
                        result *= value;
                        break;
                    case RuntimeAggregateOperation.Divide:
                        result = Mathf.Abs(value) < 0.0001f ? 0f : result / value;
                        break;
                }
            }

            return result;
        }

        private float EvaluateDelayNode(int nodeIndex, float inputValue, float intervalSeconds)
        {
            if (!IsRuntimeTrue(inputValue))
            {
                delayElapsedByNode[nodeIndex] = 0f;
                return 0f;
            }

            float interval = Mathf.Max(0.2f, intervalSeconds > 0f ? intervalSeconds : 5f);
            float elapsed = delayElapsedByNode[nodeIndex] + runtimeEvalDt;
            delayElapsedByNode[nodeIndex] = elapsed;
            return elapsed >= interval ? 1f : 0f;
        }

        private float EvaluateLatchNode(int nodeIndex, float setValue, float resetValue)
        {
            bool latched = latchStateByNode[nodeIndex];
            if (IsRuntimeTrue(resetValue))
            {
                latched = false;
            }
            else if (IsRuntimeTrue(setValue))
            {
                latched = true;
            }

            latchStateByNode[nodeIndex] = latched;
            return latched ? 1f : 0f;
        }

        private float EvaluateEdgePulseNode(int nodeIndex, float inputValue)
        {
            bool current = IsRuntimeTrue(inputValue);
            bool previous = previousInputStateByNode[nodeIndex];
            previousInputStateByNode[nodeIndex] = current;
            return current && !previous ? 1f : 0f;
        }

        private float EvaluateHysteresisNode(int nodeIndex, float inputValue, RuntimeBlueprintNode node)
        {
            float upper = node?.Upper ?? 500f;
            float lower = node?.Lower ?? 200f;
            if (upper < lower)
            {
                float temp = upper;
                upper = lower;
                lower = temp;
            }

            bool currentState = hysteresisStateByNode[nodeIndex];
            if (inputValue >= upper)
            {
                hysteresisStateKnownByNode[nodeIndex] = true;
                hysteresisStateByNode[nodeIndex] = true;
                return 1f;
            }

            if (inputValue <= lower)
            {
                hysteresisStateKnownByNode[nodeIndex] = true;
                hysteresisStateByNode[nodeIndex] = false;
                return 0f;
            }

            if (!hysteresisStateKnownByNode[nodeIndex])
            {
                hysteresisStateKnownByNode[nodeIndex] = true;
                hysteresisStateByNode[nodeIndex] = inputValue > lower;
            }

            return hysteresisStateByNode[nodeIndex] ? 1f : 0f;
        }

        private float EvaluateToggleNode(int nodeIndex, float inputValue)
        {
            bool current = IsRuntimeTrue(inputValue);
            bool previous = togglePrevInputByNode[nodeIndex];
            togglePrevInputByNode[nodeIndex] = current;
            if (current && !previous)
            {
                toggleStateByNode[nodeIndex] = !toggleStateByNode[nodeIndex];
            }

            return toggleStateByNode[nodeIndex] ? 1f : 0f;
        }

        private float EvaluatePulseShaperNode(int nodeIndex, float inputValue, RuntimeBlueprintNode node)
        {
            float holdSeconds = Mathf.Max(0.1f, node?.IntervalSeconds ?? 1f);
            bool inputActive = IsRuntimeTrue(inputValue);
            float remaining = pulseShaperRemainingByNode[nodeIndex];
            if (inputActive)
            {
                pulseShaperRemainingByNode[nodeIndex] = holdSeconds;
                return 1f;
            }

            if (remaining > 0f)
            {
                remaining -= runtimeEvalDt;
                if (remaining > 0f)
                {
                    pulseShaperRemainingByNode[nodeIndex] = remaining;
                    return 1f;
                }
            }

            pulseShaperRemainingByNode[nodeIndex] = 0f;
            return 0f;
        }

        private float EvaluateNumberChangedNode(int nodeIndex, float inputValue, int outputPortIndex)
        {
            if (numberChangeUpdatedGenerationByNode[nodeIndex] != runtimeEvalGeneration)
            {
                float previousValue = previousNumberValueByNode[nodeIndex];
                int flags = 0;
                if (previousNumberValueKnownByNode[nodeIndex])
                {
                    if (inputValue > previousValue + 0.0001f) flags |= 1;
                    else if (inputValue < previousValue - 0.0001f) flags |= 2;
                    if (flags != 0) flags |= 4;
                }

                previousNumberValueByNode[nodeIndex] = inputValue;
                previousNumberValueKnownByNode[nodeIndex] = true;
                numberChangeFlagsByNode[nodeIndex] = (byte)flags;
                numberChangeUpdatedGenerationByNode[nodeIndex] = runtimeEvalGeneration;
            }

            return outputPortIndex >= 0 && outputPortIndex < 3 &&
                   (numberChangeFlagsByNode[nodeIndex] & (1 << outputPortIndex)) != 0 ? 1f : 0f;
        }

        private float EvaluateMapRangeNode(float inputValue, RuntimeBlueprintNode node)
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

        private float EvaluateCounterNode(int nodeIndex, int depth)
        {
            float pulseValue = EvaluateRuntimeInputNumber(nodeIndex, 0, depth + 1);
            bool current = IsRuntimeTrue(pulseValue);
            bool previous = previousInputStateByNode[nodeIndex];
            float count = counterValueByNode[nodeIndex];
            float candidateCount = current && !previous ? count + 1f : count;
            // Let a Reset expression that feeds back through this counter compare against
            // the value this tick is about to publish, rather than the previous tick.
            PublishInProgressRuntimeOutput(nodeIndex, 0, candidateCount);
            bool resetActive = IsRuntimeTrue(EvaluateRuntimeInputNumber(nodeIndex, 1, depth + 1));

            previousInputStateByNode[nodeIndex] = current;
            if (resetActive)
            {
                counterValueByNode[nodeIndex] = 0f;
                return 0f;
            }

            counterValueByNode[nodeIndex] = candidateCount;
            return candidateCount;
        }

        private float EvaluateSequenceNode(int nodeIndex, int outputPortIndex, float advanceValue, float resetValue, RuntimeBlueprintNode node)
        {
            bool advanceActive = IsRuntimeTrue(advanceValue);
            bool prevAdvance = sequencePrevAdvanceByNode[nodeIndex];
            bool advanceEdge = advanceActive && !prevAdvance;
            sequencePrevAdvanceByNode[nodeIndex] = advanceActive;

            bool resetActive = IsRuntimeTrue(resetValue);
            bool prevReset = sequencePrevResetByNode[nodeIndex];
            bool resetEdge = resetActive && !prevReset;
            sequencePrevResetByNode[nodeIndex] = resetActive;

            int step = sequenceStepByNode[nodeIndex];

            if (resetEdge)
            {
                step = 0;
            }
            else if (advanceEdge)
            {
                step++;
            }

            int valuesLength = node?.Values != null && node.Values.Count > 0 ? node.Values.Count : 1;

            step = step % Mathf.Max(1, valuesLength);
            sequenceStepByNode[nodeIndex] = step;

            if (outputPortIndex == 1)
            {
                return step;
            }

            float stepValue = 0f;
            if (node?.Values != null && node.Values.Count > 0)
            {
                int clampedStep = Mathf.Clamp(step, 0, node.Values.Count - 1);
                stepValue = Mathf.Clamp(node.Values[clampedStep], 0f, 15f);
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

        private float EvaluateMerge4Node(int nodeIndex, int depth)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                float inputVal = EvaluateRuntimeInputNumber(nodeIndex, i, depth + 1);
                if (IsRuntimeTrue(inputVal))
                {
                    value |= 1 << i;
                }
            }
            return value;
        }

        private float EvaluatePixelScreenNode(int nodeIndex, int depth)
        {
            float a = EvaluateRuntimeInputNumber(nodeIndex, 0, depth + 1);
            return Mathf.Clamp(Mathf.FloorToInt(a), 0, 15);
        }

        private float EvaluateMaterialChangedNode(int nodeIndex)
        {
            RuntimeBlueprintNode node = compiledNodes[nodeIndex].BlueprintNode;
            if (!HasMaterialSelection(node))
            {
                previousMaterialAmountKnownByNode[nodeIndex] = false;
                previousMaterialAmountByNode[nodeIndex] = 0f;
                return 0f;
            }

            float amount = GetNetworkItemAmountKg(node);
            bool hadPrevious = previousMaterialAmountKnownByNode[nodeIndex];
            float previous = previousMaterialAmountByNode[nodeIndex];
            previousMaterialAmountKnownByNode[nodeIndex] = true;
            previousMaterialAmountByNode[nodeIndex] = amount;
            return hadPrevious && !Mathf.Approximately(previous, amount) ? 1f : 0f;
        }

        private float GetNetworkFillPercent()
        {
            StorageNetworkInventoryMetrics metrics = GetCurrentInventoryMetrics();
            if (!metrics.NetworkOnline || metrics.TotalCapacityKg <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(metrics.TotalStoredKg / metrics.TotalCapacityKg) * 100f;
        }

        private float GetNetworkStoredKg()
        {
            StorageNetworkInventoryMetrics metrics = GetCurrentInventoryMetrics();
            return metrics.NetworkOnline ? Mathf.Max(0f, metrics.TotalStoredKg) : 0f;
        }

        private float GetNetworkRemainingKg()
        {
            StorageNetworkInventoryMetrics metrics = GetCurrentInventoryMetrics();
            return metrics.NetworkOnline
                ? Mathf.Max(0f, metrics.TotalCapacityKg - metrics.TotalStoredKg)
                : 0f;
        }

        private float GetNetworkCapacityKg()
        {
            StorageNetworkInventoryMetrics metrics = GetCurrentInventoryMetrics();
            return metrics.NetworkOnline ? Mathf.Max(0f, metrics.TotalCapacityKg) : 0f;
        }

        private StorageNetworkInventoryMetrics GetCurrentInventoryMetrics()
        {
            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            return StorageNetworkInventoryIndexService.GetMetrics(worldId, true, allowStaleContent: true);
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

            int sourceWorldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            int targetWorldId = target.GetMyWorldId();
            if (sourceWorldId >= 0 && targetWorldId >= 0 && sourceWorldId != targetWorldId && !StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return 0f;
            }

            return ReadBuildingOutputSignal(target);
        }

        private int ReadBuildingOutputSignal(GameObject target)
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

        private bool TryReadSwitchLikeOutput(GameObject target, out int value)
        {
            value = 0;
            if (target == null)
            {
                return false;
            }

            switchLikeComponentBuffer.Clear();
            target.GetComponents(switchLikeComponentBuffer);
            for (int componentIndex = 0; componentIndex < switchLikeComponentBuffer.Count; componentIndex++)
            {
                Component component = switchLikeComponentBuffer[componentIndex];
                if (component == null)
                {
                    continue;
                }

                System.Type componentType = component.GetType();
                if (!switchLikeOutputGetterByType.TryGetValue(componentType, out Func<Component, bool> getter))
                {
                    getter = CreateSwitchLikeOutputGetter(componentType);
                    switchLikeOutputGetterByType[componentType] = getter;
                }

                if (getter == null)
                {
                    continue;
                }

                try
                {
                    value = getter(component) ? 1 : 0;
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        private static Func<Component, bool> CreateSwitchLikeOutputGetter(System.Type componentType)
        {
            PropertyInfo property = componentType?.GetProperty(
                "IsSwitchedOn",
                BindingFlags.Instance | BindingFlags.Public);
            MethodInfo getter = property?.GetGetMethod(false);
            if (getter == null || property.PropertyType != typeof(bool))
            {
                return null;
            }

            try
            {
                DynamicMethod method = new DynamicMethod(
                    "StorageNetworkLogicDiy_GetIsSwitchedOn",
                    typeof(bool),
                    new[] { typeof(Component) },
                    typeof(StorageNetworkLogicDiy),
                    true);
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Castclass, componentType);
                il.EmitCall(OpCodes.Callvirt, getter, null);
                il.Emit(OpCodes.Ret);
                return (Func<Component, bool>)method.CreateDelegate(typeof(Func<Component, bool>));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"StorageNetwork LogicDiy: switch output accessor unavailable for {componentType.FullName}: {ex.Message}");
                return null;
            }
        }

        private void UpdateRuntimeTimers(float dt)
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null)
            {
                Array.Clear(timerElapsedByNode, 0, timerElapsedByNode.Length);
                Array.Clear(timerPulseByNode, 0, timerPulseByNode.Length);
                Array.Clear(cycleIndexByNode, 0, cycleIndexByNode.Length);
                return;
            }

            for (int timerIndex = 0; timerIndex < compiledTimerNodeIndices.Length; timerIndex++)
            {
                int nodeIndex = compiledTimerNodeIndices[timerIndex];
                CompiledRuntimeNode compiledNode = compiledNodes[nodeIndex];
                RuntimeBlueprintNode node = compiledNode.BlueprintNode;
                timerPulseByNode[nodeIndex] = false;
                float interval = Mathf.Max(0.2f, node.IntervalSeconds > 0f ? node.IntervalSeconds : 5f);
                if (compiledNode.Module == RuntimeModuleKind.TimerPulse)
                {
                    float elapsed = timerElapsedByNode[nodeIndex] + Mathf.Max(0f, dt);

                    if (elapsed >= interval)
                    {
                        timerPulseByNode[nodeIndex] = true;
                        elapsed %= interval;
                    }

                    timerElapsedByNode[nodeIndex] = elapsed;
                    continue;
                }

                if (compiledNode.Module == RuntimeModuleKind.Cycle4)
                {
                    float elapsed = timerElapsedByNode[nodeIndex] + Mathf.Max(0f, dt);

                    while (elapsed >= interval)
                    {
                        cycleIndexByNode[nodeIndex] = (cycleIndexByNode[nodeIndex] + 1) % 4;
                        elapsed -= interval;
                    }

                    timerElapsedByNode[nodeIndex] = elapsed;
                }
            }
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
            return compiledConditionItemTagValid ? GetNetworkItemAmountKg() : 0f;
        }

        private float GetNetworkItemAmountKg()
        {
            return GetNetworkItemAmountKg(null);
        }

        private float GetNetworkItemAmountKg(RuntimeBlueprintNode node)
        {
            string itemKey = !string.IsNullOrEmpty(node?.SelectedMaterialKey)
                ? node.SelectedMaterialKey
                : ConditionItemKey;
            if (string.IsNullOrEmpty(itemKey))
            {
                return 0f;
            }

            int worldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            return StorageNetworkInventoryIndexService.GetMass(
                worldId,
                true,
                new Tag(itemKey),
                allowStaleContent: true);
        }

        private bool HasMaterialSelection(RuntimeBlueprintNode node)
        {
            return !string.IsNullOrEmpty(
                !string.IsNullOrEmpty(node?.SelectedMaterialKey)
                    ? node.SelectedMaterialKey
                    : ConditionItemKey);
        }

        private void CompileConditionItemTag()
        {
            compiledConditionItemTagValid = !string.IsNullOrEmpty(ConditionItemKey);
            compiledConditionItemTag = compiledConditionItemTagValid
                ? new Tag(ConditionItemKey)
                : Tag.Invalid;
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

        private void UpdateRemotePixelScreens()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null) return;

            if (remoteBindingsBlueprintVersion != compiledBlueprintVersion)
            {
                for (int remoteIndex = 0; remoteIndex < compiledRemoteNodeIndices.Length; remoteIndex++)
                {
                    int nodeIndex = compiledRemoteNodeIndices[remoteIndex];
                    UpdateRemotePixelScreenBinding(nodeIndex, compiledNodes[nodeIndex].BlueprintNode.SelectedBuildingInstanceId);
                }
                remoteBindingsBlueprintVersion = compiledBlueprintVersion;
            }

            for (int remoteIndex = 0; remoteIndex < compiledRemoteNodeIndices.Length; remoteIndex++)
            {
                int nodeIndex = compiledRemoteNodeIndices[remoteIndex];
                RuntimeBlueprintNode node = compiledNodes[nodeIndex].BlueprintNode;
                int value = 0;
                for (int bit = 0; bit < 4; bit++)
                {
                    if (IsRuntimeTrue(EvaluateRuntimeInputNumber(nodeIndex, bit, 0))) value |= 1 << bit;
                }

                SendRemotePixelScreenSignal(node, value);
            }
        }

        private void UpdateRemotePixelScreenBinding(int nodeIndex, int newTargetInstanceId)
        {
            int previousTargetInstanceId = remotePixelScreenTargetByNode[nodeIndex];
            if (previousTargetInstanceId == newTargetInstanceId) return;

            if (previousTargetInstanceId > 0 && CountRemotePixelScreenControllers(previousTargetInstanceId) == 0 &&
                StorageNetworkBuildingRegistry.TryGetBuilding(previousTargetInstanceId, out GameObject previousTarget))
            {
                WritePixelPackValue(previousTarget, 0);
            }

            remotePixelScreenTargetByNode[nodeIndex] = newTargetInstanceId > 0 ? newTargetInstanceId : 0;
        }

        private void SendRemotePixelScreenSignal(RuntimeBlueprintNode node, int value)
        {
            if (node.SelectedBuildingInstanceId <= 0 ||
                !StorageNetworkBuildingRegistry.TryGetBuilding(node.SelectedBuildingInstanceId, out GameObject target) ||
                target == null || target.GetComponent<PixelPack>() == null)
            {
                return;
            }

            int sourceWorldId = gameObject != null ? gameObject.GetMyWorldId() : -1;
            if (sourceWorldId >= 0 && target.GetMyWorldId() != sourceWorldId && !StorageSceneRegistry.IsCrossPlanetRelayOnline())
            {
                return;
            }

            value = Mathf.Clamp(value, 0, 15);
            WritePixelPackValue(target, value);
        }

        private void WritePixelPackValue(GameObject target, int value)
        {
            PixelPack pixelPack = target != null ? target.GetComponent<PixelPack>() : null;
            if (pixelPack == null || pixelPackLogicValue == null) return;
            value = Mathf.Clamp(value, 0, 15);
            int actualValue = pixelPackLogicValue(pixelPack);
            if (actualValue == value) return;

            pixelPackLogicChangeBuffer.portID = PixelPack.PORT_ID;
            pixelPackLogicChangeBuffer.prevValue = Mathf.Max(0, actualValue);
            pixelPackLogicChangeBuffer.newValue = value;
            if (pixelPackLogicValueChanged != null)
            {
                pixelPackLogicValueChanged(pixelPack, pixelPackLogicChangeBuffer);
                return;
            }

            if (pixelPackLogicValueChangedMethod != null)
            {
                pixelPackLogicInvokeArguments[0] = pixelPackLogicChangeBuffer;
                pixelPackLogicValueChangedMethod.Invoke(pixelPack, pixelPackLogicInvokeArguments);
                pixelPackLogicInvokeArguments[0] = null;
            }
        }

        private static Action<PixelPack, object> CreatePixelPackLogicValueChangedAccessor()
        {
            if (pixelPackLogicValueChangedMethod == null)
            {
                Debug.LogWarning("StorageNetwork LogicDiy: PixelPack signal method is unavailable; remote screen output is disabled.");
                return null;
            }

            try
            {
                return (Action<PixelPack, object>)Delegate.CreateDelegate(
                    typeof(Action<PixelPack, object>),
                    null,
                    pixelPackLogicValueChangedMethod);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"StorageNetwork LogicDiy: PixelPack accessor unavailable: {ex.Message}");
                return null;
            }
        }

        private static AccessTools.FieldRef<PixelPack, int> CreatePixelPackLogicValueAccessor()
        {
            try
            {
                return AccessTools.FieldRefAccess<PixelPack, int>("logicValue");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"StorageNetwork LogicDiy: PixelPack value accessor unavailable: {ex.Message}");
                return null;
            }
        }

        private string cachedForwarderName;
        private StatusItem instanceSignalForwardedStatusItem;

        private void UpdateNetworkSignalOutputStatus()
        {
            RuntimeBlueprint blueprint = TryGetRuntimeBlueprint();
            if (blueprint?.Nodes == null) return;

            string currentName = gameObject != null ? gameObject.GetProperName() : string.Empty;
            bool nameChanged = currentName != cachedForwarderName;
            bool missingStatus = false;
            for (int index = 0; index < compiledForwardingNodeIndices.Length; index++)
            {
                RuntimeBlueprintNode node = compiledNodes[compiledForwardingNodeIndices[index]].BlueprintNode;
                int targetId = node.SelectedBuildingInstanceId;
                if (targetId > 0 && !signalForwardStatusHandleByTarget.ContainsKey(targetId))
                {
                    missingStatus = true;
                    break;
                }
            }

            if (signalStatusBlueprintVersion == compiledBlueprintVersion &&
                !nameChanged &&
                !missingStatus)
            {
                return;
            }

            signalStatusBlueprintVersion = compiledBlueprintVersion;

            // Step 1: sync integer node → target mapping and collect active targets.
            signalForwardActiveTargets.Clear();
            for (int index = 0; index < compiledForwardingNodeIndices.Length; index++)
            {
                int nodeIndex = compiledForwardingNodeIndices[index];
                RuntimeBlueprintNode node = compiledNodes[nodeIndex].BlueprintNode;
                int targetId = node.SelectedBuildingInstanceId > 0 ? node.SelectedBuildingInstanceId : 0;
                networkSignalOutputTargetByNode[nodeIndex] = targetId;
                if (targetId > 0)
                {
                    signalForwardActiveTargets.Add(targetId);
                }
            }

            // Step 2: detect name change – if renamed, recreate StatusItem and refresh all.
            if (nameChanged)
            {
                cachedForwarderName = currentName;
                instanceSignalForwardedStatusItem = null;

                // Remove all existing handles; they'll be re-created with the new name
                signalForwardRemovalBuffer.Clear();
                foreach (int targetId in signalForwardStatusHandleByTarget.Keys)
                {
                    signalForwardRemovalBuffer.Add(targetId);
                }
                for (int index = 0; index < signalForwardRemovalBuffer.Count; index++)
                {
                    RemoveSignalForwardStatus(signalForwardRemovalBuffer[index]);
                }
            }

            // Step 3: remove status from targets no longer referenced.
            signalForwardRemovalBuffer.Clear();
            foreach (int targetId in signalForwardStatusHandleByTarget.Keys)
            {
                if (!signalForwardActiveTargets.Contains(targetId))
                {
                    signalForwardRemovalBuffer.Add(targetId);
                }
            }
            for (int index = 0; index < signalForwardRemovalBuffer.Count; index++)
            {
                RemoveSignalForwardStatus(signalForwardRemovalBuffer[index]);
            }

            // Step 4: add status to active targets that don't have one.
            foreach (int targetId in signalForwardActiveTargets)
            {
                if (!signalForwardStatusHandleByTarget.ContainsKey(targetId))
                {
                    AddSignalForwardStatus(targetId);
                }
            }
        }

        private void AddSignalForwardStatus(int targetInstanceId)
        {
            if (!StorageNetworkBuildingRegistry.TryGetBuilding(targetInstanceId, out GameObject target) ||
                target == null || target == gameObject)
                return;

            KSelectable selectable = target.GetComponent<KSelectable>();
            if (selectable == null) return;

            Guid handle = selectable.AddStatusItem(GetOrCreateSignalForwardedStatusItem(), this);
            if (handle == Guid.Empty) return;

            signalForwardStatusHandleByTarget[targetInstanceId] = handle;
        }

        private void RemoveSignalForwardStatus(int targetInstanceId)
        {
            if (!signalForwardStatusHandleByTarget.TryGetValue(targetInstanceId, out Guid handle)) return;

            if (StorageNetworkBuildingRegistry.TryGetBuilding(targetInstanceId, out GameObject target) &&
                target != null)
            {
                KSelectable selectable = target.GetComponent<KSelectable>();
                selectable?.RemoveStatusItem(handle);
            }

            signalForwardStatusHandleByTarget.Remove(targetInstanceId);
        }

        private void RemoveAllSignalForwardStatuses()
        {
            signalForwardRemovalBuffer.Clear();
            foreach (int targetId in signalForwardStatusHandleByTarget.Keys)
            {
                signalForwardRemovalBuffer.Add(targetId);
            }
            for (int index = 0; index < signalForwardRemovalBuffer.Count; index++)
            {
                RemoveSignalForwardStatus(signalForwardRemovalBuffer[index]);
            }

            signalForwardStatusHandleByTarget.Clear();
        }

        // Each forwarder instance creates its own StatusItem with a unique ID.
        // This allows multiple forwarders to each show their own status line on
        // the same target building. The forwarder name is baked into the text
        // directly so it always reflects the current name after a refresh.
        private StatusItem GetOrCreateSignalForwardedStatusItem()
        {
            if (instanceSignalForwardedStatusItem != null)
            {
                return instanceSignalForwardedStatusItem;
            }

            string forwarderName = gameObject != null ? gameObject.GetProperName() : string.Empty;
            string highlightedName = $"<b><color=#FFD54F>{forwarderName}</color></b>";

            string statusText = STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.SIGNAL_FORWARDED_STATUS)
                .Replace("{0}", highlightedName);
            string tooltipText = STRINGS.Get(STRINGS.UI.STORAGE_NETWORK.SIGNAL_FORWARDED_STATUS_TOOLTIP)
                .Replace("{0}", highlightedName);

            instanceSignalForwardedStatusItem = new StatusItem(
                "StorageNetworkSignalForwarded_" + GetInstanceID(),
                statusText,
                tooltipText,
                "status_item_check",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            return instanceSignalForwardedStatusItem;
        }

        private float EvaluateMusicSequencerNode(int nodeIndex, float playValue, float resetValue, RuntimeBlueprintNode node)
        {
            List<float> notes = node?.Values;
            if (notes == null || notes.Count == 0) return 0f;
            if (!IsRuntimeTrue(playValue))
            {
                musicStepStartedAtByNode[nodeIndex] = Time.time;
                musicStepStartedKnownByNode[nodeIndex] = true;
                return 0f;
            }
            bool reset = IsRuntimeTrue(resetValue);
            bool previousReset = musicPrevResetByNode[nodeIndex];
            bool resetEdge = reset && !previousReset;
            musicPrevResetByNode[nodeIndex] = reset;
            int step = musicStepByNode[nodeIndex];
            if (!musicStepKnownByNode[nodeIndex] || resetEdge)
            {
                step = 0;
                musicStepByNode[nodeIndex] = 0;
                musicStepKnownByNode[nodeIndex] = true;
                musicStepStartedAtByNode[nodeIndex] = Time.time;
                musicStepStartedKnownByNode[nodeIndex] = true;
            }
            float startedAt = musicStepStartedAtByNode[nodeIndex];
            if (!musicStepStartedKnownByNode[nodeIndex])
            {
                startedAt = Time.time;
                musicStepStartedAtByNode[nodeIndex] = startedAt;
                musicStepStartedKnownByNode[nodeIndex] = true;
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
                musicStepByNode[nodeIndex] = step;
                musicStepStartedAtByNode[nodeIndex] = Time.time;
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
            Array.Clear(latchStateByNode, 0, latchStateByNode.Length);
            Array.Clear(counterValueByNode, 0, counterValueByNode.Length);
            Array.Clear(sequenceStepByNode, 0, sequenceStepByNode.Length);
            Array.Clear(musicStepByNode, 0, musicStepByNode.Length);
            Array.Clear(musicStepKnownByNode, 0, musicStepKnownByNode.Length);
            Array.Clear(musicStepStartedAtByNode, 0, musicStepStartedAtByNode.Length);
            Array.Clear(musicStepStartedKnownByNode, 0, musicStepStartedKnownByNode.Length);
            Array.Clear(musicPrevResetByNode, 0, musicPrevResetByNode.Length);
            Array.Clear(hysteresisStateByNode, 0, hysteresisStateByNode.Length);
            Array.Clear(hysteresisStateKnownByNode, 0, hysteresisStateKnownByNode.Length);
            Array.Clear(toggleStateByNode, 0, toggleStateByNode.Length);
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
            CompileConditionItemTag();
            ConditionOutputChannel = OutputMode == ChannelMode.FourChannel ? Mathf.Clamp(ConditionOutputChannel, 0, 3) : 0;
            EvaluateWithForcedSnapshot();
            SendLogicSignal();
        }

        internal float GetSelectedMaterialAmountKgForWebEditor()
        {
            return compiledConditionItemTagValid ? GetNetworkItemAmountKg() : 0f;
        }

        internal Dictionary<string, float> GetRuntimeEvalSnapshot()
        {
            Dictionary<string, float> snapshot = new Dictionary<string, float>(runtimeEvalValues.Length);
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                CompiledRuntimeNode node = compiledNodes[nodeIndex];
                for (int outputPort = 0; outputPort < node.OutputCount; outputPort++)
                {
                    int slot = node.OutputOffset + outputPort;
                    if (runtimeEvalGenerationByOutput[slot] == runtimeEvalGeneration)
                    {
                        snapshot[node.OutputCacheKeys[outputPort]] = runtimeEvalValues[slot];
                    }
                }
            }

            return snapshot;
        }

        internal WebEditorNetworkMetrics GetWebEditorNetworkMetrics()
        {
            StorageNetworkInventoryMetrics storage = GetCurrentInventoryMetrics();
            StorageNetworkPowerSnapshot power = GetCurrentPowerSnapshot();
            return new WebEditorNetworkMetrics
            {
                TotalStoredKg = !storage.NetworkOnline ? 0f : storage.TotalStoredKg,
                TotalCapacityKg = !storage.NetworkOnline ? 0f : storage.TotalCapacityKg,
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
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId);
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
            bool crossWorld = StorageSceneRegistry.IsCrossPlanetRelayOnline();
            List<GameObject> buildings = StorageNetworkBuildingRegistry.GetBuildingsForWorld(crossWorld ? -1 : worldId);
            if (buildings.Count == 0)
            {
                StorageNetworkBuildingRegistry.RebuildFromScene();
                buildings = StorageNetworkBuildingRegistry.GetBuildingsForWorld(crossWorld ? -1 : worldId);
            }

            foreach (GameObject target in buildings)
            {
                if (target == null)
                {
                    continue;
                }
                if (target == gameObject) continue;

                KPrefabID prefabId = target.GetComponent<KPrefabID>();
                if (prefabId == null || prefabId.InstanceID == KPrefabID.InvalidInstanceID)
                {
                    continue;
                }

                bool hasLogicOutput = StorageNetworkBuildingRegistry.IsLogicOutputBuilding(target);
                bool isPixelScreen = target.GetComponent<PixelPack>() != null;
                if (!IsStorageNetworkModBuilding(target, prefabId) && !hasLogicOutput && !isPixelScreen)
                {
                    continue;
                }

                Operational operational = target.GetComponent<Operational>();
                int cell = Grid.PosToCell(target);
                Vector2I cellPosition = Grid.IsValidCell(cell) ? Grid.CellToXY(cell) : new Vector2I(-1, -1);
                LogicPorts targetLogicPorts = isPixelScreen ? target.GetComponent<LogicPorts>() : null;
                options.Add(new WebEditorBuildingOption
                {
                    InstanceId = prefabId.InstanceID,
                    Name = StripWebEditorRichText(target.GetProperName()),
                    Operational = operational == null || operational.IsOperational,
                    HasLogicOutput = hasLogicOutput,
                    SignalValue = hasLogicOutput ? ReadBuildingOutputSignal(target) : 0,
                    IsNetworkSignalOutput = target.GetComponent<StorageNetworkLogicDiy>() != null,
                    IsPixelScreen = isPixelScreen,
                    AutomationConnected = isPixelScreen && targetLogicPorts != null && targetLogicPorts.IsPortConnected(PixelPack.PORT_ID),
                    RemoteControllerCount = isPixelScreen ? CountRemotePixelScreenControllers(prefabId.InstanceID) : 0,
                    CellX = cellPosition.x,
                    CellY = cellPosition.y,
                    WorldId = target.GetMyWorldId()
                });
            }

            options.Sort((a, b) => string.Compare(a.Name, b.Name, System.StringComparison.CurrentCulture));
            return options;
        }

        private static int CountRemotePixelScreenControllers(int targetInstanceId)
        {
            int count = 0;
            foreach (GameObject building in StorageNetworkBuildingRegistry.GetBuildingsForWorld(-1))
            {
                StorageNetworkLogicDiy controller = building != null ? building.GetComponent<StorageNetworkLogicDiy>() : null;
                RuntimeBlueprint blueprint = controller?.TryGetRuntimeBlueprint();
                if (blueprint?.Nodes == null) continue;
                foreach (RuntimeBlueprintNode node in blueprint.Nodes)
                {
                    if (node != null && node.Module == "RemotePixelScreen" && node.SelectedBuildingInstanceId == targetInstanceId)
                    {
                        count++;
                    }
                }
            }
            return count;
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
            Array.Clear(runtimeEvalGenerationByOutput, 0, runtimeEvalGenerationByOutput.Length);
            Array.Clear(runtimeEvalStateByOutput, 0, runtimeEvalStateByOutput.Length);
            Array.Clear(runtimeEvalValues, 0, runtimeEvalValues.Length);
            Array.Clear(runtimeStableOutputSnapshot, 0, runtimeStableOutputSnapshot.Length);
            runtimeEvalGeneration = 0;
            runtimeEvaluationPassPrepared = false;

            // Transient timing state — rebuilt each tick.
            Array.Clear(timerElapsedByNode, 0, timerElapsedByNode.Length);
            Array.Clear(timerPulseByNode, 0, timerPulseByNode.Length);
            Array.Clear(cycleIndexByNode, 0, cycleIndexByNode.Length);

            // Edge-detection & previous-value tracking — reset to avoid stale edges.
            Array.Clear(previousInputStateByNode, 0, previousInputStateByNode.Length);
            Array.Clear(previousMaterialAmountByNode, 0, previousMaterialAmountByNode.Length);
            Array.Clear(previousMaterialAmountKnownByNode, 0, previousMaterialAmountKnownByNode.Length);
            Array.Clear(togglePrevInputByNode, 0, togglePrevInputByNode.Length);
            Array.Clear(sequencePrevAdvanceByNode, 0, sequencePrevAdvanceByNode.Length);
            Array.Clear(sequencePrevResetByNode, 0, sequencePrevResetByNode.Length);
            Array.Clear(previousNumberValueByNode, 0, previousNumberValueByNode.Length);
            Array.Clear(previousNumberValueKnownByNode, 0, previousNumberValueKnownByNode.Length);
            Array.Clear(numberChangeFlagsByNode, 0, numberChangeFlagsByNode.Length);
            Array.Clear(numberChangeUpdatedGenerationByNode, 0, numberChangeUpdatedGenerationByNode.Length);

            // Transient duration-based state — reset to avoid stale durations.
            Array.Clear(delayElapsedByNode, 0, delayElapsedByNode.Length);
            Array.Clear(pulseShaperRemainingByNode, 0, pulseShaperRemainingByNode.Length);

            // Stateful memory is intentionally preserved here. Loading/saving layout
            // and other non-topology edits must not reset a running circuit. Explicit
            // topology/parameter edits call ResetRuntimeStateForEditor instead.
        }

        private void BeginRuntimeEvaluationPass()
        {
            if (runtimeEvalGeneration == int.MaxValue)
            {
                Array.Clear(runtimeEvalGenerationByOutput, 0, runtimeEvalGenerationByOutput.Length);
                Array.Clear(numberChangeUpdatedGenerationByNode, 0, numberChangeUpdatedGenerationByNode.Length);
                runtimeEvalGeneration = 1;
                return;
            }

            runtimeEvalGeneration++;
            if (runtimeEvalGeneration == 0)
            {
                runtimeEvalGeneration = 1;
            }
        }

        private static CompiledRuntimeInput[] CreateDisconnectedInputs(int count)
        {
            return count > 0 ? new CompiledRuntimeInput[count] : Array.Empty<CompiledRuntimeInput>();
        }

        private int ResolveCompiledSourceNode(string nodeId)
        {
            if (nodeId == "system:material")
            {
                return MaterialSourceNodeIndex;
            }
            if (nodeId == "system:fixed")
            {
                return FixedSourceNodeIndex;
            }

            return !string.IsNullOrEmpty(nodeId) && compiledNodeIndexById.TryGetValue(nodeId, out int nodeIndex)
                ? nodeIndex
                : InvalidNodeIndex;
        }

        private CompiledRuntimeInput GetRuntimeInput(int nodeIndex, int portIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length || portIndex < 0)
            {
                return CompiledRuntimeInput.Disconnected;
            }

            CompiledRuntimeInput[] inputs = compiledNodes[nodeIndex].Inputs;
            return portIndex < inputs.Length ? inputs[portIndex] : CompiledRuntimeInput.Disconnected;
        }

        private static int GetRuntimeOutputSlot(CompiledRuntimeNode node, int outputPortIndex)
        {
            return node != null && outputPortIndex >= 0 && outputPortIndex < node.OutputCount
                ? node.OutputOffset + outputPortIndex
                : -1;
        }

        private void InvalidateRuntimeOutput(int nodeIndex, int outputPortIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length)
            {
                return;
            }

            int slot = GetRuntimeOutputSlot(compiledNodes[nodeIndex], outputPortIndex);
            if (slot >= 0)
            {
                runtimeEvalGenerationByOutput[slot] = 0;
                runtimeEvalStateByOutput[slot] = RuntimeEvalStateNone;
            }
        }

        private void PublishInProgressRuntimeOutput(int nodeIndex, int outputPortIndex, float value)
        {
            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length)
            {
                return;
            }

            int slot = GetRuntimeOutputSlot(compiledNodes[nodeIndex], outputPortIndex);
            if (slot >= 0)
            {
                runtimeEvalGenerationByOutput[slot] = runtimeEvalGeneration;
                runtimeEvalStateByOutput[slot] = RuntimeEvalStatePublished;
                runtimeEvalValues[slot] = value;
            }
        }

        private void SetRuntimeOutput(int nodeIndex, int outputPortIndex, float value)
        {
            if (nodeIndex < 0 || nodeIndex >= compiledNodes.Length)
            {
                return;
            }

            int slot = GetRuntimeOutputSlot(compiledNodes[nodeIndex], outputPortIndex);
            if (slot >= 0)
            {
                CompleteRuntimeOutput(slot, value);
            }
        }

        private void CompleteRuntimeOutput(int outputSlot, float value)
        {
            runtimeEvalGenerationByOutput[outputSlot] = runtimeEvalGeneration;
            runtimeEvalStateByOutput[outputSlot] = RuntimeEvalStateComplete;
            runtimeEvalValues[outputSlot] = value;
        }

        private int[] BuildSpecialNodeIndexArray(RuntimeSpecialNodeKind kind)
        {
            List<int> indices = new List<int>();
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                RuntimeModuleKind module = compiledNodes[nodeIndex].Module;
                bool include = kind switch
                {
                    RuntimeSpecialNodeKind.Timer => module == RuntimeModuleKind.TimerPulse || module == RuntimeModuleKind.Cycle4,
                    RuntimeSpecialNodeKind.Display => module != RuntimeModuleKind.Output && module != RuntimeModuleKind.Group,
                    RuntimeSpecialNodeKind.Counter => module == RuntimeModuleKind.Counter,
                    RuntimeSpecialNodeKind.Remote => module == RuntimeModuleKind.RemotePixelScreen,
                    RuntimeSpecialNodeKind.Forwarding => module == RuntimeModuleKind.NetworkSignalOutput || module == RuntimeModuleKind.BuildingSignal,
                    _ => false
                };
                if (include)
                {
                    indices.Add(nodeIndex);
                }
            }

            return indices.ToArray();
        }

        private void ClearRemovedRemotePixelScreenTargets(CompiledRuntimeNode[] oldNodes, int[] oldTargets)
        {
            if (oldNodes == null || oldTargets == null)
            {
                return;
            }

            int count = Math.Min(oldNodes.Length, oldTargets.Length);
            for (int oldIndex = 0; oldIndex < count; oldIndex++)
            {
                int oldTarget = oldTargets[oldIndex];
                if (oldTarget <= 0 || oldNodes[oldIndex]?.Module != RuntimeModuleKind.RemotePixelScreen)
                {
                    continue;
                }

                bool retained = false;
                for (int remoteIndex = 0; remoteIndex < compiledRemoteNodeIndices.Length; remoteIndex++)
                {
                    RuntimeBlueprintNode current = compiledNodes[compiledRemoteNodeIndices[remoteIndex]].BlueprintNode;
                    if (current.SelectedBuildingInstanceId == oldTarget)
                    {
                        retained = true;
                        break;
                    }
                }

                if (!retained && CountRemotePixelScreenControllers(oldTarget) == 0 &&
                    StorageNetworkBuildingRegistry.TryGetBuilding(oldTarget, out GameObject previousTarget))
                {
                    WritePixelPackValue(previousTarget, 0);
                }
            }
        }

        private void PrewarmCompiledBuildingSignalAccessors()
        {
            for (int nodeIndex = 0; nodeIndex < compiledNodes.Length; nodeIndex++)
            {
                RuntimeModuleKind module = compiledNodes[nodeIndex].Module;
                if (module != RuntimeModuleKind.BuildingSignal && module != RuntimeModuleKind.NetworkSignalOutput)
                {
                    continue;
                }

                int targetId = compiledNodes[nodeIndex].BlueprintNode.SelectedBuildingInstanceId;
                if (targetId > 0 && StorageNetworkBuildingRegistry.TryGetBuilding(targetId, out GameObject target) &&
                    target != null)
                {
                    TryReadSwitchLikeOutput(target, out _);
                }
            }
        }

        private static int GetDisplayOutputCount(RuntimeModuleKind module)
        {
            return module switch
            {
                RuntimeModuleKind.Cycle4 => 4,
                RuntimeModuleKind.Split4 => 4,
                RuntimeModuleKind.NumberChanged => 3,
                RuntimeModuleKind.Sequence => 2,
                _ => 1
            };
        }

        private static int GetRequiredInputCount(RuntimeModuleKind module, RuntimeBlueprintNode node)
        {
            switch (module)
            {
                case RuntimeModuleKind.Add:
                case RuntimeModuleKind.Subtract:
                case RuntimeModuleKind.Multiply:
                case RuntimeModuleKind.Divide:
                    return Mathf.Clamp(node?.InputCount > 0 ? node.InputCount : 2, 2, 10);
                case RuntimeModuleKind.Clamp:
                case RuntimeModuleKind.Range:
                case RuntimeModuleKind.Selector:
                    return 3;
                case RuntimeModuleKind.Merge4:
                case RuntimeModuleKind.RemotePixelScreen:
                    return 4;
                case RuntimeModuleKind.Select:
                    int portCount = node != null && node.Value > 1f ? Mathf.FloorToInt(node.Value) : 6;
                    return Mathf.Clamp(portCount + 1, 2, 64);
                default:
                    // The legacy evaluator eagerly reads inputs zero and one for every module.
                    return 2;
            }
        }

        private static bool IsMaterialRuntimeModule(RuntimeModuleKind module)
        {
            return module == RuntimeModuleKind.SystemMaterial ||
                   module == RuntimeModuleKind.MaterialCondition ||
                   module == RuntimeModuleKind.MaterialLow ||
                   module == RuntimeModuleKind.MaterialHigh ||
                   module == RuntimeModuleKind.MaterialChanged;
        }

        private static RuntimeModuleKind GetRuntimeModuleKind(string module)
        {
            switch (module)
            {
                case "Add": return RuntimeModuleKind.Add;
                case "Subtract": return RuntimeModuleKind.Subtract;
                case "Multiply": return RuntimeModuleKind.Multiply;
                case "Divide": return RuntimeModuleKind.Divide;
                case "Negate": return RuntimeModuleKind.Negate;
                case "Min": return RuntimeModuleKind.Min;
                case "Max": return RuntimeModuleKind.Max;
                case "Clamp": return RuntimeModuleKind.Clamp;
                case "Modulo": return RuntimeModuleKind.Modulo;
                case "GreaterThan": return RuntimeModuleKind.GreaterThan;
                case "Equal": return RuntimeModuleKind.Equal;
                case "LessThan": return RuntimeModuleKind.LessThan;
                case "Range": return RuntimeModuleKind.Range;
                case "Variable": return RuntimeModuleKind.Variable;
                case "Constant": return RuntimeModuleKind.Constant;
                case "TestSignal": return RuntimeModuleKind.TestSignal;
                case "BoolTrue": return RuntimeModuleKind.BoolTrue;
                case "BoolFalse": return RuntimeModuleKind.BoolFalse;
                case "BoolAnd": return RuntimeModuleKind.BoolAnd;
                case "BoolNand": return RuntimeModuleKind.BoolNand;
                case "BoolOr": return RuntimeModuleKind.BoolOr;
                case "BoolNor": return RuntimeModuleKind.BoolNor;
                case "BoolXor": return RuntimeModuleKind.BoolXor;
                case "BoolNot": return RuntimeModuleKind.BoolNot;
                case "Selector": return RuntimeModuleKind.Selector;
                case "Sequence": return RuntimeModuleKind.Sequence;
                case "MusicSequencer": return RuntimeModuleKind.MusicSequencer;
                case "Delay": return RuntimeModuleKind.Delay;
                case "Latch": return RuntimeModuleKind.Latch;
                case "EdgePulse": return RuntimeModuleKind.EdgePulse;
                case "Hysteresis": return RuntimeModuleKind.Hysteresis;
                case "Toggle": return RuntimeModuleKind.Toggle;
                case "PulseShaper": return RuntimeModuleKind.PulseShaper;
                case "NumberChanged": return RuntimeModuleKind.NumberChanged;
                case "MapRange": return RuntimeModuleKind.MapRange;
                case "Counter": return RuntimeModuleKind.Counter;
                case "RandomChance": return RuntimeModuleKind.RandomChance;
                case "TimerPulse": return RuntimeModuleKind.TimerPulse;
                case "Cycle4": return RuntimeModuleKind.Cycle4;
                case "MaterialCondition": return RuntimeModuleKind.MaterialCondition;
                case "MaterialLow": return RuntimeModuleKind.MaterialLow;
                case "MaterialHigh": return RuntimeModuleKind.MaterialHigh;
                case "MaterialChanged": return RuntimeModuleKind.MaterialChanged;
                case "InventoryPercent": return RuntimeModuleKind.InventoryPercent;
                case "InventoryStored": return RuntimeModuleKind.InventoryStored;
                case "InventoryRemaining": return RuntimeModuleKind.InventoryRemaining;
                case "InventoryCapacity": return RuntimeModuleKind.InventoryCapacity;
                case "PowerPercent": return RuntimeModuleKind.PowerPercent;
                case "PowerStored": return RuntimeModuleKind.PowerStored;
                case "PowerCapacity": return RuntimeModuleKind.PowerCapacity;
                case "PowerRemaining": return RuntimeModuleKind.PowerRemaining;
                case "BuildingStatus": return RuntimeModuleKind.BuildingStatus;
                case "BuildingSignal": return RuntimeModuleKind.BuildingSignal;
                case "NetworkSignalOutput": return RuntimeModuleKind.NetworkSignalOutput;
                case "Output": return RuntimeModuleKind.Output;
                case "Split4": return RuntimeModuleKind.Split4;
                case "Merge4": return RuntimeModuleKind.Merge4;
                case "Select": return RuntimeModuleKind.Select;
                case "PixelScreen": return RuntimeModuleKind.PixelScreen;
                case "RemotePixelScreen": return RuntimeModuleKind.RemotePixelScreen;
                case "Group": return RuntimeModuleKind.Group;
                default: return RuntimeModuleKind.Unknown;
            }
        }

        private static void CopyArrayValue<T>(T[] source, int sourceIndex, T[] destination, int destinationIndex)
        {
            if (source != null && sourceIndex >= 0 && sourceIndex < source.Length &&
                destination != null && destinationIndex >= 0 && destinationIndex < destination.Length)
            {
                destination[destinationIndex] = source[sourceIndex];
            }
        }

        private sealed class CompiledRuntimeNode
        {
            public string Id;
            public RuntimeBlueprintNode BlueprintNode;
            public RuntimeModuleKind Module;
            public CompiledRuntimeInput[] Inputs;
            public string[] OutputCacheKeys;
            public int RequiredInputCount;
            public int OutputOffset;
            public int OutputCount;
            public int DisplayOutputCount;
        }

        private readonly struct CompiledRuntimeInput
        {
            public static readonly CompiledRuntimeInput Disconnected = default;

            public CompiledRuntimeInput(int sourceNodeIndex, int sourcePortIndex, bool isConnected)
            {
                SourceNodeIndex = sourceNodeIndex;
                SourcePortIndex = sourcePortIndex;
                IsConnected = isConnected;
            }

            public int SourceNodeIndex { get; }
            public int SourcePortIndex { get; }
            public bool IsConnected { get; }
        }

        private enum RuntimeSpecialNodeKind
        {
            Timer,
            Display,
            Counter,
            Remote,
            Forwarding
        }

        private enum RuntimeModuleKind
        {
            Unknown,
            SystemMaterial,
            SystemFixed,
            Add,
            Subtract,
            Multiply,
            Divide,
            Negate,
            Min,
            Max,
            Clamp,
            Modulo,
            GreaterThan,
            Equal,
            LessThan,
            Range,
            Variable,
            Constant,
            TestSignal,
            BoolTrue,
            BoolFalse,
            BoolAnd,
            BoolNand,
            BoolOr,
            BoolNor,
            BoolXor,
            BoolNot,
            Selector,
            Sequence,
            MusicSequencer,
            Delay,
            Latch,
            EdgePulse,
            Hysteresis,
            Toggle,
            PulseShaper,
            NumberChanged,
            MapRange,
            Counter,
            RandomChance,
            TimerPulse,
            Cycle4,
            MaterialCondition,
            MaterialLow,
            MaterialHigh,
            MaterialChanged,
            InventoryPercent,
            InventoryStored,
            InventoryRemaining,
            InventoryCapacity,
            PowerPercent,
            PowerStored,
            PowerCapacity,
            PowerRemaining,
            BuildingStatus,
            BuildingSignal,
            NetworkSignalOutput,
            Output,
            Split4,
            Merge4,
            Select,
            PixelScreen,
            RemotePixelScreen,
            Group
        }

        private const int InvalidNodeIndex = -1;
        private const int MaterialSourceNodeIndex = -2;
        private const int FixedSourceNodeIndex = -3;
        private const byte RuntimeEvalStateNone = 0;
        private const byte RuntimeEvalStateEvaluating = 1;
        private const byte RuntimeEvalStatePublished = 2;
        private const byte RuntimeEvalStateComplete = 3;

        private enum RuntimeAggregateOperation
        {
            Add,
            Subtract,
            Multiply,
            Divide
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
            public bool IsNetworkSignalOutput { get; set; }
            public bool IsPixelScreen { get; set; }
            public bool AutomationConnected { get; set; }
            public int RemoteControllerCount { get; set; }
            public int CellX { get; set; }
            public int CellY { get; set; }
            public int WorldId { get; set; }
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

            public string SelectedMaterialKey { get; set; }

            public float IntervalSeconds { get; set; }

            public float Value { get; set; }

            public int InputCount { get; set; }

            public List<float> InputValues { get; set; }

            public int SelectedBuildingInstanceId { get; set; }

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
