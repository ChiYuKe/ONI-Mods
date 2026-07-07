namespace StorageNetwork.Patches
{
    public static class StorageNetworkRationTrackerPatch
    {
        // Disabled intentionally. Server food is still regular WorldInventory content, so adding
        // mirrored calories here can inflate vanilla food totals the same way resource rows were
        // double-counted.
    }
}
