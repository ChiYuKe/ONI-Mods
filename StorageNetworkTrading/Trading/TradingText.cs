using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace StorageNetwork.Trading
{
    internal sealed class TradingLocalizedPart
    {
        public string Key;
        public string[] Arguments;
        public string Literal;
    }
    internal sealed class TradingTextEntry
    {
        internal readonly string Key, Source;
        internal TradingTextEntry(string key, string source) { Key = key; Source = source; }
    }

    // New data contains fixed localization keys and formatting arguments, never translated prose.
    // English is supplied exclusively by the game's PO loader through Configure's lookup.
    internal static class TradingText
    {
        private const char Start = '\u001e', End = '\u001f';
        private static readonly Dictionary<string, string> Sources = TradingTextCatalog.Entries.ToDictionary(e => e.Key, e => e.Source);
        private static Func<string, string> lookup;
        internal static Func<string, string> NativeLabels;
        internal static bool English { get; private set; }
        internal static void Configure(string localeCode, Func<string, string> localizedLookup = null)
        {
            English = !(localeCode ?? "en").StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            lookup = localizedLookup;
        }
        internal static string Ref(string key, params string[] arguments)
        {
            using (var buffer = new MemoryStream())
            {
                using (var writer = new BinaryWriter(buffer, Encoding.UTF8, true))
                {
                    writer.Write(key);
                    writer.Write(arguments.Length);
                    foreach (string argument in arguments) writer.Write(argument ?? "");
                }
                return Start + Convert.ToBase64String(buffer.ToArray()) + End;
            }
        }
        internal static string Argument(object value, string format = null, int alignment = 0)
        {
            string text = value is IFormattable formattable ? formattable.ToString(format, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
            return alignment < 0 ? text.PadRight(-alignment) : alignment > 0 ? text.PadLeft(alignment) : text;
        }
        internal static string Render(string value) => Resolve(value, false, 0);
        internal static string Source(string value) => Resolve(value, true, 0);
        private static string Resolve(string value, bool sourceOnly, int depth)
        {
            if (string.IsNullOrEmpty(value) || depth > 16) return value;
            var output = new StringBuilder();
            int offset = 0;
            while (offset < value.Length)
            {
                int start = value.IndexOf(Start, offset);
                if (start < 0) { output.Append(value.Substring(offset)); break; }
                output.Append(value.Substring(offset, start - offset));
                int end = value.IndexOf(End, start + 1);
                if (end < 0 || end - start > 65536) { output.Append(value.Substring(start)); break; }
                try
                {
                    using (var stream = new MemoryStream(Convert.FromBase64String(value.Substring(start + 1, end - start - 1))))
                    using (var reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        string key = reader.ReadString();
                        int count = reader.ReadInt32();
                        if (count < 0 || count > 64) throw new InvalidDataException();
                        object[] args = new object[count];
                        for (int i = 0; i < count; i++) args[i] = Resolve(reader.ReadString(), sourceOnly, depth + 1);
                        string source = Sources.TryGetValue(key, out string registered) ? registered : key;
                        string localized = sourceOnly ? source : lookup?.Invoke(key);
                        if (string.IsNullOrEmpty(localized) || localized == key) localized = source;
                        output.Append(count == 0 ? localized : string.Format(CultureInfo.InvariantCulture, localized, args));
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is IOException || ex is ArgumentException)
                { output.Append(value.Substring(start, end - start + 1)); }
                offset = end + 1;
            }
            string result = output.ToString();
            return !sourceOnly && NativeLabels != null ? NativeLabels(result) : result;
        }
        internal static bool HasReference(string text) => text?.IndexOf(Start) >= 0;
        internal static List<TradingLocalizedPart> Capture(string text)
        {
            var parts = new List<TradingLocalizedPart>();
            if (string.IsNullOrEmpty(text)) return parts;
            int offset = 0;
            while (offset < text.Length)
            {
                int start = text.IndexOf(Start, offset);
                if (start < 0) { parts.Add(new TradingLocalizedPart { Literal = text.Substring(offset) }); break; }
                if (start > offset) parts.Add(new TradingLocalizedPart { Literal = text.Substring(offset, start - offset) });
                int end = text.IndexOf(End, start + 1);
                if (end < 0 || end - start > 65536) { parts.Add(new TradingLocalizedPart { Literal = text.Substring(start) }); break; }
                try
                {
                    using (var stream = new MemoryStream(Convert.FromBase64String(text.Substring(start + 1, end - start - 1))))
                    using (var reader = new BinaryReader(stream, Encoding.UTF8))
                    {
                        string key = reader.ReadString();
                        int count = reader.ReadInt32();
                        if (count < 0 || count > 64) throw new InvalidDataException();
                        var arguments = new string[count];
                        for (int i = 0; i < count; i++) arguments[i] = reader.ReadString();
                        parts.Add(new TradingLocalizedPart { Key = key, Arguments = arguments });
                    }
                }
                catch (Exception ex) when (ex is FormatException || ex is IOException || ex is ArgumentException)
                { parts.Add(new TradingLocalizedPart { Literal = text.Substring(start, end - start + 1) }); }
                offset = end + 1;
            }
            return parts;
        }
        internal static string Compose(IEnumerable<TradingLocalizedPart> parts) => parts == null ? "" :
            string.Concat(parts.Select(p => string.IsNullOrEmpty(p.Key) ? p.Literal ?? "" : Ref(p.Key, p.Arguments ?? Array.Empty<string>())));
    }
}
