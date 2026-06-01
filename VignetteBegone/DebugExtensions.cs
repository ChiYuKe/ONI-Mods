namespace VignetteBegone
{
    public static class DebugExtensions
    {
        public static string Color(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }
    }
}
