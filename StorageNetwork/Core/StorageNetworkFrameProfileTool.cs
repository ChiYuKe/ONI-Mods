using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace StorageNetwork.Core
{
    internal static class StorageNetworkFrameProfileTool
    {
        private const string EnableFileName = "FrameProfileTool.enabled";
        private const string LogPrefix = "[StorageNetwork][FrameProfile]";
        private static string modPath;

        public static void SetModPath(string path)
        {
            modPath = path;
        }

        public static void InstallIfEnabled(Game game)
        {
            if (game == null || game.gameObject == null || !IsEnabled())
            {
                return;
            }

            FrameProfileBehaviour profiler = game.gameObject.GetComponent<FrameProfileBehaviour>();
            if (profiler == null)
            {
                profiler = game.gameObject.AddComponent<FrameProfileBehaviour>();
            }

            profiler.ResetWindow();
            Debug.Log(LogPrefix + " enabled. Reporting every 60s.");
        }

        public static void ResetRuntimeState()
        {
            FrameProfileBehaviour profiler = Game.Instance != null
                ? Game.Instance.gameObject.GetComponent<FrameProfileBehaviour>()
                : null;
            if (profiler != null)
            {
                Object.Destroy(profiler);
            }
        }

        private static bool IsEnabled()
        {
            return File.Exists(Path.Combine(GetConfigDirectory(), EnableFileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, EnableFileName)));
        }

        private static string GetConfigDirectory()
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", "StorageNetwork");
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class FrameProfileBehaviour : MonoBehaviour
        {
            private readonly List<float> frameTimesMs = new List<float>(4096);
            private float windowStartedAt;
            private float totalMs;
            private float maxMs;
            private int hitchOver33;
            private int hitchOver50;
            private int hitchOver100;
            private int hitchOver200;

            public void ResetWindow()
            {
                frameTimesMs.Clear();
                windowStartedAt = Time.unscaledTime;
                totalMs = 0f;
                maxMs = 0f;
                hitchOver33 = 0;
                hitchOver50 = 0;
                hitchOver100 = 0;
                hitchOver200 = 0;
            }

            private void Update()
            {
                float frameMs = Time.unscaledDeltaTime * 1000f;
                if (frameMs <= 0f || float.IsNaN(frameMs) || float.IsInfinity(frameMs))
                {
                    return;
                }

                RecordFrame(frameMs);
            }

            private void RecordFrame(float frameMs)
            {
                frameTimesMs.Add(frameMs);
                totalMs += frameMs;
                if (frameMs > maxMs)
                {
                    maxMs = frameMs;
                }

                if (frameMs > 33.333f)
                {
                    hitchOver33++;
                }

                if (frameMs > 50f)
                {
                    hitchOver50++;
                }

                if (frameMs > 100f)
                {
                    hitchOver100++;
                }

                if (frameMs > 200f)
                {
                    hitchOver200++;
                }

                if (Time.unscaledTime - windowStartedAt >= 60f)
                {
                    LogWindow();
                    ResetWindow();
                }
            }

            private void LogWindow()
            {
                int frames = frameTimesMs.Count;
                if (frames <= 0)
                {
                    return;
                }

                frameTimesMs.Sort();
                float elapsedSeconds = Mathf.Max(0.001f, Time.unscaledTime - windowStartedAt);
                float avgMs = totalMs / frames;
                float p95 = GetPercentile(0.95f);
                float p99 = GetPercentile(0.99f);
                float fps = frames / elapsedSeconds;

                Debug.Log(string.Format(
                    "{0} {1:F1}s frames={2} fps={3:F1} avg={4:F2}ms p95={5:F2}ms p99={6:F2}ms max={7:F2}ms >33ms={8} >50ms={9} >100ms={10} >200ms={11}",
                    LogPrefix,
                    elapsedSeconds,
                    frames,
                    fps,
                    avgMs,
                    p95,
                    p99,
                    maxMs,
                    hitchOver33,
                    hitchOver50,
                    hitchOver100,
                    hitchOver200));
            }

            private float GetPercentile(float percentile)
            {
                if (frameTimesMs.Count == 0)
                {
                    return 0f;
                }

                int index = Mathf.Clamp(
                    Mathf.CeilToInt(frameTimesMs.Count * percentile) - 1,
                    0,
                    frameTimesMs.Count - 1);
                return frameTimesMs[index];
            }
        }
    }
}
