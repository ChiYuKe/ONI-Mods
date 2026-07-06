using HarmonyLib;
using System.Reflection;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Components;
using StorageNetwork.UI;
using StorageNetwork.UI.Installers;
using UnityEngine;
using UnityEngine.UI;

namespace StorageNetwork.Patches
{
    public static class SideScreenPatch
    {
        private const string TitleSettingsButtonName = "StorageNetworkTitleSettingsButton";
        private static readonly FieldInfo DetailsCodexEntryButtonField = AccessTools.Field(typeof(DetailsScreen), "CodexEntryButton");
        private static readonly FieldInfo DetailsCloseButtonField = AccessTools.Field(typeof(DetailsScreen), "CloseButton");
        private static Sprite titleSettingsButtonSprite;

        [HarmonyPatch(typeof(ManagementMenu), "OnPrefabInit")]
        public static class ManagementMenuOnPrefabInitPatch
        {
            public static void Postfix(ManagementMenu __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    StorageNetworkManagementMenuInstaller.Install(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add management menu button: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(ClusterDestinationSideScreen), "IsValidForTarget")]
        public static class ClusterDestinationSideScreenIsValidForTargetPatch
        {
            public static void Postfix(GameObject target, ref bool __result)
            {
                if (__result || target == null)
                {
                    return;
                }

                StorageNetworkRelayModule relay = target.GetComponent<StorageNetworkRelayModule>();
                RocketModuleCluster module = target.GetComponent<RocketModuleCluster>();
                __result = relay != null &&
                           module != null &&
                           module.CraftInterface != null &&
                           module.CraftInterface.HasClusterDestinationSelector();
            }
        }

        [HarmonyPatch(typeof(ComplexFabricatorSideScreen), "IsValidForTarget")]
        public static class ComplexFabricatorSideScreenIsValidForTargetPatch
        {
            public static bool Prefix(GameObject target, ref bool __result)
            {
                if (target != null && target.GetComponent<StorageNetworkOrderProductionCenter>() != null)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreenOnPrefabInitPatch
        {
            public static void Postfix(DetailsScreen __instance)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    StorageNetworkCoreSideScreenInstaller.Install(__instance);
                    StorageNetworkLogicDiySideScreenInstaller.Install(__instance);
                    StorageNetworkOrderProductionCenterSideScreenInstaller.Install(__instance);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to add core side screen: " + exception);
                }
            }
        }

        [HarmonyPatch(typeof(DetailsScreen), "Refresh")]
        public static class DetailsScreenRefreshPatch
        {
            public static void Postfix(DetailsScreen __instance, GameObject go)
            {
                if (__instance == null)
                {
                    return;
                }

                try
                {
                    RefreshStorageNetworkTitleButton(__instance, go);
                }
                catch (System.Exception exception)
                {
                    Debug.LogWarning("[StorageNetwork] Failed to refresh title port network settings button: " + exception);
                }
            }
        }

