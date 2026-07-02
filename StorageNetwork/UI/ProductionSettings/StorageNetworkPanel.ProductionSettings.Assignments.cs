using System.Collections.Generic;
using System.Linq;
using StorageNetwork.API;
using StorageNetwork.Core;
using StorageNetwork.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void AddServerAssignmentsSettingsCard(Storage storage)
        {
            if (!ShouldShowServerAssignmentsSettingsCard(storage))
            {
                return;
            }

            List<InputTargetReservation> inputReservations = StorageNetworkInputTargetReservationService.GetReservationsForTarget(storage);
            List<InputTargetReservation> outputReservations = StorageNetworkInputTargetReservationService.GetOutputSourceReservationsForTarget(storage);
            if (inputReservations.Count == 0 && outputReservations.Count == 0)
            {
                return;
            }

            GameObject card = CreateProductionCard(
                "ServerAssignmentsSettingsCard",
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.SERVER_ASSIGNMENTS_TITLE),
                0f);
            MakeProductionCardAutoHeight(card, 96f);

            GameObject columns = CreatePlainImage("ServerAssignmentColumns", card.transform, StorageNetworkPanelPalette.CardBackground);
            HorizontalLayoutGroup columnsLayout = columns.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.padding = new RectOffset(0, 0, 0, 0);
            columnsLayout.spacing = 8f;
            columnsLayout.childAlignment = TextAnchor.UpperCenter;
            columnsLayout.childControlWidth = true;
            columnsLayout.childControlHeight = true;
            columnsLayout.childForceExpandWidth = true;
            columnsLayout.childForceExpandHeight = false;
            columns.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateServerAssignmentSettingsColumn(
                columns.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_ASSIGNMENT_TITLE),
                inputReservations,
                () => StorageNetworkInputTargetReservationService.ClearReservationsForTarget(storage));
            CreateServerAssignmentSettingsColumn(
                columns.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_SOURCE_ASSIGNMENT_TITLE),
                outputReservations,
                () => StorageNetworkInputTargetReservationService.ClearOutputSourceReservationsForTarget(storage));
        }

        private static bool ShouldShowServerAssignmentsSettingsCard(Storage storage)
        {
            return storage != null &&
                   StorageNetworkStorageRules.IsServerStorage(storage) &&
                   !StorageNetworkStorageRules.IsPowerStorageServer(storage) &&
                   !StorageNetworkStorageRules.IsParticleStorageServer(storage);
        }

        private void CreateServerAssignmentSettingsColumn(
            Transform parent,
            string titleText,
            List<InputTargetReservation> reservations,
            System.Func<int> clearAllAction)
        {
            GameObject column = CreatePlainImage("ServerAssignmentColumn", parent, StorageNetworkPanelPalette.MetricBackground);
            LayoutElement columnLayout = column.AddComponent<LayoutElement>();
            columnLayout.flexibleWidth = 1f;
            columnLayout.minWidth = 0f;
            columnLayout.preferredHeight = -1f;

            VerticalLayoutGroup layout = column.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(7, 7, 5, 5);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            column.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject header = CreatePlainImage("ServerAssignmentHeader", column.transform, StorageNetworkPanelPalette.RowBackground);
            LayoutElement headerLayoutElement = header.AddComponent<LayoutElement>();
            headerLayoutElement.minHeight = 28f;
            headerLayoutElement.preferredHeight = 28f;

            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(7, 4, 2, 2);
            headerLayout.spacing = 5f;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = CreateText("Title", header.transform, titleText, 10, TextAlignmentOptions.MidlineLeft);
            title.color = StorageNetworkPanelPalette.BodyText;
            title.fontStyle = FontStyles.Bold;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Ellipsis;
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.preferredHeight = 24f;

            if (reservations.Count > 0)
            {
                GameObject clearAll = CreateStyledButton(
                    "ClearAllServerAssignments",
                    header.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_ASSIGNMENT_CLEAR_ALL),
                    () =>
                    {
                        clearAllAction?.Invoke();
                        RefreshServerAssignmentSettingsCard();
                    },
                    KleiPinkStyle());
                LayoutElement clearAllLayout = clearAll.AddComponent<LayoutElement>();
                clearAllLayout.minWidth = 92f;
                clearAllLayout.preferredWidth = 96f;
                clearAllLayout.minHeight = 22f;
                clearAllLayout.preferredHeight = 22f;
            }

            if (reservations.Count == 0)
            {
                TextMeshProUGUI none = CreateText(
                    "None",
                    column.transform,
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_ASSIGNMENT_NONE),
                    10,
                    TextAlignmentOptions.MidlineLeft);
                none.color = StorageNetworkPanelPalette.MutedText;
                none.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
                return;
            }

            foreach (InputTargetReservation reservation in reservations.OrderBy(reservation => reservation.DisplayName))
            {
                CreateServerAssignmentSettingsRow(column.transform, reservation);
            }
        }

        private void CreateServerAssignmentSettingsRow(Transform parent, InputTargetReservation reservation)
        {
            GameObject row = CreatePlainImage("ServerAssignmentRow", parent, StorageNetworkPanelPalette.RowBackground);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 28f;
            rowLayout.preferredHeight = 28f;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(7, 4, 2, 2);
            layout.spacing = 5f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI name = CreateText("Name", row.transform, reservation.DisplayName, 10, TextAlignmentOptions.MidlineLeft);
            name.color = StorageNetworkPanelPalette.BodyText;
            name.overflowMode = TextOverflowModes.Ellipsis;
            name.textWrappingMode = TextWrappingModes.NoWrap;
            LayoutElement nameLayout = name.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.preferredHeight = 24f;

            GameObject clear = CreateStyledButton(
                "ClearInputReservation",
                row.transform,
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.INPUT_ASSIGNMENT_CLEAR),
                () =>
                {
                    StorageNetworkInputTargetReservationService.ClearReservation(reservation);
                    RefreshServerAssignmentSettingsCard();
                },
                KleiBlueStyle());
            LayoutElement clearLayout = clear.AddComponent<LayoutElement>();
            clearLayout.minWidth = 46f;
            clearLayout.preferredWidth = 52f;
            clearLayout.minHeight = 22f;
            clearLayout.preferredHeight = 22f;
        }

        private void RefreshServerAssignmentSettingsCard()
        {
            productionSettingsSignature = null;
            UpdateProductionSettingsPanel(true);
            RefreshStoragePanel(StoragePanelRefreshMode.Structure);
        }
    }
}
