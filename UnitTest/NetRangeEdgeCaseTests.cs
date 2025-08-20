using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

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

        // Host networks cannot be subdivided
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

        // Should contain any address of the same family
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
        var p2p = new NetRangeV4("192.168.1.0/31");

        Assert.Equal(31, p2p.CidrPrefix);
        Assert.Equal(2, (int)p2p.TotalAddresses);
        Assert.False(p2p.IsHost);

        // In /31 networks, both addresses are usable (RFC 3021)
        Assert.Equal(p2p.NetworkAddress, p2p.FirstUsableAddress);
        Assert.Equal(p2p.NetworkAddress, p2p.LastUsableAddress);

        Assert.True(p2p.Contains(IPAddress.Parse("192.168.1.0")));
        Assert.True(p2p.Contains(IPAddress.Parse("192.168.1.1")));
        Assert.False(p2p.Contains(IPAddress.Parse("192.168.1.2")));
    }

    [Fact]
    public void EdgeCase_LargeIPv6Subnets_DontCauseOverflow()
    {
        var range = new NetRangeV6("2001:db8::/64");

        // This should work without overflow
        var subnets = range.GetSubnets(128).Take(1000).ToList();

        Assert.Equal(1000, subnets.Count);
        Assert.All(subnets, s => Assert.True(s.IsHost));
    }

    [Fact]
    public void EdgeCase_BoundaryAddresses_HandledCorrectly()
    {
        var range = new NetRangeV4("192.168.1.0/24");

        // Test boundary addresses
        Assert.True(range.Contains(IPAddress.Parse("192.168.1.0")));   // Network address
        Assert.True(range.Contains(IPAddress.Parse("192.168.1.255"))); // Broadcast address
        Assert.False(range.Contains(IPAddress.Parse("192.168.0.255"))); // Just before
        Assert.False(range.Contains(IPAddress.Parse("192.168.2.0")));   // Just after
    }
}