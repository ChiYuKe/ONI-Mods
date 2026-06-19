using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkOrderProductionCenterDiskSideScreen : SideScreenContent
    {
        private const float Width = 280f;
        private const float Height = 230f;
        private StorageNetworkOrderProductionCenter center;
        private Transform root;
        private KButton engraveButton;
        private KButton orderButton;
        private readonly TextMeshProUGUI[] slotLabels = new TextMeshProUGUI[3];
        private readonly KButton[] slotButtons = new KButton[3];
        private readonly TextMeshProUGUI[] slotButtonLabels = new TextMeshProUGUI[3];

        public StorageNetworkOrderProductionCenterDiskSideScreen()
        {
            titleKey = "STRINGS.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE";
            CheckShouldShowTopTitle = () => false;
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            EnsureBuilt();
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkOrderProductionCenter>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            center = target != null ? target.GetComponent<StorageNetworkOrderProductionCenter>() : null;
            EnsureBuilt();
            Refresh();
        }

        public override void ClearTarget()
        {
            center = null;
            base.ClearTarget();
        }

        public override int GetSideScreenSortOrder()
        {
            return 15;
        }

        private void Build()
        {
            RectTransform screenRect = gameObject.GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            screenRect.anchorMin = new Vector2(0f, 1f);
            screenRect.anchorMax = new Vector2(0f, 1f);
            screenRect.pivot = new Vector2(0f, 1f);
            screenRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width);
            screenRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);

            LayoutElement screenLayout = gameObject.GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
            screenLayout.minWidth = Width;
            screenLayout.preferredWidth = Width;
            screenLayout.minHeight = Height;
            screenLayout.preferredHeight = Height;
            screenLayout.flexibleWidth = 0f;

            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;
            GameObject panel = new GameObject("StorageNetworkDiskSideScreenContent");
            panel.transform.SetParent(parent, false);
            root = panel.transform;

            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Width);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Height);

            LayoutElement panelLayout = panel.AddComponent<LayoutElement>();
            panelLayout.minWidth = Width;
            panelLayout.preferredWidth = Width;
            panelLayout.minHeight = Height;
            panelLayout.preferredHeight = Height;
            panelLayout.flexibleWidth = 0f;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 0f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateHeader(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_SECTION_TITLE));
            engraveButton = CreateButtonRow(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ENGRAVE_BUTTON), 40f, true, () =>
            {
                center?.BeginEngraving();
                Refresh();
            });

            CreateHeader(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_CONFIG_TITLE));
            for (int i = 0; i < slotLabels.Length; i++)
            {
                int slotIndex = i;
                CreateSlotRow(slotIndex, () => OnSlotButtonPressed(slotIndex));
            }

            CreateHeader(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_ORDER_SECTION_TITLE));
            orderButton = CreateButtonRow(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_OPEN_BUTTON), 40f, true, () =>
            {
                if (center != null)
                {
                    StorageNetworkPanel.ShowOrderProductionCenter(center);
                }
            });
        }

        private void EnsureBuilt()
        {
            if (root == null)
            {
                Build();
            }
        }

        private void Refresh()
        {
            if (center == null)
            {
                return;
            }

            SetButtonInteractable(engraveButton, center.HasBlankDiskSlot);
            SetButtonInteractable(orderButton, true);

            for (int i = 0; i < slotLabels.Length; i++)
            {
                StorageNetworkOrderProductionCenter.EngravingDiskSlot slot = i < center.DiskSlots.Count ? center.DiskSlots[i] : null;
                slotLabels[i].text = string.Format("{0}. {1}", i + 1, GetSlotLabel(slot));
                slotLabels[i].ForceMeshUpdate(true);
                slotLabels[i].gameObject.GetComponent<ToolTip>()?.SetSimpleTooltip(GetSlotTooltip(slot));
                slotButtonLabels[i].text = slot != null && slot.HasDisk
                    ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_EJECT)
                    : Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_INSERT);
                slotButtonLabels[i].ForceMeshUpdate(true);
                SetButtonInteractable(slotButtons[i], true);
            }
        }

        private void OnSlotButtonPressed(int slotIndex)
        {
            if (center == null || slotIndex < 0 || slotIndex >= center.DiskSlots.Count)
            {
                return;
            }

            StorageNetworkOrderProductionCenter.EngravingDiskSlot slot = center.DiskSlots[slotIndex];
            if (slot != null && slot.HasDisk)
            {
                if (center.EjectDisk(slotIndex))
                {
                    StorageNetworkNotifications.ShowInfo(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_EJECTED));
                }
            }
            else if (center.InsertDiskFromWorld(slotIndex))
            {
                StorageNetworkNotifications.ShowInfo(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_INSERTED));
            }
            else
            {
                StorageNetworkNotifications.ShowWarning(Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_NO_AVAILABLE));
            }

            Refresh();
        }

        private void CreateHeader(string text)
        {
            GameObject header = new GameObject("SectionHeader");
            header.transform.SetParent(root, false);
            header.AddComponent<RectTransform>();
            Image image = header.AddComponent<Image>();
            ApplyWebBoxSprite(image);
            image.color = new Color(0.57f, 0.25f, 0.42f, 1f);
            header.AddComponent<LayoutElement>().preferredHeight = 24f;

            HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 8, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            TextMeshProUGUI label = CreateText(header.transform, text, 12, TextAlignmentOptions.MidlineLeft, Color.white, true);
            label.fontStyle = FontStyles.Bold;
        }

        private void CreateSlotRow(int slotIndex, System.Action onClick)
        {
            GameObject row = CreateRow("DiskSlotRow", 30f, 12, 8);
            slotLabels[slotIndex] = CreateText(row.transform, string.Empty, 11, TextAlignmentOptions.MidlineLeft, new Color(0.18f, 0.18f, 0.18f, 1f), true);
            slotLabels[slotIndex].gameObject.AddComponent<ToolTip>();
            slotButtons[slotIndex] = CreateButton(row.transform, string.Empty, 66f, 24f, onClick, out slotButtonLabels[slotIndex]);
        }

        private KButton CreateButtonRow(string text, float height, bool flexible, System.Action onClick)
        {
            GameObject row = CreateRow("ButtonRow", height, 12, 12);
            return CreateButton(row.transform, text, flexible ? 256f : 66f, 28f, onClick, out _);
        }

        private GameObject CreateRow(string name, float height, int leftPadding, int rightPadding)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(root, false);
            row.AddComponent<RectTransform>();
            row.AddComponent<LayoutElement>().preferredHeight = height;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(leftPadding, rightPadding, 3, 3);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string text, int size, TextAlignmentOptions alignment, Color color, bool flexible)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            textObject.AddComponent<RectTransform>();
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;

            LayoutElement layout = textObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = flexible ? 1f : 0f;
            layout.preferredHeight = 24f;
            return label;
        }

        private static KButton CreateButton(Transform parent, string text, float width, float height, System.Action onClick, out TextMeshProUGUI label)
        {
            GameObject buttonObject = new GameObject("KleiButton");
            buttonObject.transform.SetParent(parent, false);
            buttonObject.AddComponent<RectTransform>();

            KImage image = buttonObject.AddComponent<KImage>();
            image.type = Image.Type.Sliced;
            ApplyWebButtonSprite(image);
            image.colorStyleSetting = CreateButtonStyle();
            image.ColorState = KImage.ColorSelector.Inactive;

            KButton button = buttonObject.AddComponent<KButton>();
            button.bgImage = image;
            button.additionalKImages = new KImage[0];
            button.soundPlayer = new ButtonSoundPlayer();
            button.isInteractable = true;
            button.onClick += () => onClick?.Invoke();

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleWidth = width >= 200f ? 1f : 0f;

            label = CreateText(buttonObject.transform, text, 11, TextAlignmentOptions.Center, Color.white, false);
            label.fontStyle = FontStyles.Bold;
            label.gameObject.GetComponent<LayoutElement>().flexibleWidth = 1f;
            return button;
        }

        private static void SetButtonInteractable(KButton button, bool enabled)
        {
            if (button == null)
            {
                return;
            }

            button.isInteractable = enabled;
            KImage image = button.bgImage;
            if (image != null)
            {
                image.raycastTarget = true;
                image.ColorState = enabled ? KImage.ColorSelector.Inactive : KImage.ColorSelector.Disabled;
            }
        }

        private static void ConfigureButtonVisualFeedback(KButton button)
        {
            if (button == null)
            {
                return;
            }

            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image == null)
            {
                return;
            }

            image.raycastTarget = true;
            button.bgImage = image;

            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            AddButtonTrigger(trigger, EventTriggerType.PointerEnter, () => SetButtonState(button, KImage.ColorSelector.Hover));
            AddButtonTrigger(trigger, EventTriggerType.PointerExit, () => SetButtonState(button, KImage.ColorSelector.Inactive));
            AddButtonTrigger(trigger, EventTriggerType.PointerDown, () => SetButtonState(button, KImage.ColorSelector.Active));
            AddButtonTrigger(trigger, EventTriggerType.PointerUp, () => SetButtonState(button, KImage.ColorSelector.Hover));
        }

        private static void AddButtonTrigger(EventTrigger trigger, EventTriggerType type, System.Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static void SetButtonState(KButton button, KImage.ColorSelector state)
        {
            if (button == null || !button.isInteractable)
            {
                return;
            }

            KImage image = button.bgImage ?? button.GetComponent<KImage>();
            if (image != null)
            {
                image.ColorState = state;
            }
        }

        private static string GetSlotLabel(StorageNetworkOrderProductionCenter.EngravingDiskSlot slot)
        {
            if (slot == null || !slot.HasDisk)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_EMPTY);
            }

            int count = slot.RecipeIds?.Count(id => !string.IsNullOrEmpty(id)) ?? 0;
            return count == 0
                ? Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_BLANK)
                : StorageNetworkEngravingDisk.GetRecipeSummary(slot.RecipeIds);
        }

        private static string GetSlotTooltip(StorageNetworkOrderProductionCenter.EngravingDiskSlot slot)
        {
            if (slot == null || !slot.HasDisk)
            {
                return Loc.Get(Loc.UI.STORAGE_NETWORK.ORDER_CENTER_DISK_SLOT_EMPTY);
            }

            return StorageNetworkEngravingDisk.GetRecipeDetails(slot.RecipeIds);
        }

        private static ColorStyleSetting CreateButtonStyle()
        {
            ColorStyleSetting style = ScriptableObject.CreateInstance<ColorStyleSetting>();
            style.inactiveColor = new Color(0.22f, 0.25f, 0.34f, 1f);
            style.hoverColor = new Color(0.31f, 0.35f, 0.45f, 1f);
            style.activeColor = new Color(0.16f, 0.19f, 0.27f, 1f);
            style.disabledColor = new Color(0.42f, 0.41f, 0.40f, 1f);
            style.disabledActiveColor = style.disabledColor;
            style.disabledhoverColor = style.disabledColor;
            return style;
        }

        private static void ApplyWebBoxSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite("web_box");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
        }

        private static void ApplyWebButtonSprite(Image image)
        {
            if (image == null)
            {
                return;
            }

            Sprite sprite = Assets.GetSprite("web_button") ?? Assets.GetSprite("web_box");
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.preserveAspect = false;
            }
        }

    }
}
