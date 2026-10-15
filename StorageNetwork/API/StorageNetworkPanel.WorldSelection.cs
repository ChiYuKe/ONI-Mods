namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        public static int CurrentWorldFilterId => GetActiveWorldFilterId();
        public static System.Func<bool> ExternalTextInputFocused { get; set; }
    }
}
