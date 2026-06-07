using StorageNetwork.Components;
using UnityEngine;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkProductionSettingsText
    {
        public static string GetMaterialRequestModeName(StorageNetworkMaterialRequester requester)
        {
            if (requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = requester.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        public static string GetEnergyGeneratorSourceModeName(StorageNetworkEnergyGeneratorRequester requester)
        {
            if (requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage)
            {
                Storage source = requester.ResolveSourceStorage();
                return source != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), source.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH);
        }

        public static string GetOutputStoreModeName(StorageNetworkMaterialRequester requester)
        {
            if (requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage)
            {
                Storage target = requester.ResolveOutputStorage();
                return target != null
                    ? string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), target.GetProperName())
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO);
        }

        public static string FormatStorageOptionDetails(Storage storage)
        {
            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET_DETAILS),
                GameUtil.GetFormattedMass(storage.MassStored()),
                GameUtil.GetFormattedMass(storage.Capacity()),
                GameUtil.GetFormattedMass(Mathf.Max(0f, storage.RemainingCapacity())));
        }

        public static string ColorizeMaterialRequestStatus(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            if (MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_MISSING_SOURCE)) ||
                MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_STATUS_BLOCKED)))
            {
                return ColorText(text, "#a64c3c");
            }

            if (MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED)))
            {
                return ColorText(text, "#b5753c");
            }

            if (MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_REQUESTED)) ||
                MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED)) ||
                MatchesStatusTemplate(text, Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.TRANSFER_STATUS_MOVED)))
            {
                return ColorText(text, "#3f7f4a");
            }

            return ColorText(text, "#5a5f66");
        }

        public static string ColorText(string text, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, text);
        }

        private static bool MatchesStatusTemplate(string text, string template)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(template))
            {
                return false;
            }

            string prefix = GetTemplatePrefix(template);
            return string.IsNullOrEmpty(prefix)
                ? text == template
                : text.StartsWith(prefix);
        }

        private static string GetTemplatePrefix(string template)
        {
            int placeholderIndex = template.IndexOf('{');
            return placeholderIndex >= 0 ? template.Substring(0, placeholderIndex) : template;
        }
    }
}
