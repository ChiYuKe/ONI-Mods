using System;
using System.IO;
using System.Text;

namespace CykModUtils.IO
{
    /// <summary>
    /// 提供 UTF-8 文本读取和同目录临时文件替换，避免写入中断留下半个文件。
    /// </summary>
    public static class AtomicFileUtility
    {
        /// <summary>
        /// 尝试读取文本文件。
        /// </summary>
        public static bool TryReadAllText(
            string path,
            out string content,
            out Exception error,
            Encoding encoding = null)
        {
            content = null;
            error = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = new ArgumentException("File path cannot be empty.", nameof(path));
                return false;
            }

            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                content = File.ReadAllText(path, encoding ?? new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        /// <summary>
        /// 读取文本文件；文件不存在或读取失败时返回 fallback。
        /// </summary>
        public static string ReadAllTextOrDefault(
            string path,
            string fallback = null,
            Encoding encoding = null)
        {
            return TryReadAllText(path, out string content, out _, encoding)
                ? content
                : fallback;
        }

        /// <summary>
        /// 在目标文件同目录写入临时文件后替换目标。失败时抛出原始异常。
        /// </summary>
        public static void WriteAllTextAtomically(
            string path,
            string content,
            Encoding encoding = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(path));
            }

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(
                    temporaryPath,
                    content ?? string.Empty,
                    encoding ?? new UTF8Encoding(false));

                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Replace(temporaryPath, fullPath, null);
                        temporaryPath = null;
                        return;
                    }
                    catch (PlatformNotSupportedException)
                    {
                    }
                    catch (IOException)
                    {
                    }
                }

                File.Copy(temporaryPath, fullPath, true);
            }
            finally
            {
                if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath))
                {
                    try
                    {
                        File.Delete(temporaryPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 尝试原子写入文本文件。
        /// </summary>
        public static bool TryWriteAllTextAtomically(
            string path,
            string content,
            out Exception error,
            Encoding encoding = null)
        {
            error = null;
            try
            {
                WriteAllTextAtomically(path, content, encoding);
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }
    }
}
