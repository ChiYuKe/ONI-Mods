using System.Collections.Generic;
using System.IO;
using System.Linq;
using Klei.AI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SN_DuplicantGenetics
{
    internal sealed class DuplicantGeneticsPanel : KMonoBehaviour
    {
        private const string BundleName = "sn_duplicantgenetics";
        private const string PrefabName = "DuplicantGeneticsPanel";

        private static AssetBundle uiBundle;
        private static GameObject panelPrefab;

        private readonly List<GameObject> rowInstances = new List<GameObject>();
        private readonly List<MinionIdentity> minions = new List<MinionIdentity>();

        private RectTransform windowRect;
        private Transform rowContent;
        private GameObject rowTemplate;
        private MinionIdentity selected;

        public static DuplicantGeneticsPanel Create(Transform parent, RectTransform anchorPanel)
        {
            Debug.Log("[SN_DuplicantGenetics] 开始创建复制人档案面板。Parent=" + (parent != null ? parent.name : "null"));
            GameObject prefab = LoadPanelPrefab();
            if (prefab == null)
            {
                ShowToast("未找到复制人档案预制件");
                return null;
            }

            GameObject panelObject = Object.Instantiate(prefab, parent, false);
            panelObject.name = "SN_DuplicantGeneticsPanel";
            PreparePrefabInstance(panelObject);

            DuplicantGeneticsPanel panel = panelObject.GetComponent<DuplicantGeneticsPanel>();
            if (panel == null)
            {
                panel = panelObject.AddComponent<DuplicantGeneticsPanel>();
            }

            panel.Initialize(anchorPanel);
            Debug.Log("[SN_DuplicantGenetics] 复制人档案面板创建完成。active=" + panelObject.activeInHierarchy);
            return panel;
        }

        private static void PreparePrefabInstance(GameObject panelObject)
        {
            if (panelObject == null)
            {
                return;
            }

            foreach (CanvasScaler scaler in panelObject.GetComponentsInChildren<CanvasScaler>(true))
            {
                scaler.enabled = false;
            }

            foreach (GraphicRaycaster raycaster in panelObject.GetComponentsInChildren<GraphicRaycaster>(true))
            {
                raycaster.enabled = false;
            }

            foreach (Canvas canvas in panelObject.GetComponentsInChildren<Canvas>(true))
            {
                canvas.overrideSorting = false;
                canvas.enabled = true;
            }

            RectTransform rect = panelObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
            }
        }

        private void Initialize(RectTransform anchorPanel)
        {
            windowRect = GetComponent<RectTransform>();
            if (windowRect != null)
            {
                windowRect.anchorMin = new Vector2(0.5f, 0.5f);
                windowRect.anchorMax = new Vector2(0.5f, 0.5f);
                windowRect.pivot = new Vector2(0.5f, 0.5f);
                windowRect.localScale = Vector3.one;
                if (windowRect.sizeDelta.x <= 1f || windowRect.sizeDelta.y <= 1f)
                {
                    windowRect.sizeDelta = new Vector2(1052f, 678f);
                }

                windowRect.anchoredPosition = anchorPanel != null ? anchorPanel.anchoredPosition : Vector2.zero;
            }

            transform.SetAsLastSibling();
            BindStaticControls();
            CacheRowTemplate();
            Refresh();
        }

        private void BindStaticControls()
        {
            SetText("Header/Title", "COLONY MANAGER - 复制人档案", 18, TextAlignmentOptions.MidlineLeft, new Color(0.12f, 0.75f, 1f, 1f));
            SetText("Header/CycleText", "打印舱就绪周期: 3 / 现存周期: " + GetCurrentCycle(), 12, TextAlignmentOptions.MidlineRight, new Color(0.72f, 0.82f, 1f, 1f));
            SetText("Body/LeftPanel/Title", "复制人", 14, TextAlignmentOptions.MidlineLeft, new Color(0.16f, 0.18f, 0.20f, 1f));
            SetText("Body/LeftPanel/Header", "复制人", 14, TextAlignmentOptions.MidlineLeft, new Color(0.16f, 0.18f, 0.20f, 1f));
            SetText("Body/LeftPanel/ListTitle", "复制人", 14, TextAlignmentOptions.MidlineLeft, new Color(0.16f, 0.18f, 0.20f, 1f));

            Transform closeButton = FindDeep(transform, "CloseButton");
            if (closeButton != null)
            {
                AddClickHandler(closeButton.gameObject, () => Destroy(gameObject));
            }
            else
            {
                Debug.LogWarning("[SN_DuplicantGenetics] prefab中找不到 CloseButton。");
            }
        }

        private void CacheRowTemplate()
        {
            Transform content = FindPath(transform, "Body/LeftPanel/ScrollView/Viewport/Content");
            if (content == null)
            {
                Transform leftPanel = FindPath(transform, "Body/LeftPanel") ?? FindDeep(transform, "LeftPanel");
                Transform scrollView = leftPanel != null ? FindDeep(leftPanel, "ScrollView") : FindDeep(transform, "ScrollView");
                Transform viewport = scrollView != null ? FindDeep(scrollView, "Viewport") : null;
                content = viewport != null ? FindDeep(viewport, "Content") : null;
            }

            if (content == null)
            {
                content = FindDeep(transform, "Content");
            }

            rowContent = content;
            Transform template = content != null ? FindDeep(content, "DuplicantRow") : FindDeep(transform, "DuplicantRow");
            rowTemplate = template != null ? template.gameObject : null;

            if (rowContent == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] prefab中找不到 Body/LeftPanel/ScrollView/Viewport/Content。");
            }

            if (rowTemplate == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] prefab中找不到 DuplicantRow 模板。");
            }
            else
            {
                rowTemplate.SetActive(false);
            }
        }

        private void Refresh()
        {
            minions.Clear();
            if (Components.LiveMinionIdentities != null)
            {
                foreach (MinionIdentity identity in Components.LiveMinionIdentities.Items)
                {
                    if (identity != null && identity.gameObject != null && !identity.HasTag(GameTags.Dead))
                    {
                        minions.Add(identity);
                    }
                }
            }

            minions.Sort((a, b) => GetStress(b).CompareTo(GetStress(a)));
            selected = minions.Count > 0 ? minions[0] : null;
            RebuildRows();
            BindRightPanel(selected);
        }

        private void RebuildRows()
        {
            foreach (GameObject row in rowInstances)
            {
                if (row != null)
                {
                    Destroy(row);
                }
            }

            rowInstances.Clear();
            if (rowContent == null || rowTemplate == null)
            {
                return;
            }

            float rowHeight = GetRowHeight();
            for (int index = 0; index < minions.Count; index++)
            {
                MinionIdentity minion = minions[index];
                GameObject row = Instantiate(rowTemplate, rowContent, false);
                row.name = "DuplicantRow_" + GetName(minion);
                row.SetActive(true);
                rowInstances.Add(row);
                PositionRow(row.transform, index, rowHeight);
                BindRow(row.transform, minion);
                AddClickHandler(row, () =>
                {
                    selected = minion;
                    BindRightPanel(selected);
                });
            }

            ResizeRowContent(rowHeight);
        }

        private void BindRow(Transform row, MinionIdentity minion)
        {
            BindPortrait(row, minion);

            string name = GetName(minion);
            string summary = "压力: " + Mathf.RoundToInt(GetStress(minion)) + "%    卡路里: " +
                             Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.Calories)) + "卡    " +
                             GetCurrentChoreText(minion);

            bool textBound = false;
            textBound |= SetTextIfPresent(row, "Label", name + "\n" + summary, 12, TextAlignmentOptions.MidlineLeft, Color.white);
            textBound |= SetTextIfPresent(row, "Name", name, 14, TextAlignmentOptions.MidlineLeft, Color.white);
            textBound |= SetTextIfPresent(row, "Summary", summary, 11, TextAlignmentOptions.MidlineLeft, new Color(0.70f, 0.82f, 1f, 1f));
            textBound |= SetTextIfPresent(row, "Stress", Mathf.RoundToInt(GetStress(minion)) + "%", 11, TextAlignmentOptions.MidlineLeft, new Color(0.70f, 0.82f, 1f, 1f));
            textBound |= SetTextIfPresent(row, "Calories", Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.Calories)) + "卡", 11, TextAlignmentOptions.MidlineLeft, new Color(0.70f, 0.82f, 1f, 1f));

            if (!textBound)
            {
                EnsureText(row, "AutoRowText", name + "\n" + summary, 12, TextAlignmentOptions.MidlineLeft, Color.white, new RectOffset(72, 8, 4, 4));
            }
        }

        private float GetRowHeight()
        {
            RectTransform templateRect = rowTemplate != null ? rowTemplate.GetComponent<RectTransform>() : null;
            if (templateRect == null)
            {
                return 64f;
            }

            float height = templateRect.rect.height;
            if (height <= 1f)
            {
                height = templateRect.sizeDelta.y;
            }

            return Mathf.Max(54f, height);
        }

        private static void PositionRow(Transform row, int index, float rowHeight)
        {
            RectTransform rect = row != null ? row.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * rowHeight);
            rect.sizeDelta = new Vector2(0f, rowHeight);
            rect.localScale = Vector3.one;
        }

        private void ResizeRowContent(float rowHeight)
        {
            RectTransform contentRect = rowContent != null ? rowContent.GetComponent<RectTransform>() : null;
            if (contentRect == null)
            {
                return;
            }

            RectTransform viewportRect = rowContent.parent != null ? rowContent.parent.GetComponent<RectTransform>() : null;
            float viewportHeight = viewportRect != null ? viewportRect.rect.height : 0f;
            float contentHeight = Mathf.Max(viewportHeight, rowInstances.Count * rowHeight);

            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, contentHeight);
        }

        private static void BindPortrait(Transform row, MinionIdentity minion)
        {
            Transform iconRoot = FindDeep(row, "lab") ??
                                 FindDeep(row, "Icon") ??
                                 FindDeep(row, "Portrait") ??
                                 FindDeep(row, "Avatar");
            if (iconRoot == null)
            {
                return;
            }

            PreparePortraitSlot(iconRoot);
            Image image = iconRoot.GetComponent<Image>() ?? iconRoot.GetComponentInChildren<Image>(true);
            if (image == null)
            {
                GameObject iconObject = new GameObject("AvatarIcon");
                iconObject.transform.SetParent(iconRoot, false);
                image = iconObject.AddComponent<Image>();

                RectTransform iconRect = image.rectTransform;
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = Vector2.zero;
                iconRect.sizeDelta = new Vector2(42f, 42f);
            }

            Sprite sprite = GetMinionMiniIcon(minion);
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.enabled = true;
            iconRoot.gameObject.SetActive(true);
        }

        private static void PreparePortraitSlot(Transform iconRoot)
        {
            RectTransform rect = iconRoot != null ? iconRoot.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(30f, 0f);
            rect.sizeDelta = new Vector2(52f, 52f);

            LayoutElement layout = iconRoot.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = iconRoot.gameObject.AddComponent<LayoutElement>();
            }

            layout.minWidth = 52f;
            layout.preferredWidth = 52f;
            layout.flexibleWidth = 0f;
            layout.minHeight = 52f;
            layout.preferredHeight = 52f;
            layout.flexibleHeight = 0f;
        }

        private static Sprite GetMinionMiniIcon(MinionIdentity minion)
        {
            if (minion != null)
            {
                var personality = Db.Get().Personalities.Get(minion.personalityResourceId);
                if (personality != null)
                {
                    Sprite icon = personality.GetMiniIcon();
                    if (icon != null)
                    {
                        return icon;
                    }
                }
            }

            return Assets.GetSprite("dreamIcon_Unknown");
        }

        private void BindRightPanel(MinionIdentity minion)
        {
            Transform rightPanel = FindPath(transform, "Body/RightPanel") ?? FindDeep(transform, "RightPanel");
            if (rightPanel == null || minion == null)
            {
                return;
            }

            string detail = GetName(minion) +
                            "\n\n压力: " + Mathf.RoundToInt(GetStress(minion)) + "%" +
                            "\n生命值: " + Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.HitPoints)) + " / " + Mathf.RoundToInt(GetAmountMax(minion, Db.Get().Amounts.HitPoints)) +
                            "\n呼吸: " + Mathf.RoundToInt(GetAmountPercent(minion, Db.Get().Amounts.Breath)) + "%" +
                            "\n卡路里: " + Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.Calories)) + " kcal" +
                            "\n任务: " + GetCurrentChoreText(minion) +
                            "\n\n特质:\n" + GetTraitsText(minion);

            SetText(rightPanel, "Name", GetName(minion), 20, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "Stress", Mathf.RoundToInt(GetStress(minion)) + "%", 12, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "Calories", Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.Calories)) + " kcal", 12, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "Breath", Mathf.RoundToInt(GetAmountPercent(minion, Db.Get().Amounts.Breath)) + "%", 12, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "HP", Mathf.RoundToInt(GetAmountValue(minion, Db.Get().Amounts.HitPoints)) + " / " + Mathf.RoundToInt(GetAmountMax(minion, Db.Get().Amounts.HitPoints)), 12, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "Chore", GetCurrentChoreText(minion), 12, TextAlignmentOptions.TopLeft, Color.white);
            EnsureText(rightPanel, "AutoDetailText", detail, 14, TextAlignmentOptions.TopLeft, new Color(0.16f, 0.18f, 0.20f, 1f), new RectOffset(24, 24, 24, 24));

            string traits = GetTraitsText(minion);
            SetText(rightPanel, "Traits", traits, 12, TextAlignmentOptions.TopLeft, Color.white);
            SetText(rightPanel, "TraitText", traits, 12, TextAlignmentOptions.TopLeft, Color.white);
        }

        private static GameObject LoadPanelPrefab()
        {
            if (panelPrefab != null)
            {
                return panelPrefab;
            }

            AssetBundle bundle = LoadBundle();
            if (bundle == null)
            {
                return null;
            }

            panelPrefab = bundle.LoadAsset<GameObject>(PrefabName);
            if (panelPrefab == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] AB中找不到预制件: " + PrefabName);
                Debug.LogWarning("[SN_DuplicantGenetics] AB资源列表: " + string.Join(", ", bundle.GetAllAssetNames()));
            }

            return panelPrefab;
        }

        private static AssetBundle LoadBundle()
        {
            if (uiBundle != null)
            {
                return uiBundle;
            }

            string bundlePath = FindBundlePath();
            if (string.IsNullOrEmpty(bundlePath))
            {
                Debug.LogWarning("[SN_DuplicantGenetics] 未找到AB包: " + BundleName + ", ContentPath=" + ModEntry.ContentPath);
                return null;
            }

            Debug.Log("[SN_DuplicantGenetics] 正在加载AB: " + bundlePath);
            uiBundle = AssetBundle.LoadFromFile(bundlePath);
            if (uiBundle == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] AB加载失败: " + bundlePath);
            }
            else
            {
                Debug.Log("[SN_DuplicantGenetics] AB加载成功: " + bundlePath);
            }

            return uiBundle;
        }

        private static string FindBundlePath()
        {
            if (string.IsNullOrEmpty(ModEntry.ContentPath))
            {
                return null;
            }

            string platform = GetPlatformFolder();
            string[] candidates =
            {
                Path.Combine(ModEntry.ContentPath, "Assets", "AssetBundles", platform, BundleName),
                Path.Combine(ModEntry.ContentPath, "assets", "assetbundles", platform, BundleName),
                Path.Combine(ModEntry.ContentPath, "AssetBundles", platform, BundleName),
                Path.Combine(ModEntry.ContentPath, "assetbundles", platform, BundleName),
                Path.Combine(ModEntry.ContentPath, "Assets", "AssetBundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "assets", "assetbundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "AssetBundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "assetbundles", BundleName),
                Path.Combine(ModEntry.ContentPath, BundleName)
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static string GetPlatformFolder()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                    return "windows";
                case RuntimePlatform.LinuxPlayer:
                    return "linux";
                case RuntimePlatform.OSXPlayer:
                    return "mac";
                default:
                    return string.Empty;
            }
        }

        private static void AddClickHandler(GameObject target, System.Action onClick)
        {
            Button button = target.GetComponent<Button>();
            if (button == null)
            {
                button = target.AddComponent<Button>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }

        private void SetText(string path, string value)
        {
            Transform target = FindPathLoose(transform, path);
            if (target != null)
            {
                SetText(target, value, 12, TextAlignmentOptions.MidlineLeft, Color.white);
            }
        }

        private void SetText(string path, string value, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            Transform target = FindPathLoose(transform, path);
            if (target != null)
            {
                SetText(target, value, fontSize, alignment, color);
            }
        }

        private static void SetText(Transform root, string childName, string value)
        {
            Transform target = FindDeep(root, childName);
            if (target != null)
            {
                SetText(target, value, 12, TextAlignmentOptions.MidlineLeft, Color.white);
            }
        }

        private static void SetText(Transform root, string childName, string value, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            Transform target = FindDeep(root, childName);
            if (target != null)
            {
                SetText(target, value, fontSize, alignment, color);
            }
        }

        private static bool SetTextIfPresent(Transform root, string childName, string value, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            Transform target = FindDeep(root, childName);
            return target != null && SetExistingText(target, value, fontSize, alignment, color);
        }

        private static void SetText(Transform target, string value, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            if (SetExistingText(target, value, fontSize, alignment, color))
            {
                return;
            }

            EnsureText(target, "AutoText", value, fontSize, alignment, color, new RectOffset(8, 8, 2, 2));
        }

        private static bool SetExistingText(Transform target, string value, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                ConfigureTmp(tmp, fontSize, alignment, color);
                tmp.text = value;
                return true;
            }

            tmp = target.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                ConfigureTmp(tmp, fontSize, alignment, color);
                tmp.text = value;
                return true;
            }

            LocText locText = target.GetComponent<LocText>();
            if (locText != null)
            {
                locText.SetText(value);
                return true;
            }

            Text uiText = target.GetComponent<Text>();
            if (uiText != null)
            {
                uiText.text = value;
                uiText.fontSize = fontSize;
                uiText.alignment = ToTextAnchor(alignment);
                uiText.color = color;
                return true;
            }

            return false;
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string childName, string value, int fontSize, TextAlignmentOptions alignment, Color color, RectOffset padding)
        {
            Transform existing = parent.Find(childName);
            TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (text == null)
            {
                GameObject textObject = new GameObject(childName);
                textObject.transform.SetParent(parent, false);
                text = textObject.AddComponent<TextMeshProUGUI>();
                RectTransform rect = text.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(padding.left, padding.bottom);
                rect.offsetMax = new Vector2(-padding.right, -padding.top);
            }

            ConfigureTmp(text, fontSize, alignment, color);
            text.text = value;
            return text;
        }

        private static void ConfigureTmp(TextMeshProUGUI text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        private static TextAnchor ToTextAnchor(TextAlignmentOptions alignment)
        {
            if ((alignment & TextAlignmentOptions.Right) == TextAlignmentOptions.Right)
            {
                return TextAnchor.MiddleRight;
            }

            if ((alignment & TextAlignmentOptions.Center) == TextAlignmentOptions.Center)
            {
                return TextAnchor.MiddleCenter;
            }

            if ((alignment & TextAlignmentOptions.Top) == TextAlignmentOptions.Top)
            {
                return TextAnchor.UpperLeft;
            }

            return TextAnchor.MiddleLeft;
        }

        private static Transform FindPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            string[] parts = path.Split('/');
            Transform current = root;
            foreach (string part in parts)
            {
                current = current.Find(part);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }

        private static Transform FindPathLoose(Transform root, string path)
        {
            Transform target = FindPath(root, path);
            if (target != null || string.IsNullOrEmpty(path))
            {
                return target;
            }

            int slashIndex = path.LastIndexOf('/');
            string lastName = slashIndex >= 0 ? path.Substring(slashIndex + 1) : path;
            return FindDeep(root, lastName);
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static string GetName(MinionIdentity identity)
        {
            return identity != null ? identity.GetProperName() : "未知复制人";
        }

        private static int GetCurrentCycle()
        {
            return GameClock.Instance != null ? GameClock.Instance.GetCycle() : 0;
        }

        private static float GetStress(MinionIdentity identity)
        {
            return GetAmountValue(identity, Db.Get().Amounts.Stress);
        }

        private static float GetAmountValue(MinionIdentity identity, Amount amount)
        {
            if (identity == null || amount == null)
            {
                return 0f;
            }

            AmountInstance instance = amount.Lookup(identity.gameObject);
            return instance != null ? instance.value : 0f;
        }

        private static float GetAmountMax(MinionIdentity identity, Amount amount)
        {
            if (identity == null || amount == null)
            {
                return 1f;
            }

            AmountInstance instance = amount.Lookup(identity.gameObject);
            return instance != null ? instance.GetMax() : 1f;
        }

        private static float GetAmountPercent(MinionIdentity identity, Amount amount)
        {
            return GetAmountValue(identity, amount) / Mathf.Max(1f, GetAmountMax(identity, amount)) * 100f;
        }

        private static string GetCurrentChoreText(MinionIdentity identity)
        {
            ChoreDriver driver = identity != null ? identity.GetComponent<ChoreDriver>() : null;
            Chore chore = driver != null ? driver.GetCurrentChore() : null;
            return chore != null && chore.choreType != null ? chore.choreType.Name : "Idle";
        }

        private static string GetTraitsText(MinionIdentity identity)
        {
            List<Trait> traits = identity != null ? identity.GetComponent<Traits>()?.TraitList : null;
            if (traits == null || traits.Count == 0)
            {
                return "未记录特质";
            }

            return string.Join("\n", traits.Take(4).Select(trait => "• " + trait.Name));
        }

        private static void ShowToast(string message)
        {
            Debug.LogWarning("[SN_DuplicantGenetics] " + message);
            if (PopFXManager.Instance != null)
            {
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative, message, null);
            }
        }
    }
}
