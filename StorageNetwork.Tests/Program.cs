using System;
using System.Collections.Generic;
using System.Linq;
using StorageNetwork.Core;
using StorageNetwork.Components;
using StorageNetwork.Services;
using StorageNetwork.UI;
using UnityEngine;

internal static class Program
{
    private static int failures;
    private static int checks;

    private static int Main(string[] args)
    {
        AlgorithmOptimizationChecks.Verify(Check);
        ContentIndexRuntimeChecks.Verify(Check);
        FarmingRuntimeChecks.Verify(Check);
        VerifyDebounceCoalescesRequests();
        VerifyDebounceCancellation();
        VerifyRingBufferKeepsNewestValues();
        VerifyRingBufferClear();
        VerifyVirtualizedRange();
        VerifyPortCounterDeduplicatesInstances();
        VerifyDisabledCountersAllocateNothing();
        VerifyShadowValidationIsVersionAndAreaIsolated();
        VerifyShadowValidationIsWorldIsolated();
        VerifyDirtyMergeWindow();
        VerifyStableSourceOrder();
        VerifyStableTargetOrder();
        VerifyWorldAndRelayMerge();
        VerifyTransferTransactionDedupeAndPendingDelta();
        VerifyTransferTransactionNativeFallback();
        VerifySelectiveOutputTargetInvalidation();
        VerifyKeepRuleMutationVersion();
        VerifyMainListStructuralSignatureExcludesItemMembership();
        VerifyCategoryStructuralSignatureIgnoresLiveMass();
        VerifyTrendSamplingAndMemoryBudget();
        VerifyEnrollmentStructuralSignatureIgnoresLiveStatus();
        if (args.Length == 0)
            SourceContractChecks.Verify(SourceContractChecks.FindRepositoryRoot(), Check);
        else if (args.Length != 1 || args[0] != "--runtime-only")
            throw new ArgumentException("Expected no arguments or --runtime-only.");

        if (failures == 0)
        {
            Console.WriteLine(args.Length == 0 ? "StorageNetwork deterministic performance checks passed." :
                "StorageNetwork runtime checks passed (source contracts excluded; run Verify.ps1 for the full gate).");
            Console.WriteLine($"Assertions passed: {checks}");
            return 0;
        }

        Console.Error.WriteLine($"StorageNetwork deterministic performance checks failed: {failures}");
        return 1;
    }

    private static void VerifyDebounceCoalescesRequests()
    {
        StorageNetworkDebounceGate gate = new StorageNetworkDebounceGate(0.15f);
        gate.Request();
        Check(!gate.Advance(0.10f), "debounce waits for its threshold");
        gate.Request();
        Check(!gate.Advance(0.149f), "new input restarts the debounce window");
        Check(gate.Advance(0.001f), "debounce fires once at the threshold");
        Check(!gate.Advance(1f), "debounce does not repeat without another request");
    }

    private static void VerifyDebounceCancellation()
    {
        StorageNetworkDebounceGate gate = new StorageNetworkDebounceGate(0.15f);
        gate.Request();
        gate.Cancel();
        Check(!gate.IsPending && !gate.Advance(1f), "cancelled debounce remains idle");
    }

    private static void VerifyRingBufferKeepsNewestValues()
    {
        StorageNetworkFixedRingBuffer<int> ring = new StorageNetworkFixedRingBuffer<int>(3);
        ring.Add(1);
        ring.Add(2);
        ring.Add(3);
        ring.Add(4);
        Check(ring.Count == 3 && ring.Capacity == 3, "ring buffer remains at fixed capacity");
        Check(ring.TryGetFirst(out int first) && first == 2, "ring buffer evicts the oldest value");
        Check(ring.TryGetLast(out int last) && last == 4, "ring buffer retains the newest value");
    }

    private static void VerifyRingBufferClear()
    {
        StorageNetworkFixedRingBuffer<int> ring = new StorageNetworkFixedRingBuffer<int>(64);
        for (int i = 0; i < 100; i++)
        {
            ring.Add(i);
        }

        Check(ring.Count == 64, "trend-sized ring is capped at 64 samples");
        ring.Clear();
        Check(ring.Count == 0 && !ring.TryGetFirst(out _) && !ring.TryGetLast(out _),
            "ring buffer clear removes its logical contents");
    }

