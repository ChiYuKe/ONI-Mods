using UnityEngine;

namespace AutomaticHarvest
{
#pragma warning disable CS0649
    public class Reservoir : KMonoBehaviour
    {
        private MeterController fillMeter;
        private MeterController lightMeter;

        [MyCmpGet]
        private Storage storage;

        public enum HstatusLight
        {
            Red,
            Yellow,
            Green
        }

        private static readonly EventSystem.IntraObjectHandler<Reservoir> OnStorageChangeDelegate =
            new EventSystem.IntraObjectHandler<Reservoir>((component, data) => component.OnStorageChange(data));

        protected override void OnSpawn()
        {
            base.OnSpawn();

            fillMeter = new MeterController(
                GetComponent<KBatchedAnimController>(),
                "meter_target",
                "meter",
                Meter.Offset.Infront,
                Grid.SceneLayer.NoLayer,
                "meter_fill",
                "meter_OL");

            Subscribe((int)GameHashes.OnStorageChange, OnStorageChangeDelegate);
            OnStorageChange(null);
        }

        public void RefreshHstatusLight(HstatusLight status)
        {
            if (lightMeter == null)
            {
                lightMeter = new MeterController(
                    GetComponent<KBatchedAnimController>(),
                    "opening",
                    "status_light",
                    Meter.Offset.Infront,
                    Grid.SceneLayer.NoLayer,
                    "meter_fill",
                    "meter_OL");
            }

            float frame = status switch
            {
                HstatusLight.Red => 0f,
                HstatusLight.Yellow => 1f,
                HstatusLight.Green => 2f,
                _ => 0f
            };

            lightMeter.SetPositionPercent(frame / 3f);
        }

        private void OnStorageChange(object data)
        {
            float percentFull = storage.capacityKg > 0f ? storage.MassStored() / storage.capacityKg : 0f;
            fillMeter.SetPositionPercent(Mathf.Clamp01(percentFull));
        }
    }
#pragma warning restore CS0649
}
