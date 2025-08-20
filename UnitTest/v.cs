using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeCommonTests
{
    [Fact]
    public void HashCode_SameRanges_ProduceSameHashCode()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.1.0/24");
        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());

        var rangeV61 = new NetRangeV6("2001:db8::/32");
        var rangeV62 = new NetRangeV6("2001:db8::/32");
        Assert.Equal(rangeV61.GetHashCode(), rangeV62.GetHashCode());
    }

    [Fact]
    public void Collections_CanBeUsedInHashSets()
    {
        var set = new HashSet<NetRangeV4>
        {
            new("192.168.1.0/24"),
            new("192.168.1.0/24"),
            new("192.168.2.0/24")
        };
        Assert.Equal(2, set.Count);

        var setV6 = new HashSet<NetRangeV6>
        {
            new("2001:db8::/32"),
            new("2001:db8::/32"),
            new("2001:db8:1::/32")
        };
        Assert.Equal(2, setV6.Count);
    }

    [Fact]
    public void Collections_CanBeSorted()
    {
        var ranges = new List<NetRangeV4>
        {
            new("192.168.2.0/24"),
            new("192.168.1.0/24"),
            new("192.168.1.0/25")
        };
        ranges.Sort();

        Assert.Equal(new NetRangeV4("192.168.1.0/24"), ranges[0]);
        Assert.Equal(new NetRangeV4("192.168.1.0/25"), ranges[1]);
        Assert.Equal(new NetRangeV4("192.168.2.0/24"), ranges[2]);
    }
}

public class NetRangeEdgeCaseTests
{
    [Fact]
    public void EdgeCase_SingleHostNetworks_WorkCorrectly()
    {
        var ipv4Host = new NetRangeV4("192.168.1.100/32");
        var ipv6Host = new NetRangeV6("2001:db8::1/128");

        Assert.True(ipv4Host.IsHost);
        Assert.True(ipv6Host.IsHost);
        Assert.Equal(ipv4Host.NetworkAddress, ipv4Host.FirstUsableAddress);
        Assert.Equal(ipv4Host.NetworkAddress, ipv4Host.LastUsableAddress);
        Assert.Equal(ipv6Host.NetworkAddress, ipv6Host.FirstUsableAddress);
        Assert.Equal(ipv6Host.NetworkAddress, ipv6Host.LastUsableAddress);

        Assert.Throws<ArgumentOutOfRangeException>(() => ipv4Host.GetSubnets(33).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => ipv6Host.GetSubnets(129).ToList());
    }

    [Fact]
    public void EdgeCase_ZeroPrefixNetworks_WorkCorrectly()
    {
        var ipv4All = new NetRangeV4("0.0.0.0/0");
        var ipv6All = new NetRangeV6("::/0");

        Assert.Equal(0, ipv4All.CidrPrefix);
        Assert.Equal(0, ipv6All.CidrPrefix);
        Assert.Equal(IPAddress.Any, ipv4All.NetworkAddress);
        Assert.Equal(IPAddress.IPv6Any, ipv6All.NetworkAddress);

        Assert.True(ipv4All.Contains(IPAddress.Parse("192.168.1.1")));
        Assert.True(ipv4All.Contains(IPAddress.Parse("8.8.8.8")));
        Assert.False(ipv4All.Contains(IPAddress.Parse("2001:db8::1")));

        Assert.True(ipv6All.Contains(IPAddress.Parse("2001:db8::1")));
        Assert.True(ipv6All.Contains(IPAddress.IPv6Loopback));
        Assert.False(ipv6All.Contains(IPAddress.Parse("192.168.1.1")));
    }

