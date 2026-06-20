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

        protected override void OnSpawn()
        {
            base.OnSpawn();
            structureTemperature = GameComps.StructureTemperatures.GetHandle(gameObject);
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            bool online = IsOnline();
            SetActive(online);
            if (!online)
            {
                return;
            }

            float heatKW = GetSelfHeatKilowatts();
            if (heatKW <= 0f)
            {
                return;
            }

            GameComps.StructureTemperatures.ProduceEnergy(
                structureTemperature,
                heatKW * dt,
                BUILDING.STATUSITEMS.OPERATINGENERGY.FOOD_TRANSFER,
                dt);
        }

        private void SetActive(bool active)
        {
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

        private float GetSelfHeatKilowatts()
        {
            Building building = GetComponent<Building>();
            return building?.Def != null ? Mathf.Max(0f, building.Def.SelfHeatKilowattsWhenActive) : 0f;
        }
    }
#pragma warning restore CS0649
}
