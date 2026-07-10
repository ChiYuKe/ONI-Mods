using STRINGS;

namespace KModTool
{
    public static class KModStringUtils
    {
        public static void Add_New_BuildStrings(string buildingId, string name, string description, string effect)
        {
            Strings.Add(new string[]
            {
                "STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".NAME",
                name
            });
            Strings.Add(new string[]
            {
                "STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".DESC",
                description
            });
            Strings.Add(new string[]
            {
                "STRINGS.BUILDINGS.PREFABS." + buildingId.ToUpperInvariant() + ".EFFECT",
                effect
            });
        }
    }
}
