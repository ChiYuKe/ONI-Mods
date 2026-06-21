using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImGuiNET;
using UnityEngine;

namespace DebugUI
{
    public sealed class DebugUILogTool : DevTool
    {
        private const int MaxEntries = 1000;
        private const int InitialTailBytes = 256 * 1024;
        private static readonly object LogLock = new object();
        private static readonly List<LogEntry> Entries = new List<LogEntry>(MaxEntries);
        private static bool listening;
        private static bool initialFileLoaded;
        private static string logFilePath;
        private static long logFilePosition;
        private static float nextFilePollTime;

        private bool showLog = true;
        private bool showWarning = true;
        private bool showError = true;
        private bool showStackTrace;
        private bool autoScroll = true;
        private string filter = string.Empty;

        public DebugUILogTool()
        {
            Name = "Log Viewer / 日志查看器";
            RequiresGameRunning = false;
            EnsureListening();
        }

        public static void EnsureListening()
        {
            if (listening)
            {
                return;
            }

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
            listening = true;
            ReadInitialLogTail();
        }

        protected override void RenderTo(DevPanel panel)
        {
            Name = "Log Viewer / 日志查看器";
            PollLogFile();
            DrawToolbar();
            ImGui.Separator();
            DrawLogList();
        }

        private void DrawToolbar()
        {
            ImGui.Text("Game Logs / 游戏日志");
            ImGui.SameLine();
            if (ImGui.Button("Copy Visible / 复制可见"))
            {
                GUIUtility.systemCopyBuffer = BuildVisibleText();
            }
            ImGui.SameLine();
            if (ImGui.Button("Clear / 清空"))
            {
                Clear();
            }

            ImGui.Checkbox("Log / 普通", ref showLog);
            ImGui.SameLine();
            ImGui.Checkbox("Warning / 警告", ref showWarning);
            ImGui.SameLine();
            ImGui.Checkbox("Error / 错误", ref showError);
            ImGui.SameLine();
            ImGui.Checkbox("Stack / 堆栈", ref showStackTrace);
            ImGui.SameLine();
            ImGui.Checkbox("Auto Scroll / 自动滚动", ref autoScroll);

            ImGui.SetNextItemWidth(420f);
            ImGui.InputText("Filter / 筛选", ref filter, 256);

            int total;
            lock (LogLock)
            {
                total = Entries.Count;
            }

            ImGui.Text("Buffered / 已缓存: " + total + " / " + MaxEntries + "    Shortcut / 快捷键: Ctrl+F10");
            ImGui.Text("File / 文件: " + (string.IsNullOrEmpty(logFilePath) ? "<not found / 未找到>" : logFilePath));
        }

        private void DrawLogList()
        {
            ImGui.BeginChild("DebugUI_LogList", new Vector2(0f, 0f), true);
            List<LogEntry> snapshot = Snapshot();
            for (int i = 0; i < snapshot.Count; i++)
            {
                LogEntry entry = snapshot[i];
                if (!ShouldShow(entry))
                {
                    continue;
                }

                DrawLogEntry(entry);
            }

            if (autoScroll && ImGui.GetScrollY() >= ImGui.GetScrollMaxY() - 4f)
            {
                ImGui.SetScrollHereY(1f);
            }

            ImGui.EndChild();
        }

        private void DrawLogEntry(LogEntry entry)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, GetColor(entry.Type));
            ImGui.TextWrapped(entry.Header + " " + entry.Condition);
            ImGui.PopStyleColor();

