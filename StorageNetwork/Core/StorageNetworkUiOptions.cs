namespace StorageNetwork.Core
{
    public static class StorageNetworkUiOptions
    {
        // 如果为 true，建筑的标题栏将显示一个按钮，点击后会打开网络概览界面。为 false 通过侧边栏按钮访问网络界面。
        public static readonly bool UseTitleBarNetworkButton = false;

        // 默认情况下，连接器没有输出端口，除非它连接到一个具有输出端口的建筑（如 Storage Network Hub）。
        public static readonly bool DefaultConnectorHasOutputPort = false;
    }
}
