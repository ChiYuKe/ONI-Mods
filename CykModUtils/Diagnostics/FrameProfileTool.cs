using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CykModUtils.Diagnostics
{
    /// <summary>
    /// 通用帧耗时统计工具。启用后会定期输出 fps、平均帧耗时、p95/p99 和卡顿次数。
    /// </summary>
    public static class FrameProfileTool
    {
        /// <summary>
        /// 如果启用文件存在，则在指定对象上安装帧统计组件。
        /// </summary>
        public static bool InstallIfEnabled(FrameProfileOptions options)
        {
            FrameProfileSession session = FrameProfileSession.Create(options);
            if (session == null || session.Owner == null ||
                !IsEnabled(session.ModId, session.ModPath, session.EnableFileName))
            {
                return false;
            }

            FrameProfileBehaviour profiler = session.Owner.GetComponent<FrameProfileBehaviour>();
            if (profiler == null)
            {
                profiler = session.Owner.AddComponent<FrameProfileBehaviour>();
            }

            profiler.AddOrResetSession(session);
            Debug.Log(session.LogPrefix + " enabled. Reporting every " + session.ReportIntervalSeconds.ToString("F0") + "s.");
            return true;
        }

        /// <summary>
        /// 移除指定 Mod 的帧统计会话。
        /// </summary>
        public static void Reset(GameObject owner, string modId)
        {
            FrameProfileBehaviour profiler = owner != null ? owner.GetComponent<FrameProfileBehaviour>() : null;
            profiler?.RemoveSession(modId);
        }

        private static bool IsEnabled(string modId, string modPath, string enableFileName)
        {
            return File.Exists(Path.Combine(GetConfigDirectory(modId), enableFileName)) ||
                   (!string.IsNullOrEmpty(modPath) &&
                    File.Exists(Path.Combine(modPath, enableFileName)));
        }

        private static string GetConfigDirectory(string modId)
        {
            try
            {
                return Path.Combine(Util.RootFolder(), "mods", string.IsNullOrEmpty(modId) ? "CykModUtils" : modId);
            }
            catch
            {
                return string.Empty;
            }
        }

        private sealed class FrameProfileSession
        {
            public string ModId { get; private set; }
            public string ModPath { get; private set; }
            public string EnableFileName { get; private set; }
            public string LogPrefix { get; private set; }
            public float ReportIntervalSeconds { get; private set; }
            public GameObject Owner { get; private set; }
            public readonly List<float> FrameTimesMs = new List<float>(4096);
            public float WindowStartedAt;
            public float TotalMs;
            public float MaxMs;
            public int HitchOver33;
            public int HitchOver50;
            public int HitchOver100;
            public int HitchOver200;

            public static FrameProfileSession Create(FrameProfileOptions options)
            {
                if (options == null)
                {
                    return null;
                }

                string modId = string.IsNullOrEmpty(options.ModId) ? "CykModUtils" : options.ModId;
                return new FrameProfileSession
                {
                    ModId = modId,
                    ModPath = options.ModPath,
                    EnableFileName = string.IsNullOrEmpty(options.EnableFileName)
                        ? "FrameProfileTool.enabled"
                        : options.EnableFileName,
                    LogPrefix = string.IsNullOrEmpty(options.LogPrefix)
                        ? "[" + modId + "][FrameProfile]"
                        : options.LogPrefix,
                    ReportIntervalSeconds = Mathf.Max(1f, options.ReportIntervalSeconds),
                    Owner = options.Owner
                };
            }

            public void ResetWindow()
            {
                FrameTimesMs.Clear();
                WindowStartedAt = Time.unscaledTime;
                TotalMs = 0f;
                MaxMs = 0f;
                HitchOver33 = 0;
                HitchOver50 = 0;
                HitchOver100 = 0;
                HitchOver200 = 0;
            }
        }

        private sealed class FrameProfileBehaviour : MonoBehaviour
        {
            private readonly Dictionary<string, FrameProfileSession> sessions =
                new Dictionary<string, FrameProfileSession>();
            private readonly List<FrameProfileSession> updateBuffer =
                new List<FrameProfileSession>(8);

            public void AddOrResetSession(FrameProfileSession session)
            {
                session.ResetWindow();
                sessions[session.ModId] = session;
            }

            public void RemoveSession(string modId)
            {
                if (!string.IsNullOrEmpty(modId))
                {
                    sessions.Remove(modId);
                }

                if (sessions.Count == 0)
                {
                    Destroy(this);
                }
            }

            private void Update()
            {
                if (sessions.Count == 0)
                {
                    return;
                }

                float frameMs = Time.unscaledDeltaTime * 1000f;
                if (frameMs <= 0f || float.IsNaN(frameMs) || float.IsInfinity(frameMs))
                {
                    return;
                }

                updateBuffer.Clear();
                updateBuffer.AddRange(sessions.Values);

                foreach (FrameProfileSession session in updateBuffer)
                {
                    RecordFrame(session, frameMs);
                }

                updateBuffer.Clear();
            }

            private static void RecordFrame(FrameProfileSession session, float frameMs)
            {
                session.FrameTimesMs.Add(frameMs);
                session.TotalMs += frameMs;
                if (frameMs > session.MaxMs)
                {
                    session.MaxMs = frameMs;
                }

                if (frameMs > 33.333f)
                {
                    session.HitchOver33++;
                }

                if (frameMs > 50f)
                {
                    session.HitchOver50++;
                }

                if (frameMs > 100f)
                {
                    session.HitchOver100++;
                }

                if (frameMs > 200f)
                {
                    session.HitchOver200++;
                }

                if (Time.unscaledTime - session.WindowStartedAt >= session.ReportIntervalSeconds)
                {
                    LogWindow(session);
                    session.ResetWindow();
                }
            }

            private static void LogWindow(FrameProfileSession session)
            {
                int frames = session.FrameTimesMs.Count;
                if (frames <= 0)
                {
                    return;
                }

                session.FrameTimesMs.Sort();
                float elapsedSeconds = Mathf.Max(0.001f, Time.unscaledTime - session.WindowStartedAt);
                float avgMs = session.TotalMs / frames;
                float p95 = GetPercentile(session.FrameTimesMs, 0.95f);
                float p99 = GetPercentile(session.FrameTimesMs, 0.99f);
                float fps = frames / elapsedSeconds;

                Debug.Log(string.Format(
                    "{0} {1:F1}s frames={2} fps={3:F1} avg={4:F2}ms p95={5:F2}ms p99={6:F2}ms max={7:F2}ms >33ms={8} >50ms={9} >100ms={10} >200ms={11}",
                    session.LogPrefix,
                    elapsedSeconds,
                    frames,
                    fps,
                    avgMs,
                    p95,
                    p99,
                    session.MaxMs,
                    session.HitchOver33,
                    session.HitchOver50,
                    session.HitchOver100,
                    session.HitchOver200));
            }

            private static float GetPercentile(List<float> values, float percentile)
            {
                if (values == null || values.Count == 0)
                {
                    return 0f;
                }

                int index = Mathf.Clamp(
                    Mathf.CeilToInt(values.Count * percentile) - 1,
                    0,
                    values.Count - 1);
                return values[index];
            }
        }
    }
}
