using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace CykModUtils.Core
{
    /// <summary>
    /// 带独立前缀和启用状态的 Mod 日志器。
    /// 每个 Mod 应持有自己的实例，避免共享静态日志配置时互相覆盖。
    /// </summary>
    public sealed class ModLogger
    {
        private readonly object onceLock = new object();
        private readonly HashSet<string> emittedOnceKeys = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 创建日志器。
        /// </summary>
        /// <param name="prefix">日志前缀，通常使用 Mod 的 staticID。</param>
        /// <param name="enabled">初始是否输出日志。</param>
        public ModLogger(string prefix, bool enabled = true)
        {
            Prefix = NormalizePrefix(prefix);
            IsEnabled = enabled;
        }

        /// <summary>日志前缀。</summary>
        public string Prefix { get; }

        /// <summary>是否输出日志。</summary>
        public bool IsEnabled { get; set; }

        /// <summary>是否附带时间、线程、调用成员和源码行号。默认启用。</summary>
        public bool IncludeContext { get; set; } = true;

        /// <summary>输出普通信息。</summary>
        public void Info(
            string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Write(LogSeverity.Info, message, member, file, line);
        }

        /// <summary>输出警告。</summary>
        public void Warning(
            string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Write(LogSeverity.Warning, message, member, file, line);
        }

        /// <summary>输出错误。</summary>
        public void Error(
            string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            Write(LogSeverity.Error, message, member, file, line);
        }

        /// <summary>输出异常及可选说明。</summary>
        public void Exception(
            Exception exception,
            string message = null,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (exception == null)
            {
                Error(message ?? "Unknown exception.", member, file, line);
                return;
            }

            string content = string.IsNullOrWhiteSpace(message)
                ? exception.ToString()
                : message + Environment.NewLine + exception;
            Write(LogSeverity.Error, content, member, file, line);
        }

        /// <summary>同一个 key 只输出一次普通信息。</summary>
        /// <returns>本次确实输出时返回 true。</returns>
        public bool InfoOnce(
            string key,
            string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            return WriteOnce(LogSeverity.Info, key, message, member, file, line);
        }

        /// <summary>同一个 key 只输出一次警告。</summary>
        /// <returns>本次确实输出时返回 true。</returns>
        public bool WarningOnce(
            string key,
            string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            return WriteOnce(LogSeverity.Warning, key, message, member, file, line);
        }

        /// <summary>
        /// 清除一次性日志记录。key 为空时清除全部记录。
        /// </summary>
        public void ResetOnce(string key = null)
        {
            lock (onceLock)
            {
                if (key == null)
                {
                    emittedOnceKeys.Clear();
                }
                else
                {
                    emittedOnceKeys.Remove(key);
                }
            }
        }

        private bool WriteOnce(
            LogSeverity severity,
            string key,
            string message,
            string member,
            string file,
            int line)
        {
            if (!IsEnabled)
            {
                return false;
            }

            key = key ?? message ?? string.Empty;
            lock (onceLock)
            {
                if (!emittedOnceKeys.Add(key))
                {
                    return false;
                }
            }

            Write(severity, message, member, file, line);
            return true;
        }

        private void Write(LogSeverity severity, string message, string member, string file, int line)
        {
            if (!IsEnabled)
            {
                return;
            }

            string level = severity == LogSeverity.Warning
                ? "WARNING"
                : severity == LogSeverity.Error
                    ? "ERROR"
                    : "INFO";
            string content = message ?? string.Empty;
            string log = IncludeContext
                ? BuildContext(level, content, member, file, line)
                : "[" + level + "] [" + Prefix + "] " + content;

            switch (severity)
            {
                case LogSeverity.Warning:
                    Debug.LogWarning(log);
                    break;
                case LogSeverity.Error:
                    Debug.LogError(log);
                    break;
                default:
                    Debug.Log(log);
                    break;
            }
        }

        private string BuildContext(string level, string message, string member, string file, int line)
        {
            string fileName = Path.GetFileName(file ?? string.Empty);
            string typeName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(typeName))
            {
                typeName = "UnknownType";
            }

            return global::System.DateTime.Now.ToString("[HH:mm:ss.fff] [")
                + Thread.CurrentThread.ManagedThreadId
                + "] ["
                + level
                + "] ["
                + Prefix
                + "] ["
                + typeName
                + "."
                + (member ?? string.Empty)
                + " @ "
                + fileName
                + ":"
                + line
                + "] "
                + message;
        }

        private static string NormalizePrefix(string prefix)
        {
            return string.IsNullOrWhiteSpace(prefix) ? "CykModUtils" : prefix.Trim();
        }

        private enum LogSeverity
        {
            Info,
            Warning,
            Error
        }
    }
}
