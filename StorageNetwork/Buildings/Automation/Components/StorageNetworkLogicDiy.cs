using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkLogicDiy : KMonoBehaviour, ILogicEventSender, ISim1000ms
    {
        public static readonly HashedString PORT_ID = "StorageNetworkLogicDiyOutput";

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

        private LogicPortVisualizer outputVisualizer;

        public CellOffset OutputPortOffset { get; set; }

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

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ConditionThresholdKg = Mathf.Max(0f, ConditionThresholdKg);
            ConditionOutputChannel = Mathf.Clamp(ConditionOutputChannel, 0, 3);
            ClampOutputValue();
            Connect();
            EvaluateConditionOutput();
        }

        protected override void OnCleanUp()
        {
            Disconnect();
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
                TriggerLogicValueChanged(previousValue);
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
                TriggerLogicValueChanged(previousValue);
            }
            else
            {
                TriggerLogicValueChanged(OutputSignalValue);
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

        public void Sim1000ms(float dt)
        {
            EvaluateConditionOutput();
        }

        public int GetLogicCell()
        {
            return GetOutputCell();
        }

        public int GetLogicValue()
        {
            return OutputSignalValue;
        }

        public void OnLogicNetworkConnectionChanged(bool connected)
        {
        }

        private void Connect()
        {
            int outputCell = GetOutputCell();
            Game.Instance.logicCircuitSystem.AddToNetworks(outputCell, this, true);
            outputVisualizer = new LogicPortVisualizer(outputCell, LogicPortSpriteType.Output);
            Game.Instance.logicCircuitManager.AddVisElem(outputVisualizer);
        }

        private void Disconnect()
        {
            if (Game.Instance != null)
            {
                Game.Instance.logicCircuitSystem.RemoveFromNetworks(GetOutputCell(), this, true);
                if (outputVisualizer != null)
                {
                    Game.Instance.logicCircuitManager.RemoveVisElem(outputVisualizer);
                }
            }

            outputVisualizer = null;
        }

        private int GetOutputCell()
        {
            CellOffset offset = OutputPortOffset;
            Rotatable rotatable = GetComponent<Rotatable>();
            if (rotatable != null)
            {
                offset = rotatable.GetRotatedCellOffset(offset);
            }

            return Grid.OffsetCell(Grid.PosToCell(transform.GetPosition()), offset);
        }

        private void TriggerLogicValueChanged(int previousValue)
        {
            LogicValueChanged changed = LogicValueChanged.Pool.Get();
            changed.portID = PORT_ID;
            changed.newValue = OutputSignalValue;
            changed.prevValue = previousValue;
            gameObject.Trigger(-801688580, changed);
            LogicValueChanged.Pool.Release(changed);
        }

        private void EvaluateConditionOutput()
        {
            if (OutputSourceMode != SourceMode.MaterialCondition || string.IsNullOrEmpty(ConditionItemKey))
            {
                return;
            }

            float amountKg = GetNetworkItemAmountKg(ConditionItemKey);
            bool conditionMet = ConditionComparison == ComparisonMode.GreaterOrEqual
                ? amountKg >= ConditionThresholdKg
                : amountKg < ConditionThresholdKg;

            int newValue = BuildConditionOutputValue(conditionMet);
            SetSignalValue(newValue);
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
            StorageSceneSnapshot snapshot = StorageSceneCollector.CollectForWorld(worldId);
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
            return OutputMode == ChannelMode.FourChannel
                ? Mathf.Clamp(value, 0, 15)
                : Mathf.Clamp(value, 0, 1);
        }
    }
}
