using StorageNetwork.Core;
using UnityEngine;
using STRINGS;

namespace StorageNetwork.Components
{
#pragma warning disable CS0649
    public sealed class StorageNetworkServerHeat : KMonoBehaviour, ISim200ms
    {
        private HandleVector<int>.Handle structureTemperature;

        [MyCmpGet]
        private Operational operational;

        [MyCmpGet]
        private Storage storage;

        [MyCmpGet]
        private StorageNetworkCore core;

        [MyCmpGet]
        private Building building;

        private float selfHeatKilowatts;
        private bool activeStateInitialized;
        private bool activeState;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            structureTemperature = GameComps.StructureTemperatures.GetHandle(gameObject);
            selfHeatKilowatts = building?.Def != null
                ? Mathf.Max(0f, building.Def.SelfHeatKilowattsWhenActive)
                : 0f;
        }

        public void Sim200ms(float dt)
        {
            bool online = IsOnline();
            SetActive(online);
            if (!online)
            {
                return;
            }

            if (selfHeatKilowatts <= 0f)
            {
                return;
            }

            GameComps.StructureTemperatures.ProduceEnergy(
                structureTemperature,
                selfHeatKilowatts * dt,
                BUILDING.STATUSITEMS.OPERATINGENERGY.FOOD_TRANSFER,
                dt);
        }

        private void SetActive(bool active)
        {
            if (activeStateInitialized && activeState == active)
            {
                return;
            }

            activeStateInitialized = true;
            activeState = active;
            operational?.SetActive(active, false);
        }

        private bool IsOnline()
        {
            if (storage != null)
            {
                return StorageNetworkStorageRules.IsConnectedNetworkStorage(storage);
            }

            if (core != null)
            {
                return operational == null || operational.IsOperational;
            }

            return false;
        }
    }
#pragma warning restore CS0649
}
