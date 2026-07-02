using System;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal sealed class InputTargetReservation
    {
        private readonly Func<bool> clear;

        public InputTargetReservation(
            Storage inputStorage,
            GameObject inputObject,
            Storage targetStorage,
            string inputTypeName,
            string displayName,
            Func<bool> clear)
        {
            InputStorage = inputStorage;
            InputObject = inputObject;
            TargetStorage = targetStorage;
            InputTypeName = inputTypeName;
            DisplayName = displayName;
            this.clear = clear;
        }

        public Storage InputStorage { get; }
        public Storage PortStorage => InputStorage;
        public GameObject InputObject { get; }
        public GameObject PortObject => InputObject;
        public Storage TargetStorage { get; }
        public Storage ServerStorage => TargetStorage;
        public string InputTypeName { get; }
        public string PortTypeName => InputTypeName;
        public string DisplayName { get; }

        public bool Clear()
        {
            return clear != null && clear();
        }
    }
}
