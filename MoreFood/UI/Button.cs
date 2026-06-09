using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

namespace CykUtils
{
    public class KButton : KMonoBehaviour, IEventSystemHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public event global::System.Action OnClick;

        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            this.material = this.image.material;
            this.interactable = true;
        }

        public void SetInteractable(bool interactable)
        {
            if (interactable == this.interactable)
            {
                return;
            }
            this.interactable = interactable;
            if (this.button == null)
            {
                this.image.color = (interactable ? this.normalColor : this.disabledColor);
                return;
            }
            this.button.interactable = interactable;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }
            if (KInputManager.isFocused)
            {
                KInputManager.SetUserActive();
                KMonoBehaviour.PlaySound("event:/UI/Mouse/HUD_Click_Open");
                global::System.Action onClick = this.OnClick;
                if (onClick == null)
                {
                    return;
                }
                onClick();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!this.interactable)
            {
                return;
            }
            if (KInputManager.isFocused)
            {
                if (this.button == null)
                {
                    this.image.color = this.hoverColor;
                }
                KInputManager.SetUserActive();
                KMonoBehaviour.PlaySound("event:/UI/Mouse/HUD_Mouseover");
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (this.button == null)
            {
                this.image.color = this.normalColor;
            }
        }

        private bool interactable;

        private Material material;

        [MyCmpReq]
        public Image image;

        [MyCmpGet]
        private Button button;
        [SerializeField]
        public Color disabledColor = new Color(0.78f, 0.78f, 0.78f);
        [SerializeField]
        public Color normalColor = new Color(0.243f, 0.263f, 0.341f);
        [SerializeField]
        public Color hoverColor = new Color(0.345f, 0.373f, 0.702f);
    }
}
