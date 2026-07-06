using KSerialization;
using UnityEngine;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkLogicDiy : KMonoBehaviour, ILogicEventSender
    {
        public static readonly HashedString PORT_ID = "StorageNetworkLogicDiyOutput";

        public enum ChannelMode
        {
            SingleChannel = 0,
            FourChannel = 1
        }

        [Serialize]
        public int OutputModeValue;

        [Serialize]
        public int OutputSignalValue;

        private LogicPortVisualizer outputVisualizer;

        public CellOffset OutputPortOffset { get; set; }

        public ChannelMode OutputMode
        {
            get => (ChannelMode)Mathf.Clamp(OutputModeValue, 0, 1);
            set => SetOutputMode(value);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            ClampOutputValue();
            Connect();
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

        public void LogicTick()
        {
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
