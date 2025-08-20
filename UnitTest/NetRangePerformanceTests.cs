using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangePerformanceTests
{
    [Fact]
    public void Performance_LargeSubnetGeneration_CompletesInReasonableTime()
    {
        var range = new NetRangeV4("10.0.0.0/8");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var subnets = range.GetSubnets(16).Take(100).ToList();

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete in less than 1 second
        Assert.Equal(100, subnets.Count);
    }

    [Fact]
    public void Performance_ContainsCheck_FastForManyAddresses()
    {
        var range = new NetRangeV4("192.168.0.0/16");
        var addresses = new[]
        {
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.100.100"),
            IPAddress.Parse("192.168.255.255"),
            IPAddress.Parse("10.0.0.1"), // Outside range
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            foreach (var addr in addresses)
            {
                range.Contains(addr);
            }
        }

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should be very fast
    }
}