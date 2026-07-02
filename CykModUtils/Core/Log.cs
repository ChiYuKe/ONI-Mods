using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace CykModUtils.Core
{
    /// <summary>
    /// 统一的 Unity 日志工具，自动附带时间、线程 ID、调用类型、方法和源码行号。
    /// </summary>
    public static class Log
    {
        private static bool loggingDisabled;
        private static string prefix = "CykModUtils";

        /// <summary>
        /// 当前日志是否处于启用状态。
        /// </summary>
        public static bool IsEnabled => !loggingDisabled;

        /// <summary>
        /// 配置日志前缀和初始启用状态。建议在 Mod 入口处调用一次。
        /// </summary>
        /// <param name="logPrefix">日志前缀，通常使用 Mod 名称。</param>
        /// <param name="enabled">是否启用日志。</param>
        public static void Configure(string logPrefix, bool enabled = true)
        {
            prefix = string.IsNullOrWhiteSpace(logPrefix) ? "CykModUtils" : logPrefix;
            loggingDisabled = !enabled;
        }

        /// <summary>
        /// 启用日志输出。
        /// </summary>
        public static void Enable()
        {
            loggingDisabled = false;
        }

        /// <summary>
        /// 禁用日志输出。
        /// </summary>
        public static void Disable()
        {
            loggingDisabled = true;
        }

        /// <summary>
        /// 输出普通信息日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <param name="member">调用成员名，由编译器自动填充。</param>
        /// <param name="file">调用源码文件路径，由编译器自动填充。</param>
        /// <param name="line">调用源码行号，由编译器自动填充。</param>
        public static void Info(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write("INFO", message, member, file, line);
        }

        /// <summary>
        /// 输出警告日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <param name="member">调用成员名，由编译器自动填充。</param>
        /// <param name="file">调用源码文件路径，由编译器自动填充。</param>
        /// <param name="line">调用源码行号，由编译器自动填充。</param>
        public static void Warning(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write("WARNING", message, member, file, line);
        }

        /// <summary>
        /// 输出错误日志。
        /// </summary>
        /// <param name="message">日志内容。</param>
        /// <param name="member">调用成员名，由编译器自动填充。</param>
        /// <param name="file">调用源码文件路径，由编译器自动填充。</param>
        /// <param name="line">调用源码行号，由编译器自动填充。</param>
        public static void Error(string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            Write("ERROR", message, member, file, line);
        }

        private static void Write(string level, string message, string member, string file, int line)
        {
            if (loggingDisabled)
            {
                return;
            }

            string log = TimeStamp() + "[" + level + "] [" + prefix + "] [" + BuildContext(member, file, line) + "] " + message;
            switch (level)
            {
                case "WARNING":
                    Debug.LogWarning(log);
                    break;
                case "ERROR":
                    Debug.LogError(log);
                    break;
                default:
                    Debug.Log(log);
                    break;
            }
        }

        private static string TimeStamp()
        {
            return System.DateTime.Now.ToString("[HH:mm:ss.fff] [") + Thread.CurrentThread.ManagedThreadId + "] ";
        }

        private static string BuildContext(string member, string file, int line)
        {
            var frame = new StackTrace().GetFrame(3);
            string typeName = frame?.GetMethod()?.DeclaringType?.FullName?.Replace('+', '.') ?? "UnknownType";
            return typeName + "." + member + " @ " + Path.GetFileName(file) + ":" + line;
        }
    }
}
