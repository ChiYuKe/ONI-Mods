using StorageNetwork.Components;

namespace StorageNetwork.Core
{
    public static class StorageCategories
    {
        private const string SceneStorageKey = "scene_storage";
        private const string VanillaStorageKey = "vanilla_storage";

        public static string GetKey(Storage storage)
        {
            if (storage == null)
            {
                return SceneStorageKey;
            }

            StorageNetworkEnrollment enrollment = storage.GetComponent<StorageNetworkEnrollment>();
            return enrollment != null && enrollment.IncludedInSceneNetwork
                ? VanillaStorageKey
                : SceneStorageKey;
        }

        public static string GetName(string key)
        {
            return key == VanillaStorageKey ? "原版储存" : "储存箱";
        }

        public static int GetOrder(string key)
        {
            return key == VanillaStorageKey ? 1 : 0;
        }
    }
}
