using System;
using StorageNetwork.Components;

internal static class StorageNetworkEnergySensorLogicTests
{
    private static int failures;

    public static int Main()
    {
        AssertFalse(
            "offline network sends red",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(true, false, 0f, 100f, 20f, 80f));
        AssertFalse(
            "zero capacity sends red",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(true, true, 0f, 0f, 20f, 80f));
        AssertTrue(
            "charge at low threshold sends green",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(false, true, 20f, 100f, 20f, 80f));
        AssertTrue(
            "green signal remains green between thresholds",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(true, true, 50f, 100f, 20f, 80f));
        AssertFalse(
            "charge at high threshold sends red",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(true, true, 80f, 100f, 20f, 80f));
        AssertFalse(
            "red signal remains red between thresholds",
            StorageNetworkEnergySensorLogic.ShouldRequestPower(false, true, 50f, 100f, 20f, 80f));
        AssertEqual("negative charge clamps to zero percent", 0f, StorageNetworkEnergySensorLogic.GetPercent(-10f, 100f));
        AssertEqual("overcharge clamps to one hundred percent", 100f, StorageNetworkEnergySensorLogic.GetPercent(120f, 100f));

        if (failures > 0)
        {
            Console.Error.WriteLine(failures + " energy sensor logic test(s) failed.");
            return 1;
        }

        Console.WriteLine("All energy sensor logic tests passed.");
        return 0;
    }

    private static void AssertTrue(string name, bool value)
    {
        if (!value)
        {
            Fail(name, "true", value.ToString());
        }
    }

    private static void AssertFalse(string name, bool value)
    {
        if (value)
        {
            Fail(name, "false", value.ToString());
        }
    }

    private static void AssertEqual(string name, float expected, float actual)
    {
        if (Math.Abs(expected - actual) > 0.0001f)
        {
            Fail(name, expected.ToString(), actual.ToString());
        }
    }

    private static void Fail(string name, string expected, string actual)
    {
        failures++;
        Console.Error.WriteLine(name + ": expected " + expected + ", got " + actual + ".");
    }
}
