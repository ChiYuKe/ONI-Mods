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

        public static string GetProductionStateText(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.CurrentWorkingOrder == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_IDLE);
            }

            return fabricator.WaitingForWorker
                ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_WAITING_WORKER)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_SHORT_CRAFTING);
        }

        public static Color GetProductionStateColor(ComplexFabricator fabricator)
        {
            if (fabricator == null || fabricator.CurrentWorkingOrder == null)
            {
                return new Color(0.38f, 0.42f, 0.36f, 1f);
            }

            return fabricator.WaitingForWorker
                ? new Color(0.64f, 0.42f, 0.24f, 1f)
                : new Color(0.26f, 0.52f, 0.34f, 1f);
        }

        public static string GetCurrentRecipeText(ComplexFabricator fabricator)
        {
            return fabricator != null && fabricator.CurrentWorkingOrder != null
                ? GetRecipeDisplayName(fabricator.CurrentWorkingOrder)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
        }

        public static string GetNetworkStateText(
            StorageNetworkMaterialRequester requester,
            StorageNetworkStorageConnector connector,
            StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (requester != null)
            {
                return requester.RequestEnabled
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_ON_SHORT)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_OFF_SHORT);
            }

            if (connector != null)
            {
                return connector.OutputStoreEnabled
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_ON_SHORT)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_OFF_SHORT);
            }

            if (energyRequester != null)
            {
                return energyRequester.RequestEnabled
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_ON_SHORT)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.REQUEST_OFF_SHORT);
            }

            return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NO_COMPONENT);
        }

        public static LocString GetProductionInputMetricLabel(ComplexFabricator fabricator, StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            return energyRequester != null && fabricator == null
                ? StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_REQUIRED
                : StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PRODUCTION_METRIC_RECIPE;
        }

        public static bool IsNetworkAutomationEnabled(
            StorageNetworkMaterialRequester requester,
            StorageNetworkStorageConnector connector,
            StorageNetworkEnergyGeneratorRequester energyRequester)
        {
            if (requester != null)
            {
                return requester.RequestEnabled || requester.OutputStoreEnabled;
            }

            if (connector != null)
            {
                return connector.OutputStoreEnabled;
            }

            if (energyRequester != null)
            {
                return energyRequester.RequestEnabled;
            }

            return false;
        }

        public static string GetEnergyGeneratorFuelText(EnergyGenerator generator)
        {
            if (generator == null || generator.formula.inputs == null || generator.formula.inputs.Length == 0)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
            }

            System.Collections.Generic.List<string> names = new System.Collections.Generic.List<string>();
            foreach (EnergyGenerator.InputItem input in generator.formula.inputs)
            {
                if (input.tag != Tag.Invalid)
                {
                    names.Add(input.tag.ProperName());
                }
            }

            return names.Count > 0
                ? string.Join(", ", names)
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.NONE);
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

        private static string GetRecipeDisplayName(ComplexRecipe recipe)
        {
            return recipe != null ? recipe.GetUIName(false) : string.Empty;
        }
    }
}
