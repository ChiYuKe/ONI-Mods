using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;



namespace CykUtils
{
    /// <summary>
    /// 日志工具类，支持打印带时间戳、线程 ID、调用上下文（namespace+class+method+文件+行号）的日志。
    /// 同时会输出到 Console 和 Unity 的 Debug 控制台。
    /// </summary>
    public static class LogUtil
    {
        private static bool s_loggingDisabled;

        /// <summary>
        /// 获取当前时间戳（精确到毫秒）+ 线程 ID。
        /// </summary>
        private static string TimeStamp()
        {
            return System.DateTime.Now.ToString("[HH:mm:ss.fff] [") + Thread.CurrentThread.ManagedThreadId + "] ";
        }

        /// <summary>
        /// 构建调用上下文信息（命名空间.类.方法 @ 文件名:行号）。
        /// </summary>
        private static string BuildContext(
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            var stack = new StackTrace();
            var frame = stack.GetFrame(2); // 跳过 Log 方法和调用它的方法
            var method = frame?.GetMethod();
            // string fullName = method?.DeclaringType?.FullName ?? "UnknownType";
            string fullName = method?.DeclaringType?.FullName.Replace('+', '.') ?? "UnknownType";

            string fileName = Path.GetFileName(file);
            return $"{fullName}.{member} @ {fileName}:{line}";
        }

        /// <summary>
        /// 输出带时间戳和上下文的日志到 Console 和 Unity 控制台。
        /// </summary>
        /// <param name="level">日志等级（INFO / WARNING / ERROR）</param>
        /// <param name="message">日志内容</param>
        /// <param name="contextInfo">上下文信息（类、方法、文件、行号）</param>
        private static void WriteTimeStamped(string level, string message, string contextInfo)
        {
            string log = $"{TimeStamp()}[{level}] [{contextInfo}] {message}";
            // Console.WriteLine(log);

            switch (level)
            {
                case "INFO":
                    UnityEngine.Debug.Log(log);
                    break;
                case "WARNING":
                    UnityEngine.Debug.LogWarning(log);
                    break;
                case "ERROR":
                    UnityEngine.Debug.LogError(log);
                    break;
            }
        }

        /// <summary>
        /// 打印 Info 级别日志。
        /// </summary>
        public static void Log(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!s_loggingDisabled)
                WriteTimeStamped("INFO", message, BuildContext(member, file, line));
        }

        /// <summary>
        /// 打印 Warning 级别日志。
        /// </summary>
        public static void LogWarning(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!s_loggingDisabled)
                WriteTimeStamped("WARNING", message, BuildContext(member, file, line));
        }

        /// <summary>
        /// 打印 Error 级别日志。
        /// </summary>
        public static void LogError(string message,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            if (!s_loggingDisabled)
                WriteTimeStamped("ERROR", message, BuildContext(member, file, line));
        }

        /// <summary>
        /// 全局关闭日志输出。
        /// </summary>
        public static void DisableLogging()
        {
            s_loggingDisabled = true;
        }

        [Obsolete("我暂时用不上，因为默认就是会输出日志的")]
        public static void EnableLogging()
        {
            s_loggingDisabled = false;
        }
    }
}
