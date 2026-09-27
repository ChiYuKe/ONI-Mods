namespace StorageNetwork.Patches
{
    public static class StorageNetworkWorldInventoryMirrorPatch
    {
        // Disabled intentionally. StorageNetwork server contents remain normal Pickupables and are
        // already counted by WorldInventory. Landed rocket interior worlds are synchronized with
        // the parent asteroid via StorageNetworkWorldUtility, so vanilla ClusterUtil and WorldInventory
        // naturally aggregate related worlds when includeRelatedWorlds is true without double-counting.
    }
}
