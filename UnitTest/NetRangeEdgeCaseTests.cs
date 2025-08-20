using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeEdgeCaseTests
{
    [ Fact ]
    public void EdgeCase_SingleHostNetworks_WorkCorrectly()
    {
        var ipv4Host = new NetRangeV4 ( "192.168.1.100/32" );
        var ipv6Host = new NetRangeV6 ( "2001:db8::1/128" );
        Assert.True ( ipv4Host.IsHost );
        Assert.True ( ipv6Host.IsHost );
        Assert.Equal ( ipv4Host.NetworkAddress , ipv4Host.FirstUsableAddress );
        Assert.Equal ( ipv4Host.NetworkAddress , ipv4Host.LastUsableAddress );
        Assert.Equal ( ipv6Host.NetworkAddress , ipv6Host.FirstUsableAddress );
        Assert.Equal ( ipv6Host.NetworkAddress , ipv6Host.LastUsableAddress );
        Assert.Throws< ArgumentOutOfRangeException > ( () => ipv4Host.GetSubnets ( 33 ).ToList() );
        Assert.Throws< ArgumentOutOfRangeException > ( () => ipv6Host.GetSubnets ( 129 ).ToList() );
    }

    [ Fact ]
    public void EdgeCase_ZeroPrefixNetworks_WorkCorrectly()
    {
        var ipv4All = new NetRangeV4 ( "0.0.0.0/0" );
        var ipv6All = new NetRangeV6 ( "::/0" );
        Assert.Equal ( 0 , ipv4All.CidrPrefix );
        Assert.Equal ( 0 , ipv6All.CidrPrefix );
        Assert.Equal ( IPAddress.Any , ipv4All.NetworkAddress );
        Assert.Equal ( IPAddress.IPv6Any , ipv6All.NetworkAddress );
        Assert.True ( ipv4All.Contains ( IPAddress.Parse ( "192.168.1.1" ) ) );
        Assert.True ( ipv4All.Contains ( IPAddress.Parse ( "8.8.8.8" ) ) );
        Assert.False ( ipv4All.Contains ( IPAddress.Parse ( "2001:db8::1" ) ) );
        Assert.True ( ipv6All.Contains ( IPAddress.Parse ( "2001:db8::1" ) ) );
        Assert.True ( ipv6All.Contains ( IPAddress.IPv6Loopback ) );
        Assert.False ( ipv6All.Contains ( IPAddress.Parse ( "192.168.1.1" ) ) );
    }

    [ Fact ]
    public void EdgeCase_Point2PointNetworks_WorkCorrectly()
    {
        var p2P = new NetRangeV4 ( "192.168.1.0/31" );
        Assert.Equal ( 31 , p2P.CidrPrefix );
        Assert.Equal ( 2 , (int) p2P.TotalAddresses );
        Assert.False ( p2P.IsHost );
        Assert.Equal ( p2P.NetworkAddress , p2P.FirstUsableAddress );
        Assert.Equal ( p2P.NetworkAddress , p2P.LastUsableAddress );
        Assert.True ( p2P.Contains ( IPAddress.Parse ( "192.168.1.0" ) ) );
        Assert.True ( p2P.Contains ( IPAddress.Parse ( "192.168.1.1" ) ) );
        Assert.False ( p2P.Contains ( IPAddress.Parse ( "192.168.1.2" ) ) );
    }

    [ Fact ]
    public void EdgeCase_LargeIPv6Subnets_DontCauseOverflow()
    {
        var range = new NetRangeV6 ( "2001:db8::/64" );
        var subnets = range.GetSubnets ( 128 ).Take ( 1000 ).ToList();
        Assert.Equal ( 1000 , subnets.Count );
        Assert.All ( subnets , s => Assert.True ( s.IsHost ) );
    }

    [ Fact ]
    public void EdgeCase_BoundaryAddresses_HandledCorrectly()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.True ( range.Contains ( IPAddress.Parse ( "192.168.1.0" ) ) );
        Assert.True ( range.Contains ( IPAddress.Parse ( "192.168.1.255" ) ) );
        Assert.False ( range.Contains ( IPAddress.Parse ( "192.168.0.255" ) ) );
        Assert.False ( range.Contains ( IPAddress.Parse ( "192.168.2.0" ) ) );
    }
}
