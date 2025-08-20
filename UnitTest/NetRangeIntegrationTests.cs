using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeIntegrationTests
{
    [Fact]
    public void Integration_NetworkSubdivision_WorksCorrectly()
    {
        // Start with a /24 network
        var mainNetwork = new NetRangeV4("192.168.1.0/24");

        // Subdivide into /26 subnets
        var subnets = mainNetwork.GetSubnets(26).ToList();

        Assert.Equal(4, subnets.Count);

        // Verify each subnet is properly contained in the main network
        foreach (var subnet in subnets)
        {
            Assert.True(subnet.IsSubnetOf(mainNetwork));
            Assert.True(mainNetwork.IsSupernetOf(subnet));
        }

        // Verify subnets don't overlap with each other
        for (int i = 0; i < subnets.Count; i++)
        {
            for (int j = i + 1; j < subnets.Count; j++)
            {
                Assert.False(subnets[i].OverlapsWith(subnets[j]));
            }
        }

        // Verify we can create a supernet
        var supernet = mainNetwork.GetSupernet(16);
        Assert.Equal(new NetRangeV4("192.168.0.0/16"), supernet);
        Assert.True(mainNetwork.IsSubnetOf(supernet));
    }

    [Fact]
    public void Integration_IPv6NetworkOperations_WorkCorrectly()
    {
        // Start with a /48 network (typical ISP allocation)
        var ispNetwork = new NetRangeV6("2001:db8::/48");

        // Create customer networks (/56)
        var customerNetworks = ispNetwork.GetSubnets(56).Take(10).ToList();

        Assert.Equal(10, customerNetworks.Count);

        // Each customer can create /64 subnets
        var firstCustomer = customerNetworks[0];
        var customerSubnets = firstCustomer.GetSubnets(64).Take(5).ToList();

        Assert.Equal(5, customerSubnets.Count);

        // Verify hierarchy
        foreach (var subnet in customerSubnets)
        {
            Assert.True(subnet.IsSubnetOf(firstCustomer));
            Assert.True(subnet.IsSubnetOf(ispNetwork));
        }
    }

    [Fact]
    public void Integration_MixedAddressFamilies_HandleCorrectly()
    {
        var ipv4Range = new NetRangeV4("192.168.1.0/24");
        var ipv6Range = new NetRangeV6("2001:db8::/64");

        var ipv4Address = IPAddress.Parse("192.168.1.100");
        var ipv6Address = IPAddress.Parse("2001:db8::1");

        // IPv4 range should contain IPv4 address but not IPv6
        Assert.True(ipv4Range.Contains(ipv4Address));
        Assert.False(ipv4Range.Contains(ipv6Address));

        // IPv6 range should contain IPv6 address but not IPv4
        Assert.True(ipv6Range.Contains(ipv6Address));
        Assert.False(ipv6Range.Contains(ipv4Address));
    }
}