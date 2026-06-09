using KSerialization;
using UnityEngine;

#pragma warning disable CS0649
public class AutomaticHarvestLogic : KMonoBehaviour, IActivationRangeTarget, ISim200ms
{
    public static readonly HashedString PORT_ID = "AutomaticHarvestLogicLogicPort";

    [MyCmpGet]
    private Storage storage;

    [MyCmpGet]
    private LogicPorts logicPorts;

    [Serialize]
    private int lowThresholdValue;

    [Serialize]
    private int highThresholdValue = 100;

    [Serialize]
    public bool activated;

    private static readonly EventSystem.IntraObjectHandler<AutomaticHarvestLogic> OnCopySettingsDelegate =
        new EventSystem.IntraObjectHandler<AutomaticHarvestLogic>((component, data) => component.OnCopySettings(data));

    private static readonly EventSystem.IntraObjectHandler<AutomaticHarvestLogic> UpdateLogicCircuitDelegate =
        new EventSystem.IntraObjectHandler<AutomaticHarvestLogic>((component, data) => component.UpdateLogicCircuit(data));

    public float PercentFull
    {
        get
        {
            float capacity = storage.Capacity();
            return capacity > 0f ? storage.MassStored() / capacity : 0f;
        }
    }

    public float ActivateValue
    {
        get => highThresholdValue;
        set
        {
            highThresholdValue = (int)value;
            UpdateLogicCircuit(null);
        }
    }

    public float DeactivateValue
    {
        get => lowThresholdValue;
        set
        {
            lowThresholdValue = (int)value;
            UpdateLogicCircuit(null);
        }
    }

    public float MinValue => 0f;

    public float MaxValue => 100f;

    public bool UseWholeNumbers => true;

    public string ActivateTooltip => AutomaticHarvest.STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.ACTIVATE_TOOLTIP;

    public string DeactivateTooltip => AutomaticHarvest.STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.DEACTIVATE_TOOLTIP;

    public string ActivationRangeTitleText => AutomaticHarvest.STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.SIDESCREEN_TITLE;

    public string ActivateSliderLabelText => AutomaticHarvest.STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.SIDESCREEN_ACTIVATE;

    public string DeactivateSliderLabelText => AutomaticHarvest.STRINGS.BUILDINGS.AUTOMATICHARVESTCONFIG.SIDESCREEN_DEACTIVATE;

    protected override void OnPrefabInit()
    {
        base.OnPrefabInit();
        Subscribe((int)GameHashes.CopySettings, OnCopySettingsDelegate);
    }

    protected override void OnSpawn()
    {
        base.OnSpawn();
        Subscribe((int)GameHashes.OnStorageChange, UpdateLogicCircuitDelegate);
        UpdateLogicCircuit(null);
    }

    public void Sim200ms(float dt)
    {
        UpdateLogicCircuit(null);
    }

    private void UpdateLogicCircuit(object data)
    {
        float percentFull = PercentFull * 100f;

        if (activated)
        {
            if (percentFull <= lowThresholdValue)
            {
                activated = false;
            }
        }
        else if (percentFull >= highThresholdValue)
        {
            activated = true;
        }

        logicPorts.SendSignal(PORT_ID, activated ? 1 : 0);
    }

    private void OnCopySettings(object data)
    {
        AutomaticHarvestLogic source = ((GameObject)data).GetComponent<AutomaticHarvestLogic>();
        if (source == null)
        {
            return;
        }

        ActivateValue = source.ActivateValue;
        DeactivateValue = source.DeactivateValue;
    }
}
#pragma warning restore CS0649
