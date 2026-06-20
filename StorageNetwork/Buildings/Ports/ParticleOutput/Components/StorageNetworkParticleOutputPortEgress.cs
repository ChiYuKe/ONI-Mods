using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using STRINGS;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkParticleOutputPortEgress : KMonoBehaviour, ISim1000ms, IHighEnergyParticleDirection, IProgressBarSideScreen, ISingleSliderControl, ISliderControl, ISidescreenButtonControl
    {
        public const float DefaultThresholdParticles = 50f;
        public const float MinThresholdParticles = 50f;
        public const float MaxThresholdParticles = 500f;
        private const float MinPayload = 0.1f;
        private const float MinLaunchInterval = 2f;
        public const float DefaultOutputLimitParticles = 1000f;
        public const float MinOutputLimitParticles = 1f;
        public const float MaxOutputLimitParticles = 1000000f;

        [Serialize]
        public bool OutputRequestEnabled = true;

        [Serialize]
        public int SourceModeValue;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public bool OutputLimitEnabled;

        [Serialize]
        public float OutputLimitParticles;

        [Serialize]
        public float OutputLimitUsedParticles;

        [MyCmpReq]
        private Building building;

        [MyCmpGet]
        private Operational operational;

        [SerializeField]
        private EightDirection direction = EightDirection.Right;

        [SerializeField]
        private float particleThreshold = DefaultThresholdParticles;

        private float launchTimer;

        public EightDirection Direction
        {
            get => direction;
            set => direction = value;
        }

        public float ParticleThreshold => particleThreshold;

        public float AvailableParticles => GetAvailableParticles();

        public bool OutputLimitReached => IsOutputLimitReached();

        public StorageNetworkMaterialRequester.RequestMode CurrentSourceMode
        {
            get => (StorageNetworkMaterialRequester.RequestMode)Mathf.Clamp(SourceModeValue, 0, 1);
            set => SourceModeValue = (int)value;
        }

        public string SidescreenButtonText => OutputRequestEnabled
            ? Loc.Get(Loc.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_DISABLE_BUTTON)
            : Loc.Get(Loc.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_ENABLE_BUTTON);

        public string SidescreenButtonTooltip => OutputRequestEnabled
            ? Loc.Get(Loc.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_DISABLE_TOOLTIP)
            : Loc.Get(Loc.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_ENABLE_TOOLTIP);

        public void SetParticleThreshold(float value)
        {
            particleThreshold = Mathf.Clamp(value, MinThresholdParticles, MaxThresholdParticles);
        }

        public void SetOutputLimitEnabled(bool enabled)
        {
            OutputLimitEnabled = enabled;
            if (enabled && OutputLimitParticles <= 0f)
            {
                OutputLimitParticles = DefaultOutputLimitParticles;
            }
        }

        public void ResetOutputLimitUsed()
        {
            OutputLimitUsedParticles = 0f;
        }

        public void SetSourceStorage(Storage source)
        {
            KPrefabID prefabId = source != null ? source.GetComponent<KPrefabID>() : null;
            SourceStorageInstanceId = prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SpecificStorage;
        }

        public void UseAutomaticSourceStorage()
        {
            CurrentSourceMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        public Storage ResolveSourceStorage()
        {
            if (SourceStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage source = info?.Storage;
                KPrefabID prefabId = source != null ? source.GetComponent<KPrefabID>() : null;
                if (info?.Minion == null &&
                    prefabId != null &&
                    prefabId.InstanceID == SourceStorageInstanceId &&
                    StorageNetworkStorageRules.IsParticleStorageServer(source))
                {
                    return source;
                }
            }

            return null;
        }

        public string SliderTitleKey => "STRINGS.UI.UISIDESCREENS.RADBOLTTHRESHOLDSIDESCREEN.TITLE";

        public string SliderUnits => global::STRINGS.UI.UNITSUFFIXES.HIGHENERGYPARTICLES.PARTRICLES;

        public void SetButtonTextOverride(ButtonMenuTextOverride textOverride)
        {
        }

        public bool SidescreenEnabled()
        {
            return true;
        }

        public bool SidescreenButtonInteractable()
        {
            return true;
        }

        public void OnSidescreenButtonPressed()
        {
            OutputRequestEnabled = !OutputRequestEnabled;
        }

        public int HorizontalGroupID()
        {
            return -1;
        }

        public int ButtonSideScreenSortOrder()
        {
            return 18;
        }

        public void Sim1000ms(float dt)
        {
            if (!OutputRequestEnabled || IsOutputLimitReached() || operational != null && !operational.IsOperational)
            {
                return;
            }

            launchTimer += dt;
            float requestAmount = GetLaunchAmount();
            if (launchTimer < MinLaunchInterval || GetAvailableParticles() < requestAmount)
            {
                return;
            }

            launchTimer = 0f;
            float payload = StorageNetworkParticleStorageService.Consume(gameObject, requestAmount, GetSpecificSourceStorage());
            if (payload < MinPayload)
            {
                return;
            }

            if (OutputLimitEnabled)
            {
                OutputLimitUsedParticles += payload;
            }

            GameObject particleObject = GameUtil.KInstantiate(
                Assets.GetPrefab("HighEnergyParticle"),
                Grid.CellToPosCCC(building.GetHighEnergyParticleOutputCell(), Grid.SceneLayer.FXFront2),
                Grid.SceneLayer.FXFront2);

            if (particleObject == null)
            {
                return;
            }

            particleObject.SetActive(true);
            HighEnergyParticle particle = particleObject.GetComponent<HighEnergyParticle>();
            if (particle != null)
            {
                particle.payload = payload;
                particle.SetDirection(Direction);
            }
        }

        public float GetProgressBarMaxValue()
        {
            return particleThreshold;
        }

        public float GetProgressBarFillPercentage()
        {
            if (particleThreshold <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(GetAvailableParticles() / particleThreshold);
        }

        public string GetProgressBarTitleLabel()
        {
            return global::STRINGS.UI.UISIDESCREENS.RADBOLTTHRESHOLDSIDESCREEN.PROGRESS_BAR_LABEL;
        }

        public string GetProgressBarLabel()
        {
            return Mathf.FloorToInt(GetAvailableParticles()) + "/" + Mathf.FloorToInt(particleThreshold);
        }

        public string GetProgressBarTooltip()
        {
            return global::STRINGS.UI.UISIDESCREENS.RADBOLTTHRESHOLDSIDESCREEN.PROGRESS_BAR_TOOLTIP;
        }

        public int SliderDecimalPlaces(int index)
        {
            return 0;
        }

        public float GetSliderMin(int index)
        {
            return MinThresholdParticles;
        }

        public float GetSliderMax(int index)
        {
            return MaxThresholdParticles;
        }

        public float GetSliderValue(int index)
        {
            return particleThreshold;
        }

        public void SetSliderValue(float value, int index)
        {
            SetParticleThreshold(value);
        }

        public string GetSliderTooltipKey(int index)
        {
            return "STRINGS.UI.UISIDESCREENS.RADBOLTTHRESHOLDSIDESCREEN.TOOLTIP";
        }

        string ISliderControl.GetSliderTooltip(int index)
        {
            return string.Format(Strings.Get("STRINGS.UI.UISIDESCREENS.RADBOLTTHRESHOLDSIDESCREEN.TOOLTIP"), particleThreshold);
        }

        private float GetAvailableParticles()
        {
            return StorageNetworkParticleStorageService.GetAvailable(gameObject, GetSpecificSourceStorage());
        }

        private bool IsOutputLimitReached()
        {
            return OutputLimitEnabled && Mathf.Max(0f, OutputLimitUsedParticles) >= Mathf.Max(0f, OutputLimitParticles) - 0.01f;
        }

        private float GetLaunchAmount()
        {
            float threshold = Mathf.Max(MinPayload, particleThreshold);
            if (!OutputLimitEnabled)
            {
                return threshold;
            }

            return Mathf.Min(threshold, Mathf.Max(0f, OutputLimitParticles - OutputLimitUsedParticles));
        }

        private Storage GetSpecificSourceStorage()
        {
            return CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? ResolveSourceStorage()
                : null;
        }
    }
}
