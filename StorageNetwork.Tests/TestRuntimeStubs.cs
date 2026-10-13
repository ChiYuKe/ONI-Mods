using System;
using System.Collections.Generic;

namespace StorageNetwork.Core
{
    internal sealed class StorageInfo
    {
        public string TypeKey { get; set; }

        public UnityEngine.GameObject GameObject { get; set; }

        public IReadOnlyList<UnityEngine.GameObject> StoredItems { get; set; }
    }

    /// <summary>
    /// Test double for the marker-backed rollout switch. Linking the real shadow
    /// validator against this type lets deterministic tests execute the production
    /// version/quarantine state machine without loading the game runtime.
    /// </summary>
    internal static class StorageNetworkPerformanceMode
    {
        public static bool LegacyFullScanEnabled { get; set; }

        public static bool ShadowValidationEnabled { get; set; }

        public static bool ShadowValidationFullEnabled { get; set; }
    }
}

internal sealed partial class Storage
{
    public Storage(int instanceId, float massKg)
    {
        InstanceId = instanceId;
        MassKg = massKg;
        gameObject = new UnityEngine.GameObject(instanceId);
    }

    public int InstanceId { get; }

    public float MassKg { get; set; }

    public int GetInstanceID() => InstanceId;

    public float MassStored() => items.Count == 0 ? MassKg : System.Linq.Enumerable.Sum(items, item => item.GetComponent<PrimaryElement>()?.Mass ?? 0f);
}

internal sealed class Studyable
{
    public bool Studied { get; set; }
}

namespace StorageNetwork.Components
{
    internal sealed class StorageNetworkEnrollment
    {
        public StorageNetworkEnrollment(int instanceId, string name)
        {
            InstanceId = instanceId;
            gameObject = new UnityEngine.GameObject(instanceId, properName: name);
            Studyable = new Studyable();
        }

        public int InstanceId { get; }

        public bool IncludedInSceneNetwork { get; set; }

        public UnityEngine.GameObject gameObject { get; }

        public Studyable Studyable { get; }

        public int GetInstanceID() => InstanceId;

        public T GetComponent<T>() where T : class
        {
            return Studyable as T;
        }
    }
}

namespace UnityEngine
{
    internal sealed partial class GameObject
    {
        public GameObject(int instanceId = 0, string key = null, string properName = null)
        {
            InstanceId = instanceId;
            Key = key ?? string.Empty;
            ProperName = properName ?? string.Empty;
        }

        public int InstanceId { get; }

        public string Key { get; }

        public string ProperName { get; }

        public int GetInstanceID() => InstanceId;

        public string GetProperName() => ProperName;
    }

    internal static class Time
    {
        public static float unscaledTime { get; set; }
        public static int frameCount { get; set; }
        public static float time { get; set; }
    }

    internal static class Mathf
    {
        public static float Abs(float value) => Math.Abs(value);
        public static float Round(float value) => (float)Math.Round(value);
        public static int CeilToInt(float value) => (int)Math.Ceiling(value);

        public static float Max(float left, float right) => Math.Max(left, right);
        public static float Min(float left, float right) => Math.Min(left, right);
    }

    internal static class Debug
    {
        private static readonly List<string> errors = new List<string>();

        public static IReadOnlyList<string> Errors => errors;

        public static void LogError(object message)
        {
            errors.Add(message?.ToString() ?? string.Empty);
        }

        public static void Reset()
        {
            errors.Clear();
        }
    }
}

namespace StorageNetwork.UI
{
    internal static class StorageNetworkCycleTime
    {
        public static float Current { get; set; }

        public static float GetCurrent() => Current;
    }
}
