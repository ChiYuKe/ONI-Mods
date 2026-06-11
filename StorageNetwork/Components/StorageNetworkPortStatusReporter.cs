using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkPortStatusReporter : KMonoBehaviour, ISim1000ms
    {
        private static StatusItem portStatusItem;
        private static StatusItem cacheStatusItem;
        private static StatusItem automationStatusItem;
        private static StatusItem manualStatusItem;

        private KSelectable selectable;
        private StorageNetworkPort port;
        private Storage mainStorage;
        private StorageNetworkStorageConnector connector;
        private StorageNetworkPortRequester requester;
        private StorageNetworkPortPickupBufferStorage pickupBuffer;
        private Guid portStatusHandle;
        private Guid cacheStatusHandle;
        private Guid automationStatusHandle;
        private Guid manualStatusHandle;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureComponents();
            RefreshStatusItem();
        }

        protected override void OnCleanUp()
        {
            RemoveStatusItem();
            base.OnCleanUp();
        }

        public void Sim1000ms(float dt)
        {
            RefreshStatusItem();
        }

        private void RefreshStatusItem()
        {
            EnsureComponents();
            if (selectable == null || port == null)
            {
                return;
            }

            StatusItem statusItem = GetPortStatusItem();
            AddStatusItemIfMissing(ref portStatusHandle, statusItem);
            AddStatusItemIfMissing(ref cacheStatusHandle, GetCacheStatusItem());

            if (connector != null || requester != null)
            {
                AddStatusItemIfMissing(ref automationStatusHandle, GetAutomationStatusItem());
            }
            else
            {
                RemoveStatusItem(ref automationStatusHandle);
            }

            if (port.SupportsManualDuplicantOperation)
            {
                AddStatusItemIfMissing(ref manualStatusHandle, GetManualStatusItem());
            }
            else
            {
                RemoveStatusItem(ref manualStatusHandle);
            }
        }

        private void RemoveStatusItem()
        {
            RemoveStatusItem(ref portStatusHandle);
            RemoveStatusItem(ref cacheStatusHandle);
            RemoveStatusItem(ref automationStatusHandle);
            RemoveStatusItem(ref manualStatusHandle);
        }

        private void AddStatusItemIfMissing(ref Guid handle, StatusItem statusItem)
        {
            if (handle != Guid.Empty || selectable == null || statusItem == null)
            {
                return;
            }

            handle = selectable.AddStatusItem(statusItem, this);
        }

        private void RemoveStatusItem(ref Guid handle)
        {
            if (handle == Guid.Empty)
            {
                return;
            }

            selectable?.RemoveStatusItem(handle);
            handle = Guid.Empty;
        }

        private void EnsureComponents()
        {
            selectable ??= GetComponent<KSelectable>();
            port ??= GetComponent<StorageNetworkPort>();
            mainStorage ??= StorageNetworkPortPickupBufferStorage.FindMainStorage(gameObject);
            connector ??= GetComponent<StorageNetworkStorageConnector>();
            requester ??= GetComponent<StorageNetworkPortRequester>();
            pickupBuffer ??= GetComponent<StorageNetworkPortPickupBufferStorage>();
        }

        private static StatusItem GetPortStatusItem()
        {
            if (portStatusItem != null)
            {
                return portStatusItem;
            }

            portStatusItem = new StatusItem(
                "StorageNetworkPortStatus",
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ITEM_NAME),
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ITEM_TOOLTIP),
                "status_item_check",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.None.ID,
                129022,
                false);
            portStatusItem.resolveStringCallback = (name, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.BuildName() : name;
            };
            portStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.BuildTooltip() : tooltip;
            };
            return portStatusItem;
        }

        private static StatusItem GetCacheStatusItem()
        {
            if (cacheStatusItem != null)
            {
                return cacheStatusItem;
            }

            cacheStatusItem = CreatePortStatusItem("StorageNetworkPortCacheStatus");
            cacheStatusItem.resolveStringCallback = (name, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.GetCacheLine() : name;
            };
            cacheStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.BuildTooltip() : tooltip;
            };
            return cacheStatusItem;
        }

        private static StatusItem GetAutomationStatusItem()
        {
            if (automationStatusItem != null)
            {
                return automationStatusItem;
            }

            automationStatusItem = CreatePortStatusItem("StorageNetworkPortAutomationStatus");
            automationStatusItem.resolveStringCallback = (name, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.GetAutomationSummaryLine() : name;
            };
            automationStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.BuildTooltip() : tooltip;
            };
            return automationStatusItem;
        }

        private static StatusItem GetManualStatusItem()
        {
            if (manualStatusItem != null)
            {
                return manualStatusItem;
            }

            manualStatusItem = CreatePortStatusItem("StorageNetworkPortManualStatus");
            manualStatusItem.resolveStringCallback = (name, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.GetManualLine() : name;
            };
            manualStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkPortStatusReporter reporter = data as StorageNetworkPortStatusReporter;
                return reporter != null ? reporter.BuildTooltip() : tooltip;
            };
            return manualStatusItem;
        }

        private static StatusItem CreatePortStatusItem(string id)
        {
            return new StatusItem(
                id,
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ITEM_NAME),
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ITEM_TOOLTIP),
                "status_item_check",
                StatusItem.IconType.Info,
                NotificationType.Neutral,
                false,
                OverlayModes.None.ID,
                129022,
                false);
        }

        private string BuildName()
        {
            EnsureComponents();
            string online = StorageSceneRegistry.HasOnlineCoreInWorld(StorageTargetSelector.GetObjectWorldId(gameObject))
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_ONLINE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE);
            return string.Format(
                "{0}：{1}  {2}",
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ITEM_NAME),
                online,
                GetDirectionText());
        }

        private string BuildTooltip()
        {
            EnsureComponents();
            StringBuilder builder = new StringBuilder();
            AppendLine(builder, GetNetworkLine());
            AppendLine(builder, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_DIRECTION), GetDirectionText()));
            AppendLine(builder, GetCacheLine());
            AppendLine(builder, string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_REMAINING),
                GetRemainingCapacityText()));
            AppendLine(builder, GetFilterLine());

            if (port != null && port.SupportsManualDuplicantOperation)
            {
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL),
                    port.IsManualDuplicantOperationAllowed()
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_ALLOWED)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_FORBIDDEN)));
            }

            if (connector != null)
            {
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_INPUT_ENABLED),
                    connector.IsOutputStoreEnabled()
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.ON)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.OFF)));
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_POLICY),
                    GetOutputStoreModeName(connector)));
            }

            if (requester != null)
            {
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_OUTPUT_ENABLED),
                    requester.RequestEnabled
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.ON)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.OFF)));
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SOURCE_POLICY),
                    GetRequestSourceModeName(requester)));
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_OUTPUT_AMOUNT),
                    GameUtil.GetFormattedMass(requester.GetOutputAmountKg())));
                if (!string.IsNullOrEmpty(requester.LastStatus))
                {
                    AppendLine(builder, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_REQUEST), requester.LastStatus));
                }
            }

            if (connector != null && !string.IsNullOrEmpty(connector.LastOutputStatus))
            {
                AppendLine(builder, string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_REQUEST), connector.LastOutputStatus));
            }

            Storage bufferStorage = pickupBuffer != null ? pickupBuffer.GetBufferStorage() : null;
            if (bufferStorage != null && bufferStorage.items != null && bufferStorage.items.Count > 0)
            {
                AppendLine(builder, string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_BUFFER),
                    GameUtil.GetFormattedMass(bufferStorage.MassStored())));
            }

            return builder.ToString();
        }

        private string GetNetworkLine()
        {
            int worldId = StorageTargetSelector.GetObjectWorldId(gameObject);
            return StorageSceneRegistry.HasOnlineCoreInWorld(worldId)
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_ONLINE)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_OFFLINE);
        }

        private string GetDirectionText()
        {
            if (port == null)
            {
                return "?";
            }

            return port.IsInput
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_PORT_DIRECTION_INPUT)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_PORT_DIRECTION_OUTPUT);
        }

        private string GetCacheLine()
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_CACHE),
                GetStoredMassText(),
                GetCapacityText());
        }

        private string GetAutomationSummaryLine()
        {
            if (connector != null)
            {
                return string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_INPUT_SUMMARY),
                    connector.IsOutputStoreEnabled()
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.ON)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.OFF),
                    GetOutputStoreModeName(connector));
            }

            if (requester != null)
            {
                return string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_OUTPUT_SUMMARY),
                    requester.RequestEnabled
                        ? Loc.Get(Loc.UI.STORAGE_NETWORK.ON)
                        : Loc.Get(Loc.UI.STORAGE_NETWORK.OFF),
                    GetRequestSourceModeName(requester));
            }

            return string.Empty;
        }

        private string GetManualLine()
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL),
                port != null && port.IsManualDuplicantOperationAllowed()
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_ALLOWED)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_MANUAL_FORBIDDEN));
        }

        private string GetStoredMassText()
        {
            return mainStorage != null
                ? GameUtil.GetFormattedMass(mainStorage.MassStored())
                : GameUtil.GetFormattedMass(0f);
        }

        private string GetCapacityText()
        {
            return mainStorage != null
                ? GameUtil.GetFormattedMass(mainStorage.Capacity())
                : GameUtil.GetFormattedMass(0f);
        }

        private string GetRemainingCapacityText()
        {
            return mainStorage != null
                ? GameUtil.GetFormattedMass(Mathf.Max(0f, mainStorage.RemainingCapacity()))
                : GameUtil.GetFormattedMass(0f);
        }

        private string GetFilterLine()
        {
            TreeFilterable filterable = GetComponent<TreeFilterable>();
            if (filterable == null)
            {
                return string.Format(
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS),
                    Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS_ANY));
            }

            List<Tag> tags = filterable.AcceptedTags != null
                ? filterable.AcceptedTags.Where(tag => tag != Tag.Invalid).ToList()
                : new List<Tag>();
            string text = tags.Count == 0
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS_EMPTY)
                : string.Join(", ", tags.Take(4).Select(tag => tag.ProperName()));
            if (tags.Count > 4)
            {
                text += string.Format(" +{0}", tags.Count - 4);
            }

            return string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_FILTERS), text);
        }

        private static string GetRequestSourceModeName(StorageNetworkPortRequester requester)
        {
            if (requester == null)
            {
                return string.Empty;
            }

            if (requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = requester.ResolveSourceStorage();
                return source != null
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        private static string GetOutputStoreModeName(StorageNetworkStorageConnector connector)
        {
            if (connector == null)
            {
                return string.Empty;
            }

            if (connector.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = connector.ResolveOutputStorage();
                return target != null
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private static void AppendLine(StringBuilder builder, string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(line);
        }
    }
}
