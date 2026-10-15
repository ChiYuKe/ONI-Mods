using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace StorageNetwork.Trading
{
    internal sealed class TradingAiSettings
    {
        public string Provider = "";
        public string Endpoint = "";
        public string Model = "";
        public int ReplyProbability = 30;
        public string ProtectedApiKey = "";
        [JsonIgnore] public string ApiKey = "";
        internal bool CanChat() => ValidationKey() == null &&
            (!string.IsNullOrWhiteSpace(ApiKey) || new Uri(Endpoint).IsLoopback);

        internal string ValidationKey()
        {
            if (string.IsNullOrWhiteSpace(Provider) || string.IsNullOrWhiteSpace(Model)) return "AI_REQUIRED";
            if (ReplyProbability < 0 || ReplyProbability > 100) return "AI_INVALID_PROBABILITY";
            if (ApiKey != null && (ApiKey.Contains("\r") || ApiKey.Contains("\n"))) return "AI_INVALID_KEY";
            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
                !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) return "AI_INVALID_URL";
            return null;
        }
    }

    // The game adapter selects a sidecar beside the current .sav file.
    internal sealed partial class TradingAiSettingsStore
    {
        private readonly string path;
        private readonly string legacyPath;
        internal TradingAiSettingsStore(string path) : this(path, null) { }
        internal TradingAiSettingsStore(string path, string legacyPath) { this.path = path; this.legacyPath = legacyPath; }
        internal bool Load(out TradingAiSettings settings)
        {
            settings = new TradingAiSettings();
            try
            {
                string source = File.Exists(path) ? path : legacyPath;
                if (string.IsNullOrEmpty(source) || !File.Exists(source)) return true;
                var loaded = JsonConvert.DeserializeObject<TradingAiSettings>(File.ReadAllText(source));
                if (loaded == null) return false;
                loaded.ApiKey = string.IsNullOrEmpty(loaded.ProtectedApiKey) ? "" : TradingAiSecret.Unprotect(loaded.ProtectedApiKey);
                if (source != path) Save(loaded);
                settings = loaded;
                return true;
            }
            catch { return false; }
        }
        internal void Save(TradingAiSettings settings)
        {
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("No active save file.");
            if (settings.ValidationKey() != null) throw new ArgumentException("Invalid AI settings.");
            var saved = new TradingAiSettings { Provider = settings.Provider.Trim(), Endpoint = settings.Endpoint.Trim().TrimEnd('/'),
                Model = settings.Model.Trim(), ReplyProbability = settings.ReplyProbability,
                ProtectedApiKey = string.IsNullOrEmpty(settings.ApiKey) ? "" : TradingAiSecret.Protect(settings.ApiKey) };
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, JsonConvert.SerializeObject(saved, Formatting.Indented), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }

    // DPAPI protects the key for the current Windows account. No plaintext key
    // is written to configuration, game saves or diagnostic logs.
    internal static class TradingAiSecret
    {
        [StructLayout(LayoutKind.Sequential)] private struct Blob { public int Length; public IntPtr Data; }
        [DllImport("crypt32.dll", SetLastError = true)] private static extern bool CryptProtectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
        [DllImport("crypt32.dll", SetLastError = true)] private static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
        [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr memory);
        internal static string Protect(string value) => Convert.ToBase64String(Transform(Encoding.UTF8.GetBytes(value), true));
        internal static string Unprotect(string value)
        {
            byte[] bytes = Transform(Convert.FromBase64String(value), false);
            try { return Encoding.UTF8.GetString(bytes); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }
        private static byte[] Transform(byte[] bytes, bool protect)
        {
            var input = new Blob { Length = bytes.Length, Data = Marshal.AllocHGlobal(bytes.Length) };
            var output = new Blob();
            try
            {
                Marshal.Copy(bytes, 0, input.Data, bytes.Length);
                bool success = protect ? CryptProtectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                    : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
                if (!success) throw new InvalidOperationException("Cannot protect local AI credentials.");
                var result = new byte[output.Length];
                Marshal.Copy(output.Data, result, 0, result.Length);
                return result;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
                Marshal.Copy(bytes, 0, input.Data, bytes.Length);
                Marshal.FreeHGlobal(input.Data);
                if (output.Data != IntPtr.Zero) LocalFree(output.Data);
            }
        }
    }
}
