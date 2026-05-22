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
            blocker.color = new Color(0f, 0f, 0f, 0.22f);

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
            GameObject window = CreateBox("Window", parent, new Color(0.12f, 0.13f, 0.15f, 0.98f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(500f, 350f);

            VerticalLayoutGroup layout = window.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 6, 6);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.32f, 1f));
            LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
            headerLayoutElement.minHeight = 34f;
            headerLayoutElement.preferredHeight = 34f;
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(10, 4, 0, 0);
            headerLayout.spacing = 6f;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = true;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;

            TextMeshProUGUI title = CreateText("Title", header.transform, "储存网络", 16, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            title.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject closeButton = CreateButton("CloseButton", header.transform, "X", Close);
            LayoutElement closeLayout = closeButton.AddComponent<LayoutElement>();
            closeLayout.preferredWidth = 30f;
            closeLayout.preferredHeight = 30f;

            GameObject summary = CreateBox("Summary", window.transform, new Color(0.22f, 0.23f, 0.24f, 0.98f));
            LayoutElement summaryLayout = summary.AddComponent<LayoutElement>();
            summaryLayout.minHeight = 66f;
            summaryLayout.preferredHeight = 66f;
            summaryText = CreateText("SummaryText", summary.transform, string.Empty, 14, TextAlignmentOptions.TopLeft);
            summaryText.lineSpacing = 4f;
            Stretch(summaryText.rectTransform(), 10f, 7f);

            GameObject list = CreateBox("List", window.transform, new Color(0.68f, 0.67f, 0.62f, 0.98f));
            list.AddComponent<LayoutElement>().flexibleHeight = 1f;
            listText = CreateText("ListText", list.transform, string.Empty, 14, TextAlignmentOptions.TopLeft);
            listText.color = new Color(0.12f, 0.13f, 0.13f, 1f);
            listText.lineSpacing = 8f;
            listText.textWrappingMode = TextWrappingModes.Normal;
            Stretch(listText.rectTransform(), 12f, 10f);
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
                builder.Append("<b>");
                builder.Append(storage.Name);
                builder.Append("</b>    ");
                builder.Append(GameUtil.GetFormattedMass(storage.StoredKg));
                builder.Append(" / ");
                builder.Append(GameUtil.GetFormattedMass(storage.CapacityKg));
                builder.Append("  ");
                builder.Append(Mathf.RoundToInt(percent * 100f));
                builder.AppendLine("%");
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
                builder.Append("<b>");
                builder.Append(hub.GetProperName());
                builder.Append("</b>    ");
                builder.Append(GameUtil.GetFormattedMass(hub.TotalStoredKg));
                builder.Append(" / ");
                builder.Append(GameUtil.GetFormattedMass(hub.TotalCapacityKg));
                builder.Append("  ");
                builder.Append(Mathf.RoundToInt(percent * 100f));
                builder.Append("%  ");
                builder.Append(hub.ConnectedStorages.Count);
                builder.AppendLine(" 个储存");
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
    }
}