    [Fact]
    public void EdgeCase_Point2PointNetworks_WorkCorrectly()
    {
        var p2P = new NetRangeV4("192.168.1.0/31");

        Assert.Equal(31, p2P.CidrPrefix);
        Assert.Equal(2, (int)p2P.TotalAddresses);
        Assert.False(p2P.IsHost);
        Assert.Equal(p2P.NetworkAddress, p2P.FirstUsableAddress);
        Assert.Equal(p2P.NetworkAddress, p2P.LastUsableAddress);

        Assert.True(p2P.Contains(IPAddress.Parse("192.168.1.0")));
        Assert.True(p2P.Contains(IPAddress.Parse("192.168.1.1")));
        Assert.False(p2P.Contains(IPAddress.Parse("192.168.1.2")));
    }

    [Fact]
    public void EdgeCase_LargeIPv6Subnets_DontCauseOverflow()
    {
        var range = new NetRangeV6("2001:db8::/64");
        var subnets = range.GetSubnets(128).Take(1000).ToList();

        Assert.Equal(1000, subnets.Count);
        Assert.All(subnets, s => Assert.True(s.IsHost));
    }

    [Fact]
    public void EdgeCase_BoundaryAddresses_HandledCorrectly()
    {
        var range = new NetRangeV4("192.168.1.0/24");

        Assert.True(range.Contains(IPAddress.Parse("192.168.1.0")));
        Assert.True(range.Contains(IPAddress.Parse("192.168.1.255")));
        Assert.False(range.Contains(IPAddress.Parse("192.168.0.255")));
        Assert.False(range.Contains(IPAddress.Parse("192.168.2.0")));
    }
}

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

public class NetRangePerformanceTests
{
    [Fact]
    public void Performance_LargeSubnetGeneration_CompletesInReasonableTime()
    {
        var range = new NetRangeV4("10.0.0.0/8");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var subnets = range.GetSubnets(16).Take(100).ToList();
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 1000);
        Assert.Equal(100, subnets.Count);
    }

    [Fact]
    public void Performance_ContainsCheck_FastForManyAddresses()
    {
        var range = new NetRangeV4("192.168.0.0/16");
        IPAddress?[] addresses = new[]
        {
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.100.100"),
            IPAddress.Parse("192.168.255.255"),
            IPAddress.Parse("10.0.0.1"),
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
        Assert.True(stopwatch.ElapsedMilliseconds < 1000);
    }
}

public class NetRangeValidationTests
{
    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("172.16.0.0/12")]
    [InlineData("0.0.0.0/0")]
    [InlineData("255.255.255.255/32")]
    public void Validation_WellKnownIPv4Networks_ParseCorrectly(string cidr)
    {
        var parseSuccess = NetRangeV4.TryParse(cidr, out var range);
        Assert.True(parseSuccess);

        var constructorRange = new NetRangeV4(cidr);
        Assert.Equal(range, constructorRange);
    }

    [Theory]
    [InlineData("2001:db8::/32")]
    [InlineData("fe80::/10")]
    [InlineData("fc00::/7")]
    [InlineData("::/0")]
    [InlineData("::1/128")]
    public void Validation_WellKnownIPv6Networks_ParseCorrectly(string cidr)
    {
        var parseSuccess = NetRangeV6.TryParse(cidr, out var range);
        Assert.True(parseSuccess);

        var constructorRange = new NetRangeV6(cidr);
        Assert.Equal(range, constructorRange);
    }

    [Fact]
    public void Validation_ConsistencyBetweenConstructorAndTryParse()
    {
        var testCases = new[] { "192.168.1.0/24", "10.0.0.0/8", "127.0.0.1/32" };

        foreach (var cidr in testCases)
        {
            var success = NetRangeV4.TryParse(cidr, out var parsedRange);
            Assert.True(success);

            var constructedRange = new NetRangeV4(cidr);
            Assert.Equal(parsedRange.NetworkAddress, constructedRange.NetworkAddress);
            Assert.Equal(parsedRange.CidrPrefix, constructedRange.CidrPrefix);
            Assert.Equal(parsedRange.TotalAddresses, constructedRange.TotalAddresses);
        }
    }
}