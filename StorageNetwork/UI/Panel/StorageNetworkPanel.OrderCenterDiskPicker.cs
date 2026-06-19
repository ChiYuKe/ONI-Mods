using System.Collections.Generic;
using StorageNetwork.Buildings;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;

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
                "选择刻录盘",
                options,
                string.Format("可选刻录盘：{0} 个", Mathf.Max(0, options.Count - 1)),
                "选择一个刻录盘后，复制人会前往订单生产中心完成装盘；只有完成任务后刻录盘才会放入对应槽位。");
        }

        private static List<ProductionPickerOption> BuildOrderCenterDiskPickerOptions(StorageNetworkOrderProductionCenter center, int slotIndex)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    "任意刻录盘",
                    "选择当前可用刻录盘列表中的一张盘",
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
                            StorageNetworkNotifications.ShowInfo("已安排复制人装盘。");
                            CloseStandaloneOutputFilterPicker();
                        }
                        else
                        {
                            StorageNetworkNotifications.ShowWarning("无法安排装盘，目标槽位或刻录盘已不可用。");
                            ShowOrderCenterDiskPicker(center, slotIndex);
                        }
                    },
                    StorageNetworkEngravingDiskConfig.ID));
            }

            return options;
        }

        private static string BuildDiskPickerDetails(StorageNetworkOrderProductionCenter center, StorageNetworkEngravingDisk disk)
        {
            if (center == null || disk == null)
            {
                return string.Empty;
            }

            string summary = disk.IsBlank ? "空白刻录盘" : disk.GetRecipeSummary(2);
            Pickupable pickupable = disk.GetComponent<Pickupable>();
            if (pickupable != null && pickupable.storage != null)
            {
                string storageName = pickupable.storage.gameObject != null ? pickupable.storage.gameObject.GetProperName() : "储存网络";
                return string.Format("{0}  ·  {1}", summary, storageName);
            }

            float distance = Vector3.Distance(center.transform.GetPosition(), disk.transform.GetPosition());
            return string.Format("{0}  ·  距离 {1:0.0}", summary, distance);
        }
    }
}
