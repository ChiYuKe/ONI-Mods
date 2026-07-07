using StorageNetwork.Components;
using StorageNetwork.UI.WebEditor;

namespace StorageNetwork.UI
{
    public sealed partial class StorageNetworkPanel
    {
        public static void ShowLogicDiyOutputModePicker(StorageNetworkLogicDiy logic)
        {
            StorageNetworkLogicDiyWebEditor.Open(logic);
        }
    }
}
