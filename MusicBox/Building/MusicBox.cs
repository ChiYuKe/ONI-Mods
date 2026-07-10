using KModTool;
using KSerialization;
using System.Collections.Generic;
using UnityEngine;

namespace MusicBox.Building
{
    public sealed class MusicBoxComponent : KMonoBehaviour, ISim200ms, ISingleSliderControl
    {
        public static readonly HashedString PORT_ID = "MusicBoxInput";

        private const float VolumeBoost = 5f;
        private const float MusicParticlesDuration = 4f;
        private const float MusicParticlesCooldown = 2f;
        private static readonly Vector3 MusicParticlesOffset = new Vector3(0f, 0f, 0.1f);

        [MyCmpGet]
        private LogicPorts logicPorts = null;

        private int lastSignalValue = -1;
        private GameObject musicParticles;
        private float musicParticlesTimeRemaining;
        private float musicParticlesCooldownRemaining;

        [Serialize]
        private int volumePercent = 100;

        // 信号值 1-12 对应 12 个半音
        private static readonly Dictionary<int, int> NoteSoundMap = new Dictionary<int, int>
        {
            {1,  ModAssets.Sounds.C},
            {2,  ModAssets.Sounds.CS},
            {3,  ModAssets.Sounds.D},
            {4,  ModAssets.Sounds.DS},
            {5,  ModAssets.Sounds.E},
            {6,  ModAssets.Sounds.F},
            {7,  ModAssets.Sounds.FS},
            {8,  ModAssets.Sounds.G},
            {9,  ModAssets.Sounds.GS},
            {10, ModAssets.Sounds.A},
            {11, ModAssets.Sounds.AS},
            {12, ModAssets.Sounds.B},
        };

        #region ISingleSliderControl

        public string SliderTitleKey => "STRINGS.UI.UISIDESCREENS.MUSICBOX.VOLUME_TITLE";
        public string SliderUnits => "%";

        public int SliderDecimalPlaces(int index) => 0;
        public float GetSliderMin(int index) => 0f;
        public float GetSliderMax(int index) => 500f;

        public float GetSliderValue(int index) => volumePercent;

        public void SetSliderValue(float value, int index)
        {
            volumePercent = Mathf.RoundToInt(Mathf.Clamp(value, 0f, 500f));
        }

        public string GetSliderTooltipKey(int index) => "STRINGS.UI.UISIDESCREENS.MUSICBOX.VOLUME_TOOLTIP";

        public string GetSliderTooltip(int index) => $"{volumePercent}%";

        #endregion

        protected override void OnSpawn()
        {
            base.OnSpawn();
            logicPorts = logicPorts ?? GetComponent<LogicPorts>() ?? gameObject.AddOrGet<LogicPorts>();
        }

        public void Sim200ms(float dt)
        {
            UpdateMusicParticles(dt);

            if (logicPorts == null) return;

            int currentSignal = logicPorts.GetInputValue(PORT_ID);

            if (currentSignal != lastSignalValue)
            {
                lastSignalValue = currentSignal;

                if (NoteSoundMap.TryGetValue(currentSignal, out int soundKey))
                {
                    float gameVol = ModAssets.GetSFXVolume();
                    float finalVolume = gameVol * (volumePercent / 100f) * VolumeBoost;
                    AudioUtil.PlaySound(soundKey, gameObject.transform.GetPosition(), finalVolume, 1f);
                    SpawnMusicParticles();
                }
            }
        }

        private void SpawnMusicParticles()
        {
            if (musicParticlesCooldownRemaining > 0f) return;

            if (musicParticles == null)
            {
                GameObject prefab = EffectPrefabs.Instance?.HappySingerFX;
                if (prefab == null)
                {
                    Debug.LogWarning("MusicBox: HappySingerFX is unavailable.");
                    return;
                }

                musicParticles = Util.KInstantiate(
                    prefab,
                    transform.GetPosition() + MusicParticlesOffset);
                musicParticles.transform.SetParent(transform);
                musicParticles.SetActive(true);
            }

            musicParticlesTimeRemaining = MusicParticlesDuration;
            musicParticlesCooldownRemaining = MusicParticlesCooldown;
        }

        private void UpdateMusicParticles(float dt)
        {
            if (musicParticlesCooldownRemaining > 0f)
            {
                musicParticlesCooldownRemaining = Mathf.Max(0f, musicParticlesCooldownRemaining - dt);
            }

            if (musicParticles == null) return;

            musicParticlesTimeRemaining -= dt;
            if (musicParticlesTimeRemaining <= 0f)
            {
                DestroyMusicParticles();
            }
        }

        private void DestroyMusicParticles()
        {
            if (musicParticles == null) return;

            musicParticles.SetActive(false);
            Util.KDestroyGameObject(musicParticles);
            musicParticles = null;
            musicParticlesTimeRemaining = 0f;
        }

        protected override void OnCleanUp()
        {
            DestroyMusicParticles();
            base.OnCleanUp();
        }
    }
}
