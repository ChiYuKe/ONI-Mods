using System.Collections.Generic;
using HarmonyLib;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.UI;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Patches
{
    public static class SideScreenPatch
    {
        private const string SideScreenName = "StorageNetworkHubSideScreen";

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreenOnPrefabInitPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                EnsureRegistered(__instance);
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), nameof(DetailsScreen.Refresh))]
        public static class DetailsScreenRefreshPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                EnsureRegistered(__instance);
                StorageNetworkTitleButton.Refresh(__instance);
            }
        }

        private static void EnsureRegistered(DetailsScreen detailsScreen)
        {
            if (detailsScreen == null)
            {
                return;
            }

            try
            {
                Register(detailsScreen);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[StorageNetwork] Failed to register side screen: " + exception);
            }
        }

        private static void Register(DetailsScreen detailsScreen)
        {
            Traverse traverse = Traverse.Create(detailsScreen);
            List<DetailsScreen.SideScreenRef> sideScreens =
                traverse.Field("sideScreens").GetValue<List<DetailsScreen.SideScreenRef>>();
            GameObject contentBody = traverse.Field("sideScreenContentBody").GetValue<GameObject>();

            if (sideScreens == null || contentBody == null || sideScreens.Exists(screen => screen.name == SideScreenName))
            {
                return;
            }

            GameObject screenObject = new GameObject(SideScreenName);
            screenObject.transform.SetParent(contentBody.transform, false);
            screenObject.SetActive(false);

            RectTransform rectTransform = screenObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            StorageNetworkHubSideScreen sideScreen = screenObject.AddComponent<StorageNetworkHubSideScreen>();
            sideScreen.name = SideScreenName;
            sideScreen.ContentContainer = screenObject;

            sideScreens.Add(new DetailsScreen.SideScreenRef
            {
                name = SideScreenName,
                offset = Vector2.zero,
                screenPrefab = sideScreen,
                tab = DetailsScreen.SidescreenTabTypes.Config
            });

            Debug.Log("[StorageNetwork] Registered hub side screen.");
        }

        private static class StorageNetworkTitleButton
        {
            private const string ButtonName = "StorageNetworkTitleButton";

            public static void Refresh(DetailsScreen detailsScreen)
            {
                if (detailsScreen == null)
                {
                    return;
                }

                KButton button = GetOrCreate(detailsScreen);
                if (button == null)
                {
                    return;
                }

                if (!StorageNetworkUiOptions.UseTitleBarNetworkButton)
                {
                    button.gameObject.SetActive(false);
                    return;
                }

                StorageNetworkStorageConnector connector = detailsScreen.target != null
                    ? detailsScreen.target.GetComponent<StorageNetworkStorageConnector>()
                    : null;

                bool show = connector != null;
                button.gameObject.SetActive(show);
                if (!show)
                {
                    return;
                }

                StorageNetworkHub hub = StorageNetworkRegistry.GetConnectedHub(connector);
                button.isInteractable = hub != null;
                button.ClearOnClick();
                button.onClick += () =>
                {
                    StorageNetworkHub connectedHub = StorageNetworkRegistry.GetConnectedHub(connector);
                    if (connectedHub != null)
                    {
                        StorageNetworkPanel.Show(connectedHub, connector.Storage);
                    }
                };

                ToolTip tooltip = button.GetComponent<ToolTip>() ?? button.gameObject.AddComponent<ToolTip>();
                tooltip.SetSimpleTooltip(hub != null
                    ? STRINGS.UI.STORAGE_NETWORK.VIEW_CONNECTED_NETWORK_TOOLTIP
                    : STRINGS.UI.STORAGE_NETWORK.VIEW_CONNECTED_NETWORK_UNAVAILABLE_TOOLTIP);
            }

            private static KButton GetOrCreate(DetailsScreen detailsScreen)
            {
                Component title = Traverse.Create(detailsScreen).Field("TabTitle").GetValue<Component>();
                EditableTitleBar titleBar = title as EditableTitleBar;
                KButton anchorButton = titleBar != null ? titleBar.editNameButton : null;
                Transform parent = anchorButton != null && anchorButton.transform.parent != null
                    ? anchorButton.transform.parent
                    : title != null ? title.transform : null;
                if (parent == null)
                {
                    return null;
                }

                Transform existing = parent.Find(ButtonName);
                if (existing != null)
                {
                    return existing.GetComponent<KButton>();
                }

                KButton template = Traverse.Create(detailsScreen).Field("PinResourceButton").GetValue<KButton>();
                if (template == null)
                {
                    template = Traverse.Create(detailsScreen).Field("CodexEntryButton").GetValue<KButton>();
                }

                if (template == null)
                {
                    return null;
                }

                GameObject buttonObject = Object.Instantiate(template.gameObject, parent, false);
                buttonObject.name = ButtonName;

                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                if (rect != null)
                {
                    RectTransform anchorRect = anchorButton != null ? anchorButton.GetComponent<RectTransform>() : null;
                    if (anchorRect != null)
                    {
                        rect.anchorMin = anchorRect.anchorMin;
                        rect.anchorMax = anchorRect.anchorMax;
                        rect.pivot = anchorRect.pivot;
                        rect.anchoredPosition = anchorRect.anchoredPosition + new Vector2(-26f, 0f);
                    }

                    rect.sizeDelta = new Vector2(24f, 24f);
                    rect.SetAsLastSibling();
                }

                KButton button = buttonObject.GetComponent<KButton>();
                button.ClearOnClick();

                foreach (LocText text in buttonObject.GetComponentsInChildren<LocText>(true))
                {
                    text.gameObject.SetActive(false);
                }

                Image buttonBackground = buttonObject.GetComponent<Image>();
                foreach (Image childImage in buttonObject.GetComponentsInChildren<Image>(true))
                {
                    if (childImage != buttonBackground)
                    {
                        childImage.gameObject.SetActive(false);
                    }
                }

                GameObject iconObject = new GameObject("StorageNetworkIcon");
                iconObject.transform.SetParent(buttonObject.transform, false);
                RectTransform iconRect = iconObject.AddComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(5f, 5f);
                iconRect.offsetMax = new Vector2(-5f, -5f);

                Image icon = iconObject.AddComponent<Image>();
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.color = Color.white;
                Sprite sprite = StorageNetworkSprites.GetOverviewIcon();
                if (sprite != null)
                {
                    icon.sprite = sprite;
                }

                return button;
            }
        }
    }
}
