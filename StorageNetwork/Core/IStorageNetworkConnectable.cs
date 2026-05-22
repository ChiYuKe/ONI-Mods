namespace StorageNetwork.Core
{
    public interface IStorageNetworkConnectable
    {
        int Cell { get; }

        string DisplayName { get; }

        Storage Storage { get; }

        bool CanShareStorage { get; }
    }
}
