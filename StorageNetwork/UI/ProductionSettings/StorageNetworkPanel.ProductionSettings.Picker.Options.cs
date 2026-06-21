using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using static StorageNetwork.STRINGS;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        private void ShowMaterialSourcePicker(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_AUTO_DESC),
                requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                () =>
                {
                    requester.UseAutomaticMaterialSource();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && requester.ResolveSourceStorage() == captured,
                    () =>
                    {
                        requester.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowEnergyGeneratorSourcePicker(Storage ownerStorage, StorageNetworkEnergyGeneratorRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.ENERGY_GENERATOR_SOURCE_DESC),
                    requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        requester.UseAutomaticMaterialSource();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    requester.CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && requester.ResolveSourceStorage() == captured,
                    () =>
                    {
                        requester.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowOutputStorePicker(Storage ownerStorage, StorageNetworkMaterialRequester requester)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                () =>
                {
                    requester.UseAutomaticOutputStorage();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    requester.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && requester.ResolveOutputStorage() == captured,
                    () =>
                    {
                        requester.SetOutputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowStorageConnectorOutputStorePicker(Storage ownerStorage, StorageNetworkStorageConnector connector)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                connector.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                () =>
                {
                    connector.UseAutomaticOutputStorage();
                    CloseProductionPicker();
                    UpdateProductionSettingsPanel(true);
                })
            };

            foreach (Storage target in GetNetworkStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    connector.CurrentOutputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && connector.ResolveOutputStorage() == captured,
                    () =>
                    {
                        connector.SetOutputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowInputPortStorePicker(Storage ownerStorage, StorageNetworkLiquidInputPortIngress ingress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                    () =>
                    {
                        ingress.UseAutomaticInputStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage target in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && ingress.ResolveInputStorage() == captured,
                    () =>
                    {
                        ingress.SetInputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowInputPortStorePicker(Storage ownerStorage, StorageNetworkGasInputPortIngress ingress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                    () =>
                    {
                        ingress.UseAutomaticInputStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage target in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && ingress.ResolveInputStorage() == captured,
                    () =>
                    {
                        ingress.SetInputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowInputPortStorePicker(Storage ownerStorage, StorageNetworkSolidInputPortIngress ingress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_AUTO_DESC),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                    () =>
                    {
                        ingress.UseAutomaticInputStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage target in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    ingress.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && ingress.ResolveInputStorage() == captured,
                    () =>
                    {
                        ingress.SetInputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private void ShowOutputPortSourcePicker(Storage ownerStorage, StorageNetworkLiquidOutputPortEgress egress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_AUTO_DESC),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        egress.UseAutomaticSourceStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage source in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = source;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && egress.ResolveSourceStorage() == captured,
                    () =>
                    {
                        egress.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowOutputPortSourcePicker(Storage ownerStorage, StorageNetworkGasOutputPortEgress egress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_AUTO_DESC),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        egress.UseAutomaticSourceStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage source in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = source;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && egress.ResolveSourceStorage() == captured,
                    () =>
                    {
                        egress.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowOutputPortSourcePicker(Storage ownerStorage, StorageNetworkSolidOutputPortEgress egress)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_AUTO_DESC),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        egress.UseAutomaticSourceStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage source in StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage, ownerStorage?.storageFilters))
            {
                Storage captured = source;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    StorageNetworkProductionSettingsText.FormatStorageOptionDetails(captured),
                    egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && egress.ResolveSourceStorage() == captured,
                    () =>
                    {
                        egress.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowOutputPortLiquidFilterPicker(Storage ownerStorage, StorageNetworkLiquidOutputPortEgress egress)
        {
            ShowProductionPicker(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortLiquidFilterOptions(ownerStorage, egress, CloseProductionPicker, () => UpdateProductionSettingsPanel(true)));
        }

        private static List<ProductionPickerOption> BuildOutputPortLiquidFilterOptions(
            Storage ownerStorage,
            StorageNetworkLiquidOutputPortEgress egress,
            System.Action closePicker,
            System.Action refreshSettings)
        {
            Storage specificSource = egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? egress.ResolveSourceStorage()
                : null;
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_ANY),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_DESC),
                    !egress.GetSelectedOutputElement().HasValue,
                    () =>
                    {
                        egress.SetOutputElementAndRefresh(null);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    })
            };

            foreach (SimHashes elementHash in NetworkStorageTransferService.GetAvailableLiquidElementsInNetwork(ownerStorage, specificSource))
            {
                SimHashes captured = elementHash;
                Element element = ElementLoader.FindElementByHash(captured);
                string elementName = element != null ? element.name : captured.ToString();
                float available = GetAvailableElementAmount(ownerStorage, captured, specificSource);
                options.Add(new ProductionPickerOption(
                    elementName,
                    GameUtil.GetFormattedMass(available),
                    egress.GetSelectedOutputElement() == captured,
                    () =>
                    {
                        egress.SetOutputElementAndRefresh(captured);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    },
                    captured.CreateTag()));
            }

            if (options.Count == 1)
            {
                options.Add(new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_PORT_FILTER_EMPTY),
                    string.Empty,
                    false,
                    null));
            }

            return options;
        }

        private void ShowOutputPortGasFilterPicker(Storage ownerStorage, StorageNetworkGasOutputPortEgress egress)
        {
            ShowProductionPicker(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortGasFilterOptions(ownerStorage, egress, CloseProductionPicker, () => UpdateProductionSettingsPanel(true)));
        }

        private static List<ProductionPickerOption> BuildOutputPortGasFilterOptions(
            Storage ownerStorage,
            StorageNetworkGasOutputPortEgress egress,
            System.Action closePicker,
            System.Action refreshSettings)
        {
            Storage specificSource = egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? egress.ResolveSourceStorage()
                : null;
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_ANY),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_DESC),
                    !egress.GetSelectedOutputElement().HasValue,
                    () =>
                    {
                        egress.SetOutputElementAndRefresh(null);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    })
            };

            foreach (SimHashes elementHash in NetworkStorageTransferService.GetAvailableGasElementsInNetwork(ownerStorage, specificSource))
            {
                SimHashes captured = elementHash;
                Element element = ElementLoader.FindElementByHash(captured);
                string elementName = element != null ? element.name : captured.ToString();
                float available = GetAvailableElementAmount(ownerStorage, captured, specificSource);
                options.Add(new ProductionPickerOption(
                    elementName,
                    GameUtil.GetFormattedMass(available),
                    egress.GetSelectedOutputElement() == captured,
                    () =>
                    {
                        egress.SetOutputElementAndRefresh(captured);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    },
                    captured.CreateTag()));
            }

            if (options.Count == 1)
            {
                options.Add(new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.GAS_OUTPUT_PORT_FILTER_EMPTY),
                    string.Empty,
                    false,
                    null));
            }

            return options;
        }

        private void ShowOutputPortMaterialFilterPicker(Storage ownerStorage, StorageNetworkSolidOutputPortEgress egress)
        {
            ShowProductionPicker(
                Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_SELECT),
                BuildOutputPortMaterialFilterOptions(ownerStorage, egress, CloseProductionPicker, () => UpdateProductionSettingsPanel(true)));
        }

        private static List<ProductionPickerOption> BuildOutputPortMaterialFilterOptions(
            Storage ownerStorage,
            StorageNetworkSolidOutputPortEgress egress,
            System.Action closePicker,
            System.Action refreshSettings)
        {
            Storage specificSource = egress.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? egress.ResolveSourceStorage()
                : null;
            Tag? selected = egress.GetSelectedOutputTag();
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_ANY),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_DESC),
                    !selected.HasValue,
                    () =>
                    {
                        egress.SetOutputTagAndRefresh(null);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    })
            };

            foreach (Tag tag in NetworkStorageTransferService.GetAvailableSolidItemTagsInNetwork(ownerStorage, specificSource))
            {
                Tag captured = tag;
                float available = GetAvailableItemAmount(ownerStorage, captured, specificSource);
                options.Add(new ProductionPickerOption(
                    StorageItemUtility.GetTagDisplayName(captured),
                    GameUtil.GetFormattedMass(available),
                    selected.HasValue && selected.Value == captured,
                    () =>
                    {
                        egress.SetOutputTagAndRefresh(captured);
                        closePicker?.Invoke();
                        refreshSettings?.Invoke();
                    },
                    captured));
            }

            if (options.Count == 1)
            {
                options.Add(new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_OUTPUT_PORT_FILTER_EMPTY),
                    string.Empty,
                    false,
                    null));
            }

            return options;
        }

        private void ShowPowerOutputSourcePicker(Storage ownerStorage, StorageNetworkPowerOutputPortGenerator output)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_OUTPUT_PORT_SOURCE_AUTO_DESC),
                    output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        output.UseAutomaticSourceStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage source in GetPowerStorageTargets(ownerStorage))
            {
                Storage captured = source;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    FormatPowerStorageOptionDetails(captured),
                    output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && output.ResolveSourceStorage() == captured,
                    () =>
                    {
                        output.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowParticleOutputSourcePicker(Storage ownerStorage, StorageNetworkParticleOutputPortEgress output)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_MODE_SEARCH),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.PARTICLE_OUTPUT_PORT_SOURCE_AUTO_DESC),
                    output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SearchNetwork,
                    () =>
                    {
                        output.UseAutomaticSourceStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage source in GetParticleStorageTargets(ownerStorage))
            {
                Storage captured = source;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SOURCE), captured.GetProperName()),
                    FormatParticleStorageOptionDetails(captured),
                    output.CurrentSourceMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage && output.ResolveSourceStorage() == captured,
                    () =>
                    {
                        output.SetSourceStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.MATERIAL_REQUEST_SELECT_SOURCE), options);
        }

        private void ShowPowerInputStorePicker(Storage ownerStorage, StorageNetworkPowerInputPortConsumer input)
        {
            List<ProductionPickerOption> options = new List<ProductionPickerOption>
            {
                new ProductionPickerOption(
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_AUTO),
                    Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.POWER_INPUT_PORT_STORE_AUTO_DESC),
                    input.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.AutoNetwork,
                    () =>
                    {
                        input.UseAutomaticInputStorage();
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    })
            };

            foreach (Storage target in GetPowerStorageTargets(ownerStorage))
            {
                Storage captured = target;
                options.Add(new ProductionPickerOption(
                    string.Format(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_TARGET), captured.GetProperName()),
                    FormatPowerStorageOptionDetails(captured),
                    input.CurrentInputStoreMode == StorageNetworkMaterialRequester.OutputStoreMode.SpecificStorage && input.ResolveInputStorage() == captured,
                    () =>
                    {
                        input.SetInputStorage(captured);
                        CloseProductionPicker();
                        UpdateProductionSettingsPanel(true);
                    }));
            }

            ShowProductionPicker(Get(StorageNetwork.STRINGS.UI.STORAGE_NETWORK.OUTPUT_STORE_MODE_SPECIFIC), options);
        }

        private static float GetAvailableElementAmount(Storage ownerStorage, SimHashes elementHash, Storage specificSource)
        {
            Tag tag = elementHash.CreateTag();
            float amount = 0f;
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject)).Storages;
            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == ownerStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source))
                {
                    continue;
                }

                amount += source.GetAmountAvailable(tag);
            }

            return amount;
        }

        private static float GetAvailableItemAmount(Storage ownerStorage, Tag tag, Storage specificSource)
        {
            float amount = 0f;
            IEnumerable<Storage> sources = specificSource != null
                ? new[] { specificSource }
                : StorageSceneCollector.CollectLightweightForWorld(StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject)).Storages;
            foreach (Storage source in sources)
            {
                if (source == null ||
                    source == ownerStorage ||
                    !StorageNetworkStorageRules.IsServerStorage(source) ||
                    !StorageNetworkStorageRules.IsConnectedNetworkStorage(source))
                {
                    continue;
                }

                amount += source.GetAmountAvailable(tag);
            }

            return amount;
        }

        private static List<Storage> GetNetworkStorageTargets(Storage ownerStorage)
        {
            return StorageNetworkStorageRules.GetNetworkStorageTargets(ownerStorage);
        }

        private static IEnumerable<Storage> GetPowerStorageTargets(Storage ownerStorage)
        {
            int worldId = StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject);
            foreach (Storage storage in StorageSceneCollector.CollectLightweightForWorld(worldId).Storages)
            {
                if (storage == null || storage == ownerStorage || storage.GetComponent<StorageNetworkPowerStorage>() == null)
                {
                    continue;
                }

                if (StorageNetworkStorageRules.IsConnectedNetworkStorage(storage))
                {
                    yield return storage;
                }
            }
        }

        private static IEnumerable<Storage> GetParticleStorageTargets(Storage ownerStorage)
        {
            int worldId = StorageTargetSelector.GetObjectWorldId(ownerStorage?.gameObject);
            foreach (Storage storage in StorageSceneCollector.CollectLightweightForWorld(worldId).Storages)
            {
                if (storage == null ||
                    storage == ownerStorage ||
                    !StorageNetworkStorageRules.IsParticleStorageServer(storage) ||
                    storage.GetComponent<HighEnergyParticleStorage>() == null)
                {
                    continue;
                }

                if (StorageNetworkStorageRules.IsConnectedNetworkStorage(storage))
                {
                    yield return storage;
                }
            }
        }

        private static string FormatPowerStorageOptionDetails(Storage storage)
        {
            StorageNetworkPowerStorage powerStorage = storage != null ? storage.GetComponent<StorageNetworkPowerStorage>() : null;
            if (powerStorage == null)
            {
                return string.Empty;
            }

            return string.Format(
                "{0} / {1}",
                GameUtil.GetFormattedJoules(powerStorage.RawJoulesAvailable, "F2", GameUtil.TimeSlice.None),
                GameUtil.GetFormattedJoules(powerStorage.CapacityJoules, "F2", GameUtil.TimeSlice.None));
        }

        private static string FormatParticleStorageOptionDetails(Storage storage)
        {
            HighEnergyParticleStorage particleStorage = storage != null ? storage.GetComponent<HighEnergyParticleStorage>() : null;
            if (particleStorage == null)
            {
                return string.Empty;
            }

            return string.Format(
                "{0} / {1}",
                FormatParticles(particleStorage.Particles),
                FormatParticles(particleStorage.Capacity()));
        }
    }
}
