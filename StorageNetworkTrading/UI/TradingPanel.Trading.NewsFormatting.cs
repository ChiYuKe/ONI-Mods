using System;
using System.Text;
using System.Text.RegularExpressions;
using StorageNetwork.Trading;

namespace StorageNetworkTrading.UI
{
    public sealed partial class TradingPanel
    {
        // Apply semantic emphasis only when displaying news. Stored messages keep
        // their localization keys and original arguments, including existing saves.
        private static string FormatTradingNewsBody(string body)
        {
            var output = new StringBuilder();
            foreach (var part in TradingText.Capture(body))
            {
                if (part.Key == null) { output.Append(part.Literal); continue; }
                var arguments = (string[])part.Arguments.Clone();
                for (int i = 0; i < arguments.Length; i++)
                {
                    string tint = NewsArgumentColor(part.Key, i);
                    if (tint == null) continue;
                    string value = Regex.Replace(TradingText.Render(arguments[i]), "<[^>]*>", "");
                    // Names may be player authored; keep them from introducing TMP markup.
                    value = value.Replace("<", "‹").Replace(">", "›");
                    arguments[i] = "<b><color=#" + tint + ">" + value + "</color></b>";
                }
                output.Append(TradingText.Render(TradingText.Ref(part.Key, arguments)));
            }
            return output.ToString();
        }

        private static string NewsArgumentColor(string key, int index)
        {
            const string person = "753D69", destination = "245C87", time = "805315", cargo = "28684B";
            if (key.Contains("ARRIVAL_PLAN") || key.EndsWith("TIP_ROUTE_PLAN", StringComparison.Ordinal))
                return index == 0 ? person : index == 1 ? destination : index == 2 || index == 3 ? time : index == 4 ? cargo : null;
            if (key.Contains("ARRIVAL_NAMED"))
                return index == 0 ? time : index == 1 ? cargo : index == 3 ? person : null;
            if (key.Contains("TIP_ROUTE_")) return index == 0 ? cargo : index == 1 ? time : null;
            if (key.Contains("TIP_CARGO_")) return cargo;
            if (key.Contains("DEPARTURE_")) return index == 0 ? person : index == 1 ? time : index == 2 ? cargo : null;
            return null;
        }
    }
}
