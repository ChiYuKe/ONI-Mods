using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using StorageNetwork.Services;
using UnityEngine;

internal static class ContentIndexRuntimeChecks
{
    private static readonly Tag Material = new Tag("Dirt");
    private static readonly Tag Unrelated = new Tag("Other");

    public static void Verify(Action<bool, string> check)
    {
        VerifyScopedRefresh(check);
        VerifyTransfersAndDetails(check);
        VerifyUnknownChanges(check);
        VerifyFoodAndTagDetails(check);
        VerifyRepeatedTransfers(check);
        Reset();
    }

    private static void Reset()
    {
        StorageNetworkContentIndexService.ResetRuntimeState();
        StorageSceneRegistry.Storages.Clear();
        StorageSceneRegistry.OfflineWorlds.Clear();
        StorageSceneRegistry.MembershipVersion++;
        StorageSceneRegistry.CapabilityVersion++;
        StorageSceneRegistry.Relay = false;
        StorageNetworkWorldUtility.Parents.Clear();
        StorageNetworkPerformanceMode.LegacyFullScanEnabled = false;
        StorageNetworkPerformanceMode.ShadowValidationEnabled = false;
        StorageNetworkPerformanceMode.ShadowValidationFullEnabled = false;
        StorageNetworkPerformanceCounters.ResetRuntimeState();
        StorageNetworkPerformanceCounters.SetEnabled(true);
        Time.frameCount = 100;
    }

    private static GameObject Item(Tag tag, float amount, float temperature = 300f, SimHashes element = SimHashes.Dirt)
    {
        var item = new GameObject();
        item.MatchTags = new StorageItemUtility.StorageMatchTags
        { PrefabIdTag = tag, PrefabTag = tag, ElementTag = tag, TransferTag = tag };
        item.Set(new PrimaryElement { Mass = amount, Temperature = temperature, ElementID = element });
        item.Set(new Pickupable { TotalAmount = amount });
        var prefab = new KPrefabID { gameObject = item };
        prefab.Tags.Add(tag);
        item.Set(prefab);
        return item;
    }

    private static Storage Store(int id, int world, float amount)
    {
        var storage = new Storage(id, 0f);
        storage.gameObject.WorldId = world;
        storage.gameObject.Set(new KPrefabID { InstanceID = id, gameObject = storage.gameObject });
        if (amount > 0) storage.items.Add(Item(Material, amount));
        StorageSceneRegistry.Storages.Add(storage);
        StorageSceneRegistry.MembershipVersion++;
        return storage;
    }

    private static void Change(Storage storage, float amount)
    {
        storage.items.Clear();
        if (amount > 0) storage.items.Add(Item(Material, amount));
        storage.OnStorageChange?.Invoke(null);
    }

    private static void Warm()
    {
        StorageNetworkContentIndexService.BeginTransferTransaction();
        StorageNetworkContentIndexService.EndTransferTransaction();
        StorageNetworkPerformanceCounters.ConsumeSnapshot();
    }

    private static float Amount(int world, bool related = false, bool stale = false) =>
        StorageNetworkContentIndexService.GetAmount(world, related, Material, null, stale);

    private static void VerifyScopedRefresh(Action<bool, string> check)
    {
        Reset();
        Storage local = Store(1, 0, 10f), remote = Store(2, 1, 20f), rocket = Store(3, 2, 30f);
        Warm();
        Change(remote, 25f);
        check(StorageNetworkContentIndexService.GetStorageAmount(local, Material) == 10f &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 0,
            "actual single-storage query leaves unrelated dirty worlds untouched");
        Change(local, 11f);
        check(Amount(0) == 11f && StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 1,
            "actual strict world query refreshes only its dirty dependencies");
        check(Amount(1) == 25f, "deferred remote dirty record remains available to its later strict query");

        Change(local, 12f);
        check(Amount(0, stale: true) == 11f, "same-frame allowed-stale query preserves its established snapshot");
        Time.frameCount++;
        Change(local, 13f);
        check(Amount(0, stale: true) == 13f, "repeated invalidation preserves first dirty frame and catches up next frame");

        StorageNetworkWorldUtility.Parents[2] = 0;
        Change(rocket, 31f);
        check(Amount(0) == 44f && Amount(2) == 44f, "landed rocket and parent queries share the exact related-world scope");
        StorageNetworkWorldUtility.Parents.Clear();
        check(Amount(0) == 13f && Amount(2) == 31f, "rocket departure removes related-world contribution immediately");
        StorageSceneRegistry.Relay = true;
        Change(remote, 26f);
        check(Amount(0, related: true) == 70f && Amount(0) == 13f, "relay merged scope flushes all worlds without broadening local queries");
        StorageSceneRegistry.Relay = false;

        Change(remote, 26f);
        Time.frameCount++;
        remote.gameObject.WorldId = 0;
        StorageNetworkContentIndexService.InvalidateWorld(1);
        check(Amount(1) == 0f && Amount(0) == 39f, "migration invalidation removes old-world ownership before scoped reads");
        rocket.gameObject.WorldId = 0;
        StorageSceneRegistry.MembershipVersion++;
        check(Amount(2) == 0f && Amount(0) == 70f, "registry-only migration synchronizes both world aggregates");
        remote.Live = false;
        StorageNetworkContentIndexService.Invalidate(remote);
        check(Amount(0) == 44f, "destroyed dirty storage unregisters before aggregate read");
        StorageSceneRegistry.OfflineWorlds.Add(0);
        check(Amount(0) == 0f, "offline core still hides network inventory");
        StorageSceneRegistry.OfflineWorlds.Clear();
        local.Connected = false;
        StorageNetworkContentIndexService.Invalidate(local);
        check(Amount(0) == 31f, "disconnected server stops contributing after native refresh");
        StorageNetworkContentIndexService.ResetRuntimeState();
        check(Amount(0) == 31f, "reload reseeds live state without old subscriptions or contributions");
    }