            if (showStackTrace && !string.IsNullOrEmpty(entry.StackTrace))
            {
                ImGui.TextWrapped(entry.StackTrace);
            }
        }

        private static Vector4 GetColor(LogType type)
        {
            if (type == LogType.Warning)
            {
                return new Vector4(1f, 0.78f, 0.25f, 1f);
            }

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                return new Vector4(1f, 0.36f, 0.32f, 1f);
            }

            return new Vector4(0.82f, 0.88f, 0.95f, 1f);
        }

        private bool ShouldShow(LogEntry entry)
        {
            if (entry.Type == LogType.Warning)
            {
                if (!showWarning)
                {
                    return false;
                }
            }
            else if (entry.Type == LogType.Error || entry.Type == LogType.Exception || entry.Type == LogType.Assert)
            {
                if (!showError)
                {
                    return false;
                }
            }
            else if (!showLog)
            {
                return false;
            }

            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            return IndexOf(entry.Condition, filter) >= 0 || IndexOf(entry.StackTrace, filter) >= 0 || IndexOf(entry.Header, filter) >= 0;
        }

        private static int IndexOf(string text, string value)
        {
            return (text ?? string.Empty).IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        private string BuildVisibleText()
        {
            List<LogEntry> snapshot = Snapshot();
            StringBuilder builder = new StringBuilder(snapshot.Count * 96);
            for (int i = 0; i < snapshot.Count; i++)
            {
                LogEntry entry = snapshot[i];
                if (!ShouldShow(entry))
                {
                    continue;
                }

                builder.Append(entry.Header).Append(' ').AppendLine(entry.Condition);
                if (showStackTrace && !string.IsNullOrEmpty(entry.StackTrace))
                {
                    builder.AppendLine(entry.StackTrace);
                }
            }

            return builder.ToString();
        }

        private static void Clear()
        {
            lock (LogLock)
            {
                Entries.Clear();
            }
        }

        private static List<LogEntry> Snapshot()
        {
            lock (LogLock)
            {
                return new List<LogEntry>(Entries);
            }
        }

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            AddEntry(new LogEntry(System.DateTime.Now, type, condition, stackTrace));
        }

        private static void ReadInitialLogTail()
        {
            if (initialFileLoaded)
            {
                return;
            }

            initialFileLoaded = true;
            logFilePath = GetLogFilePath();
            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            {
                return;
            }

            try
            {
                using (FileStream stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long start = Math.Max(0L, stream.Length - InitialTailBytes);
                    stream.Seek(start, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        string text = reader.ReadToEnd();
                        logFilePosition = stream.Position;
                        AddFileText(text, start > 0L);
                    }
                }
            }
            catch (Exception exception)
            {
                AddEntry(new LogEntry(System.DateTime.Now, LogType.Warning, "[DebugUI] Failed to read log file: " + exception.Message, string.Empty));
            }
        }

        private static void PollLogFile()
        {
            if (Time.unscaledTime < nextFilePollTime)
            {
                return;
            }

            nextFilePollTime = Time.unscaledTime + 0.5f;
            if (string.IsNullOrEmpty(logFilePath))
            {
                logFilePath = GetLogFilePath();
            }

            if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
            {
                return;
            }

            try
            {
                using (FileStream stream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (stream.Length < logFilePosition)
                    {
                        logFilePosition = 0L;
                    }

                    if (stream.Length == logFilePosition)
                    {
                        return;
                    }

                    stream.Seek(logFilePosition, SeekOrigin.Begin);
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true))
                    {
                        string text = reader.ReadToEnd();
                        logFilePosition = stream.Position;
                        AddFileText(text, false);
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetLogFilePath()
        {
            string consolePath = Application.consoleLogPath;
            if (!string.IsNullOrEmpty(consolePath))
            {
                return consolePath;
            }

            string localLow = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData) + "Low",
                "Klei",
                "Oxygen Not Included",
                "Player.log");
            return localLow;
        }

        private static void AddFileText(string text, bool trimmedHead)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int start = 0;
            if (trimmedHead && lines.Length > 0)
            {
                start = 1;
            }

            for (int i = start; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                AddEntry(new LogEntry(System.DateTime.Now, GuessLogType(line), "[file] " + line, string.Empty));
            }
        }

        private static LogType GuessLogType(string line)
        {
            if (IndexOf(line, "exception") >= 0 || IndexOf(line, "[error]") >= 0 || IndexOf(line, " error") >= 0)
            {
                return LogType.Error;
            }

            if (IndexOf(line, "warning") >= 0 || IndexOf(line, "[warning]") >= 0)
            {
                return LogType.Warning;
            }

            return LogType.Log;
        }

        private static void AddEntry(LogEntry entry)
        {
            lock (LogLock)
            {
                if (Entries.Count >= MaxEntries)
                {
                    Entries.RemoveAt(0);
                }

                Entries.Add(entry);
            }
        }

        private struct LogEntry
        {
            public readonly string Header;
            public readonly string Condition;
            public readonly string StackTrace;
            public readonly LogType Type;

            public LogEntry(System.DateTime time, LogType type, string condition, string stackTrace)
            {
                Header = "[" + time.ToString("HH:mm:ss.fff") + "][" + type + "]";
                Type = type;
                Condition = condition ?? string.Empty;
                StackTrace = stackTrace ?? string.Empty;
            }
        }
    }
}
