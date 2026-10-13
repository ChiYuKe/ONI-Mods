using System;
using System.Collections.Generic;
using System.Linq;

internal sealed class DirtyMergeWindowModel
{
    private readonly HashSet<int> dirty = new HashSet<int>();
    private readonly Dictionary<int, int> firstDirtyFrame = new Dictionary<int, int>();

    public int MutationVersion { get; private set; }

    public void MarkDirty(int storageId, int frame)
    {
        if (dirty.Add(storageId))
        {
            firstDirtyFrame[storageId] = frame;
        }

        MutationVersion++;
    }

    public int GetFirstDirtyFrame(int storageId) => firstDirtyFrame[storageId];
}

internal sealed class TransferTransactionModel
{
    private readonly HashSet<int> touchedSet = new HashSet<int>();
    private readonly List<int> touched = new List<int>();
    private readonly Dictionary<(int StorageId, string Tag), float> pendingAmounts =
        new Dictionary<(int StorageId, string Tag), float>();

    public IReadOnlyList<int> Touched => touched;

    public void Record(int sourceId, int targetId, string tag, float amount)
    {
        Touch(sourceId);
        Touch(targetId);
        AddDelta(sourceId, tag, -amount);
        AddDelta(targetId, tag, amount);
    }

    public float ApplyPending(int storageId, string tag, float indexedAmount)
    {
        return Math.Max(
            0f,
            indexedAmount + (pendingAmounts.TryGetValue((storageId, tag), out float delta)
                ? delta
                : 0f));
    }

    private void Touch(int storageId)
    {
        if (touchedSet.Add(storageId))
        {
            touched.Add(storageId);
        }
    }

    private void AddDelta(int storageId, string tag, float delta)
    {
        (int StorageId, string Tag) key = (storageId, tag);
        pendingAmounts[key] = pendingAmounts.TryGetValue(key, out float current)
            ? current + delta
            : delta;
    }
}

internal sealed class TransferTransactionFallbackModel
{
    private readonly HashSet<int> touched = new HashSet<int>();
    private readonly HashSet<int> nativeReadRequired = new HashSet<int>();
    private readonly HashSet<int> forcedNativeRead = new HashSet<int>();

    public void OnNativeStorageChange(int storageId)
    {
        touched.Add(storageId);
        nativeReadRequired.Add(storageId);
    }

    public void RecordExplicitTransfer(int storageId)
    {
        touched.Add(storageId);
        if (!forcedNativeRead.Contains(storageId))
        {
            nativeReadRequired.Remove(storageId);
        }
    }

    public void ForceNativeReadsForTouchedStorages()
    {
        foreach (int storageId in touched)
        {
            nativeReadRequired.Add(storageId);
            forcedNativeRead.Add(storageId);
        }
    }

    public bool RequiresNativeRead(int storageId) => nativeReadRequired.Contains(storageId);
}

internal readonly struct OutputTargetCacheDecision
{
    public OutputTargetCacheDecision(
        int sourceStorageId,
        int targetStorageId,
        IEnumerable<string> tags,
        bool sourceIsExcluded)
    {
        SourceStorageId = sourceStorageId;
        TargetStorageId = targetStorageId;
        Tags = new HashSet<string>(tags, StringComparer.Ordinal);
        SourceIsExcluded = sourceIsExcluded;
    }

    public int SourceStorageId { get; }

    public int TargetStorageId { get; }

    public HashSet<string> Tags { get; }

    public bool SourceIsExcluded { get; }
}

internal static class OutputTargetInvalidationModel
{
    public static bool ShouldInvalidate(
        OutputTargetCacheDecision decision,
        int changedStorageId,
        IEnumerable<string> changedTags,
        float amountDelta,
        float massDelta)
    {
        if (decision.SourceIsExcluded && decision.SourceStorageId == changedStorageId)
        {
            return false;
        }

        bool sameTarget = decision.TargetStorageId == changedStorageId;
        bool matchingAmount = changedTags.Any(decision.Tags.Contains);
        bool exactMatch = decision.Tags.SetEquals(changedTags);
        bool gainsAmount = amountDelta > 0f;
        bool losesAmount = amountDelta < 0f;
        bool gainsCapacity = massDelta < 0f;
        bool winnerStrengthened = sameTarget && gainsAmount && exactMatch;
        bool competitorMayImprove =
            !sameTarget && ((gainsAmount && matchingAmount) || gainsCapacity);
        bool winnerMayWeaken =
            sameTarget && !winnerStrengthened &&
            (losesAmount || gainsCapacity || massDelta > 0f);
        return competitorMayImprove || winnerMayWeaken;
    }
}

internal readonly struct SourceCandidate
{
    public SourceCandidate(int instanceId, float amount, int worldId)
    {
        InstanceId = instanceId;
        Amount = amount;
        WorldId = worldId;
    }

    public int InstanceId { get; }

    public float Amount { get; }

    public int WorldId { get; }
}

internal readonly struct TargetCandidate
{
    public TargetCandidate(int instanceId, float available, bool filterAccepting, float remaining)
    {
        InstanceId = instanceId;
        Available = available;
        FilterAccepting = filterAccepting;
        Remaining = remaining;
    }

    public int InstanceId { get; }

    public float Available { get; }

    public bool FilterAccepting { get; }

    public float Remaining { get; }
}

internal static class TargetSelectionContractModel
{
    public static TargetCandidate Select(IEnumerable<TargetCandidate> candidates)
    {
        return candidates
            .OrderByDescending(candidate => candidate.Available)
            .ThenByDescending(candidate => candidate.FilterAccepting)
            .ThenByDescending(candidate => candidate.Remaining)
            .ThenBy(candidate => candidate.InstanceId)
            .First();
    }
}

internal sealed class KeepRuleVersionModel
{
    private string product;
    private string recipe;
    private float target;
    private bool hasRule;

    public int Version { get; private set; }

    public void Set(string nextProduct, string nextRecipe, float nextTarget)
    {
        if (hasRule &&
            product == nextProduct &&
            recipe == nextRecipe &&
            target == nextTarget)
        {
            return;
        }

        product = nextProduct;
        recipe = nextRecipe;
        target = nextTarget;
        hasRule = true;
        Version++;
    }

    public bool Clear(string targetProduct)
    {
        if (!hasRule || product != targetProduct)
        {
            return false;
        }

        hasRule = false;
        Version++;
        return true;
    }
}

internal static class ContentQueryContractModel
{
    public static IReadOnlyList<SourceCandidate> MergeAndSort(
        IEnumerable<SourceCandidate> candidates,
        int requestedWorld,
        bool relayOnline)
    {
        return candidates
            .Where(candidate => relayOnline || candidate.WorldId == requestedWorld)
            .OrderByDescending(candidate => candidate.Amount)
            .ThenBy(candidate => candidate.InstanceId)
            .ToList();
    }

    public static float SumWorlds(
        IEnumerable<SourceCandidate> candidates,
        int requestedWorld,
        bool relayOnline)
    {
        return candidates
            .Where(candidate => relayOnline || candidate.WorldId == requestedWorld)
            .Sum(candidate => candidate.Amount);
    }
}
