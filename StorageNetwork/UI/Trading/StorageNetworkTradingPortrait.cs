using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Trading;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    // Reusable UI-only portrait. Uses the same symbol composition as the duplicant table.
    internal sealed class StorageNetworkTradingPortrait : MonoBehaviour
    {
        private TradingAppearance appearance;
        private int supplierId = -1;
        private GameObject portrait;
        private Image fallback;
        private bool dirty;
        private Coroutine pending;

        internal void Bind(TradingSupplier supplier)
        {
            if (supplierId == supplier.Id && appearance == supplier.Appearance) return;
            supplierId = supplier.Id;
            appearance = supplier.Appearance;
            dirty = true;
        }

        private void Update()
        {
            if (dirty && pending == null) pending = StartCoroutine(Rebuild());
        }

        private void OnDisable()
        {
            if (pending != null) { dirty = true; StopCoroutine(pending); }
            pending = null;
        }

        private IEnumerator Rebuild()
        {
            // Always yield first, so even an unavailable template clears the
            // coroutine handle after StartCoroutine has returned it to Update.
            yield return null;
            dirty = false;
            if (portrait != null) { portrait.SetActive(false); Destroy(portrait); }
            EnsureFallback();
            if (appearance == null) { pending = null; yield break; }
            GameObject template = Assets.UIPrefabs.TableScreenWidgets.MinionPortrait;
            if (template == null) { pending = null; yield break; }
            portrait = Util.KInstantiateUI(template, gameObject, force_active: false);
            portrait.name = "SupplierCompositePortrait";
            CrewPortrait crew = portrait.GetComponent<CrewPortrait>();
            if (crew != null) { crew.useLabels = false; crew.enabled = false; }
            foreach (Graphic graphic in portrait.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            RectTransform rect = portrait.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            LayoutElement layout = portrait.GetComponent<LayoutElement>();
            if (layout != null) layout.ignoreLayout = true;
            KBatchedAnimController controller = portrait.GetComponentInChildren<KBatchedAnimController>(true);
            if (controller == null) { pending = null; yield break; }
            controller.gameObject.SetActive(true);
            portrait.SetActive(true);
            // Let native KMonoBehaviour Start initialize animation and symbol overrides.
            yield return null;
            if (!dirty && controller != null && isActiveAndEnabled)
            {
                try
                {
                    ApplyAppearance(controller);
                    // Its canvas renderer is added when the native animation
                    // batch initializes, after the prefab has been instantiated.
                    KBatchedAnimCanvasRenderer renderer = controller.GetComponent<KBatchedAnimCanvasRenderer>();
                    if (renderer != null) ((IMaskable)renderer).RecalculateMasking();
                    if (crew != null && crew.targetImage != null) crew.targetImage.enabled = false;
                    fallback.enabled = false;
                }
                catch (Exception ex)
                {
                    portrait.SetActive(false);
                    Debug.LogWarning("[StorageNetwork] Supplier portrait could not be composed: " + ex);
                }
            }
            pending = null;
        }

        private void EnsureFallback()
        {
            if (fallback == null)
            {
                var imageObject = new GameObject("PortraitFallback", typeof(RectTransform), typeof(Image));
                imageObject.transform.SetParent(transform, false);
                fallback = imageObject.GetComponent<Image>();
                fallback.sprite = Assets.GetSprite("unknown");
                fallback.preserveAspect = true;
                fallback.raycastTarget = false;
                fallback.rectTransform.anchorMin = Vector2.zero;
                fallback.rectTransform.anchorMax = Vector2.one;
                fallback.rectTransform.offsetMin = fallback.rectTransform.offsetMax = Vector2.zero;
            }
            fallback.enabled = true;
        }

        private void ApplyAppearance(KBatchedAnimController controller)
        {
            Personality personality = string.IsNullOrEmpty(appearance.BasePersonalityId) ? null : Db.Get().Personalities.TryGet(appearance.BasePersonalityId);
            if (personality == null) personality = Db.Get().Personalities.resources.FirstOrDefault(p => p.model == GameTags.Minions.Models.Standard);
            if (personality == null) throw new InvalidOperationException("Supplier base personality is unavailable.");
            var slots = Db.Get().AccessorySlots;
            KCompBuilder.BodyData body = MinionStartingStats.CreateBodyData(personality);
            if (!string.IsNullOrEmpty(appearance.HeadShape) && slots.HeadShape.Lookup(appearance.HeadShape) != null) body.headShape = appearance.HeadShape;
            if (!string.IsNullOrEmpty(appearance.Hair) && slots.Hair.Lookup(appearance.Hair) != null) body.hair = appearance.Hair;
            if (!string.IsNullOrEmpty(appearance.Eyes) && slots.Eyes.Lookup(appearance.Eyes) != null) body.eyes = appearance.Eyes;
            if (!string.IsNullOrEmpty(appearance.Mouth) && slots.Mouth.Lookup(appearance.Mouth) != null) body.mouth = appearance.Mouth;
            // CrewPortrait's UI controller has SymbolOverrideController, not the
            // Accessorizer attached to living duplicants. Compose its symbols directly.
            SymbolOverrideController overrides = controller.GetComponent<SymbolOverrideController>();
            if (overrides == null) throw new InvalidOperationException("Native portrait has no symbol override controller.");
            overrides.RemoveAllSymbolOverrides();
            Accessory[] accessories =
            {
                slots.HeadShape.Lookup(body.headShape), slots.Hair.Lookup(body.hair),
                slots.Eyes.Lookup(body.eyes), slots.Mouth.Lookup(body.mouth),
                slots.Body.Lookup(body.body), slots.Arm.Lookup(body.arms),
                slots.ArmLower.Lookup(body.armslower), slots.Neck.Lookup(body.neck),
                slots.Pelvis.Lookup(body.pelvis), slots.Leg.Lookup(body.legs),
                slots.Foot.Lookup(body.foot), slots.Hand.Lookup(body.hand),
                slots.Cuff.Lookup(body.cuff), slots.Belt.Lookup(body.belt),
                slots.ArmLowerSkin.Lookup(body.armLowerSkin), slots.ArmUpperSkin.Lookup(body.armUpperSkin),
                slots.LegSkin.Lookup(body.legSkin)
            };
            var visible = new HashSet<KAnimHashedString>();
            var hidden = new HashSet<KAnimHashedString>();
            foreach (AccessorySlot slot in slots.resources) hidden.Add(slot.targetSymbolId);
            foreach (Accessory accessory in accessories)
            {
                if (accessory == null) continue;
                overrides.AddSymbolOverride(accessory.slot.targetSymbolId, accessory.symbol, accessory.slot.overrideLayer);
                visible.Add(accessory.slot.targetSymbolId);
                hidden.Remove(accessory.slot.targetSymbolId);
            }
            foreach (Accessory required in new[] { accessories[0], accessories[1], accessories[2], accessories[3] })
                if (required == null) throw new InvalidOperationException("Supplier face accessory is unavailable.");
            controller.BatchSetSymbolsVisiblity(visible, true);
            controller.BatchSetSymbolsVisiblity(hidden, false);
            controller.animScale = 0.25f;
            controller.TintColour = Color.white;
            controller.Play("ui_idle", KAnim.PlayMode.Loop);
            controller.SetSymbolVisiblity("snapTo_neck", false);
            controller.SetSymbolVisiblity("snapTo_goggles", false);
            controller.SetSymbolVisiblity(slots.Hat.targetSymbolId, false);
            controller.SetSymbolVisiblity(slots.HatHair.targetSymbolId, false);
        }
    }
}
