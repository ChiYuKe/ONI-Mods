namespace StorageNetwork.API
{
    /// <summary>
    /// 世界建筑文字面板的数据模型。外部模组可以返回这个对象来自定义建筑上方显示的文字。
    /// </summary>
    public sealed class StorageNetworkWorldPanelContent
    {
        public StorageNetworkWorldPanelContent(string title, string lineOne, string lineTwo, string lineThree)
        {
            Title = title ?? string.Empty;
            LineOne = lineOne ?? string.Empty;
            LineTwo = lineTwo ?? string.Empty;
            LineThree = lineThree ?? string.Empty;
        }

        public string Title { get; }

        public string LineOne { get; }

        public string LineTwo { get; }

        public string LineThree { get; }
    }
}
