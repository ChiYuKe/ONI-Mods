using System;
using System.IO;
using System.Text;

namespace StorageNetwork.LogicDiy.Persistence
{
    /// <summary>Small transactional file boundary for logic-editor save data.</summary>
    internal static class LogicDiyConfigStore
    {
        public static string ReadAllText(string path)
        {
            return string.IsNullOrEmpty(path) || !File.Exists(path) ? null : File.ReadAllText(path, Encoding.UTF8);
        }

        public static void WriteAtomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            string temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, content ?? string.Empty, Encoding.UTF8);
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temporaryPath, path, null);
                    return;
                }
                catch (PlatformNotSupportedException) { }
                catch (IOException) { }
            }

            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }
    }
}