    private static void VerifyVirtualizedRange()
    {
        StorageNetworkVirtualizedRange shortList =
            StorageNetworkVirtualizedRange.Calculate(
                20, 32, 3, 24f, 3f, 8f, 0f, 100f);
        Check(shortList.First == 0 && shortList.LastExclusive == 20,
            "short lists bypass viewport virtualization");

        StorageNetworkVirtualizedRange top =
            StorageNetworkVirtualizedRange.Calculate(
                100, 32, 3, 24f, 3f, 8f, 0f, 100f);
        Check(top.First == 0 && top.LastExclusive == 7,
            "virtualized range includes a bounded top viewport and overscan");

        StorageNetworkVirtualizedRange middle =
            StorageNetworkVirtualizedRange.Calculate(
                100, 32, 3, 24f, 3f, 8f, 270f, 100f);
        Check(middle.First == 6 && middle.LastExclusive == 17,
            "virtualized range advances without materializing preceding rows");

        StorageNetworkVirtualizedRange end =
            StorageNetworkVirtualizedRange.Calculate(
                100, 32, 3, 24f, 3f, 8f, 2600f, 100f);
        Check(end.First == 93 && end.LastExclusive == 100,
            "virtualized range clamps to the final item window");
    }

    private static void VerifyPortCounterDeduplicatesInstances()
    {
        StorageNetworkPerformanceCounters.ResetRuntimeState();
        StorageNetworkPerformanceCounters.RecordPortRequestAttempt(999);
        Check(StorageNetworkPerformanceCounters.ConsumeSnapshot().PortRequestAttempts == 0,
            "disabled performance counters use the zero-cost guard");

        StorageNetworkPerformanceCounters.SetEnabled(true);
        StorageNetworkPerformanceCounters.RecordPortRequestAttempt(101);
        StorageNetworkPerformanceCounters.RecordPortRequestAttempt(101);
        StorageNetworkPerformanceCounters.RecordPortRequestAttempt(202);

        StorageNetworkPerformanceSnapshot snapshot = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        Check(snapshot.PortRequestAttempts == 3, "port request counter counts attempts");
        Check(snapshot.ActivePortCount == 2, "active port counter deduplicates instances");

        StorageNetworkPerformanceSnapshot empty = StorageNetworkPerformanceCounters.ConsumeSnapshot();
        Check(empty.PortRequestAttempts == 0 && empty.ActivePortCount == 0,
            "port counters reset after snapshot");
        StorageNetworkPerformanceCounters.SetEnabled(false);
    }

    private static void VerifyShadowValidationIsVersionAndAreaIsolated()
    {
        ConfigureShadowValidation();
        StorageNetworkShadowValidationService.ReportMismatch(
            StorageNetworkShadowArea.InventoryAmount,
            7,
            5,
            101,
            "expected test mismatch");
        Check(StorageNetworkShadowValidationService.IsContentWorldQuarantined(7),
            "shadow mismatch quarantines its world");

        StorageNetworkShadowValidationService.ReportMatch(
            StorageNetworkShadowArea.SourceOrder,
            7,
            99);
        Check(StorageNetworkShadowValidationService.IsContentWorldQuarantined(7),
            "a different shadow area cannot clear quarantine");

        StorageNetworkShadowValidationService.ReportMatch(
            StorageNetworkShadowArea.InventoryAmount,
            7,
            5);
        Check(StorageNetworkShadowValidationService.IsContentWorldQuarantined(7),
            "the failed version cannot clear its own quarantine");

        StorageNetworkShadowValidationService.ReportMatch(
            StorageNetworkShadowArea.InventoryAmount,
            7,
            6);
        Check(!StorageNetworkShadowValidationService.IsContentWorldQuarantined(7),
            "a matching higher version recovers the quarantined area");
    }

    private static void VerifyDisabledCountersAllocateNothing()
    {
        StorageNetworkPerformanceCounters.ResetRuntimeState();
        StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
        StorageNetworkPerformanceCounters.RecordPortRequestAttempt(123);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 100_000; index++)
        {
            StorageNetworkPerformanceCounters.RecordInventoryIndexRebuild();
            StorageNetworkPerformanceCounters.RecordNetworkSourceScan();
            StorageNetworkPerformanceCounters.RecordPortRequestAttempt(123);
        }
        long after = GC.GetAllocatedBytesForCurrentThread();

