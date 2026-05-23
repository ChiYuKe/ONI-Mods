using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using StorageNetwork.Components;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : MonoBehaviour, IInputHandler
    {

        private static StorageNetworkPanel instance;
        private static Dictionary<string, Sprite> spriteCache;
        private StorageNetworkHub targetHub;
        private bool overviewMode;
        private TextMeshProUGUI summaryText;
        private RectTransform categoryContent;
        private RectTransform listContent;
        private RectTransform windowRect;
        private GameObject modalRoot;
        private KInputController registeredController;
        private string selectedCategoryKey;
        private string selectedItemKey;
        private Storage selectedItemStorage;
        private readonly Dictionary<string, bool> expandedStorageTypes = new Dictionary<string, bool>();
        private readonly Dictionary<Storage, bool> expandedStorages = new Dictionary<Storage, bool>();
        private float refreshElapsed;
        private string lastListSignature;
        private const bool DebugLogging = true;
        private const string CategoryStorage = "storage";
        private const string CategoryLiquid = "liquid";
        private const string CategoryGas = "gas";
        private const string CategoryConveyor = "conveyor";
        private const string CategoryOther = "other";

        public string handlerName => gameObject.name;

        public KInputHandler inputHandler { get; set; }

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
            lastListSignature = null;
            Refresh(true);
        }

        private void SetOverview()
        {
            targetHub = null;
            overviewMode = true;
            lastListSignature = null;
            Refresh(true);
        }

        private void OnEnable()
        {
            RegisterInputHandler();
        }

        private void OnDisable()
        {
            UnregisterInputHandler();
        }

        private void Update()
        {
            if (targetHub == null && !overviewMode)
            {
                return;
            }

            refreshElapsed += Time.unscaledDeltaTime;
            if (refreshElapsed >= 1f)
            {
                refreshElapsed = 0f;
                Refresh();
            }
        }

        private void BuildWindow(Transform parent)
        {
            GameObject window = CreateBox("Window", parent, new Color(0.78f, 0.79f, 0.80f, 0.98f));
            ApplyThinBoxSprite(window.GetComponent<Image>());
            windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.anchoredPosition = Vector2.zero;
            windowRect.sizeDelta = new Vector2(960f, 850f); 

            GameObject header = CreateBox("Header", window.transform, new Color(0.43f, 0.20f, 0.34f, 1f));
            SetTopStretch(header.GetComponent<RectTransform>(), 6f, 6f, 6f, 28f);

            TextMeshProUGUI title = CreateText("Title", header.transform, "储存网络", 14, TextAlignmentOptions.MidlineLeft);
            title.fontStyle = FontStyles.Bold;
            Stretch(title.rectTransform(), 12f, 0f);

            GameObject closeButton = CreateGameButton("CloseButton", header.transform, "X", Close);
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

            HorizontalLayoutGroup listColumns = list.AddComponent<HorizontalLayoutGroup>();
            listColumns.padding = new RectOffset(8, 8, 8, 8);
            listColumns.spacing = 8f;
            listColumns.childAlignment = TextAnchor.UpperLeft;
            listColumns.childControlWidth = true;
            listColumns.childControlHeight = true;
            listColumns.childForceExpandWidth = false;
            listColumns.childForceExpandHeight = true;

            GameObject categories = CreateBox("Categories", list.transform, new Color(0.74f, 0.73f, 0.68f, 1f));
            LayoutElement categoryLayout = categories.AddComponent<LayoutElement>();
            categoryLayout.minWidth = 130f;
            categoryLayout.preferredWidth = 130f;
            categoryLayout.flexibleWidth = 0f;
            AddVerticalContainer(categories, 4f, 4, 4, 4, 4);

            GameObject categoryContentObject = new GameObject("CategoryContent");
            categoryContentObject.transform.SetParent(categories.transform, false);
            categoryContent = categoryContentObject.AddComponent<RectTransform>();
            categoryContent.anchorMin = new Vector2(0f, 1f);
            categoryContent.anchorMax = new Vector2(1f, 1f);
            categoryContent.pivot = new Vector2(0.5f, 1f);
            categoryContent.offsetMin = Vector2.zero;
            categoryContent.offsetMax = Vector2.zero;
            AddVerticalContainer(categoryContentObject, 5f, 0, 0, 0, 0);
            categoryContentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject rightList = CreateBox("RightList", list.transform, new Color(0.80f, 0.79f, 0.74f, 1f));
            rightList.AddComponent<LayoutElement>().flexibleWidth = 1f;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(rightList.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            SetStretch(viewportRect, 8f, 22f, 8f, 8f);
            viewport.AddComponent<RectMask2D>();

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewport.transform, false);
            listContent = contentObject.AddComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = Vector2.zero;
            listContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup listLayout = contentObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 5f;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Scrollbar scrollbar = CreateScrollbar(rightList.transform);

            ScrollRect scrollRect = rightList.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = listContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 28f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.verticalScrollbarSpacing = 4f;

            rightList.AddComponent<ScrollWheelBlocker>();
        }

        private void Refresh(bool forceRebuild = false)
        {
            if (targetHub == null || summaryText == null || listContent == null)
            {
                if (overviewMode)
                {
                    RefreshOverview(forceRebuild);
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
                if (forceRebuild || lastListSignature != "empty")
                {
                    lastListSignature = "empty";
                    ClearCategories();
                    ClearList();
                    CreateInfoRow("未连接储存建筑", "把储存建筑贴近储存网络线缆，或让线缆经过建筑相邻格。");
                }

                return;
            }

            string listSignature = BuildListSignature(targetHub.ConnectedStorages);
            if (forceRebuild || listSignature != lastListSignature)
            {
                lastListSignature = listSignature;
                RebuildStorageRows(targetHub.ConnectedStorages);
            }
        }

        private void RebuildStorageRows(IEnumerable<StorageNetworkStorageInfo> storages)
        {
            ClearList();
            ClearCategories();

            List<StorageNetworkCategoryGroup> groups = BuildCategoryGroups(storages).ToList();
            EnsureSelectedCategory(groups);
            foreach (StorageNetworkCategoryGroup group in groups)
            {
                CreateCategoryButton(group);
            }

            StorageNetworkCategoryGroup selectedGroup = groups.FirstOrDefault(group => group.Key == selectedCategoryKey);
            if (selectedGroup == null)
            {
                CreateInfoRow("没有储存内容", string.Empty);
                RebuildLayout();
                return;
            }

            foreach (IGrouping<string, StorageNetworkStorageInfo> group in selectedGroup.Storages.GroupBy(GetStorageTypeKey).OrderBy(group => GetStorageTypeName(group.First())))
            {
                CreateStorageTypeRow(group.ToList());
            }

            RebuildLayout();
        }

        private static string BuildListSignature(IEnumerable<StorageNetworkStorageInfo> storages)
        {
            return string.Join("|", storages
                .OrderBy(GetStorageTypeKey)
                .ThenBy(storage => storage.Storage != null ? storage.Storage.GetInstanceID() : 0)
                .Select(storage =>
                {
                string items = string.Join(",", storage.Storage.items
                        .Where(item => item != null)
                        .GroupBy(GetStoredItemKey)
                        .OrderBy(group => group.Key)
                        .Select(group => string.Format("{0}:{1}:{2:0.###}",
                            group.Key,
                            group.Count(),
                            group.Sum(item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f))));

                    return string.Format("{0}:{1}:{2:0.###}:{3:0.###}:{4}",
                        GetStorageTypeKey(storage),
                        storage.Storage != null ? storage.Storage.GetInstanceID() : 0,
                        storage.StoredKg,
                        storage.CapacityKg,
                        items);
                }));
        }

        private void RefreshOverview(bool forceRebuild = false)
        {
            if (summaryText == null || listContent == null)
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
                if (forceRebuild || lastListSignature != "overview-empty")
                {
                    lastListSignature = "overview-empty";
                    ClearCategories();
                    ClearList();
                    CreateInfoRow("未建造储存网络核心", string.Empty);
                }

                return;
            }

            string overviewSignature = "overview|" + string.Join("|", hubs.Select(hub =>
                string.Format("{0}:{1:0.###}:{2:0.###}:{3}",
                    hub.GetInstanceID(),
                    hub.TotalStoredKg,
                    hub.TotalCapacityKg,
                    hub.ConnectedStorages.Count)));

            if (!forceRebuild && overviewSignature == lastListSignature)
            {
                return;
            }

            lastListSignature = overviewSignature;
            ClearCategories();
            ClearList();
            foreach (StorageNetworkHub hub in hubs)
            {
                float percent = hub.TotalCapacityKg > 0f ? hub.TotalStoredKg / hub.TotalCapacityKg : 0f;
                CreateInfoRow(
                    hub.GetProperName(),
                    string.Format("{0} / {1}  {2}%  {3} 个储存",
                        GameUtil.GetFormattedMass(hub.TotalStoredKg),
                        GameUtil.GetFormattedMass(hub.TotalCapacityKg),
                        Mathf.RoundToInt(percent * 100f),
                        hub.ConnectedStorages.Count));
            }

            RebuildLayout();
        }

        private void RebuildLayout()
        {
            if (listContent == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(listContent);
        }

        private void ClearList()
        {
            if (listContent == null)
            {
                return;
            }

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
        }

        private void ClearCategories()
        {
            if (categoryContent == null)
            {
                return;
            }

            for (int i = categoryContent.childCount - 1; i >= 0; i--)
            {
                Destroy(categoryContent.GetChild(i).gameObject);
            }
        }

        private void CloseModal()
        {
            if (modalRoot != null)
            {
                Destroy(modalRoot);
                modalRoot = null;
            }
        }

        private void Close()
        {
            CloseModal();
            gameObject.SetActive(false);
        }

        public void OnKeyDown(KButtonEvent e)
        {
            if (e.Consumed)
            {
                return;
            }

            if (modalRoot != null)
            {
                if (e.TryConsume(global::Action.Escape))
                {
                    CloseModal();
                    return;
                }

                e.Consumed = true;
                return;
            }

            if (e.TryConsume(global::Action.Escape))
            {
                Close();
                return;
            }

            if (!IsMouseOverWindow())
            {
                return;
            }

            if (!e.TryConsume(global::Action.ZoomIn))
            {
                e.TryConsume(global::Action.ZoomOut);
            }
        }

        private bool IsMouseOverWindow()
        {
            if (windowRect == null)
            {
                return false;
            }

            Vector2 localMousePosition = windowRect.InverseTransformPoint(KInputManager.GetMousePos());
            return windowRect.rect.Contains(localMousePosition);
        }

        private void RegisterInputHandler()
        {
            if (registeredController != null || KInputManager.currentController == null)
            {
                return;
            }

            registeredController = KInputManager.currentController;
            KInputHandler.Add(registeredController, this, 100);
        }

        private void UnregisterInputHandler()
        {
            if (registeredController == null)
            {
                return;
            }

            KInputHandler.Remove(registeredController, this);
            registeredController = null;
        }

        private static void LogDebug(string message)
        {
            if (DebugLogging)
            {
                Debug.Log("[StorageNetworkPanel] " + message);
            }
        }
    }
}
