using System.Collections.Generic;
using System.IO;
using StorageNetwork.API;
using UnityEngine;

namespace SN_DuplicantGenetics
{
    internal sealed class ExampleHeaderButtonProvider : IStorageNetworkPanelHeaderButtonProvider
    {
        private const string BundleName = "sn_duplicantgenetics";
        private const string PanelPrefabName = "DuplicantGeneticsPanel";
        private static AssetBundle uiBundle;
        private static GameObject panelPrefab;
        private static GameObject openPanel;

        public IEnumerable<StorageNetworkPanelHeaderButton> GetHeaderButtons()
        {
            yield return new StorageNetworkPanelHeaderButton(
                "sn_duplicant_genetics",
                "示例",
                OnClick,
                "来自SN_DuplicantGenetics的主面板扩展按钮",
                "storage_network_overlay",
                "*",
                72f, 
                10);
        }

        private static void OnClick(StorageNetworkPanelHeaderButtonContext context)
        {
            Debug.Log("[SN_DuplicantGenetics] 主面板按钮点击。Canvas=" + (context.CanvasObject != null ? context.CanvasObject.name : "null"));
            if (openPanel != null)
            {
                Debug.Log("[SN_DuplicantGenetics] 关闭已打开的AB面板。");
                Object.Destroy(openPanel);
                openPanel = null;
                return;
            }

            GameObject prefab = LoadPanelPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] 未找到AB面板预制件，请把 " + BundleName + " 放到附属模组的 Assets/AssetBundles 目录。");
                PopFXManager.Instance.SpawnFX(PopFXManager.Instance.sprite_Negative, "未找到面板", null);
                return;
            }

            openPanel = context.InstantiatePanelPrefab(prefab, "SN_DuplicantGeneticsPanel");
            RectTransform rect = openPanel != null ? openPanel.GetComponent<RectTransform>() : null;
            if (rect != null)
            {
                openPanel.transform.SetAsLastSibling();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                if (Mathf.Abs(rect.sizeDelta.x) <= 0.01f || Mathf.Abs(rect.sizeDelta.y) <= 0.01f)
                {
                    rect.sizeDelta = new Vector2(520f, 360f);
                }

                Debug.Log("[SN_DuplicantGenetics] AB面板已实例化。name=" + openPanel.name +
                          ", parent=" + (openPanel.transform.parent != null ? openPanel.transform.parent.name : "null") +
                          ", size=" + rect.sizeDelta +
                          ", active=" + openPanel.activeInHierarchy);
            }
            else
            {
                Debug.LogWarning("[SN_DuplicantGenetics] AB面板根对象没有RectTransform: " + openPanel);
            }
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

            panelPrefab = bundle.LoadAsset<GameObject>(PanelPrefabName);
            if (panelPrefab == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] AB中找不到预制件: " + PanelPrefabName);
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

            Debug.Log("[SN_DuplicantGenetics] 正在加载AB包: " + bundlePath);
            uiBundle = AssetBundle.LoadFromFile(bundlePath);
            if (uiBundle == null)
            {
                Debug.LogWarning("[SN_DuplicantGenetics] AB加载失败: " + bundlePath);
            }
            else
            {
                Debug.Log("[SN_DuplicantGenetics] AB加载成功。资源列表: " + string.Join(", ", uiBundle.GetAllAssetNames()));
            }

            return uiBundle;
        }

        private static string FindBundlePath()
        {
            if (string.IsNullOrEmpty(ModEntry.ContentPath))
            {
                return null;
            }

            string[] candidates =
            {
                Path.Combine(ModEntry.ContentPath, "Assets", "AssetBundles", GetPlatformFolder(), BundleName),
                Path.Combine(ModEntry.ContentPath, "assets", "assetbundles", GetPlatformFolder(), BundleName),
                Path.Combine(ModEntry.ContentPath, "AssetBundles", GetPlatformFolder(), BundleName),
                Path.Combine(ModEntry.ContentPath, "assetbundles", GetPlatformFolder(), BundleName),
                Path.Combine(ModEntry.ContentPath, "Assets", "AssetBundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "assets", "assetbundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "assetbundles", BundleName),
                Path.Combine(ModEntry.ContentPath, "AssetBundles", BundleName),
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
    }
}


