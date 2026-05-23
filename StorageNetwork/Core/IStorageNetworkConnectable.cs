namespace StorageNetwork.Core
{
    public interface IStorageNetworkConnectable
    {
        int Cell { get; }

        int InputCell { get; }

        int OutputCell { get; }

        string DisplayName { get; }

        Storage Storage { get; }

        bool AllowsNetworkPull { get; }

        bool CanShareStorage { get; }
    }
}