        private static void RefreshStorageNetworkTitleButton(DetailsScreen detailsScreen, GameObject target)
        {
            Storage storage = target != null ? target.GetComponent<Storage>() : null;
            KButton template = DetailsCodexEntryButtonField?.GetValue(detailsScreen) as KButton;
            if (template == null || template.transform.parent == null)
            {
                return;
            }

            Transform parent = template.transform.parent;
            Transform existing = parent.Find(TitleSettingsButtonName);
            bool builtInButton = StorageNetworkStorageRules.IsNetworkPortStorage(storage) ||
                                 StorageNetworkStorageRules.HasSettingsButtonTag(storage);
            StorageNetworkSettingsButtonState settingsButtonState = StorageNetworkInterfaceResolver.GetSettingsButtonState(storage);
            bool shouldShow = builtInButton || settingsButtonState.IsVisible;
            if (!shouldShow)
            {
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            GameObject buttonObject = existing != null
                ? existing.gameObject
                : Object.Instantiate(template.gameObject, parent, false);
            buttonObject.name = TitleSettingsButtonName;
            buttonObject.SetActive(true);

            StorageNetworkTitleButtonState state = buttonObject.GetComponent<StorageNetworkTitleButtonState>();
            if (state == null)
            {
                state = buttonObject.AddComponent<StorageNetworkTitleButtonState>();
                state.Initialize(buttonObject);
            }

            state.SetTarget(storage, builtInButton);
            PlaceBeforeCloseButton(detailsScreen, buttonObject.transform);
        }

        private static Sprite GetTitleSettingsButtonSprite()
        {
            if (titleSettingsButtonSprite == null)
            {
                titleSettingsButtonSprite = StorageNetworkSpriteLoader.GetSprite("storage_network_overlay") ??
                                            Assets.GetSprite("icon_category_storage") ??
                                            Assets.GetSprite("icon_category_shipping") ??
                                            Assets.GetSprite("unknown");
            }

            return titleSettingsButtonSprite;
        }

        private static void PlaceBeforeCloseButton(DetailsScreen detailsScreen, Transform buttonTransform)
        {
            KButton closeButton = DetailsCloseButtonField?.GetValue(detailsScreen) as KButton;
            if (closeButton != null && closeButton.transform.parent == buttonTransform.parent)
            {
                buttonTransform.SetSiblingIndex(closeButton.transform.GetSiblingIndex());
            }
            else
            {
                buttonTransform.SetAsLastSibling();
            }
        }

        private sealed class StorageNetworkTitleButtonState : KMonoBehaviour
        {
            private KButton button;
            private ToolTip tooltip;
            private Image icon;
            private readonly System.Collections.Generic.List<Image> backgroundImages = new System.Collections.Generic.List<Image>();
            private readonly System.Collections.Generic.List<Image> extraIconImages = new System.Collections.Generic.List<Image>();
            private Storage targetStorage;
            private bool builtInButton;

            public void Initialize(GameObject buttonObject)
            {
                button = buttonObject.GetComponent<KButton>() ?? buttonObject.GetComponentInChildren<KButton>(true);
                tooltip = buttonObject.GetComponent<ToolTip>() ?? buttonObject.GetComponentInChildren<ToolTip>(true);

                LocText text = buttonObject.GetComponentInChildren<LocText>(true);
                if (text != null)
                {
                    text.gameObject.SetActive(false);
                }

                ConfigureImages(buttonObject);
                RefreshIcon();

                tooltip?.SetSimpleTooltip(StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_NETWORK_SETTINGS_TOOLTIP));

                if (button != null)
                {
                    button.isInteractable = true;
                    button.ClearOnClick();
                    button.onClick += OnClick;
                }
            }

            public void SetTarget(Storage storage, bool builtInButton)
            {
                targetStorage = storage;
                this.builtInButton = builtInButton;
                StorageNetworkSettingsButtonState state = StorageNetworkInterfaceResolver.GetSettingsButtonState(storage);
                bool enabled = builtInButton && !state.IsVisible
                    ? true
                    : state.IsEnabled;
                RefreshIcon();
                if (button != null)
                {
                    button.isInteractable = targetStorage != null && enabled;
                }

                string tooltipText = !string.IsNullOrEmpty(state.Tooltip)
                    ? state.Tooltip
                    : StorageNetwork.STRINGS.Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PORT_NETWORK_SETTINGS_TOOLTIP);
                tooltip?.SetSimpleTooltip(tooltipText);
            }

            private void RefreshIcon()
            {
                foreach (Image background in backgroundImages)
                {
                    if (background == null)
                    {
                        continue;
                    }

                    background.enabled = true;
                    background.color = new Color(0.22f, 0.24f, 0.31f, 1f);
                }

                foreach (Image extraIcon in extraIconImages)
                {
                    if (extraIcon != null)
                    {
                        extraIcon.enabled = false;
                    }
                }

                Sprite sprite = GetTitleSettingsButtonSprite();
                if (icon != null && sprite != null)
                {
                    icon.sprite = sprite;
                    icon.color = Color.white;
                    icon.preserveAspect = true;
                    icon.enabled = true;
                }
            }

            private void OnClick()
            {
                if (targetStorage != null)
                {
                    if (!builtInButton)
                    {
                        StorageNetworkSettingsButtonState state = StorageNetworkInterfaceResolver.GetSettingsButtonState(targetStorage);
                        if (!state.IsEnabled)
                        {
                            return;
                        }
                    }

                    StorageNetworkPanel.ShowSettings(targetStorage);
                }
            }

            private void ConfigureImages(GameObject buttonObject)
            {
                Image[] images = buttonObject.GetComponentsInChildren<Image>(true);
                icon = null;
                backgroundImages.Clear();
                extraIconImages.Clear();
                foreach (Image image in images)
                {
                    if (image == null)
                    {
                        continue;
                    }

                    string imageName = image.name.ToLowerInvariant();
                    bool isBackground = imageName.Contains("bg") ||
                        imageName.Contains("background") ||
                        image.gameObject == buttonObject;
                    if (isBackground)
                    {
                        backgroundImages.Add(image);
                        continue;
                    }

                    if (icon == null)
                    {
                        icon = image;
                    }
                    else
                    {
                        extraIconImages.Add(image);
                    }
                }
            }
        }
    }
}
