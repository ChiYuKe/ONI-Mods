namespace LogicNetwork
{
    internal static class LogicNetworkStrings
    {
        private const string BuildingKeyPrefix = "STRINGS.BUILDINGS.PREFABS.LOGICNETWORKEMITTER.";

        public const string BuildingName = "逻辑网络信号输出器";
        public const string BuildingDescription = "用于向自动化线路输出逻辑网络信号的小型自动化建筑。";
        public const string BuildingEffect = "提供一个自动化信号输出端。";
        public const string LogicPort = "逻辑网络信号输出";
        public const string LogicPortActive = "输出绿色信号。";
        public const string LogicPortInactive = "输出红色信号。";
        public const string SideScreenTitle = "信号输出设置";
        public const string EditorTitle = "逻辑编辑器";
        public const string OpenSettings = "打开本地逻辑编辑器";
        public const string OpenSettingsTooltip = "在浏览器中打开本地节点编辑器，保存后同步到这个信号输出器。";

        public static void RegisterBuildingStrings()
        {
            Strings.Add(BuildingKeyPrefix + "NAME", BuildingName);
            Strings.Add(BuildingKeyPrefix + "DESC", BuildingDescription);
            Strings.Add(BuildingKeyPrefix + "EFFECT", BuildingEffect);
            Strings.Add(BuildingKeyPrefix + "LOGIC_PORT", LogicPort);
            Strings.Add(BuildingKeyPrefix + "LOGIC_PORT_ACTIVE", LogicPortActive);
            Strings.Add(BuildingKeyPrefix + "LOGIC_PORT_INACTIVE", LogicPortInactive);
        }
    }
}