        Check(after == before,
            "disabled diagnostic counter hot paths allocate zero managed bytes");
        Check(StorageNetworkPerformanceCounters.ConsumeSnapshot().PortRequestAttempts == 0,
            "disabled diagnostic counter hot paths perform no collection writes");

        StorageNetworkPerformanceMode.ShadowValidationEnabled = false;
        StorageNetworkShadowValidationService.ResetRuntimeState();
        StorageNetworkShadowValidationService.ShouldValidate(
            StorageNetworkShadowArea.InventoryAmount,
            1,
            1);
        before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 100_000; index++)
        {
            StorageNetworkShadowValidationService.ShouldValidate(
                StorageNetworkShadowArea.InventoryAmount,
                1,
                index);
        }
        after = GC.GetAllocatedBytesForCurrentThread();
        Check(after == before,
            "disabled shadow validation hot path allocates zero managed bytes");
    }

    private static void VerifyShadowValidationIsWorldIsolated()
    {
        ConfigureShadowValidation();
        StorageNetworkShadowValidationService.ReportMismatch(
            StorageNetworkShadowArea.TargetCapacity,
            10,
            2,
            201,
            "world 10 mismatch");
        StorageNetworkShadowValidationService.ReportMismatch(
            StorageNetworkShadowArea.TargetCapacity,
            11,
            2,
            202,
            "world 11 mismatch");
        StorageNetworkShadowValidationService.ReportMatch(
            StorageNetworkShadowArea.TargetCapacity,
            10,
            3);

        Check(!StorageNetworkShadowValidationService.IsContentWorldQuarantined(10),
            "higher-version recovery is scoped to the matching world");
        Check(StorageNetworkShadowValidationService.IsContentWorldQuarantined(11),
            "recovery in one world leaves another world quarantined");

        StorageNetworkPerformanceMode.ShadowValidationEnabled = false;
        Check(!StorageNetworkShadowValidationService.ShouldValidate(
                StorageNetworkShadowArea.InventoryMetrics,
                10,
                1),
            "disabled shadow validation pays only its guard");
    }

    private static void VerifyDirtyMergeWindow()
    {
        DirtyMergeWindowModel window = new DirtyMergeWindowModel();
        window.MarkDirty(1001, 40);
        window.MarkDirty(1001, 41);

        Check(window.GetFirstDirtyFrame(1001) == 40,
            "repeated storage events preserve the first dirty frame");
        Check(window.MutationVersion == 2,
            "repeated storage events still advance the mutation version");
    }

    private static void VerifyStableSourceOrder()
    {
        SourceCandidate[] candidates =
        {
            new SourceCandidate(30, 4f, 1),
            new SourceCandidate(20, 9f, 1),
            new SourceCandidate(10, 9f, 1),
            new SourceCandidate(40, 1f, 1)
        };

        int[] order = ContentQueryContractModel.MergeAndSort(candidates, 1, false)
            .Select(candidate => candidate.InstanceId)
            .ToArray();
        Check(order.SequenceEqual(new[] { 10, 20, 30, 40 }),
            "source order is amount descending then InstanceID ascending");
    }

    private static void VerifyStableTargetOrder()
    {
        TargetCandidate[] candidates =
        {
            new TargetCandidate(40, 8f, false, 100f),
            new TargetCandidate(30, 8f, true, 1f),
            new TargetCandidate(20, 8f, true, 5f),
            new TargetCandidate(10, 8f, true, 5f),
            new TargetCandidate(50, 9f, false, 0.1f)
        };

        Check(TargetSelectionContractModel.Select(candidates).InstanceId == 50,
            "target amount remains the primary ordering criterion");
        Check(TargetSelectionContractModel.Select(candidates.Take(4)).InstanceId == 10,
            "target ties use filter, capacity, then ascending InstanceID");
    }

    private static void VerifyWorldAndRelayMerge()
    {
        SourceCandidate[] candidates =
        {
            new SourceCandidate(1, 3f, 10),
            new SourceCandidate(2, 7f, 11),
            new SourceCandidate(3, 5f, 10)
        };

        Check(Math.Abs(ContentQueryContractModel.SumWorlds(candidates, 10, false) - 8f) < 0.0001f,
            "per-world query excludes unreachable worlds");
        Check(Math.Abs(ContentQueryContractModel.SumWorlds(candidates, 10, true) - 15f) < 0.0001f,
            "relay query merges each world exactly once");
        Check(ContentQueryContractModel.MergeAndSort(candidates, 10, false).Count == 2 &&
              ContentQueryContractModel.MergeAndSort(candidates, 10, true).Count == 3,
            "source scope changes only when the relay is online");
    }

    private static void VerifyTransferTransactionDedupeAndPendingDelta()
    {
        TransferTransactionModel transaction = new TransferTransactionModel();
        transaction.Record(100, 200, "Iron", 4f);
        transaction.Record(100, 200, "Iron", 3f);

        Check(transaction.Touched.SequenceEqual(new[] { 100, 200 }),
            "one transaction deduplicates source and target commit endpoints");
        Check(Math.Abs(transaction.ApplyPending(100, "Iron", 10f) - 3f) < 0.0001f,
            "same-transaction source query sees accumulated pending removal");
        Check(Math.Abs(transaction.ApplyPending(200, "Iron", 1f) - 8f) < 0.0001f,
            "same-transaction target query sees accumulated pending addition");
    }

    private static void VerifyTransferTransactionNativeFallback()
    {
        TransferTransactionFallbackModel transaction = new TransferTransactionFallbackModel();
        transaction.OnNativeStorageChange(100);
        transaction.RecordExplicitTransfer(100);
        Check(!transaction.RequiresNativeRead(100),
            "an explicit transfer delta replaces its provisional native-event fallback");

        transaction.OnNativeStorageChange(200);
        transaction.ForceNativeReadsForTouchedStorages();
        transaction.RecordExplicitTransfer(200);
        Check(transaction.RequiresNativeRead(200),
            "a strict-query native fallback cannot be cancelled by a later delta");

        transaction.OnNativeStorageChange(300);
        Check(transaction.RequiresNativeRead(300),
            "an unexplained storage mutation remains on the authoritative native path");
    }

    private static void VerifySelectiveOutputTargetInvalidation()
    {
        OutputTargetCacheDecision winner = new OutputTargetCacheDecision(
            sourceStorageId: 1,
            targetStorageId: 10,
            tags: new[] { "Iron", "Metal", "Solid" },
            sourceIsExcluded: true);

        Check(!OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 10,
                changedTags: new[] { "Iron", "Metal", "Solid" },
                amountDelta: 1f,
                massDelta: 1f),
            "a cached winner that gains matching material remains valid");
        Check(OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 10,
                changedTags: new[] { "Copper", "Metal", "Solid" },
                amountDelta: 1f,
                massDelta: 1f),
            "partial tag overlap does not hide a cached winner's capacity loss");
        Check(OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 20,
                changedTags: new[] { "Iron" },
                amountDelta: 1f,
                massDelta: 1f),
            "a competitor gaining matching material invalidates the cached winner");
        Check(OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 20,
                changedTags: new[] { "Water" },
                amountDelta: -1f,
                massDelta: -1f),
            "a competitor gaining capacity invalidates the cached winner");
        Check(OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 10,
                changedTags: new[] { "Iron" },
                amountDelta: -1f,
                massDelta: -1f),
            "a cached winner losing matching material is reselected");
        Check(!OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 1,
                changedTags: new[] { "Iron" },
                amountDelta: -1f,
                massDelta: -1f),
            "the source encoded as excluded cannot invalidate its own decision");
        OutputTargetCacheDecision sourceStillCandidate = new OutputTargetCacheDecision(
            sourceStorageId: 1,
            targetStorageId: 10,
            tags: new[] { "Iron", "Metal", "Solid" },
            sourceIsExcluded: false);
        Check(OutputTargetInvalidationModel.ShouldInvalidate(
                sourceStillCandidate,
                changedStorageId: 1,
                changedTags: new[] { "Iron" },
                amountDelta: -1f,
                massDelta: -1f),
            "a source ID used only for reservations remains part of the candidate domain");
        Check(!OutputTargetInvalidationModel.ShouldInvalidate(
                winner,
                changedStorageId: 20,
                changedTags: new[] { "Water" },
                amountDelta: -1f,
                massDelta: 1f),
            "an unrelated competitor becoming weaker leaves the cached winner intact");
    }

    private static void VerifyKeepRuleMutationVersion()
    {
        KeepRuleVersionModel model = new KeepRuleVersionModel();
        model.Set("Iron", "Smelter", 100f);
        int firstVersion = model.Version;
        model.Set("Iron", "Smelter", 100f);
        Check(model.Version == firstVersion,
            "an identical Keep Rule set leaves the mutation version unchanged");
        model.Set("Iron", "Smelter", 200f);
        Check(model.Version == firstVersion + 1,
            "a changed Keep Rule target advances the mutation version once");
        Check(!model.Clear("Copper") && model.Version == firstVersion + 1,
            "clearing a missing Keep Rule is a version no-op");
        Check(model.Clear("Iron") && model.Version == firstVersion + 2,
            "clearing an existing Keep Rule advances the mutation version once");
    }

    private static void VerifyCategoryStructuralSignatureIgnoresLiveMass()
    {
        List<StorageNetworkCategorySummaryItemTotal> first = new List<StorageNetworkCategorySummaryItemTotal>
        {
            new StorageNetworkCategorySummaryItemTotal("Copper", "Copper", 5f, new GameObject(1)),
            new StorageNetworkCategorySummaryItemTotal("Iron", "Iron", 8f, new GameObject(2))
        };
        List<StorageNetworkCategorySummaryItemTotal> changedValues = new List<StorageNetworkCategorySummaryItemTotal>
        {
            new StorageNetworkCategorySummaryItemTotal("Copper", "Copper", 500f, new GameObject(1)),
            new StorageNetworkCategorySummaryItemTotal("Iron", "Iron", 0.5f, new GameObject(2))
        };
        string firstStructure = StorageNetworkCategorySummarySignature.BuildStructure("metal", first);
        string changedStructure = StorageNetworkCategorySummarySignature.BuildStructure("metal", changedValues);
        Check(firstStructure == changedStructure,
            "category mass changes leave the actual structural signature stable");

        changedValues.Reverse();
        Check(firstStructure == StorageNetworkCategorySummarySignature.BuildStructure("metal", changedValues),
            "category structural signature is independent of mass-driven input order");

        changedValues.Add(new StorageNetworkCategorySummaryItemTotal(
            "Gold",
            "Gold",
            1f,
            new GameObject(3)));
        Check(firstStructure != StorageNetworkCategorySummarySignature.BuildStructure("metal", changedValues),
            "adding a category key changes the actual structural signature");
    }

    private static void VerifyMainListStructuralSignatureExcludesItemMembership()
    {
        StorageInfo first = new StorageInfo
        {
            TypeKey = "solid",
            GameObject = new GameObject(20),
            StoredItems = new[]
            {
                new GameObject(101, "Iron"),
                new GameObject(102, "Iron")
            }
        };
        StorageInfo second = new StorageInfo
        {
            TypeKey = "solid",
            GameObject = new GameObject(10),
            StoredItems = new[] { new GameObject(103, "Copper") }
        };
        Func<StorageInfo, string> typeKey = info => info.TypeKey;
        Func<GameObject, string> itemKey = item => item.Key;

        string signature = StorageNetworkPanelListSignature.BuildStorageListSignature(
            new[] { first, second },
            "",
            typeKey,
            itemKey);
        string reordered = StorageNetworkPanelListSignature.BuildStorageListSignature(
            new[] { second, first },
            "",
            typeKey,
            itemKey);
        Check(signature == reordered,
            "main-list structural signature is stable across source enumeration order");

        first.StoredItems = new[]
        {
            new GameObject(101, "Iron"),
            new GameObject(102, "Iron"),
            new GameObject(104, "Iron")
        };
        string quantityOnly = StorageNetworkPanelListSignature.BuildStorageListSignature(
            new[] { first, second },
            "",
            typeKey,
            itemKey);
        Check(signature == quantityOnly,
            "duplicate item quantity changes do not alter the actual main-list structure");

        first.StoredItems = new[]
        {
            new GameObject(101, "Iron"),
            new GameObject(105, "Gold")
        };
        string addedKey = StorageNetworkPanelListSignature.BuildStorageListSignature(
            new[] { first, second },
            "",
            typeKey,
            itemKey);
        Check(signature == addedKey,
            "item-key membership is reconciled below the main-list structural signature");

        StorageInfo third = new StorageInfo
        {
            TypeKey = "solid",
            GameObject = new GameObject(30),
            StoredItems = new[] { new GameObject(106, "Gold") }
        };
        string addedStorage = StorageNetworkPanelListSignature.BuildStorageListSignature(
            new[] { first, second, third },
            "",
            typeKey,
            itemKey);
        Check(signature != addedStorage,
            "adding a storage still changes the main-list structural signature");
    }

    private static void VerifyTrendSamplingAndMemoryBudget()
    {
        StorageNetworkCategorySummaryTrendSampler sampler =
            new StorageNetworkCategorySummaryTrendSampler();
        sampler.Record(
            "metal",
            new[] { new StorageNetworkCategorySummaryItemTotal("Iron", "Iron", 1f, null) },
            0f,
            0f);
        sampler.Record(
            "metal",
            new[] { new StorageNetworkCategorySummaryItemTotal("Iron", "Iron", 10f, null) },
            1f,
            9f);
        Check(!sampler.GetTrendPerCycle("metal", "Iron").HasValue,
            "trend sampler coalesces updates inside its ten-second interval");

        sampler.Record(
            "metal",
            new[] { new StorageNetworkCategorySummaryItemTotal("Iron", "Iron", 11f, null) },
            2f,
            10f);
        float? trend = sampler.GetTrendPerCycle("metal", "Iron");
        Check(trend.HasValue && Math.Abs(trend.Value - 5f) < 0.0001f,
            "trend sampler records the next point at ten seconds");

        sampler.Record(
            "metal",
            Array.Empty<StorageNetworkCategorySummaryItemTotal>(),
            3f,
            11f);
        Check(sampler.SeriesCount == 0 && !sampler.GetTrendPerCycle("metal", "Iron").HasValue,
            "trend sampler evicts keys that disappear from the category");

        List<StorageNetworkCategorySummaryItemTotal> many =
            new List<StorageNetworkCategorySummaryItemTotal>();
        for (int index = 0; index < 600; index++)
        {
            string key = "item-" + index;
            many.Add(new StorageNetworkCategorySummaryItemTotal(key, key, index, null));
        }
        sampler.Record("stress", many, 4f, 20f);
        Check(sampler.SeriesCount <= 512 && sampler.ApproximateBytes <= 1024 * 1024,
            "trend sampler enforces its one-megabyte memory budget");
    }

    private static void VerifyEnrollmentStructuralSignatureIgnoresLiveStatus()
    {
        StorageNetworkEnrollment enrollment = new StorageNetworkEnrollment(300, "Reservoir");
        string disconnected = StorageNetworkEnrollableWindowSignature.Build(
            new[] { enrollment },
            2,
            "");
        enrollment.IncludedInSceneNetwork = true;
        enrollment.Studyable.Studied = true;
        string connectedAndStudied = StorageNetworkEnrollableWindowSignature.Build(
            new[] { enrollment },
            2,
            "");
        Check(disconnected == connectedAndStudied,
            "enrollment connection/research status leaves the actual structural signature stable");

        string changedSearch = StorageNetworkEnrollableWindowSignature.Build(
            new[] { enrollment },
            2,
            "reservoir");
        Check(disconnected != changedSearch,
            "enrollment search text remains part of the actual structural signature");
    }

    private static void ConfigureShadowValidation()
    {
        StorageNetworkPerformanceMode.ShadowValidationEnabled = true;
        StorageNetworkPerformanceMode.ShadowValidationFullEnabled = false;
        StorageNetworkShadowValidationService.ResetRuntimeState();
        Time.unscaledTime = 100f;
        Debug.Reset();
    }

    private static void Check(bool condition, string name)
    {
        checks++;
        if (!condition)
        {
            failures++;
            Console.Error.WriteLine($"FAIL: {name}");
        }
    }
}
