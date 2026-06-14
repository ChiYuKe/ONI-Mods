using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    internal static class StorageNetworkGeyserText
    {
        public static string GetElementName(Geyser geyser)
        {
            if (geyser == null || geyser.configuration == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
            }

            Element element = ElementLoader.FindElementByHash(geyser.configuration.GetElement());
            string elementName = element != null ? element.name : geyser.configuration.GetElement().CreateTag().ProperName();
            return StorageNetworkTextFormatting.StripKleiLinkFormatting(elementName);
        }

        public static string GetAverageRate(Geyser geyser)
        {
            return geyser != null && geyser.configuration != null
                ? GameUtil.GetFormattedMass(geyser.configuration.GetAverageEmission(), GameUtil.TimeSlice.PerSecond, GameUtil.MetricMassFormat.UseThreshold, true, "{0:0.#}")
                : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ORDER_UNKNOWN);
        }

        public static string GetStorageListDetails(Geyser geyser)
        {
            if (geyser == null || geyser.configuration == null)
            {
                return Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_ANALYZED);
            }

            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_OUTPUT),
                GetElementName(geyser),
                GetAverageRate(geyser));
        }

        public static string GetEnrollmentDetails(StorageNetwork.Components.StorageNetworkEnrollment enrollment)
        {
            Geyser geyser = enrollment != null ? enrollment.GetComponent<Geyser>() : null;
            if (geyser == null || geyser.configuration == null)
            {
                return string.Empty;
            }

            return string.Format(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENROLLABLE_GEYSER_OUTPUT),
                GetElementName(geyser),
                GetAverageRate(geyser));
        }
    }
}
