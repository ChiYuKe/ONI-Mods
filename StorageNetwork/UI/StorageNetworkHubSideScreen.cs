using System.Text;
using StorageNetwork.Components;
using StorageNetwork.Core;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public class StorageNetworkHubSideScreen : SideScreenContent
    {
        private StorageNetworkHub targetHub;
        private LocText contentText;
        private Button viewNetworkButton;
        private Button pullToggleButton;
        private LocText pullToggleLabel;

        public override string GetTitle()
        {
            return STRINGS.UI.STORAGE_NETWORK.SIDE_SCREEN_TITLE;
        }

        public override bool IsValidForTarget(GameObject target)
        {
            return target != null && target.GetComponent<StorageNetworkHub>() != null;
        }

        public override void SetTarget(GameObject target)
        {
            base.SetTarget(target);
            targetHub = target.GetComponent<StorageNetworkHub>();
            Refresh();
        }

        public override void ClearTarget()
        {
            targetHub = null;
            base.ClearTarget();
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            CreateContent();
        }

        private void Update()
        {
            if (targetHub != null)
            {
                Refresh();
            }
        }

        private void CreateContent()
        {
            Transform parent = ContentContainer != null ? ContentContainer.transform : transform;

            GameObject root = new GameObject("StorageNetworkConfigRoot");
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(8f, 8f);
            rootRect.offsetMax = new Vector2(-8f, -8f);

            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject textObject = new GameObject("StorageNetworkSummary");
            textObject.transform.SetParent(root.transform, false);

            RectTransform rectTransform = textObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 160f);
            textObject.AddComponent<LayoutElement>().preferredHeight = 160f;

            contentText = textObject.AddComponent<LocText>();
            contentText.alignment = TMPro.TextAlignmentOptions.TopLeft;
            contentText.fontSize = 14f;
            contentText.textWrappingMode = TMPro.TextWrappingModes.Normal;

            GameObject buttonObject = CreateButton(root.transform);
            viewNetworkButton = buttonObject.GetComponent<Button>();
            viewNetworkButton.onClick.AddListener(() =>
            {
                if (targetHub != null)
                {
                    StorageNetworkPanel.Show(targetHub);
                }
            });

            GameObject toggleObject = CreateButton(root.transform);
            pullToggleButton = toggleObject.GetComponent<Button>();
            pullToggleLabel = toggleObject.transform.Find("Label")?.GetComponent<LocText>();
            pullToggleButton.onClick.AddListener(() =>
            {
                if (targetHub != null)
                {
                    targetHub.AllowsNetworkPull = !targetHub.AllowsNetworkPull;
                    targetHub.RefreshNetworkTotals();
                    Refresh();
                }
            });
        }

        private static GameObject CreateButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("ViewStorageNetworkButton");
            buttonObject.transform.SetParent(parent, false);
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0f, 36f);
            buttonObject.AddComponent<LayoutElement>().preferredHeight = 36f;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.22f, 0.3f, 1f);

            Button button = buttonObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.22f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.28f, 0.3f, 0.4f, 1f);
            colors.pressedColor = new Color(0.12f, 0.14f, 0.2f, 1f);
            button.colors = colors;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(buttonObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            LocText label = labelObject.AddComponent<LocText>();
            label.SetText(STRINGS.UI.STORAGE_NETWORK.VIEW_NETWORK_BUTTON);
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 14f;
            label.color = Color.white;
            label.raycastTarget = false;

            ToolTip tooltip = buttonObject.AddComponent<ToolTip>();
            tooltip.toolTip = STRINGS.UI.STORAGE_NETWORK.VIEW_NETWORK_TOOLTIP;
            return buttonObject;
        }

        private void Refresh()
        {
            if (contentText == null || targetHub == null)
            {
                return;
            }

            targetHub.RefreshNetworkTotals();
            if (pullToggleLabel != null)
            {
                pullToggleLabel.SetText(targetHub.AllowsNetworkPull ? "允许建筑从网络取料" : "禁止建筑从网络取料");
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format(
                STRINGS.UI.STORAGE_NETWORK.SUMMARY,
                GameUtil.GetFormattedMass(targetHub.TotalStoredKg),
                GameUtil.GetFormattedMass(targetHub.TotalCapacityKg)));
            builder.AppendLine();

            if (targetHub.ConnectedStorages.Count == 0)
            {
                builder.AppendLine(STRINGS.UI.STORAGE_NETWORK.NO_STORAGES);
            }
            else
            {
                foreach (StorageNetworkStorageInfo storage in targetHub.ConnectedStorages)
                {
                    builder.Append(storage.Name);
                    builder.Append(": ");
                    builder.Append(GameUtil.GetFormattedMass(storage.StoredKg));
                    builder.Append(" / ");
                    builder.AppendLine(GameUtil.GetFormattedMass(storage.CapacityKg));
                }
            }

            contentText.text = builder.ToString();
        }
    }
}
