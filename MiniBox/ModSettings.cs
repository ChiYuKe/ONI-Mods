using PeterHan.PLib.Options;
using static MiniBox.ModOptions;

namespace MiniBox
{
    internal static class ModSettings
    {
        public static Settings Current => SingletonOptions<Settings>.Instance;
    }
}




