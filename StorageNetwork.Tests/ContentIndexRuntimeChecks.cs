using System;
using System.Collections.Generic;
using System.Linq;
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
        VerifyScopedTransferBaselines(check);
        VerifyPendingSourceCandidates(check);
        VerifyPendingSourceDifferential(check);
        VerifyPortTransferBaseline(check);
        VerifyTransfersAndDetails(check);
        VerifyUnknownChanges(check);
        VerifyFoodAndTagDetails(check);
        VerifyRepeatedTransfers(check);
        VerifyItemCandidateIndex(check);
        VerifyRequestWakeVersions(check);
        Reset();
    }

    private static void VerifyRequestWakeVersions(Action<bool, string> check)
    {
        Reset();
        Storage local = Store(1, 0, 10f), other = Store(2, 0, 10f), target = Store(3, 0, 0f), remote = Store(4, 1, 10f);
        other.items.Clear(); other.items.Add(Item(Unrelated, 10f));
        Warm();
        var wanted = new[] { Material };
        long initial = StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null);
        var tags = other.items[0].MatchTags;
        StorageNetworkContentIndexService.BeginTransferTransaction(0);
        Change(other, 9f); other.items[0].MatchTags = tags;
        target.items.Add(Item(Unrelated, 1f)); target.OnStorageChange?.Invoke(null);
        StorageNetworkContentIndexService.RecordTransferMutation(other, target, tags, GameTags.Solid, 1f, 1f);
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null) == initial,
            "known unrelated-material transfer does not wake a material-specific request");
        Change(remote, 11f);
        check(StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null) == initial,
            "unknown mutation in an unreachable world does not wake a local request");
        Change(local, 12f);
        check(StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null) != initial,
            "pending unknown local mutations wake without flushing native inventories");
        var counts = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(counts.RefreshedStorages == 0,
            "checking sleeping-request generations does not refresh dirty storages");
        long beforeTopology = StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null);
        StorageSceneRegistry.ConnectivityVersion++;
        check(StorageNetworkContentIndexService.GetRequestWakeVersion(0, wanted, null) != beforeTopology,
            "core and relay connectivity changes wake requests");
    }

    private static void VerifyItemCandidateIndex(Action<bool, string> check)
    {
        Reset();
        Storage storage = Store(1, 0, 1f);
        storage.items.Clear();
        for (int i = 0; i < 1003; i++)
            storage.items.Add(Item(i % 333 == 0 ? Material : Unrelated, 1f));
        Warm();
        var tags = new HashSet<Tag> { Material };
        var result = new List<GameObject>();
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(result.SequenceEqual(storage.items.Where(i => i.MatchTags.Contains(Material))),
            "item buckets retain exact native order across a 1003-item storage");
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().ScannedItems == 1003,
            "cold item bucket scans the storage once");
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().ScannedItems == 0 && result.Count == 4,
            "warm sparse item bucket returns four candidates without scanning 999 unrelated items");
        tags.Add(Unrelated);
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(result.SequenceEqual(storage.items), "multi-tag union preserves native order without duplicates");

        StorageNetworkContentIndexService.BeginTransferTransaction(0);
        GameObject replacement = Item(Material, 2f);
        storage.items[1] = replacement;
        storage.OnStorageChange?.Invoke(null);
        tags.Remove(Unrelated);
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(result.Contains(replacement) && result.Count == 5,
            "same-count object replacement invalidates candidate buckets inside a transfer transaction");
        storage.items.Reverse();
        StorageNetworkContentIndexService.InvalidateAll();
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(result.SequenceEqual(storage.items.Where(i => i.MatchTags.Contains(Material))),
            "unknown global mutation refreshes candidate order before transaction commit");
        StorageNetworkContentIndexService.EndTransferTransaction();

        Tag dynamicTag = new Tag("Dynamic");
        replacement.GetComponent<KPrefabID>().Tags.Add(dynamicTag);
        storage.OnStorageChange?.Invoke(null);
        tags.Clear(); tags.Add(dynamicTag);
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, tags, result);
        check(result.Count == 1 && result[0] == replacement,
            "candidate matching includes native prefab tags outside the four material tags");
        StorageNetworkContentIndexService.FillStorageItemCandidates(storage, null, result);
        check(result.SequenceEqual(storage.items), "unfiltered candidates retain every native list slot");
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

    private static void VerifyScopedTransferBaselines(Action<bool, string> check)
    {
        Reset();
        Storage source = Store(1, 0, 10f), target = Store(2, 0, 0f);
        Storage localOther = Store(3, 0, 40f), remote = Store(4, 1, 20f);
        Warm();
        Change(source, 11f);
        Change(localOther, 41f);
        Change(remote, 25f);
        StorageNetworkContentIndexService.BeginTransferTransaction(0, false);
        var scoped = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(scoped.RefreshedStorages == 2 && scoped.ScannedItems == 2 && Amount(0) == 52f,
            "world-scoped transfer establishes strict local baselines without scanning dirty remote storage");
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(Amount(1) == 25f && StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 1,
            "remote dirty baseline survives scoped transfer for a later strict query");

        Change(source, 12f);
        Change(target, 1f);
        Change(localOther, 42f);
        Change(remote, 26f);
        StorageNetworkContentIndexService.BeginTransferTransaction(source, target);
        var direct = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(direct.RefreshedStorages == 2 && direct.ScannedItems == 2,
            "direct transfer refreshes exactly its endpoints even with dirty siblings in the same world");
        var tags = source.items[0].MatchTags;
        Change(source, 7f);
        Change(target, 6f);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, 5f, 5f);
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkContentIndexService.GetStorageAmount(source, Material) == 7f &&
              StorageNetworkContentIndexService.GetStorageAmount(target, Material) == 6f &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 0,
            "direct transfer retains exact pending deltas without rescanning endpoint contents");
        check(StorageNetworkContentIndexService.GetStorageAmount(localOther, Material) == 42f &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 1,
            "direct transfer leaves unrelated same-world dirty records queued");

        // Explicit cross-world targets can be outside the entry point's scope.
        StorageNetworkContentIndexService.BeginTransferTransaction(0, false);
        Change(source, 5f);
        Change(remote, 28f);
        StorageNetworkContentIndexService.RecordTransferMutation(source, remote, tags, GameTags.Solid, 2f, 2f);
        check(StorageNetworkContentIndexService.GetStorageAmount(remote, Material) == 28f && Amount(1) == 28f,
            "dirty out-of-scope endpoint uses native baseline without double-applying pending deltas");
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(Amount(0) == 53f && Amount(1) == 28f,
            "cross-world explicit transfer commits exact world totals after scoped entry");

        Change(remote, 30f);
        StorageNetworkContentIndexService.BeginTransferTransaction(0, false);
        StorageNetworkContentIndexService.BeginTransferTransaction(1, false);
        Change(source, 4f);
        Change(remote, 31f);
        StorageNetworkContentIndexService.RecordTransferMutation(source, remote, tags, GameTags.Solid, 1f, 1f);
        check(StorageNetworkContentIndexService.GetStorageAmount(remote, Material) == 31f,
            "nested transfer into an unprepared world keeps native dirty endpoint authoritative");
        StorageNetworkContentIndexService.EndTransferTransaction();
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(Amount(0) == 52f && Amount(1) == 31f,
            "nested scoped transactions preserve conservation and outermost commit");

        Reset();
        source = Store(1, 0, 10f);
        remote = Store(2, 1, 20f);
        Storage rocket = Store(3, 2, 30f);
        Warm();
        StorageNetworkWorldUtility.Parents[2] = 0;
        Change(source, 11f);
        Change(remote, 21f);
        Change(rocket, 31f);
        StorageNetworkContentIndexService.BeginTransferTransaction(0);
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 2 && Amount(0) == 42f,
            "transfer scope includes landed rockets while leaving independent colonies dirty");
        StorageNetworkContentIndexService.EndTransferTransaction();
        StorageSceneRegistry.Relay = true;
        StorageNetworkContentIndexService.BeginTransferTransaction(0);
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 1 && Amount(0, true) == 63f,
            "online relay transfer establishes strict baselines across the entire reachable network");
        StorageNetworkContentIndexService.EndTransferTransaction();
        StorageNetworkPerformanceMode.LegacyFullScanEnabled = true;
        StorageNetworkContentIndexService.BeginTransferTransaction(source, source);
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 3,
            "legacy diagnostic mode retains full refresh for endpoint-scoped transfers");
        StorageNetworkContentIndexService.EndTransferTransaction();
    }

    private static void VerifyPendingSourceCandidates(Action<bool, string> check)
    {
        Reset();
        Storage source = Store(1, 0, 20f), target = Store(2, 0, 0f), other = Store(3, 0, 10f);
        Storage newlyMatching = null;
        for (int i = 0; i < 1000; i++)
        {
            Storage unrelated = Store(i + 4, 0, 0f);
            unrelated.items.Add(Item(Unrelated, 1f));
            if (i == 0) newlyMatching = unrelated;
        }
        Storage remote = Store(2001, 1, 40f), rocket = Store(2002, 2, 8f);
        Warm();
        StorageNetworkContentIndexService.BeginTransferTransaction(0, false);
        var tags = source.items[0].MatchTags;
        Change(source, 15f);
        Change(target, 5f);
        StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, 5f, 5f);
        var result = Sources(Material);
        var operations = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        check(result.Count == 3 && result[0] == source && result[1] == other && result[2] == target &&
              operations.PendingSourceChecks == 3 && operations.RefreshedStorages == 0,
            "pending-source lookup checks three material candidates instead of 1,003 local servers");
        Change(newlyMatching, 16f);
        result = Sources(Material);
        check(result.Count == 4 && result[0] == newlyMatching && result[1] == source &&
              result[2] == other && result[3] == target &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().PendingSourceChecks == 4,
            "unknown native change adds a newly matching endpoint absent from the committed tag bucket");
        var filtered = new List<Storage>();
        StorageNetworkContentIndexService.FillSourceStorages(0, false,
            new[] { Tag.Invalid, Material, Material }, new HashSet<Storage> { source, target }, null, filtered, false);
        check(filtered.Count == 2 && filtered[0] == newlyMatching && filtered[1] == other,
            "candidate union preserves exclusions and deduplicates repeated source tags");

        StorageNetworkWorldUtility.Parents[2] = 0;
        result = Sources(Material);
        check(result.Count == 5 && result[3] == rocket && result[4] == target,
            "pending-source candidate union includes landed rockets in descending amount order");
        StorageSceneRegistry.Relay = true;
        filtered.Clear();
        StorageNetworkContentIndexService.FillSourceStorages(0, true, new[] { Material }, null, null, filtered, false);
        check(filtered.Count == 6 && filtered[0] == remote && filtered[1] == newlyMatching,
            "pending-source candidate union retains relay-wide source order");
        StorageSceneRegistry.Relay = false;
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(Amount(0) == 54f && Amount(1) == 40f,
            "candidate queries preserve exact totals through unknown-change commit and landed aggregation");

        // Multiple tags use the maximum matched amount per source, rather than
        // counting the same object once for every matching category.
        source.items.Add(Item(Unrelated, 30f));
        source.OnStorageChange?.Invoke(null);
        StorageNetworkContentIndexService.BeginTransferTransaction(0, false);
        Change(target, 7f);
        StorageNetworkContentIndexService.TouchTransferStorage(target);
        StorageNetworkPerformanceCounters.ConsumeSnapshot();
        StorageNetworkContentIndexService.FillSourceStorages(0, false,
            new[] { Material, Unrelated }, null, null, filtered, false);
        check(filtered.Count == 1004 && filtered[0] == source && filtered[1] == newlyMatching &&
              filtered[2] == other && filtered[3] == rocket && filtered[4] == target,
            "multi-tag pending-source lookup preserves max-amount ranking and all unrelated-tag matches");
        check(StorageNetworkPerformanceCounters.ConsumeSnapshot().PendingSourceChecks == 1004,
            "dense material categories visit each reachable server once without a second candidate pass");
        StorageNetworkContentIndexService.EndTransferTransaction();
    }

    private static void VerifyPortTransferBaseline(Action<bool, string> check)
    {
        Reset();
        Storage port = Store(1, 0, 10f), server = Store(2, 0, 0f);
        port.Server = false;
        Warm();
        StorageNetworkContentIndexService.BeginTransferTransaction(0);
        var tags = port.items[0].MatchTags;
        Change(port, 5f);
        Change(server, 5f);
        StorageNetworkContentIndexService.RecordTransferMutation(port, server, tags, GameTags.Solid, 5f, 5f);
        check(Amount(0) == 5f && StorageNetworkPerformanceCounters.ConsumeSnapshot().RefreshedStorages == 0,
            "non-indexed port endpoint does not force native rebuilds of an otherwise known server transfer");
        StorageNetworkContentIndexService.EndTransferTransaction();
        check(StorageNetworkContentIndexService.GetStorageAmount(server, Material) == 5f &&
              StorageNetworkPerformanceCounters.ConsumeSnapshot().ScannedItems == 0,
            "port-to-server commit preserves the incremental fast path without indexing the port");
    }

    private static void VerifyPendingSourceDifferential(Action<bool, string> check)
    {
        Reset();
        var random = new Random(20261015);
        var storages = new List<Storage>();
        for (int i = 0; i < 48; i++) storages.Add(Store(i + 1, i % 3, random.Next(1, 40)));
        Warm();
        for (int iteration = 0; iteration < 120; iteration++)
        {
            StorageNetworkWorldUtility.Parents.Clear();
            if (iteration % 2 == 0) StorageNetworkWorldUtility.Parents[2] = 0;
            StorageSceneRegistry.Relay = iteration % 3 == 0;
            bool related = iteration % 4 != 0;
            int worldId = iteration % 3;
            Storage source = storages[random.Next(storages.Count)];
            Storage target = storages[(storages.IndexOf(source) + 1) % storages.Count];
            // Leave an independent mutation queued before the scoped baseline.
            Storage changed = storages[random.Next(storages.Count)];
            Change(changed, random.Next(1, 40));
            StorageNetworkContentIndexService.BeginTransferTransaction(worldId, related);
            float sourceAmount = source.MassStored(), targetAmount = target.MassStored();
            float moved = Math.Min(0.25f, sourceAmount);
            Change(source, sourceAmount - moved);
            Change(target, targetAmount + moved);
            var tags = new StorageItemUtility.StorageMatchTags
            { PrefabIdTag = Material, PrefabTag = Material, ElementTag = Material, TransferTag = Material };
            StorageNetworkContentIndexService.RecordTransferMutation(source, target, tags, GameTags.Solid, moved, moved);
            Storage unknown = storages[random.Next(storages.Count)];
            Change(unknown, random.Next(1, 40));
            StorageNetworkContentIndexService.TouchTransferStorage(unknown);
            var excluded = new HashSet<Storage> { storages[random.Next(storages.Count)] };
            var result = new List<Storage>();
            StorageNetworkContentIndexService.FillSourceStorages(worldId, related,
                new[] { Tag.Invalid, Material, Material }, excluded, null, result, false);
            bool allWorlds = related && StorageSceneRegistry.Relay;
            var expected = storages.Where(storage => !excluded.Contains(storage) &&
                    (allWorlds || StorageNetworkWorldUtility.AreWorldsSameOrLanded(storage.gameObject.WorldId, worldId)) &&
                    storage.MassStored() > PICKUPABLETUNING.MINIMUM_PICKABLE_AMOUNT)
                .OrderByDescending(storage => storage.MassStored()).ThenBy(storage => storage.InstanceId).ToList();
            check(result.SequenceEqual(expected),
                "pending-source lookup matches native full-scan reference across deterministic mutation/world scenario " + iteration);
            StorageNetworkContentIndexService.EndTransferTransaction();
        }
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
