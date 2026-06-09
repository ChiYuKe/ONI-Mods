using System.Collections.Generic;
using System.Linq;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkPortRequester : KMonoBehaviour, ISim200ms, ISingleSliderControl
    {
        [Serialize]
        public bool RequestEnabled;

        [Serialize]
        public int Mode;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public bool LimitEnabled;

        [Serialize]
        public float LimitKg = Config.Instance.DefaultMaterialRequestLimitKg;

        [Serialize]
        public float RequestedKg;

        [Serialize]
        public float OutputAmountKg;

        [Serialize]
        private bool defaultStateInitialized;

        private Storage storage;
        private StorageNetworkPort port;
        private TreeFilterable filterable;
        private float requestCooldown;
        private string lastStatus;

        public string LastStatus => lastStatus;

        public string SliderTitleKey => "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_OUTPUT_AMOUNT";

        public string SliderUnits => " kg";

        public StorageNetworkMaterialRequester.RequestMode CurrentMode
        {
            get => (StorageNetworkMaterialRequester.RequestMode)Mathf.Clamp(Mode, 0, 1);
            set => Mode = (int)value;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureComponents();
            InitializeDefaultState();
        }

        public void Sim200ms(float dt)
        {
            EnsureComponents();
            if (!RequestEnabled || storage == null || port == null || port.IsInput || port.Kind == StorageNetworkPortKind.PowerOutput)
            {
                lastStatus = string.Empty;
                requestCooldown = 0f;
                return;
            }

            if (requestCooldown > 0f)
            {
                requestCooldown -= dt;
                return;
            }

            HashSet<Tag> tags = GetRequestedTags();
            if (tags.Count == 0)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_NO_QUEUE);
                requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
                return;
            }

            float targetPacketAmount = GetOutputAmountKg();
            float remainingPacketAmount = Mathf.Max(0f, targetPacketAmount - storage.MassStored());
            if (remainingPacketAmount <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED);
                return;
            }

            float remainingLimit = LimitEnabled ? Mathf.Max(0f, LimitKg - RequestedKg) : float.MaxValue;
            if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED);
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
                return;
            }

            float requestedAmount = Mathf.Min(Mathf.Max(0f, storage.RemainingCapacity()), remainingLimit, remainingPacketAmount);
            Storage source = CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage ? ResolveSourceStorage() : null;
            float moved = NetworkStorageTransferService.TransferFromNetworkToStorage(tags, requestedAmount, storage, new[] { storage }, source);
            if (moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                RequestedKg += moved;
                lastStatus = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_REQUESTED), GameUtil.GetFormattedMass(moved), GetRequestName(tags));
                requestCooldown = 0f;
                return;
            }

            lastStatus = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_MISSING_SOURCE), GetRequestName(tags));
            requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
        }

        public Storage ResolveSourceStorage()
        {
            if (SourceStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage candidate = info?.Storage;
                if (StorageNetworkStorageRules.IsServerStorage(candidate) &&
                    StorageNetworkStorageRules.IsStorageCompatibleWithFilters(candidate, storage?.storageFilters) &&
                    GetStorageInstanceId(candidate) == SourceStorageInstanceId)
                {
                    return candidate;
                }
            }

            return null;
        }

        public void SetSourceStorage(Storage source)
        {
            SourceStorageInstanceId = GetStorageInstanceId(source);
            CurrentMode = StorageNetworkMaterialRequester.RequestMode.SpecificStorage;
        }

        public void UseAutomaticMaterialSource()
        {
            CurrentMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        public float GetRequestedAmountForDisplay()
        {
            return RequestedKg;
        }

        public void ResetRequestedAmount()
        {
            RequestedKg = 0f;
        }

        public float GetOutputAmountKg()
        {
            EnsureComponents();
            float maxAmount = GetMaxOutputAmountKg();
            if (OutputAmountKg <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                return maxAmount;
            }

            return Mathf.Clamp(OutputAmountKg, GetMinOutputAmountKg(), maxAmount);
        }

        public void SetOutputAmountKg(float value)
        {
            EnsureComponents();
            OutputAmountKg = Mathf.Clamp(value, GetMinOutputAmountKg(), GetMaxOutputAmountKg());
        }

        public float GetMinOutputAmountKg()
        {
            EnsureComponents();
            if (port != null && port.Kind == StorageNetworkPortKind.GasOutput)
            {
                return 0.1f;
            }

            return 1f;
        }

        public float GetMaxOutputAmountKg()
        {
            EnsureComponents();
            float conduitPacketMax = GetConduitPacketMaxKg();
            float storageMax = storage != null ? storage.capacityKg : conduitPacketMax;
            return Mathf.Max(GetMinOutputAmountKg(), Mathf.Min(conduitPacketMax, storageMax));
        }

        public int SliderDecimalPlaces(int index)
        {
            EnsureComponents();
            return port != null && port.Kind == StorageNetworkPortKind.GasOutput ? 1 : 0;
        }

        public float GetSliderMin(int index)
        {
            EnsureComponents();
            return CanShowOutputAmountSlider() ? GetMinOutputAmountKg() : 0f;
        }

        public float GetSliderMax(int index)
        {
            EnsureComponents();
            return CanShowOutputAmountSlider() ? GetMaxOutputAmountKg() : 0f;
        }

        public float GetSliderValue(int index)
        {
            return GetOutputAmountKg();
        }

        public void SetSliderValue(float value, int index)
        {
            SetOutputAmountKg(value);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_OUTPUT_AMOUNT_TOOLTIP";
        }

        public string GetSliderTooltip(int index)
        {
            return string.Format(
                Loc.Get(Loc.UI.STORAGE_NETWORK.PORT_OUTPUT_AMOUNT_TOOLTIP),
                GameUtil.GetFormattedMass(GetOutputAmountKg()));
        }

        private void EnsureComponents()
        {
            storage ??= GetComponent<Storage>();
            port ??= GetComponent<StorageNetworkPort>();
            filterable ??= GetComponent<TreeFilterable>();
        }

        private bool CanShowOutputAmountSlider()
        {
            return storage != null && port != null && !port.IsInput && port.Kind != StorageNetworkPortKind.PowerOutput;
        }

        private float GetConduitPacketMaxKg()
        {
            if (port == null)
            {
                return 1f;
            }

            switch (port.Kind)
            {
                case StorageNetworkPortKind.SolidOutput:
                    return 20f;
                case StorageNetworkPortKind.LiquidOutput:
                    return 10f;
                case StorageNetworkPortKind.GasOutput:
                    return 1f;
                default:
                    return 1f;
            }
        }

        private void InitializeDefaultState()
        {
            if (defaultStateInitialized)
            {
                return;
            }

            defaultStateInitialized = true;
            if (port != null && !port.IsInput && port.Kind != StorageNetworkPortKind.PowerOutput)
            {
                RequestEnabled = true;
            }
        }

        private HashSet<Tag> GetRequestedTags()
        {
            HashSet<Tag> tags = new HashSet<Tag>();
            if (filterable == null || filterable.AcceptedTags == null || filterable.AcceptedTags.Count == 0)
            {
                return tags;
            }

            foreach (Tag tag in filterable.AcceptedTags.Where(tag => tag != Tag.Invalid))
            {
                AddTagOrDiscoveredChildren(tags, tag);
            }

            return tags;
        }

        private static void AddTagOrDiscoveredChildren(HashSet<Tag> tags, Tag tag)
        {
            if (DiscoveredResources.Instance == null)
            {
                tags.Add(tag);
                return;
            }

            IEnumerable<Tag> discovered = DiscoveredResources.Instance.GetDiscoveredResourcesFromTag(tag);
            if (discovered == null || !discovered.Any())
            {
                tags.Add(tag);
                return;
            }

            foreach (Tag discoveredTag in discovered.Where(discoveredTag => discoveredTag != Tag.Invalid))
            {
                tags.Add(discoveredTag);
            }
        }

        private static string GetRequestName(HashSet<Tag> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.NONE);
            }

            if (tags.Count == 1)
            {
                return tags.First().ProperName();
            }

            return string.Join(", ", tags.Take(3).Select(tag => tag.ProperName()));
        }

        private static int GetStorageInstanceId(Storage candidate)
        {
            KPrefabID prefabId = candidate != null ? candidate.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
