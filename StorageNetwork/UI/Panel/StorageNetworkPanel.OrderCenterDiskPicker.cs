using System.Collections.Generic;
using StorageNetwork.Buildings;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        public static void ShowOrderCenterDiskPicker(StorageNetworkOrderProductionCenter center, int slotIndex)
        {
            if (center == null)
            {
                return;
            }

            List<ProductionPickerOption> options = BuildOrderCenterDiskPickerOptions(center, slotIndex);
            ShowStandaloneOutputFilterPicker(
                Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_TITLE),
                options,
                string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_COUNT), Mathf.Max(0, options.Count - 1)),
                Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_HINT));
        }

        private static List<ProductionPickerOption> BuildOrderCenterDiskPickerOptions(StorageNetworkOrderProductionCenter center, int slotIndex)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_ANY),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_ANY_DESC),
                    false,
                    null)
            };

            foreach (StorageNetworkEngravingDisk disk in StorageNetworkOrderProductionCenter.FindAvailableDisks())
            {
                StorageNetworkEngravingDisk capturedDisk = disk;
                options.Add(new ProductionPickerOption(
                    capturedDisk.GetProperName(),
                    BuildDiskPickerDetails(center, capturedDisk),
                    false,
                    () =>
                    {
                        if (center != null && center.QueueDiskInstall(slotIndex, capturedDisk))
                        {
                            StorageNetworkNotifications.ShowSuccess(center.gameObject, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_ASSIGNED));
                            CloseStandaloneOutputFilterPicker();
                        }
                        else
                        {
                            StorageNetworkNotifications.ShowWarning(center != null ? center.gameObject : null, Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_ASSIGN_FAILED));
                            ShowOrderCenterDiskPicker(center, slotIndex);
                        }
                    },
                    StorageNetworkEngravingDiskConfig.ID,
                    BuildDiskPickerTooltip(capturedDisk)));
            }

            return options;
        }

        private static string BuildDiskPickerDetails(StorageNetworkOrderProductionCenter center, StorageNetworkEngravingDisk disk)
        {
            if (center == null || disk == null)
            {
                return string.Empty;
            }

            string summary = disk.IsBlank ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK) : disk.GetRecipeSummary(2);
            Pickupable pickupable = disk.GetComponent<Pickupable>();
            if (pickupable != null && pickupable.storage != null)
            {
                string storageName = pickupable.storage.gameObject != null ? pickupable.storage.gameObject.GetProperName() : Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_STORAGE_FALLBACK);
                return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_DETAIL_STORAGE), summary, storageName);
            }

            float distance = Vector3.Distance(center.transform.GetPosition(), disk.transform.GetPosition());
            return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_PICKER_DETAIL_DISTANCE), summary, distance);
        }

        private static string BuildDiskPickerTooltip(StorageNetworkEngravingDisk disk)
        {
            if (disk == null)
            {
                return string.Empty;
            }

            return disk.IsBlank
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK)
                : disk.GetCompactRecipeDetails();
        }
    }
}
