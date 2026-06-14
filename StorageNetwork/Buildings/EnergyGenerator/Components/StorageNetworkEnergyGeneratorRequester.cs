using System.Collections.Generic;
using KSerialization;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;
using Loc = StorageNetwork.STRINGS;

namespace StorageNetwork.Components
{
    /// <summary>
    /// 发电机燃料请求组件。挂在带 EnergyGenerator 输入配方的建筑上，从储存网络补充燃料到自身 Storage。
    /// </summary>
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class StorageNetworkEnergyGeneratorRequester : KMonoBehaviour, ISim1000ms
    {
        [Serialize]
        public bool RequestEnabled;

        [Serialize]
        public int Mode;

        [Serialize]
        public int SourceStorageInstanceId = KPrefabID.InvalidInstanceID;

        [Serialize]
        public bool LimitEnabled;

        [Serialize]
        public float LimitKg = Config.Instance.DefaultMaterialRequestLimitKg;

        [Serialize]
        public float RequestedKg;

        [MyCmpGet]
        private EnergyGenerator generator;

        [MyCmpGet]
        private Storage storage;

        private string lastStatus;
        private float requestCooldown;

        public string LastStatus => lastStatus;

        public StorageNetworkMaterialRequester.RequestMode CurrentMode
        {
            get => (StorageNetworkMaterialRequester.RequestMode)Mathf.Clamp(Mode, 0, 1);
            set => Mode = (int)value;
        }

        public void Sim1000ms(float dt)
        {
            EnsureComponents();
            StorageNetworkEnrollment enrollment = GetComponent<StorageNetworkEnrollment>();
            if (generator == null || storage == null || enrollment == null || !enrollment.IncludedInSceneNetwork || !RequestEnabled)
            {
                lastStatus = string.Empty;
                requestCooldown = 0f;
                return;
            }

            if (!HasFuelInputs(generator))
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_NO_QUEUE);
                return;
            }

            if (requestCooldown > 0f)
            {
                requestCooldown -= dt;
                return;
            }

            int worldId = StorageNetworkWorldUtility.GetObjectWorldId(gameObject);
            if (!StorageSceneRegistry.HasOnlineCoreInWorld(worldId))
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.CORE_OFFLINE_TITLE);
                requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
                return;
            }

            bool movedAny = false;
            bool requestedAny = false;
            float remainingLimit = LimitEnabled ? Mathf.Max(0f, LimitKg - RequestedKg) : float.MaxValue;
            if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_LIMIT_REACHED);
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
                return;
            }

            float totalMoved = 0f;
            string blockedFuel = null;
            foreach (EnergyGenerator.InputItem input in generator.formula.inputs)
            {
                if (remainingLimit <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    break;
                }

                if (input.tag == Tag.Invalid || input.maxStoredMass <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                float missing = Mathf.Max(0f, input.maxStoredMass - storage.GetAmountAvailable(input.tag));
                if (missing <= PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    continue;
                }

                requestedAny = true;
                float moved = NetworkStorageTransferService.TransferFromNetworkToStorage(
                    new[] { input.tag },
                    Mathf.Min(missing, remainingLimit),
                    storage,
                    GetExcludedStorages(),
                    GetSpecificSourceStorage());
                if (moved > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    movedAny = true;
                    totalMoved += moved;
                    RequestedKg += moved;
                    remainingLimit -= moved;
                }
                else if (string.IsNullOrEmpty(blockedFuel))
                {
                    blockedFuel = input.tag.ProperName();
                }
            }

            if (!requestedAny)
            {
                lastStatus = Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_SATISFIED);
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
                return;
            }

            if (movedAny)
            {
                lastStatus = string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.TRANSFER_STATUS_MOVED), GameUtil.GetFormattedMass(totalMoved));
                requestCooldown = Config.Instance.MaterialRequestSuccessCooldownSeconds;
                return;
            }

            lastStatus = !string.IsNullOrEmpty(blockedFuel)
                ? string.Format(Loc.Get(Loc.UI.STORAGE_NETWORK.TRANSFER_STATUS_BLOCKED), blockedFuel)
                : Loc.Get(Loc.UI.STORAGE_NETWORK.MATERIAL_STATUS_WAITING_CONTENTS);
            requestCooldown = Config.Instance.MaterialRequestRetryCooldownSeconds;
        }

        public static bool HasFuelInputs(EnergyGenerator candidate)
        {
            if (candidate == null || candidate.formula.inputs == null || candidate.formula.inputs.Length == 0)
            {
                return false;
            }

            foreach (EnergyGenerator.InputItem input in candidate.formula.inputs)
            {
                if (input.tag != Tag.Invalid && input.maxStoredMass > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                {
                    return true;
                }
            }

            return false;
        }

        public Storage ResolveSourceStorage()
        {
            if (SourceStorageInstanceId == KPrefabID.InvalidInstanceID)
            {
                return null;
            }

            foreach (StorageInfo info in StorageSceneCollector.Collect().Storages)
            {
                Storage candidate = info?.Storage;
                if (StorageNetworkStorageRules.IsServerStorage(candidate) &&
                    GetStorageInstanceId(candidate) == SourceStorageInstanceId)
                {
                    return candidate;
                }
            }

            return null;
        }

        public void SetSourceStorage(Storage source)
        {
            SourceStorageInstanceId = GetStorageInstanceId(source);
            CurrentMode = StorageNetworkMaterialRequester.RequestMode.SpecificStorage;
        }

        public void UseAutomaticMaterialSource()
        {
            CurrentMode = StorageNetworkMaterialRequester.RequestMode.SearchNetwork;
            SourceStorageInstanceId = KPrefabID.InvalidInstanceID;
        }

        public float GetRequestedAmountForDisplay()
        {
            return RequestedKg;
        }

        public void ResetRequestedAmount()
        {
            RequestedKg = 0f;
        }

        private Storage GetSpecificSourceStorage()
        {
            return CurrentMode == StorageNetworkMaterialRequester.RequestMode.SpecificStorage
                ? ResolveSourceStorage()
                : null;
        }

        private IEnumerable<Storage> GetExcludedStorages()
        {
            yield return storage;
        }

        private void EnsureComponents()
        {
            if (generator == null)
            {
                generator = GetComponent<EnergyGenerator>();
            }

            if (storage == null)
            {
                storage = GetComponent<Storage>();
            }
        }

        private static int GetStorageInstanceId(Storage candidate)
        {
            KPrefabID prefabId = candidate != null ? candidate.GetComponent<KPrefabID>() : null;
            return prefabId != null ? prefabId.InstanceID : KPrefabID.InvalidInstanceID;
        }
    }
}
