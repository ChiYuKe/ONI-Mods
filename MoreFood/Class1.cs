using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine;
using HarmonyLib;

namespace EternalDecay.Content.Comps
{
    public class AETE_CursorDurationMarker : KMonoBehaviour, ISimEveryTick
    {
        public void SimEveryTick(float dt)
        {
            if (this.elapsed < this.duration)
            {
                if (this.ring != null)
                {
                    this.ring.fillAmount = Mathf.Clamp01(1f - this.elapsed / this.duration);
                }
                this.elapsed += dt;
                Transform transform = base.transform;
                Vector3 mousePos = KInputManager.GetMousePos();
                mousePos.z = -5f;
                transform.position = mousePos;
                return;
            }
            if (base.isActiveAndEnabled)
            {
                base.gameObject.SetActive(false);
            }
        }

        public void Show(float duration, Color color)
        {
            base.gameObject.SetActive(true);
            if (this.ring == null)
            {
                this.InitRing();
            }
            this.ring.color = color;
            this.elapsed = 0f;
            this.duration = duration;
        }
        private void InitRing()
        {
            this.ring = base.gameObject.AddOrGet<Image>();
            this.ring.type = Image.Type.Filled;
            this.ring.fillMethod = Image.FillMethod.Radial360;
            this.ring.sprite = Assets.GetSprite("akisextratwitchevents_small_ring");
            CanvasGroup canvasGroup = base.gameObject.AddOrGet<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            RectTransform rectTransform = base.gameObject.AddOrGet<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.localScale *= 0.5f;
        }


        private Image ring;
        private float duration;
        private float elapsed;
    }

}
