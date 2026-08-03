using System;
using System.Collections.Generic;
using StorageNetwork.Core;
using UnityEngine;

namespace StorageNetwork.Services
{
    internal enum StorageNetworkShadowArea
    {
        InventoryAmount,
        InventoryMetrics,
        SourceOrder,
        StorageAmount,
        TargetCapacity,
        TargetSelection,
        Reservation,
        ProductionPlan,
        PowerSnapshot
    }

    /// <summary>
    /// Developer rollout guard for the optimized runtime. Disabled builds pay a
    /// single branch and perform no collection writes or managed allocations.
    /// </summary>
    internal static class StorageNetworkShadowValidationService
    {
        private const float ForcedSampleSeconds = 10f;
        private const float LogThrottleSeconds = 10f;
        private static readonly Dictionary<ValidationKey, int> LastValidatedVersions =
            new Dictionary<ValidationKey, int>();
        private static readonly Dictionary<ValidationKey, float> NextForcedSamples =
            new Dictionary<ValidationKey, float>();
        private static readonly Dictionary<MismatchKey, float> NextMismatchLogs =
            new Dictionary<MismatchKey, float>();
        private static readonly Dictionary<ValidationKey, int> QuarantinedVersions =
            new Dictionary<ValidationKey, int>();
        private static uint sampleState = 0x9E3779B9u;

        public static bool ShouldValidate(
            StorageNetworkShadowArea area,
            int worldId,
            int version)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return false;
            }

            ValidationKey key = new ValidationKey(area, worldId);
            if (StorageNetworkPerformanceMode.ShadowValidationFullEnabled ||
                QuarantinedVersions.ContainsKey(key))
            {
                return true;
            }

            if (!LastValidatedVersions.TryGetValue(key, out int validatedVersion) ||
                validatedVersion != version)
            {
                LastValidatedVersions[key] = version;
                NextForcedSamples[key] = Time.unscaledTime + ForcedSampleSeconds;
                return true;
            }

            float now = Time.unscaledTime;
            if (!NextForcedSamples.TryGetValue(key, out float nextForced) || now >= nextForced)
            {
                NextForcedSamples[key] = now + ForcedSampleSeconds;
                return true;
            }

            // Allocation-free deterministic one-percent sampling.
            sampleState = unchecked(sampleState * 1664525u + 1013904223u);
            return sampleState % 100u == 0u;
        }

        public static bool IsContentWorldQuarantined(int worldId)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return false;
            }

            foreach (ValidationKey key in QuarantinedVersions.Keys)
            {
                if (key.WorldId == worldId)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ReportMatch(
            StorageNetworkShadowArea area,
            int worldId,
            int version)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return;
            }

            ValidationKey key = new ValidationKey(area, worldId);
            if (QuarantinedVersions.TryGetValue(key, out int failedVersion) &&
                version > failedVersion)
            {
                QuarantinedVersions.Remove(key);
            }
        }

        public static void ReportMismatch(
            StorageNetworkShadowArea area,
            int worldId,
            int version,
            int signature,
            string detail)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return;
            }

            ValidationKey validationKey = new ValidationKey(area, worldId);
            QuarantinedVersions[validationKey] = version;
            MismatchKey key = new MismatchKey(area, worldId, signature);
            float now = Time.unscaledTime;
            if (NextMismatchLogs.TryGetValue(key, out float nextLog) && now < nextLog)
            {
                return;
            }

            NextMismatchLogs[key] = now + LogThrottleSeconds;
            Debug.LogError(
                $"[StorageNetwork][Shadow] {area} mismatch in world {worldId}; " +
                $"using native result and rebuilding the derived index. {detail}");
        }

        public static bool ShouldUseFallback(
            StorageNetworkShadowArea area,
            int worldId,
            int version)
        {
            if (!StorageNetworkPerformanceMode.ShadowValidationEnabled)
            {
                return false;
            }

            return QuarantinedVersions.TryGetValue(
                       new ValidationKey(area, worldId),
                       out int failedVersion) &&
                   version <= failedVersion;
        }

        public static bool ApproximatelyEqual(float left, float right)
        {
            float difference = Mathf.Abs(left - right);
            return difference <= 0.001f ||
                   difference <= Mathf.Max(Mathf.Abs(left), Mathf.Abs(right)) * 0.00001f;
        }

        public static void ResetRuntimeState()
        {
            LastValidatedVersions.Clear();
            NextForcedSamples.Clear();
            NextMismatchLogs.Clear();
            QuarantinedVersions.Clear();
            sampleState = 0x9E3779B9u;
        }

        private readonly struct ValidationKey : IEquatable<ValidationKey>
        {
            private readonly StorageNetworkShadowArea area;
            private readonly int worldId;

            public ValidationKey(StorageNetworkShadowArea area, int worldId)
            {
                this.area = area;
                this.worldId = worldId;
            }

            public int WorldId => worldId;

            public bool Equals(ValidationKey other)
            {
                return area == other.area && worldId == other.worldId;
            }

            public override bool Equals(object obj)
            {
                return obj is ValidationKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int)area * 397) ^ worldId;
                }
            }
        }

        private readonly struct MismatchKey : IEquatable<MismatchKey>
        {
            private readonly StorageNetworkShadowArea area;
            private readonly int worldId;
            private readonly int signature;

            public MismatchKey(StorageNetworkShadowArea area, int worldId, int signature)
            {
                this.area = area;
                this.worldId = worldId;
                this.signature = signature;
            }

            public bool Equals(MismatchKey other)
            {
                return area == other.area &&
                       worldId == other.worldId &&
                       signature == other.signature;
            }

            public override bool Equals(object obj)
            {
                return obj is MismatchKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (int)area;
                    hashCode = (hashCode * 397) ^ worldId;
                    return (hashCode * 397) ^ signature;
                }
            }
        }
    }
}
