using HarmonyLib;
using StorageNetwork.ProductionOrders;

namespace StorageNetwork.Patches
{
    public static class ProductionOrderPersistencePatch
    {
        [HarmonyPatch(typeof(Game), "Save")]
        public static class GameSavePatch
        {
            public static void Prefix()
            {
                ProductionOrderService.SaveOrders();
            }
        }
    }
}
