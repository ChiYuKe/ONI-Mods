using System;
using System.Collections.Generic;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkPortStatusSilencer : KMonoBehaviour, ISim1000ms
    {
        [MyCmpReq]
        private KSelectable selectable;

        private StatusItemGroup statusItems;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            statusItems = selectable.GetStatusItemGroup();
            statusItems.OnAddStatusItem += OnAddStatusItem;
            RemoveSuppressedStatusItems();
        }

        protected override void OnCleanUp()
        {
            if (statusItems != null)
            {
                statusItems.OnAddStatusItem -= OnAddStatusItem;
            }

            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RemoveSuppressedStatusItems();
        }

        private void OnAddStatusItem(StatusItemGroup.Entry entry, StatusItemCategory category)
        {
            if (IsSuppressedStatusItem(entry.item))
            {
                selectable.RemoveStatusItem(entry.id, true);
            }
        }

        private void RemoveSuppressedStatusItems()
        {
            if (statusItems == null)
            {
                return;
            }

            List<Guid> suppressedIds = null;
            foreach (StatusItemGroup.Entry entry in statusItems)
            {
                if (!IsSuppressedStatusItem(entry.item))
                {
                    continue;
                }

                if (suppressedIds == null)
                {
                    suppressedIds = new List<Guid>();
                }

                suppressedIds.Add(entry.id);
            }

            if (suppressedIds == null)
            {
                return;
            }

            foreach (Guid id in suppressedIds)
            {
                selectable.RemoveStatusItem(id, true);
            }
        }

        private static bool IsSuppressedStatusItem(StatusItem item)
        {
            if (item == null || Db.Get() == null)
            {
                return false;
            }

            var buildingStatusItems = Db.Get().BuildingStatusItems;
            return item == buildingStatusItems.NeedGasOut ||
                   item == buildingStatusItems.NeedLiquidOut ||
                   item == buildingStatusItems.NeedSolidOut ||
                   item == buildingStatusItems.NeedGasIn ||
                   item == buildingStatusItems.NeedLiquidIn ||
                   item == buildingStatusItems.GasPipeEmpty ||
                   item == buildingStatusItems.LiquidPipeEmpty ||
                   item == buildingStatusItems.ConduitBlockedMultiples ||
                   item == buildingStatusItems.SolidConduitBlockedMultiples ||
                   item == buildingStatusItems.NoStorageFilterSet;
        }
    }
}
