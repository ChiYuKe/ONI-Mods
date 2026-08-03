using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal enum StorageNetworkReservationKind
    {
        SolidInput,
        LiquidInput,
        GasInput,
        SolidOutput,
        LiquidOutput,
        GasOutput
    }

    internal sealed class InputTargetReservation
    {
        private readonly StorageNetworkReservationKind kind;
        private string displayName;

        public InputTargetReservation(
            Storage inputStorage,
            Storage targetStorage,
            string inputTypeName,
            StorageNetworkReservationKind kind)
        {
            InputStorage = inputStorage;
            InputObject = inputStorage != null ? inputStorage.gameObject : null;
            TargetStorage = targetStorage;
            InputTypeName = inputTypeName;
            this.kind = kind;
        }

        public Storage InputStorage { get; }
        public Storage PortStorage => InputStorage;
        public GameObject InputObject { get; }
        public GameObject PortObject => InputObject;
        public Storage TargetStorage { get; }
        public Storage ServerStorage => TargetStorage;
        public string InputTypeName { get; }
        internal StorageNetworkReservationKind Kind => kind;
        public string PortTypeName => InputTypeName;
        public string DisplayName
        {
            get
            {
                if (displayName == null)
                {
                    string properName =
                        InputStorage != null ? InputStorage.GetProperName() : InputTypeName;
                    displayName = string.IsNullOrEmpty(properName)
                        ? InputTypeName
                        : string.Format("{0} - {1}", InputTypeName, properName);
                }

                return displayName;
            }
        }

        public bool Clear()
        {
            if (InputObject == null)
            {
                return false;
            }

            switch (kind)
            {
                case StorageNetworkReservationKind.SolidInput:
                    StorageNetworkSolidInputPortIngress solidInput =
                        InputObject.GetComponent<StorageNetworkSolidInputPortIngress>();
                    if (!StorageSceneRegistry.IsLive(solidInput)) return false;
                    solidInput.UseAutomaticInputStorage();
                    return true;
                case StorageNetworkReservationKind.LiquidInput:
                    StorageNetworkLiquidInputPortIngress liquidInput =
                        InputObject.GetComponent<StorageNetworkLiquidInputPortIngress>();
                    if (!StorageSceneRegistry.IsLive(liquidInput)) return false;
                    liquidInput.UseAutomaticInputStorage();
                    return true;
                case StorageNetworkReservationKind.GasInput:
                    StorageNetworkGasInputPortIngress gasInput =
                        InputObject.GetComponent<StorageNetworkGasInputPortIngress>();
                    if (!StorageSceneRegistry.IsLive(gasInput)) return false;
                    gasInput.UseAutomaticInputStorage();
                    return true;
                case StorageNetworkReservationKind.SolidOutput:
                    StorageNetworkSolidOutputPortEgress solidOutput =
                        InputObject.GetComponent<StorageNetworkSolidOutputPortEgress>();
                    if (!StorageSceneRegistry.IsLive(solidOutput)) return false;
                    solidOutput.UseAutomaticSourceStorage();
                    return true;
                case StorageNetworkReservationKind.LiquidOutput:
                    StorageNetworkLiquidOutputPortEgress liquidOutput =
                        InputObject.GetComponent<StorageNetworkLiquidOutputPortEgress>();
                    if (!StorageSceneRegistry.IsLive(liquidOutput)) return false;
                    liquidOutput.UseAutomaticSourceStorage();
                    return true;
                case StorageNetworkReservationKind.GasOutput:
                    StorageNetworkGasOutputPortEgress gasOutput =
                        InputObject.GetComponent<StorageNetworkGasOutputPortEgress>();
                    if (!StorageSceneRegistry.IsLive(gasOutput)) return false;
                    gasOutput.UseAutomaticSourceStorage();
                    return true;
                default:
                    return false;
            }
        }
    }
}
