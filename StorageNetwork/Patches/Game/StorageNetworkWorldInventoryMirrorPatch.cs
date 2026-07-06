namespace StorageNetwork.Patches
{
    public static class StorageNetworkWorldInventoryMirrorPatch
    {
        // Disabled intentionally. StorageNetwork server contents remain normal Pickupables and are
        // already counted by WorldInventory, so mirroring the network index into these queries
        // double-counts resource rows in the vanilla resource panel.
    }
}
