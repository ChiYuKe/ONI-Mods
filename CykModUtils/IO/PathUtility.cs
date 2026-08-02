using System;
using System.IO;

namespace CykModUtils.IO
{
    /// <summary>
    /// 路径规范化和根目录边界检查工具。
    /// </summary>
    public static class PathUtility
    {
        /// <summary>
        /// 把相对路径解析到 rootDirectory 下，并拒绝通过 ".." 越出根目录。
        /// </summary>
        public static bool TryResolveUnderRoot(
            string rootDirectory,
            string relativePath,
            out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(rootDirectory) ||
                string.IsNullOrWhiteSpace(relativePath) ||
                Path.IsPathRooted(relativePath))
            {
                return false;
            }

            try
            {
                string root = Path.GetFullPath(rootDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string candidate = Path.GetFullPath(Path.Combine(root, relativePath));
                string prefix = root + Path.DirectorySeparatorChar;
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                fullPath = candidate;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 返回规范化绝对路径；输入无效时返回 null。
        /// </summary>
        public static string TryGetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                return Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(path.Trim()));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 创建目录并返回其规范化绝对路径。
        /// </summary>
        public static string EnsureDirectory(string path)
        {
            string fullPath = TryGetFullPath(path);
            if (fullPath == null)
            {
                return null;
            }

            Directory.CreateDirectory(fullPath);
            return fullPath;
        }
    }
}
