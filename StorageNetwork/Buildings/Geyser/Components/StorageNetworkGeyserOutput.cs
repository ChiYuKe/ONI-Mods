using System;
using System.Collections.Generic;
using System.Text;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    public sealed class StorageNetworkGeyserOutput : KMonoBehaviour, ISim200ms
    {
        [MyCmpGet]
        private StorageNetworkEnrollment enrollment = null;

        [MyCmpGet]
        private ElementEmitter emitter = null;

        [MyCmpGet]
        private PrimaryElement primaryElement = null;

        private bool isErupting = false;
        private bool lastAppliedEmitting = false;
        private bool hasAppliedEmitting = false;
        private bool isNetworkCapturing = false;
        private Guid networkStatusHandle = Guid.Empty;

        private static StatusItem networkStatusItem;

        private static readonly EventSystem.IntraObjectHandler<StorageNetworkGeyserOutput> OnGeyserEruptionDelegate =
            new EventSystem.IntraObjectHandler<StorageNetworkGeyserOutput>((component, data) => component.OnGeyserEruption(data));

        protected override void OnSpawn()
        {
            base.OnSpawn();
            Subscribe((int)GameHashes.GeyserEruption, OnGeyserEruptionDelegate);
            isErupting = emitter != null && emitter.IsSimActive;
            AddStatusItem();
            ApplyEmitterState();
        }

        protected override void OnCleanUp()
        {
            if (emitter != null && isErupting)
            {
                emitter.SetEmitting(true);
            }

            RemoveStatusItem();
            base.OnCleanUp();
        }

        public void Sim200ms(float dt)
        {
            if (!IsEruptingNow())
            {
                isNetworkCapturing = false;
                return;
            }

            string captureState = GetCaptureState();
            if (captureState != null)
            {
                bool preventWorldOutput = ShouldPreventWorldOutput();
                isNetworkCapturing = preventWorldOutput;
                SetNativeEmitterEnabled(!preventWorldOutput);
                if (preventWorldOutput)
                {
                    SuppressNativeOverpressureStatus();
                }

                return;
            }

            ElementConverter.OutputElement output = emitter.outputElement;
            float temperature = output.minOutputTemperature > 0f
                ? output.minOutputTemperature
                : primaryElement?.Temperature ?? 293.15f;
            float mass = GetOutputMass(output, dt);
            if (mass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return;
            }

            ElementOutputTargetQuery targetQuery = FindOutputTargetQuery(output);
            if (targetQuery.Targets.Count == 0)
            {
                if (targetQuery.HasCandidateIgnoringCapacity)
                {
                    isNetworkCapturing = true;
                    SetNativeEmitterEnabled(false);
                    SuppressNativeOverpressureStatus();
                    if (Config.Instance.AllowGeyserWorldOutputFallback)
                    {
                        emitter.ForceEmit(mass, output.addedDiseaseIdx, GetDiseaseCount(output, dt), temperature);
                    }
                }
                else
                {
                    bool preventWorldOutput = ShouldPreventWorldOutput();
                    isNetworkCapturing = preventWorldOutput;
                    SetNativeEmitterEnabled(!preventWorldOutput);
                    if (preventWorldOutput)
                    {
                        SuppressNativeOverpressureStatus();
                    }
                }

                return;
            }

            isNetworkCapturing = true;
            SetNativeEmitterEnabled(false);
            SuppressNativeOverpressureStatus();

            float overflow = StoreElementInNetwork(output.elementHash, mass, temperature, output.addedDiseaseIdx, GetDiseaseCount(output, dt), targetQuery.Targets);
            if (Config.Instance.AllowGeyserWorldOutputFallback &&
                overflow > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                emitter.ForceEmit(overflow, output.addedDiseaseIdx, GetDiseaseCount(output, dt), temperature);
            }
        }

        public void ApplyEmitterState()
        {
            if (emitter == null)
            {
                return;
            }

            if (!IsEruptingNow())
            {
                isNetworkCapturing = false;
                hasAppliedEmitting = false;
                return;
            }

            if (GetCaptureState() != null)
            {
                bool preventWorldOutput = ShouldPreventWorldOutput();
                isNetworkCapturing = preventWorldOutput;
                SetNativeEmitterEnabled(!preventWorldOutput);
                if (preventWorldOutput)
                {
                    SuppressNativeOverpressureStatus();
                }

                return;
            }

            ElementConverter.OutputElement output = emitter.outputElement;
            ElementOutputTargetQuery targetQuery = FindOutputTargetQuery(output);
            isNetworkCapturing = targetQuery.Targets.Count > 0 ||
                targetQuery.HasCandidateIgnoringCapacity ||
                ShouldPreventWorldOutput();
            SetNativeEmitterEnabled(!isNetworkCapturing);
            if (isNetworkCapturing)
            {
                SuppressNativeOverpressureStatus();
            }
        }

        private void OnGeyserEruption(object data)
        {
            isErupting = data is bool erupting && erupting;
            if (!isErupting)
            {
                isNetworkCapturing = false;
                hasAppliedEmitting = false;
                return;
            }

            ApplyEmitterState();
        }

        private bool ShouldTryCapture()
        {
            if (enrollment == null ||
                emitter == null ||
                !enrollment.DirectGeyserOutputToNetwork ||
                !enrollment.IncludedInSceneNetwork ||
                !enrollment.IsAnalyzedGeyser())
            {
                return false;
            }

            ElementConverter.OutputElement output = emitter.outputElement;
            return output.elementHash != SimHashes.Vacuum && output.massGenerationRate > 0f;
        }

        private bool ShouldPreventWorldOutput()
        {
            return !Config.Instance.AllowGeyserWorldOutputFallback &&
                enrollment != null &&
                emitter != null &&
                enrollment.IncludedInSceneNetwork &&
                enrollment.DirectGeyserOutputToNetwork;
        }

        private bool IsEruptingNow()
        {
            return isErupting || emitter != null && emitter.IsSimActive;
        }

        private static float GetOutputMass(ElementConverter.OutputElement output, float dt)
        {
            return Mathf.Max(0f, output.massGenerationRate) * dt;
        }

        private static int GetDiseaseCount(ElementConverter.OutputElement output, float dt)
        {
            return Mathf.RoundToInt(output.addedDiseaseCount * dt);
        }

        private int GetOutputWorldId()
        {
            if (gameObject == null)
            {
                return -1;
            }

            int worldId = gameObject.GetMyWorldId();
            if (worldId != byte.MaxValue && worldId >= 0)
            {
                return worldId;
            }

            int cell = Grid.PosToCell(gameObject);
            return Grid.IsValidCell(cell) ? Grid.WorldIdx[cell] : -1;
        }

        public bool CanCaptureOutput()
        {
            // 纯查询：当前是否"既能入网,也能找到目标"。
            // UI 显示接入状态时调用,不应再有副作用(历史版本里这个方法会被 ApplyEmitterState
            // 拿来当 SetEmitting 的反向条件,导致"找不到容器就打开原版 emitter"的 bug)。
            if (emitter == null)
            {
                return false;
            }

            if (GetCaptureState() != null)
            {
                return false;
            }

            return FindOutputTargets(emitter.outputElement).Count > 0;
        }

        public bool IsRuntimeErupting()
        {
            return IsEruptingNow();
        }

        private string GetCaptureState()
        {
            if (enrollment == null)
            {
                return "[StorageNetwork][Geyser] Missing enrollment component.";
            }

            if (emitter == null)
            {
                return "[StorageNetwork][Geyser] Missing ElementEmitter component.";
            }

            if (!enrollment.IncludedInSceneNetwork)
            {
                return "[StorageNetwork][Geyser] Not enrolled into the storage network.";
            }

            if (!enrollment.DirectGeyserOutputToNetwork)
            {
                return "[StorageNetwork][Geyser] Direct network output is disabled.";
            }

            if (!enrollment.IsAnalyzedGeyser())
            {
                return "[StorageNetwork][Geyser] Geyser is not analyzed yet.";
            }

            if (emitter.outputElement.elementHash == SimHashes.Vacuum)
            {
                return "[StorageNetwork][Geyser] Output element is vacuum.";
            }

            if (emitter.outputElement.massGenerationRate <= 0f)
            {
                return "[StorageNetwork][Geyser] Output mass generation rate is zero.";
            }

            int worldId = GetOutputWorldId();
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                return string.Format("[StorageNetwork][Geyser] No online core in world {0}.", worldId);
            }

            if (enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage &&
                enrollment.ResolveGeyserOutputStorage() == null)
            {
                return "[StorageNetwork][Geyser] Specific target server is missing or unavailable.";
            }

            return null;
        }

        private List<Storage> FindOutputTargets(ElementConverter.OutputElement output)
        {
            if (output.elementHash == SimHashes.Vacuum)
            {
                return new List<Storage>();
            }

            Storage specificTarget = enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? enrollment.ResolveGeyserOutputStorage()
                : null;
            return NetworkStorageTransferService.FindElementOutputTargets(output.elementHash, null, specificTarget, null, GetOutputWorldId());
        }

        private ElementOutputTargetQuery FindOutputTargetQuery(ElementConverter.OutputElement output)
        {
            if (output.elementHash == SimHashes.Vacuum)
            {
                return new ElementOutputTargetQuery();
            }

            Storage specificTarget = enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? enrollment.ResolveGeyserOutputStorage()
                : null;
            return StorageTargetSelector.FindElementOutputTargetsWithCapacityState(output.elementHash, null, specificTarget, null, GetOutputWorldId());
        }

        private bool HasOutputCandidateIgnoringCapacity(ElementConverter.OutputElement output)
        {
            if (output.elementHash == SimHashes.Vacuum)
            {
                return false;
            }

            Storage specificTarget = enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage
                ? enrollment.ResolveGeyserOutputStorage()
                : null;
            return NetworkStorageTransferService.HasElementOutputCandidateIgnoringCapacity(output.elementHash, null, specificTarget, null, GetOutputWorldId());
        }

        private void SetNativeEmitterEnabled(bool enabled)
        {
            if (emitter == null)
            {
                return;
            }

            if (hasAppliedEmitting && lastAppliedEmitting == enabled && emitter.IsSimActive == enabled)
            {
                return;
            }

            emitter.SetEmitting(enabled);
            lastAppliedEmitting = enabled;
            hasAppliedEmitting = true;
        }

        private void SuppressNativeOverpressureStatus()
        {
            if (!isNetworkCapturing)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(Db.Get().MiscStatusItems.SpoutOverPressure, false);
            }
        }

        private void AddStatusItem()
        {
            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable == null || networkStatusHandle != Guid.Empty)
            {
                return;
            }

            networkStatusHandle = selectable.AddStatusItem(GetNetworkStatusItem(), this);
        }

        private void RemoveStatusItem()
        {
            if (networkStatusHandle == Guid.Empty)
            {
                return;
            }

            KSelectable selectable = GetComponent<KSelectable>();
            if (selectable != null)
            {
                selectable.RemoveStatusItem(networkStatusHandle);
            }

            networkStatusHandle = Guid.Empty;
        }

        private static StatusItem GetNetworkStatusItem()
        {
            if (networkStatusItem != null)
            {
                return networkStatusItem;
            }

            networkStatusItem = new StatusItem(
                "StorageNetworkGeyserOutput",
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_ITEM),
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_TOOLTIP),
                "status_item_need_resource",
                StatusItem.IconType.Custom,
                NotificationType.Neutral,
                false,
                OverlayModes.None.ID,
                129022,
                false);

            networkStatusItem.resolveStringCallback = (text, data) =>
            {
                StorageNetworkGeyserOutput output = data as StorageNetworkGeyserOutput;
                return output != null ? output.BuildStatusTitle() : text;
            };
            networkStatusItem.resolveTooltipCallback = (tooltip, data) =>
            {
                StorageNetworkGeyserOutput output = data as StorageNetworkGeyserOutput;
                return output != null ? output.BuildStatusTooltip() : tooltip;
            };

            return networkStatusItem;
        }

        private string BuildStatusTitle()
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_ITEM),
                ColorizeStatus(GetOutputDestinationText()));
        }

        private string BuildStatusTooltip()
        {
            ElementConverter.OutputElement output = emitter != null ? emitter.outputElement : default;
            StringBuilder builder = new StringBuilder();
            builder.Append(BuildStatusTitle());
            builder.AppendLine();
            builder.AppendLine(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_NETWORK),
                ColorizeNetwork(enrollment != null && enrollment.IncludedInSceneNetwork),
                ColorizeNetwork(StorageSceneRegistry.HasOnlineCoreInWorld(GetOutputWorldId()))));
            builder.AppendLine(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_DIRECT),
                ColorizeEnabled(enrollment != null && enrollment.DirectGeyserOutputToNetwork),
                ColorizeInfo(IsEruptingNow() ? Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_ERUPTING) : Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_NOT_ERUPTING))));
            builder.AppendLine(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_OUTPUT),
                ColorizeInfo(GetElementName(output.elementHash)),
                ColorizeAmount(FormatRate(output.massGenerationRate))));
            builder.AppendLine(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_POLICY),
                ColorizeInfo(GetStorePolicyText())));
            builder.AppendLine(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_TARGETS),
                ColorizeAmount(GetTargetSummaryText(output))));
            builder.Append(string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_LINE_STATUS),
                ColorizeStatus(GetOutputDestinationText())));

            return builder.ToString();
        }

        private string GetOutputDestinationText()
        {
            if (emitter == null)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_MISSING_EMITTER);
            }

            if (GetCaptureState() != null)
            {
                return ShouldPreventWorldOutput()
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_NETWORK_PAUSED)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_WORLD_OUTPUT);
            }

            ElementOutputTargetQuery targetQuery = FindOutputTargetQuery(emitter.outputElement);
            if (targetQuery.Targets.Count > 0)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_NETWORK_OUTPUT);
            }

            if (targetQuery.HasCandidateIgnoringCapacity)
            {
                return Config.Instance.AllowGeyserWorldOutputFallback
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_OVERFLOW_OUTPUT)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_FULL_PAUSED);
            }

            return ShouldPreventWorldOutput()
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_NETWORK_PAUSED)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_WORLD_OUTPUT);
        }

        private string GetStorePolicyText()
        {
            if (enrollment == null)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
            }

            if (enrollment.CurrentGeyserOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = enrollment.ResolveGeyserOutputStorage();
                return target != null
                    ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Loc.Get(Loc.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        private string GetTargetSummaryText(ElementConverter.OutputElement output)
        {
            if (emitter == null || output.elementHash == SimHashes.Vacuum || GetCaptureState() != null)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.NONE);
            }

            ElementOutputTargetQuery targetQuery = FindOutputTargetQuery(output);
            if (targetQuery.Targets.Count == 0)
            {
                return targetQuery.HasCandidateIgnoringCapacity
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_TARGET_FULL)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.NONE);
            }

            float remaining = 0f;
            foreach (Storage target in targetQuery.Targets)
            {
                if (target != null)
                {
                    remaining += Mathf.Max(0f, target.RemainingCapacity());
                }
            }

            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_TARGET_SUMMARY),
                targetQuery.Targets.Count,
                GameUtil.GetFormattedMass(remaining));
        }

        private static string GetElementName(SimHashes elementHash)
        {
            if (elementHash == SimHashes.Vacuum)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
            }

            Element element = ElementLoader.FindElementByHash(elementHash);
            return element != null ? element.name : elementHash.CreateTag().ProperName();
        }

        private static string FormatRate(float kgPerSecond)
        {
            return GameUtil.GetFormattedMass(Mathf.Max(0f, kgPerSecond), GameUtil.TimeSlice.PerSecond, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}");
        }

        private static string ColorizeEnabled(bool enabled)
        {
            return Colorize(enabled ? Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_ENABLED) : Loc.Get(Loc.UI.STORAGE_NETWORK.STATUS_DISABLED), enabled ? "#55d17a" : "#d86a6a");
        }

        private static string ColorizeNetwork(bool online)
        {
            return Colorize(online ? Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_ONLINE) : Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_STATUS_SHORT_OFFLINE), online ? "#55d17a" : "#d86a6a");
        }

        private static string ColorizeInfo(string text)
        {
            return Colorize(text, "#8ec7ff");
        }

        private static string ColorizeAmount(string text)
        {
            return Colorize(text, "#f0c96a");
        }

        private static string ColorizeStatus(string text)
        {
            bool warning = text == Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_WORLD_OUTPUT) ||
                text == Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_OVERFLOW_OUTPUT) ||
                text == Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_FULL_PAUSED) ||
                text == Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_NETWORK_PAUSED) ||
                text == Loc.Get(Loc.UI.STORAGE_NETWORK.GEYSER_STATUS_MISSING_EMITTER);
            return Colorize(text, warning ? "#d86a6a" : "#55d17a");
        }

        private static string Colorize(string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        private static float StoreElementInNetwork(
            SimHashes elementHash,
            float mass,
            float temperature,
            byte diseaseIdx,
            int diseaseCount,
            List<Storage> targets)
        {
            Element element = ElementLoader.FindElementByHash(elementHash);
            if (element == null)
            {
                return mass;
            }

            float remaining = mass;
            if (targets == null || targets.Count == 0)
            {
                return remaining;
            }

            foreach (Storage target in targets)
            {
                if (remaining <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                float amount = Mathf.Min(remaining, Mathf.Max(0f, target.RemainingCapacity()));
                if (amount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                if (element.IsGas)
                {
                    target.AddGasChunk(elementHash, amount, temperature, diseaseIdx, diseaseCount, false, true);
                }
                else if (element.IsLiquid)
                {
                    target.AddLiquid(elementHash, amount, temperature, diseaseIdx, diseaseCount, false, true);
                }
                else if (element.IsSolid)
                {
                    GameObject resource = element.substance.SpawnResource(target.transform.GetPosition(), amount, temperature, diseaseIdx, diseaseCount, true, false, true);
                    target.Store(resource, hide_popups: true, block_events: false, do_disease_transfer: true, is_deserializing: false);
                }
                else
                {
                    break;
                }

                remaining -= amount;
            }

            return Mathf.Max(0f, remaining);
        }
    }
}
