using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeIntegrationTests
{
    [Fact]
    public void Integration_NetworkSubdivision_WorksCorrectly()
    {
        var mainNetwork = new NetRangeV4("192.168.1.0/24");
        var subnets = mainNetwork.GetSubnets(26).ToList();

        Assert.Equal(4, subnets.Count);

        foreach (var subnet in subnets)
        {
            Assert.True(subnet.IsSubnetOf(mainNetwork));
            Assert.True(mainNetwork.IsSupernetOf(subnet));
        }

        for (int i = 0; i < subnets.Count; i++)
        {
            for (int j = i + 1; j < subnets.Count; j++)
            {
                Assert.False(subnets[i].OverlapsWith(subnets[j]));
            }
        }

        var supernet = mainNetwork.GetSupernet(16);
        Assert.Equal(new NetRangeV4("192.168.0.0/16"), supernet);
        Assert.True(mainNetwork.IsSubnetOf(supernet));
    }

    [Fact]
    public void Integration_IPv6NetworkOperations_WorkCorrectly()
    {
        var ispNetwork = new NetRangeV6("2001:db8::/48");
        var customerNetworks = ispNetwork.GetSubnets(56).Take(10).ToList();

        Assert.Equal(10, customerNetworks.Count);

        var firstCustomer = customerNetworks[0];
        var customerSubnets = firstCustomer.GetSubnets(64).Take(5).ToList();

        Assert.Equal(5, customerSubnets.Count);

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

        Assert.True(ipv4Range.Contains(ipv4Address));
        Assert.False(ipv4Range.Contains(ipv6Address));
        Assert.True(ipv6Range.Contains(ipv6Address));
        Assert.False(ipv6Range.Contains(ipv4Address));
    }
}