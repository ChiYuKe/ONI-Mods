using System.Linq;
using StorageNetwork.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel : KScreen, IInputHandler
    {
        private void CreateGeyserRow(StorageInfo storageInfo, Transform parent)
        {
            Geyser geyser = storageInfo.Geyser;
            if (geyser == null)
            {
                return;
            }

            bool expanded = expandedGeysers.TryGetValue(geyser, out bool isExpanded) && isExpanded;
            GameObject row = CreateBox("GeyserRow", parent, new Color(0.88f, 0.87f, 0.82f, 1f));
            AddVerticalContainer(row, 0f, 0, 0, 0, 0);

            string details = StorageNetworkGeyserText.GetStorageListDetails(geyser);
            bool erupting = IsGeyserErupting(geyser);
            CreateFoldoutHeader(
                row.transform,
                expanded,
                storageInfo.Name,
                details,
                new Color(0.72f, 0.72f, 0.68f, 1f),
                13,
                320f,
                () =>
                {
                    expandedGeysers[geyser] = !expanded;
                    RefreshStoragePanel(StoragePanelRefreshMode.Structure);
                },
                erupting
                    ? Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_ERUPTING)
                    : Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GEYSER_NOT_ERUPTING),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.STORAGE_SETTINGS),
                () => ShowGeyserSettingsDialog(geyser),
                erupting ? new Color(0.28f, 0.48f, 0.34f, 1f) : new Color(0.62f, 0.24f, 0.24f, 1f));

            if (!expanded)
            {
                row.AddComponent<LayoutElement>().preferredHeight = 34f;
                return;
            }

            AddGeyserDescriptorDetails(row.transform, geyser);
            row.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static bool IsGeyserErupting(Geyser geyser)
        {
            StorageNetwork.Components.StorageNetworkGeyserOutput networkOutput = geyser != null
                ? geyser.GetComponent<StorageNetwork.Components.StorageNetworkGeyserOutput>()
                : null;
            if (networkOutput != null)
            {
                return networkOutput.IsRuntimeErupting();
            }

            ElementEmitter emitter = geyser != null ? geyser.GetComponent<ElementEmitter>() : null;
            return emitter != null && emitter.IsSimActive;
        }

        private void AddGeyserDescriptorDetails(Transform parent, Geyser geyser)
        {
            GameObject details = CreateBox("GeyserDetails", parent, new Color(0.82f, 0.82f, 0.77f, 1f));
            VerticalLayoutGroup layout = details.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 2f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            foreach (Descriptor descriptor in geyser.GetDescriptors(geyser.gameObject).Where(descriptor => descriptor.type == Descriptor.DescriptorType.Effect))
            {
                TextMeshProUGUI text = CreateText("GeyserDescriptor", details.transform, StorageNetworkTextFormatting.StripKleiLinkFormatting(descriptor.text), 11, TextAlignmentOptions.MidlineLeft);
                text.color = new Color(0.22f, 0.23f, 0.22f, 1f);
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Overflow;
                text.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
            }

            details.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
