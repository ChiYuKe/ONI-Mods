using System;
using System.Collections.Generic;
using StorageNetwork.API;
using StorageNetwork.Components;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

internal static class FarmingRuntimeChecks
{
    private static readonly Tag Seed = new Tag("Seed");
    private static int nextId;

    public static void Verify(Action<bool, string> check)
    {
        Reset();
        Storage earlier = Store(true), later = Store(true), source = Store(false);
        earlier.items.Add(Item(1f, true));
        later.items.Add(Item(2f, true));
        Components.PlantablePlots.Items.Add(new PlantablePlot { GetActiveRequest = new FetchChore { tagsFirst = Seed, originalAmount = 1f } });
        int returned = 0;
        NetworkStorageTransferService.ReturnCallback = (_, item) => returned++;
        StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, new[] { Seed });
        check(returned == 1 && later.items.Count == 0 && earlier.items.Count == 1,
            "actual farming reconciliation honors earlier port seed reservation priority");

        Reset();
        Storage outer = Store(true), inner = Store(true);
        outer.items.Add(Item(1f, true));
        outer.items.Add(Item(2f, true));
        inner.items.Add(Item(3f, true));
        var returnedMass = new List<float>();
        bool nested = false;
        NetworkStorageTransferService.ReturnCallback = (port, item) =>
        {
            returnedMass.Add(StorageItemUtility.GetMass(item));
            if (port == outer && !nested)
            {
                nested = true;
                StorageNetworkFarmingSupplyService.SupplyNextPlanting(inner, null, null);
            }
        };
        StorageNetworkFarmingSupplyService.SupplyNextPlanting(outer, null, null);
        check(outer.items.Count == 0 && inner.items.Count == 0 && returnedMass.Count == 3 &&
              returnedMass[0] == 1f && returnedMass[1] == 3f && returnedMass[2] == 2f,
            "actual nested return callback retains separate workspaces and original item iteration order");

        Reset();
        source = Store(false);
        later = Store(true);
        source.items.Add(Item(5f, false));
        later.CapacityKg = 2f;
        Components.PlantablePlots.Items.Add(new PlantablePlot { GetActiveRequest = new FetchChore { tagsFirst = Seed, originalAmount = 3f } });
        var allowed = new HashSet<Tag> { Seed };
        var supplied = StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, allowed);
        check(supplied.MovedKg == 2f && later.MassStored() == 2f && source.MassStored() == 3f &&
              later.items[0].GetComponent<KPrefabID>().HasTag(StorageNetworkTags.ReservedForFarming) && allowed.Count == 1,
            "actual seed split respects capacity, reservation marker and caller-owned filters");
        Components.PlantablePlots.Items.Clear();
        Time.time += 0.6f;
        StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, new[] { new Tag("OtherSeed") });
        check(later.items.Count == 0, "changed filter returns reserved seeds without materialized filtered dictionaries");

        Reset();
        source = Store(false);
        later = Store(true);
        source.items.Add(Item(1f, false));
        StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, null);
        Components.PlantablePlots.Items.Add(new PlantablePlot { GetActiveRequest = new FetchChore { tagsFirst = Seed, originalAmount = 1f } });
        Time.time = 0.6f;
        check(StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, null).MovedKg == 0f,
            "existing two-second retry still gates unchanged idle port");
        Time.time = 2.1f;
        check(StorageNetworkFarmingSupplyService.SupplyNextPlanting(later, source, null).MovedKg == 1f,
            "retry resumes at original cadence using refreshed demand snapshot");
        Reset();
    }

    private static void Reset()
    {
        StorageNetworkFarmingSupplyService.ResetRuntimeState();
        StorageNetworkContentIndexService.ResetRuntimeState();
        StorageSceneRegistry.Storages.Clear();
        StorageSceneRegistry.OfflineWorlds.Clear();
        StorageSceneRegistry.MembershipVersion++;
        StorageNetworkPerformanceMode.LegacyFullScanEnabled = false;
        StorageNetworkPerformanceMode.ShadowValidationEnabled = false;
        Components.PlantablePlots.Items.Clear();
        NetworkStorageTransferService.ReturnCallback = null;
        Time.time = 0f;
        nextId = 0;
    }

    private static Storage Store(bool port)
    {
        var storage = new Storage(++nextId, 0f) { Server = !port };
        storage.gameObject.Set(new KPrefabID { InstanceID = storage.InstanceId, gameObject = storage.gameObject });
        if (port) storage.gameObject.Set(new StorageNetworkSolidOutputPortEgress());
        StorageSceneRegistry.Storages.Add(storage);
        StorageSceneRegistry.MembershipVersion++;
        return storage;
    }

    private static GameObject Item(float amount, bool reserved)
    {
        var item = new GameObject();
        item.MatchTags = new StorageItemUtility.StorageMatchTags
        { PrefabIdTag = Seed, PrefabTag = Seed, ElementTag = Seed, TransferTag = Seed };
        item.Set(new PrimaryElement { Mass = amount, ElementID = SimHashes.Dirt });
        item.Set(new Pickupable { TotalAmount = amount, gameObject = item });
        var prefab = new KPrefabID { gameObject = item };
        prefab.Tags.Add(Seed);
        if (reserved) prefab.Tags.Add(StorageNetworkTags.ReservedForFarming);
        item.Set(prefab);
        return item;
    }
}
