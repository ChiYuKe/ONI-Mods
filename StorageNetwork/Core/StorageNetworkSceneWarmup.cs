namespace StorageNetwork.Core
{
    // Update continues while simulation is paused. Strict callers retain the
    // synchronous completion path; UI readers wait for the complete directory.
    internal sealed class StorageNetworkSceneWarmup : KMonoBehaviour
    {
        private void Update()
        {
            if (StorageSceneRegistry.AdvanceSceneSeed()) enabled = false;
        }
    }
}
