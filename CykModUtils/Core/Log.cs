using System;
using System.Runtime.CompilerServices;

namespace CykModUtils.Core
{
    /// <summary>
    /// 统一的 Unity 日志工具，自动附带时间、线程 ID、调用类型、方法和源码行号。
    /// </summary>
    public static class Log
    {
        private static ModLogger defaultLogger = new ModLogger("CykModUtils");

        /// <summary>
        /// 当前日志是否处于启用状态。
        /// </summary>
        public static bool IsEnabled => defaultLogger.IsEnabled;

        /// <summary>
        /// 当前兼容层使用的默认日志器。
        /// 新代码应优先为每个 Mod 创建独立的 <see cref="ModLogger"/>。
        /// </summary>
        public static ModLogger DefaultLogger => defaultLogger;

        /// <summary>
        /// 创建不会影响其他 Mod 的独立日志器。
        /// </summary>
        public static ModLogger Create(string logPrefix, bool enabled = true)
        {
            return new ModLogger(logPrefix, enabled);
        }

        /// <summary>
        /// 配置日志前缀和初始启用状态。建议在 Mod 入口处调用一次。
        /// </summary>
        /// <param name="logPrefix">日志前缀，通常使用 Mod 名称。</param>
        /// <param name="enabled">是否启用日志。</param>
        public static void Configure(string logPrefix, bool enabled = true)
        {
            defaultLogger = new ModLogger(logPrefix, enabled);
        }

        /// <summary>
        /// 启用日志输出。
        /// </summary>
        public static void Enable()
        {
            defaultLogger.IsEnabled = true;
        }

        /// <summary>
        /// 禁用日志输出。
        /// </summary>
        public static void Disable()
        {
            defaultLogger.IsEnabled = false;
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
            defaultLogger.Info(message, member, file, line);
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
            defaultLogger.Warning(message, member, file, line);
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
            defaultLogger.Error(message, member, file, line);
        }

        /// <summary>输出异常及可选说明。</summary>
        public static void Exception(Exception exception, string message = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            defaultLogger.Exception(exception, message, member, file, line);
        }

        /// <summary>同一个 key 只输出一次普通信息。</summary>
        public static bool InfoOnce(string key, string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return defaultLogger.InfoOnce(key, message, member, file, line);
        }

        /// <summary>同一个 key 只输出一次警告。</summary>
        public static bool WarningOnce(string key, string message, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        {
            return defaultLogger.WarningOnce(key, message, member, file, line);
        }

        /// <summary>清除一次性日志记录。</summary>
        public static void ResetOnce(string key = null)
        {
            defaultLogger.ResetOnce(key);
        }
    }
}
