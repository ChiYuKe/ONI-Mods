using System.Text;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed class StorageNetworkPanel : MonoBehaviour
    {
        private static StorageNetworkPanel instance;

        private StorageNetworkHub targetHub;
        private bool overviewMode;
        private TextMeshProUGUI summaryText;
        private TextMeshProUGUI listText;

        public static void Show(StorageNetworkHub hub)
        {
            if (hub == null)
            {
                return;
            }

            if (instance == null)
            {
                instance = Create();
            }

            instance.SetTarget(hub);
            instance.gameObject.SetActive(true);
        }

        public static void ShowOverview()
        {
            if (instance == null)
            {
                instance = Create();
            }

            instance.SetOverview();
            instance.gameObject.SetActive(true);
        }

        public static void CloseOverview()
        {
            if (instance != null && instance.overviewMode)
            {
                instance.Close();
            }
        }

        public static void CloseIfTarget(StorageNetworkHub hub)
        {
            if (instance != null && instance.targetHub == hub)
            {
                instance.Close();
            }
        }

        private static StorageNetworkPanel Create()
        {
            Transform parent = GameScreenManager.Instance?.ssOverlayCanvas?.transform;
            GameObject root = new GameObject("StorageNetworkPanel");
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
            }

            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image blocker = root.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.08f);

            StorageNetworkPanel panel = root.AddComponent<StorageNetworkPanel>();
            panel.BuildWindow(root.transform);
            root.SetActive(false);
            return panel;
        }

        private void SetTarget(StorageNetworkHub hub)
        {
            targetHub = hub;
            overviewMode = false;
            Refresh();
        }

        private void SetOverview()
        {
            targetHub = null;
            overviewMode = true;
            Refresh();
        }

        private void Update()
        {
            if (targetHub != null || overviewMode)
            {
                Refresh();
            }
        }

        private void BuildWindow(Transform parent)
        {
            GameObject window = CreateBox("Window", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(880f, 360f);

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);

            TextMeshProUGUI title = CreateText("Title", header.transform, "储存网络", 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateButton("CloseButton", header.transform, "X", Close);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-4f, 0f);
            closeRect.sizeDelta = new Vector2(24f, 22f);

            GameObject content = CreateBox("Content", window.transform, new Color(0.88f, 0.89f, 0.91f, 0.98f));
            SetStretch(content.GetComponent<RectTransform>(), 8f, 8f, 8f, 42f);

            GameObject summary = CreateBox("Summary", content.transform, new Color(0.36f, 0.42f, 0.47f, 1f));
            SetTopStretch(summary.GetComponent<RectTransform>(), 8f, 8f, 8f, 54f);
            summaryText = CreateText("SummaryText", summary.transform, string.Empty, 14, TextAlignmentOptions.TopLeft);
            summaryText.lineSpacing = 4f;
            Stretch(summaryText.rectTransform(), 12f, 7f);

            GameObject list = CreateBox("List", content.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            SetStretch(list.GetComponent<RectTransform>(), 8f, 8f, 8f, 70f);
            listText = CreateText("ListText", list.transform, string.Empty, 13, TextAlignmentOptions.TopLeft);
            listText.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            listText.lineSpacing = 10f;
            listText.textWrappingMode = TextWrappingModes.Normal;
            Stretch(listText.rectTransform(), 14f, 12f);
        }

        private void Refresh()
        {
            if (targetHub == null || summaryText == null || listText == null)
            {
                if (overviewMode)
                {
                    RefreshOverview();
                }

                return;
            }

            targetHub.RefreshNetwork();
            summaryText.text =
                "<b>网络总览</b>\n" +
                string.Format("已连接储存：{0}    容量：{1} / {2}",
                    targetHub.ConnectedStorages.Count,
                    GameUtil.GetFormattedMass(targetHub.TotalStoredKg),
                    GameUtil.GetFormattedMass(targetHub.TotalCapacityKg));

            if (targetHub.ConnectedStorages.Count == 0)
            {
                listText.text = "未连接储存建筑。\n\n把储存建筑贴近储存网络线缆，或让线缆经过建筑相邻格。";
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (StorageNetworkStorageInfo storage in targetHub.ConnectedStorages)
            {
                float percent = storage.CapacityKg > 0f ? storage.StoredKg / storage.CapacityKg : 0f;
                builder.Append("<mark=#ecebe4aa><b> ");
                builder.Append(storage.Name);
                builder.Append("</b>    ");
                builder.Append(GameUtil.GetFormattedMass(storage.StoredKg));
                builder.Append(" / ");
                builder.Append(GameUtil.GetFormattedMass(storage.CapacityKg));
                builder.Append("  ");
                builder.Append(Mathf.RoundToInt(percent * 100f));
                builder.AppendLine("% </mark>");
                builder.AppendLine();
            }

            listText.text = builder.ToString();
        }

        private void RefreshOverview()
        {
            if (summaryText == null || listText == null)
            {
                return;
            }

            StorageNetworkHub[] hubs = StorageNetworkRegistry.RegisteredHubs
                .Where(hub => hub != null)
                .OrderBy(hub => hub.GetProperName())
                .ToArray();

            float totalStored = 0f;
            float totalCapacity = 0f;
            foreach (StorageNetworkHub hub in hubs)
            {
                hub.RefreshNetwork();
                totalStored += hub.TotalStoredKg;
                totalCapacity += hub.TotalCapacityKg;
            }

            summaryText.text =
                "<b>储存网络概览</b>\n" +
                string.Format("网络核心：{0}    总容量：{1} / {2}",
                    hubs.Length,
                    GameUtil.GetFormattedMass(totalStored),
                    GameUtil.GetFormattedMass(totalCapacity));

            if (hubs.Length == 0)
            {
                listText.text = "未建造储存网络核心。";
                return;
            }

            StringBuilder builder = new StringBuilder();
            foreach (StorageNetworkHub hub in hubs)
            {
                float percent = hub.TotalCapacityKg > 0f ? hub.TotalStoredKg / hub.TotalCapacityKg : 0f;
                builder.Append("<mark=#ecebe4aa><b> ");
                builder.Append(hub.GetProperName());
                builder.Append("</b>    ");
                builder.Append(GameUtil.GetFormattedMass(hub.TotalStoredKg));
                builder.Append(" / ");
                builder.Append(GameUtil.GetFormattedMass(hub.TotalCapacityKg));
                builder.Append("  ");
                builder.Append(Mathf.RoundToInt(percent * 100f));
                builder.Append("%  ");
                builder.Append(hub.ConnectedStorages.Count);
                builder.AppendLine(" 个储存 </mark>");
                builder.AppendLine();
            }

            listText.text = builder.ToString();
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private static GameObject CreateBox(string name, Transform parent, Color color)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent, false);
            box.AddComponent<RectTransform>();
            Image image = box.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Sliced;
            return box;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.fontSize = size;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static GameObject CreateButton(string name, Transform parent, string text, System.Action onClick)
        {
            GameObject buttonObject = CreateBox(name, parent, new Color(0.2f, 0.22f, 0.3f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            TextMeshProUGUI label = CreateText("Label", buttonObject.transform, text, 16, TextAlignmentOptions.Center);
            Stretch(label.rectTransform(), 0f, 0f);
            return buttonObject;
        }

        private static void Stretch(RectTransform rectTransform, float horizontal, float vertical)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(horizontal, vertical);
            rectTransform.offsetMax = new Vector2(-horizontal, -vertical);
        }

        private static void SetStretch(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopStretch(RectTransform rectTransform, float left, float right, float top, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }
    }
}
