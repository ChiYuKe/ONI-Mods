using UnityEngine;

namespace FunnyComponents.Components
{
    [AddComponentMenu("KMonoBehaviour/scripts/ColorPulseMarker")]
    public sealed class ColorPulseMarker : KMonoBehaviour
    {
        private static readonly Color[] Palette =
        {
            new Color(0.95f, 0.35f, 0.30f),
            new Color(0.30f, 0.70f, 1.00f),
            new Color(0.40f, 0.95f, 0.55f),
            new Color(1.00f, 0.82f, 0.30f)
        };

        private Light2D light2d;
        private float timer;
        private int colorIndex;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            light2d = gameObject.AddOrGet<Light2D>();
            light2d.Range = 2.5f;
            light2d.Lux = 550;
            light2d.Color = Palette[0];
        }

        private void Update()
        {
            if (light2d == null)
            {
                return;
            }

            timer += Time.deltaTime;
            if (timer >= 1.25f)
            {
                timer = 0f;
                colorIndex = (colorIndex + 1) % Palette.Length;
                light2d.Color = Palette[colorIndex];
            }

            light2d.Lux = 180 + Mathf.RoundToInt(Mathf.PingPong(Time.time * 120f, 120f));
            light2d.FullRefresh();
        }
    }
}