    private static List<Storage> Sources(Tag tag)
    {
        var sources = new List<Storage>();
        StorageNetworkContentIndexService.FillSourceStorages(0, false, new[] { tag }, null, null, sources, false);
        return sources;
    }

    private static void VerifyTransfersAndDetails(Action<bool, string> check)
    {
        Reset();
        Storage source = Store(1, 0, 20f), target = Store(2, 0, 0f), other = Store(3, 0, 10f);
        for (int i = 0; i < 200; i++) source.items.Add(Item(new Tag("Unrelated" + i), 5f));
        source.items.Add(Item(Unrelated, 10f));
        other.items.Add(Item(Unrelated, 10f));
        Warm();
        var beforeOrder = Sources(Unrelated);
        check(beforeOrder.Count == 2 && beforeOrder[0] == source && beforeOrder[1] == other,
            "equal source amounts retain ascending stable IDs");
        StorageNetworkPerformanceCounters.ConsumeSnapshot();
        var tags = source.items[0].MatchTags;
        StorageNetworkContentIndexService.BeginTransferTransaction();
        StorageNetworkContentIndexService.BeginTransferTransaction();
        source.items[0] = Item(Material, 15f, 280f);
        target.items.Add(Item(Material, 5f, 320f));
        source.OnStorageChange?.Invoke(null);
        target.OnStorageChange?.Invoke(null);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, 5f, 5f);
        check(StorageNetworkContentIndexService.GetStorageAmount(source, Material) == 15f &&
              StorageNetworkContentIndexService.GetStorageAmount(target, Material) == 5f,
            "same-transaction real index reads include pending symmetric deltas");
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().UpdatedTags == 0,
            "inner transfer scope does not commit outer transaction");
        StorageNetworkContentIndexService.EndTransferTransaction();
        var commit = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(commit.RefreshedStorages == 0 && commit.ScannedItems == 0 && commit.UpdatedTags == 8,
            "actual known transfer updates eight endpoint-tag entries regardless of 200 unrelated materials");
        var afterOrder = Sources(Unrelated);
        check(afterOrder.Count == 2 && afterOrder[0] == source && afterOrder[1] == other &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().SourceBucketSorts == 0,
            "unrelated source bucket order stays cached after known transfer");
        var amounts = Sources(Material);
        check(amounts.Count == 3 && amounts[0] == source && amounts[1] == other && amounts[2] == target,
            "changed bucket reorders using original amount comparator");
        StorageNetworkPerformanceCounters.ConsumeSnapshot();
        Time.frameCount++;
        var totals = new Dictionary<Tag, StorageNetworkIndexedItemTotal>();
        check(StorageNetworkContentIndexService.TryFillStorageItemTotals(new[] { source, target }, totals, false, out float mass) &&
              totals[Material].MassKg == 20f && totals[Material].AverageTemperature == 290f && mass == 1030f,
            "native split/merge detail reconstruction retains exact mass and weighted temperature");
        var details = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        Sources(Unrelated);
        check(details.DetailRefreshes == 2 && details.RefreshedStorages == 0 &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().SourceBucketSorts == 0,
            "detail rebuilding does not invalidate committed source aggregates or buckets");
        check(StorageNetworkContentIndexService.GetRemainingCapacity(target) == 9995f,
            "known delta preserves remaining capacity");
    }

    private static void VerifyRepeatedTransfers(Action<bool, string> check)
    {
        Reset();
        Storage left = Store(1, 0, 128f), right = Store(2, 0, 128f);
        Warm();
        bool exact = true;
        for (int iteration = 0; iteration < 1000; iteration++)
        {
            Storage from = iteration % 2 == 0 ? left : right;
            Storage to = iteration % 2 == 0 ? right : left;
            float moved = 0.125f;
            var tags = from.items[0].MatchTags;
            StorageNetworkContentIndexService.BeginTransferTransaction();
            Change(from, from.MassStored() - moved);
            Change(to, to.MassStored() + moved);
            StorageNetworkContentIndexService.RecordTransferMutation(from, to, tags, GameTags.Solid, moved, moved);
            StorageNetworkContentIndexService.EndTransferTransaction();
            exact &= StorageNetworkContentIndexService.GetStorageAmount(from, Material) == from.MassStored();
            exact &= StorageNetworkContentIndexService.GetStorageAmount(to, Material) == to.MassStored();
            exact &= Amount(0) == 256f;
        }
        var operations = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(exact && operations.RefreshedStorages == 0 && operations.ScannedItems == 0,
            "1,000 real index transactions preserve native endpoint amounts and conservation without rescanning");
    }

    private static void VerifyFoodAndTagDetails(Action<bool, string> check)
    {
        Reset();
        Storage source = Store(1, 0, 0f), target = Store(2, 0, 0f);
        var food = Item(Material, 4f);
        food.Set(new Edible { FoodID = "Meal", Calories = 400f, Units = 4f });
        food.Set(new PlantableSeed());
        source.items.Add(food);
        Warm();
        var extra = new Tag("Forbidden");
        food.GetComponent<KPrefabID>().Tags.Add(extra);
        // Indexed identity still points at live native tags, even without a mass event.
        check(StorageNetworkContentIndexService.GetAmount(0, false, Material, new[] { extra }, false) == 0f,
            "forbidden-tag queries continue to consult live native item tags");
        check(StorageNetworkContentIndexService.GetCountWithAdditionalTag(0, false, Material, extra) == 4 &&
              StorageNetworkContentIndexService.HasPlantableSeed(0, false, Material, extra),
            "additional-tag and plantable seed views retain their original semantics");
        StorageNetworkContentIndexService.BeginTransferTransaction();
        source.items.Clear();
        var merged = Item(Material, 4f);
        merged.Set(new Edible { FoodID = "Meal", Calories = 380f, Units = 4f });
        target.items.Add(merged);
        source.OnStorageChange?.Invoke(null);
        target.OnStorageChange?.Invoke(null);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, food.MatchTags, GameTags.Solid, 4f, 4f);
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkContentIndexService.GetEdibleCalories(0, false, "Meal", null) == 380f &&
              !StorageNetworkContentIndexService.HasPlantableSeed(0, false, Material, extra),
            "food and seed details refresh from replacement native objects, not guessed transfer metadata");
        source.Minion = true;
        target.Production = true;
        StorageNetworkContentIndexService.InvalidateWorld(0);
        check(Sources(Material).Count == 0 && Amount(0) == 4f,
            "minion/production eligibility excludes sources without hiding inventory counts");
    }

    private static void VerifyUnknownChanges(Action<bool, string> check)
    {
        Reset();
        Storage source = Store(1, 0, 10f), target = Store(2, 0, 0f);
        Warm();
        var tags = source.items[0].MatchTags;
        StorageNetworkContentIndexService.BeginTransferTransaction();
        Change(source, 8f);
        Change(target, 2f);
        StorageNetworkContentIndexService.TouchTransferStorage(source);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, 2f, 2f);
        check(StorageNetworkContentIndexService.GetStorageAmount(source, Material) == 8f,
            "forced native endpoint read stays authoritative during transaction");
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 1 && Amount(0) == 10f,
            "known delta does not cancel explicit unknown-change fallback");

        StorageNetworkContentIndexService.BeginTransferTransaction();
        Change(source, 6f);
        Change(target, 4f);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, 2f, 2f);
        source.CountsCapacity = false;
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkContentIndexService.GetMetrics(0, false, false).TotalStoredKg == 4f,
            "changed capacity eligibility falls back to native contributions at commit");

        var water = new Tag("Water");
        source.items.Add(Item(water, 7f, 290f, SimHashes.Water));
        source.OnStorageChange?.Invoke(null);
        var liquids = new Dictionary<SimHashes, float>();
        StorageNetworkContentIndexService.FillWorldLiquidMasses(0, liquids);
        check(liquids[SimHashes.Water] == 7f, "unknown native item addition rebuilds element mass and content");
        StorageNetworkContentIndexService.BeginTransferTransaction();
        var waterTags = source.items[source.items.Count - 1].MatchTags;
        source.items.RemoveAt(source.items.Count - 1);
        target.items.Add(Item(water, 7f, 290f, SimHashes.Water));
        source.OnStorageChange?.Invoke(null);
        target.OnStorageChange?.Invoke(null);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, waterTags, GameTags.Liquid, 7f, 7f);
        StorageNetworkContentIndexService.EndTransferTransaction();
        StorageNetworkPerformanceCounters.ConsumeSnapshot();
        StorageNetworkContentIndexService.TryFillStorageLiquidMasses(target, liquids);
        var elementRead = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(liquids[SimHashes.Water] == 7f && elementRead.DetailRefreshes == 0 && elementRead.RefreshedStorages == 0,
            "known transfer reconciles native liquid masses without rebuilding food/display details");
        var totals = new Dictionary<Tag, StorageNetworkIndexedItemTotal>();
        StorageNetworkContentIndexService.TryFillStorageItemTotals(new[] { target }, totals, false, out _);
        check(totals[water].MassKg == 7f && StorageNetworkPerformanceCounters.ConsumeSnapshot().DetailRefreshes == 1,
            "element-only refresh keeps object/display details dirty until actually queried");
    }
}
