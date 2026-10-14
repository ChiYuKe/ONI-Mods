using System;
using StorageNetwork.Trading;

internal static class TradingRefreshCacheChecks
{
    internal static void Verify(Action<bool, string> check)
    {
        var cache = new TradingRefreshCache<(int World, string Good), float>();
        int reads = 0;
        float amount = 10;
        Func<float> load = () => { reads++; return amount; };
        for (int i = 0; i < 100; i++) cache.Get((0, "Iron"), load);
        check(reads == 1, "one UI refresh performs a single inventory read for repeated queries");
        amount = 0;
        check(cache.Get((0, "Iron"), load) == 10, "display queries share a consistent within-refresh snapshot");
        check(cache.Get((1, "Iron"), load) == 0 && reads == 2, "different worlds never share display inventory results");
        cache.Clear();
        check(cache.Get((0, "Iron"), load) == 0 && reads == 3, "the next refresh observes changed inventory");
        cache.Get((0, "Iron"), load);
        check(reads == 3, "empty inventories are cached as well as positive results");
        cache.Clear();
        amount = 25;
        check(cache.Get((0, "Iron"), load) == 25, "clearing before confirmation forces a fresh inventory read");
    }
}
