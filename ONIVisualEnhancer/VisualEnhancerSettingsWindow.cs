using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ONIVisualEnhancer
{
    internal sealed class VisualEnhancerSettingsWindow : MonoBehaviour
    {
        private const float Width = 560f;
        private const float Height = 760f;

        private GameObject canvasRoot;
        private RectTransform panelRect;
        private TextMeshProUGUI presetLabel;
        private TextMeshProUGUI tintValue;
        private TextMeshProUGUI vignetteValue;
        private TextMeshProUGUI scanlineValue;
        private TextMeshProUGUI grainValue;
        private TextMeshProUGUI brightnessValue;
        private TextMeshProUGUI shadowValue;
        private TextMeshProUGUI letterboxValue;
        private TextMeshProUGUI scanlineDensityValue;
        private TextMeshProUGUI grainScaleValue;
        private TextMeshProUGUI grainSpeedValue;
        private TextMeshProUGUI pulseValue;
        private TextMeshProUGUI exposureValue;
        private TextMeshProUGUI contrastValue;
        private TextMeshProUGUI saturationValue;
        private TextMeshProUGUI temperatureValue;
        private TextMeshProUGUI hueShiftValue;
        private TextMeshProUGUI chromaticAberrationValue;
        private TextMeshProUGUI lensDistortionValue;
        private TextMeshProUGUI bloomValue;
        private TextMeshProUGUI liquidColorValue;
        private TextMeshProUGUI liquidShineValue;
        private TextMeshProUGUI liquidFlowValue;
        private TextMeshProUGUI solidColorValue;
        private TextMeshProUGUI solidShineValue;
        private TextMeshProUGUI materialTextureScaleValue;
        private KImage hideVignetteToggleImage;
        private TextMeshProUGUI hideVignetteToggleText;
        private KImage cameraPostProcessToggleImage;
        private TextMeshProUGUI cameraPostProcessToggleText;
        private KImage materialAdjustmentsToggleImage;
        private TextMeshProUGUI materialAdjustmentsToggleText;
        private bool visible;

        public void Toggle()
        {
            EnsureCreated();
            SetVisible(!visible);
        }

        private void Awake()
        {
            EnsureCreated();
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
            }
        }

        private void EnsureCreated()
        {
            if (canvasRoot != null)
            {
                return;
            }

            canvasRoot = new GameObject("ONIVisualEnhancerSettingsCanvas");
            canvasRoot.transform.SetParent(transform, false);

            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            canvasRoot.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject shade = CreateImage("Shade", canvasRoot.transform, new Color(0f, 0f, 0f, 0.28f));
            Stretch(shade.GetComponent<RectTransform>(), 0f, 0f);

            GameObject panel = CreateImage("Panel", canvasRoot.transform, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplySprite(panel.GetComponent<Image>(), "web_box", Color.white, Image.Type.Sliced, false, 2f);
            panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(Width, Height);
            panelRect.anchoredPosition = Vector2.zero;

            CreateHeader(panel.transform);
            CreateBody(panel.transform);
        }

        private void CreateHeader(Transform parent)
        {
            GameObject header = CreateImage("Header", parent, new Color(0.36f, 0.42f, 0.47f, 1f));
            RectTransform rect = header.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(8f, -58f);
            rect.offsetMax = new Vector2(-8f, -8f);

            TextMeshProUGUI title = CreateText("Title", header.transform, "缺氧视觉增强", 15, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.95f, 0.97f, 0.99f, 1f);
            Stretch(title.rectTransform(), 12f, 42f, 0f, 0f);

            GameObject close = CreateButton("Close", header.transform, string.Empty, () => SetVisible(false), BlueStyle());
            RectTransform closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(28f, 26f);
            closeRect.anchoredPosition = new Vector2(-8f, 0f);

            Image icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(close.transform, false);
            icon.sprite = GetSprite("cancel");
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Stretch(icon.rectTransform(), 6f, 6f);
        }

        private void CreateBody(Transform parent)
        {
            GameObject body = CreateImage("Body", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(12f, 12f);
            bodyRect.offsetMax = new Vector2(-12f, -72f);

            RectTransform content = CreateScrollBody(body.transform);
            CreatePresetSection(content);
            CreateToggleSection(content);
            CreateCameraPostProcessToggle(content);
            CreateCameraPostProcessingSection(content);
            CreateMaterialToggleSection(content);
            CreateMaterialSection(content);
            CreatePostProcessingSection(content);
            CreateFooter(content);
            Refresh();
        }

        private void CreatePresetSection(Transform parent)
        {
            GameObject card = CreateCard("PresetCard", parent, 76f);
            AddCardTitle(card.transform, "滤镜预设");

            GameObject row = CreateRow("PresetRow", card.transform, 36f, 8f);
            CreateButton("PreviousPreset", row.transform, "<", () => VisualEnhancerController.CyclePreset(-1), BlueStyle(), 42f);
            presetLabel = CreateText("PresetName", row.transform, string.Empty, 13, TextAlignmentOptions.Center);
            presetLabel.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            presetLabel.fontStyle = FontStyles.Bold;
            presetLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            CreateButton("NextPreset", row.transform, ">", () => VisualEnhancerController.CyclePreset(1), BlueStyle(), 42f);
        }

        private void CreateToggleSection(Transform parent)
        {
            GameObject card = CreateCard("VignetteCard", parent, 56f);
            GameObject row = CreateRow("VignetteRow", card.transform, 34f, 10f);

            TextMeshProUGUI label = CreateText("Label", row.transform, "隐藏原版暗角 / 警报暗角", 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject toggle = CreateButton("Toggle", row.transform, string.Empty, ToggleGameVignette, PinkStyle(), 72f);
            hideVignetteToggleImage = toggle.GetComponent<KImage>();
            hideVignetteToggleText = CreateText("ToggleText", toggle.transform, string.Empty, 11, TextAlignmentOptions.Center);
            hideVignetteToggleText.color = Color.white;
            Stretch(hideVignetteToggleText.rectTransform(), 4f, 0f);
        }

        private void CreatePostProcessingSection(Transform parent)
        {
            GameObject card = CreateCard("PostProcessingCard", parent, 438f);
            AddCardTitle(card.transform, "叠加层后期参数");
            brightnessValue = CreateSliderRow(card.transform, "亮度", VisualEnhancerSettings.Brightness, VisualEnhancerSettings.SetBrightness);
            shadowValue = CreateSliderRow(card.transform, "暗部压低", VisualEnhancerSettings.Shadow, VisualEnhancerSettings.SetShadow);
            tintValue = CreateSliderRow(card.transform, "色调强度", VisualEnhancerSettings.TintIntensity, VisualEnhancerSettings.SetTintIntensity);
            vignetteValue = CreateSliderRow(card.transform, "暗角强度", VisualEnhancerSettings.VignetteIntensity, VisualEnhancerSettings.SetVignetteIntensity);
            letterboxValue = CreateSliderRow(card.transform, "电影遮罩", VisualEnhancerSettings.Letterbox, VisualEnhancerSettings.SetLetterbox);
            scanlineValue = CreateSliderRow(card.transform, "扫描线强度", VisualEnhancerSettings.ScanlineIntensity, VisualEnhancerSettings.SetScanlineIntensity);
            scanlineDensityValue = CreateSliderRow(card.transform, "扫描线密度", VisualEnhancerSettings.ScanlineDensity, VisualEnhancerSettings.SetScanlineDensity);
            grainValue = CreateSliderRow(card.transform, "颗粒强度", VisualEnhancerSettings.GrainIntensity, VisualEnhancerSettings.SetGrainIntensity);
            grainScaleValue = CreateSliderRow(card.transform, "颗粒大小", VisualEnhancerSettings.GrainScale, VisualEnhancerSettings.SetGrainScale);
            grainSpeedValue = CreateSliderRow(card.transform, "颗粒速度", VisualEnhancerSettings.GrainSpeed, VisualEnhancerSettings.SetGrainSpeed);
            pulseValue = CreateSliderRow(card.transform, "色彩脉冲", VisualEnhancerSettings.Pulse, VisualEnhancerSettings.SetPulse);
        }

        private void CreateCameraPostProcessToggle(Transform parent)
        {
            GameObject card = CreateCard("CameraPostProcessToggleCard", parent, 56f);
            GameObject row = CreateRow("CameraPostProcessRow", card.transform, 34f, 10f);

            TextMeshProUGUI label = CreateText("Label", row.transform, "启用相机后处理（需要 Shader 包）", 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject toggle = CreateButton("Toggle", row.transform, string.Empty, ToggleCameraPostProcess, PinkStyle(), 72f);
            cameraPostProcessToggleImage = toggle.GetComponent<KImage>();
            cameraPostProcessToggleText = CreateText("ToggleText", toggle.transform, string.Empty, 11, TextAlignmentOptions.Center);
            cameraPostProcessToggleText.color = Color.white;
            Stretch(cameraPostProcessToggleText.rectTransform(), 4f, 0f);
        }

        private void CreateCameraPostProcessingSection(Transform parent)
        {
            GameObject card = CreateCard("CameraPostProcessingCard", parent, 326f);
            AddCardTitle(card.transform, "相机后期参数");
            exposureValue = CreateSliderRow(card.transform, "曝光", VisualEnhancerSettings.Exposure, VisualEnhancerSettings.SetExposure);
            contrastValue = CreateSliderRow(card.transform, "对比度", VisualEnhancerSettings.Contrast, VisualEnhancerSettings.SetContrast);
            saturationValue = CreateSliderRow(card.transform, "饱和度", VisualEnhancerSettings.Saturation, VisualEnhancerSettings.SetSaturation);
            temperatureValue = CreateSliderRow(card.transform, "色温", VisualEnhancerSettings.Temperature, VisualEnhancerSettings.SetTemperature);
            hueShiftValue = CreateSliderRow(card.transform, "色相偏移", VisualEnhancerSettings.HueShift, VisualEnhancerSettings.SetHueShift);
            chromaticAberrationValue = CreateSliderRow(card.transform, "色散", VisualEnhancerSettings.ChromaticAberration, VisualEnhancerSettings.SetChromaticAberration);
            lensDistortionValue = CreateSliderRow(card.transform, "镜头畸变", VisualEnhancerSettings.LensDistortion, VisualEnhancerSettings.SetLensDistortion);
            bloomValue = CreateSliderRow(card.transform, "泛光", VisualEnhancerSettings.Bloom, VisualEnhancerSettings.SetBloom);
        }

        private void CreateMaterialToggleSection(Transform parent)
        {
            GameObject card = CreateCard("MaterialToggleCard", parent, 56f);
            GameObject row = CreateRow("MaterialToggleRow", card.transform, 34f, 10f);

            TextMeshProUGUI label = CreateText("Label", row.transform, "启用材质参数调节", 12, TextAlignmentOptions.MidlineLeft);
            label.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject toggle = CreateButton("Toggle", row.transform, string.Empty, ToggleMaterialAdjustments, PinkStyle(), 72f);
            materialAdjustmentsToggleImage = toggle.GetComponent<KImage>();
            materialAdjustmentsToggleText = CreateText("ToggleText", toggle.transform, string.Empty, 11, TextAlignmentOptions.Center);
            materialAdjustmentsToggleText.color = Color.white;
            Stretch(materialAdjustmentsToggleText.rectTransform(), 4f, 0f);
        }

        private void CreateMaterialSection(Transform parent)
        {
            GameObject card = CreateCard("MaterialCard", parent, 252f);
            AddCardTitle(card.transform, "材质参数");
            liquidColorValue = CreateSliderRow(card.transform, "液体色彩", VisualEnhancerSettings.LiquidColor, VisualEnhancerSettings.SetLiquidColor);
            liquidShineValue = CreateSliderRow(card.transform, "液体反光", VisualEnhancerSettings.LiquidShine, VisualEnhancerSettings.SetLiquidShine);
            liquidFlowValue = CreateSliderRow(card.transform, "液体流动", VisualEnhancerSettings.LiquidFlow, VisualEnhancerSettings.SetLiquidFlow);
            solidColorValue = CreateSliderRow(card.transform, "固体色彩", VisualEnhancerSettings.SolidColor, VisualEnhancerSettings.SetSolidColor);
            solidShineValue = CreateSliderRow(card.transform, "固体反光", VisualEnhancerSettings.SolidShine, VisualEnhancerSettings.SetSolidShine);
            materialTextureScaleValue = CreateSliderRow(card.transform, "纹理缩放", VisualEnhancerSettings.MaterialTextureScale, VisualEnhancerSettings.SetMaterialTextureScale);
        }

        private TextMeshProUGUI CreateSliderRow(Transform parent, string label, float value, System.Action<float> apply)
        {
            GameObject row = CreateRow(label + "Row", parent, 34f, 8f);

            TextMeshProUGUI name = CreateText("Name", row.transform, label, 11, TextAlignmentOptions.MidlineLeft);
            name.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            name.fontStyle = FontStyles.Bold;
            name.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;

            Slider slider = CreateSlider(row.transform, value);
            slider.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            TextMeshProUGUI valueText = CreateText("Value", row.transform, value.ToString("0.00"), 11, TextAlignmentOptions.MidlineRight);
            valueText.color = new Color(0.28f, 0.29f, 0.29f, 1f);
            valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;

            slider.onValueChanged.AddListener(next =>
            {
                apply(next);
                valueText.text = next.ToString("0.00");
                VisualEnhancerController.ApplySettingsChanged();
            });

            return valueText;
        }

        private void CreateFooter(Transform parent)
        {
            GameObject row = CreateRow("Footer", parent, 34f, 10f);
            row.AddComponent<LayoutElement>().preferredHeight = 36f;
            CreateButton("Reset", row.transform, "重置", ResetIntensities, BlueStyle(), 110f);
            CreateButton("Close", row.transform, "关闭", () => SetVisible(false), BlueStyle(), 110f);
        }

        private void ToggleGameVignette()
        {
            VisualEnhancerSettings.SetHideGameVignette(!VisualEnhancerSettings.HideGameVignette);
            VisualEnhancerController.ApplySettingsChanged();
            Refresh();
        }

        private void ToggleCameraPostProcess()
        {
            VisualEnhancerSettings.SetCameraPostProcessEnabled(!VisualEnhancerSettings.CameraPostProcessEnabled);
            VisualEnhancerController.ApplySettingsChanged();
            Refresh();
        }

        private void ToggleMaterialAdjustments()
        {
            VisualEnhancerSettings.SetMaterialAdjustmentsEnabled(!VisualEnhancerSettings.MaterialAdjustmentsEnabled);
            VisualEnhancerController.ApplySettingsChanged();
            Refresh();
        }

        private void ResetIntensities()
        {
            VisualEnhancerSettings.SetTintIntensity(1f);
            VisualEnhancerSettings.SetVignetteIntensity(1f);
            VisualEnhancerSettings.SetScanlineIntensity(1f);
            VisualEnhancerSettings.SetGrainIntensity(1f);
            VisualEnhancerSettings.SetBrightness(1f);
            VisualEnhancerSettings.SetShadow(0f);
            VisualEnhancerSettings.SetLetterbox(1f);
            VisualEnhancerSettings.SetScanlineDensity(1f);
            VisualEnhancerSettings.SetGrainScale(1f);
            VisualEnhancerSettings.SetGrainSpeed(1f);
            VisualEnhancerSettings.SetPulse(0f);
            VisualEnhancerSettings.SetExposure(1f);
            VisualEnhancerSettings.SetContrast(1f);
            VisualEnhancerSettings.SetSaturation(1f);
            VisualEnhancerSettings.SetTemperature(1f);
            VisualEnhancerSettings.SetHueShift(1f);
            VisualEnhancerSettings.SetChromaticAberration(0f);
            VisualEnhancerSettings.SetLensDistortion(0f);
            VisualEnhancerSettings.SetBloom(0f);
            VisualEnhancerSettings.SetLiquidColor(1f);
            VisualEnhancerSettings.SetLiquidShine(1f);
            VisualEnhancerSettings.SetLiquidFlow(1f);
            VisualEnhancerSettings.SetSolidColor(1f);
            VisualEnhancerSettings.SetSolidShine(1f);
            VisualEnhancerSettings.SetMaterialTextureScale(1f);
            VisualEnhancerController.ApplySettingsChanged();
            Rebuild();
        }

        public void Refresh()
        {
            if (presetLabel != null)
            {
                presetLabel.text = VisualEnhancerSettings.GetCurrentPreset().Name;
            }

            SetText(tintValue, VisualEnhancerSettings.TintIntensity);
            SetText(vignetteValue, VisualEnhancerSettings.VignetteIntensity);
            SetText(scanlineValue, VisualEnhancerSettings.ScanlineIntensity);
            SetText(grainValue, VisualEnhancerSettings.GrainIntensity);
            SetText(brightnessValue, VisualEnhancerSettings.Brightness);
            SetText(shadowValue, VisualEnhancerSettings.Shadow);
            SetText(letterboxValue, VisualEnhancerSettings.Letterbox);
            SetText(scanlineDensityValue, VisualEnhancerSettings.ScanlineDensity);
            SetText(grainScaleValue, VisualEnhancerSettings.GrainScale);
            SetText(grainSpeedValue, VisualEnhancerSettings.GrainSpeed);
            SetText(pulseValue, VisualEnhancerSettings.Pulse);
            SetText(exposureValue, VisualEnhancerSettings.Exposure);
            SetText(contrastValue, VisualEnhancerSettings.Contrast);
            SetText(saturationValue, VisualEnhancerSettings.Saturation);
            SetText(temperatureValue, VisualEnhancerSettings.Temperature);
            SetText(hueShiftValue, VisualEnhancerSettings.HueShift);
            SetText(chromaticAberrationValue, VisualEnhancerSettings.ChromaticAberration);
            SetText(lensDistortionValue, VisualEnhancerSettings.LensDistortion);
            SetText(bloomValue, VisualEnhancerSettings.Bloom);
            SetText(liquidColorValue, VisualEnhancerSettings.LiquidColor);
            SetText(liquidShineValue, VisualEnhancerSettings.LiquidShine);
            SetText(liquidFlowValue, VisualEnhancerSettings.LiquidFlow);
            SetText(solidColorValue, VisualEnhancerSettings.SolidColor);
            SetText(solidShineValue, VisualEnhancerSettings.SolidShine);
            SetText(materialTextureScaleValue, VisualEnhancerSettings.MaterialTextureScale);

            if (hideVignetteToggleImage != null)
            {
                hideVignetteToggleImage.colorStyleSetting = VisualEnhancerSettings.HideGameVignette ? PinkStyle() : BlueStyle();
                hideVignetteToggleImage.ColorState = KImage.ColorSelector.Inactive;
            }

            if (hideVignetteToggleText != null)
            {
                hideVignetteToggleText.text = VisualEnhancerSettings.HideGameVignette ? "开" : "关";
            }

            if (cameraPostProcessToggleImage != null)
            {
                cameraPostProcessToggleImage.colorStyleSetting = VisualEnhancerSettings.CameraPostProcessEnabled ? PinkStyle() : BlueStyle();
                cameraPostProcessToggleImage.ColorState = KImage.ColorSelector.Inactive;
            }

            if (cameraPostProcessToggleText != null)
            {
                cameraPostProcessToggleText.text = VisualEnhancerSettings.CameraPostProcessEnabled ? "开" : "关";
            }

            if (materialAdjustmentsToggleImage != null)
            {
                materialAdjustmentsToggleImage.colorStyleSetting = VisualEnhancerSettings.MaterialAdjustmentsEnabled ? PinkStyle() : BlueStyle();
                materialAdjustmentsToggleImage.ColorState = KImage.ColorSelector.Inactive;
            }

            if (materialAdjustmentsToggleText != null)
            {
                materialAdjustmentsToggleText.text = VisualEnhancerSettings.MaterialAdjustmentsEnabled ? "开" : "关";
            }
        }

        private void Rebuild()
        {
            if (canvasRoot != null)
            {
                Destroy(canvasRoot);
                canvasRoot = null;
            }

            EnsureCreated();
            SetVisible(visible);
        }

        private void SetVisible(bool next)
        {
            visible = next;
            if (canvasRoot != null)
            {
                canvasRoot.SetActive(visible);
                if (visible)
                {
                    CenterPanel();
                    Refresh();
                }
            }
        }

        private void CenterPanel()
        {
            if (panelRect == null)
            {
                return;
            }

            panelRect.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateCard(string name, Transform parent, float height)
        {
            GameObject card = CreateImage(name, parent, new Color(0.83f, 0.83f, 0.77f, 1f));
            ApplySprite(card.GetComponent<Image>(), "web_box", Color.white, Image.Type.Sliced, false, 2f);
            VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 8);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            card.AddComponent<LayoutElement>().preferredHeight = height;
            return card;
        }

        private static RectTransform CreateScrollBody(Transform parent)
        {
            GameObject viewport = CreateImage("ScrollViewport", parent, new Color(0.72f, 0.72f, 0.66f, 1f));
            Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("ScrollContent");
            contentObject.transform.SetParent(viewport.transform, false);
            RectTransform content = contentObject.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(12f, 0f);
            content.offsetMax = new Vector2(-24f, -12f);

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 12, 12);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(viewport.transform);
            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.10f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.08f;
            scroll.scrollSensitivity = 26f;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 2f;
            return content;
        }

        private static Scrollbar CreateScrollbar(Transform parent)
        {
            GameObject scrollbarObject = new GameObject("Scrollbar");
            scrollbarObject.transform.SetParent(parent, false);
            RectTransform scrollbarRect = scrollbarObject.AddComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = Vector2.one;
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-13f, 4f);
            scrollbarRect.offsetMax = new Vector2(-4f, -4f);

            Image background = scrollbarObject.AddComponent<Image>();
            ApplySprite(background, "build_menu_scrollbar_frame", new Color(0.09f, 0.10f, 0.12f, 1f), Image.Type.Sliced, false, 1f);

            GameObject slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObject.transform, false);
            RectTransform slidingRect = slidingArea.AddComponent<RectTransform>();
            Stretch(slidingRect, 0f, 0f);
            slidingRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(slidingArea.transform, false);
            RectTransform handleRect = handleObject.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(16f, -10f);

            Image handleImage = handleObject.AddComponent<Image>();
            ApplySprite(handleImage, "build_menu_scrollbar_inner", new Color(0.63f, 0.64f, 0.68f, 1f), Image.Type.Sliced, false, 1f);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.interactable = true;
            scrollbar.transition = Selectable.Transition.None;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.handleRect = handleRect;
            scrollbar.targetGraphic = handleImage;
            return scrollbar;
        }

        private static void AddCardTitle(Transform parent, string text)
        {
            TextMeshProUGUI title = CreateText("CardTitle", parent, text, 11, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.24f, 0.25f, 0.25f, 1f);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private static GameObject CreateRow(string name, Transform parent, float height, float spacing)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = height;
            return row;
        }

        private static Slider CreateSlider(Transform parent, float value)
        {
            GameObject root = new GameObject("Slider");
            root.transform.SetParent(parent, false);
            root.AddComponent<RectTransform>();
            root.AddComponent<LayoutElement>().preferredHeight = 22f;

            Slider slider = root.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 2f;
            slider.value = value;

            GameObject background = CreateImage("Background", root.transform, new Color(0.34f, 0.34f, 0.32f, 1f));
            Stretch(background.GetComponent<RectTransform>(), 0f, 0f, 6f, 6f);

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            Stretch(fillAreaRect, 6f, 6f, 7f, 7f);

            GameObject fill = CreateImage("Fill", fillArea.transform, OniPinkInactive());
            Stretch(fill.GetComponent<RectTransform>(), 0f, 0f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(root.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            Stretch(handleAreaRect, 8f, 8f, 0f, 0f);

            GameObject handle = CreateImage("Handle", handleArea.transform, Color.white);
            ApplySprite(handle.GetComponent<Image>(), "game_speed_selected_med", Color.white, Image.Type.Simple, true, 1f);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 22f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();

            return slider;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, System.Action onClick, ColorStyleSetting style, float preferredWidth = -1f)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage image = buttonObject.AddComponent<KImage>();
            ApplySprite(image, "web_button", Color.white, Image.Type.Sliced, false, 2f);
            image.colorStyleSetting = style;
            image.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.onClick += () => onClick?.Invoke();

            if (!string.IsNullOrEmpty(text))
            {
                TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 11, TextAlignmentOptions.Center);
                label.color = new Color(0.94f, 0.96f, 0.98f, 1f);
                Stretch(label.rectTransform(), 4f, 0f);
            }

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = preferredWidth > 0f ? preferredWidth : 88f;
            layout.preferredHeight = 30f;
            return buttonObject;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.AddComponent<RectTransform>();
            Image image = gameObject.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return gameObject;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static void SetText(TextMeshProUGUI text, float value)
        {
            if (text != null)
            {
                text.text = value.ToString("0.00");
            }
        }

        private static void Stretch(RectTransform rect, float horizontal, float vertical)
        {
            Stretch(rect, horizontal, horizontal, vertical, vertical);
        }

        private static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void ApplySprite(Image image, string spriteName, Color fallbackColor, Image.Type type, bool preserveAspect, float pixelsPerUnitMultiplier)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = GetSprite(spriteName);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = type;
                image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
                image.fillCenter = true;
            }

            image.color = fallbackColor;
            image.preserveAspect = preserveAspect;
        }

        private static Sprite GetSprite(string spriteName)
        {
            return Assets.GetSprite(spriteName);
        }

        private static ColorStyleSetting BlueStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = new Color(0.11f, 0.12f, 0.16f, 1f);
            style.inactiveColor = new Color(0.17f, 0.19f, 0.25f, 1f);
            style.hoverColor = new Color(0.25f, 0.28f, 0.35f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = new Color(0.62f, 0.62f, 0.59f, 1f);
            style.disabledhoverColor = new Color(0.50f, 0.49f, 0.46f, 1f);
            return style;
        }

        private static ColorStyleSetting PinkStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.activeColor = OniPinkActive();
            style.inactiveColor = OniPinkInactive();
            style.hoverColor = OniPinkHover();
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = Color.clear;
            style.disabledhoverColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            return style;
        }

        private static Color OniPinkInactive()
        {
            return new Color(0.5294118f, 0.2724914f, 0.4009516f, 1f);
        }

        private static Color OniPinkHover()
        {
            return new Color(0.6176471f, 0.3315311f, 0.4745891f, 1f);
        }

        private static Color OniPinkActive()
        {
            return new Color(0.7941176f, 0.4496107f, 0.6242238f, 1f);
        }
    }
}
