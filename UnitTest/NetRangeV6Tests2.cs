using System.Net;
using System.Numerics;
using NetRangeManager.Models;

namespace UnitTest;

public partial class NetRangeV6Tests2
{
    #region Konstruktor Tests
    [ Fact ]
    public void Constructor_ValidCidr_CreatesCorrectInstance()
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Equal ( IPAddress.Parse ( "2001:db8::" ) , range.NetworkAddress );
        Assert.Equal ( 32 , range.CidrPrefix );
        Assert.Equal ( BigInteger.Pow ( 2 , 96 ) , range.TotalAddresses );
        Assert.False ( range.IsHost );
    }

    [ Fact ]
    public void Constructor_ValidIpAndPrefix_CreatesCorrectInstance()
    {
        var ip = IPAddress.Parse ( "2001:db8::1" );
        var range = new NetRangeV6 ( ip , 64 );
        Assert.Equal ( IPAddress.Parse ( "2001:db8::" ) , range.NetworkAddress );
        Assert.Equal ( 64 , range.CidrPrefix );
        Assert.False ( range.IsHost );
    }

    [ Fact ]
    public void Constructor_HostAddress_MarksAsHost()
    {
        var range = new NetRangeV6 ( "2001:db8::1/128" );
        Assert.True ( range.IsHost );
        Assert.Equal ( 1 , (int) range.TotalAddresses );
    }

    [ Theory ]
    [ InlineData ( null ) ]
    [ InlineData ( "" ) ]
    [ InlineData ( " " ) ]
    public void Constructor_NullOrEmptyCidr_ThrowsArgumentNullException ( string cidr ) { Assert.Throws< ArgumentNullException > ( () => new NetRangeV6 ( cidr ) ); }

    [ Theory ]
    [ InlineData ( "2001:db8::" ) ]
    [ InlineData ( "2001:db8::/" ) ]
    [ InlineData ( "/64" ) ]
    [ InlineData ( "2001:db8::/129" ) ]
    [ InlineData ( "2001:db8::/-1" ) ]
    [ InlineData ( "192.168.1.0/24" ) ]
    public void Constructor_InvalidCidr_ThrowsArgumentException ( string cidr ) { Assert.Throws< ArgumentException > ( () => new NetRangeV6 ( cidr ) ); }

    [ Fact ]
    public void Constructor_NullIpAddress_ThrowsArgumentNullException() { Assert.Throws< ArgumentNullException > ( () => new NetRangeV6 ( null! , 64 ) ); }

    [ Fact ]
    public void Constructor_IPv4Address_ThrowsArgumentException()
    {
        var ipv4 = IPAddress.Parse ( "192.168.1.1" );
        Assert.Throws< ArgumentException > ( () => new NetRangeV6 ( ipv4 , 64 ) );
    }

    [ Theory ]
    [ InlineData ( -1 ) ]
    [ InlineData ( 129 ) ]
    public void Constructor_InvalidPrefix_ThrowsArgumentOutOfRangeException ( int prefix )
    {
        var ip = IPAddress.Parse ( "2001:db8::1" );
        Assert.Throws< ArgumentOutOfRangeException > ( () => new NetRangeV6 ( ip , prefix ) );
    }
    #endregion

    #region Property Tests
    [ Fact ]
    public void Properties_StandardNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV6 ( "2001:db8::/64" );
        Assert.Equal ( IPAddress.Parse ( "2001:db8::" ) , range.NetworkAddress );
        Assert.Equal ( IPAddress.Parse ( "2001:db8::" ) , range.FirstUsableAddress );
        Assert.Equal ( IPAddress.Parse ( "2001:db8::ffff:ffff:ffff:ffff" ) , range.LastUsableAddress );
        Assert.Equal ( range.LastUsableAddress , range.LastAddressInRange );
    }

    [ Fact ]
    public void IsLoopback_DetectsLoopbackAddress()
    {
        var loopback = new NetRangeV6 ( "::1/128" );
        var notLoopback = new NetRangeV6 ( "2001:db8::1/128" );
        Assert.True ( loopback.IsLoopback );
        Assert.False ( notLoopback.IsLoopback );
    }

    [ Theory ]
    [ InlineData ( "fe80::/10" , true ) ]
    [ InlineData ( "fe80:1234::/64" , true ) ]
    [ InlineData ( "2001:db8::/32" , false ) ]
    public void IsLinkLocal_DetectsLinkLocalAddresses ( string cidr , bool expected )
    {
        var range = new NetRangeV6 ( cidr );
        Assert.Equal ( expected , range.IsLinkLocal );
    }

    [ Theory ]
    [ InlineData ( "fc00::/7" , true ) ]
    [ InlineData ( "fd00:1234::/32" , true ) ]
    [ InlineData ( "2001:db8::/32" , false ) ]
    public void IsUniqueLocal_DetectsUniqueLocalAddresses ( string cidr , bool expected )
    {
        var range = new NetRangeV6 ( cidr );
        Assert.Equal ( expected , range.IsUniqueLocal );
    }
    #endregion

    #region Contains Tests
    [ Theory ]
    [ InlineData ( "2001:db8::/32" , "2001:db8::1" , true ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db8:ffff:ffff:ffff:ffff:ffff:ffff" , true ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db9::1" , false ) ]
    [ InlineData ( "2001:db8::/64" , "2001:db8::1" , true ) ]
    [ InlineData ( "2001:db8::/64" , "2001:db8:0:1::1" , false ) ]
    public void Contains_IPv6Address_ReturnsCorrectResult ( string cidr , string testIp , bool expected )
    {
        var range = new NetRangeV6 ( cidr );
        var ip = IPAddress.Parse ( testIp );
        Assert.Equal ( expected , range.Contains ( ip ) );
    }

    [ Fact ]
    public void Contains_IPv4Address_ReturnsFalse()
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        var ipv4 = IPAddress.Parse ( "192.168.1.1" );
        Assert.False ( range.Contains ( ipv4 ) );
    }

    [ Fact ]
    public void Contains_NullAddress_ThrowsArgumentNullException()
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Throws< ArgumentNullException > ( () => range.Contains ( null! ) );
    }
    #endregion

    #region Overlap Tests
    [ Theory ]
    [ InlineData ( "2001:db8::/32" , "2001:db8::/64" , true ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db8:1::/64" , true ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db9::/32" , false ) ]
    public void OverlapsWith_ReturnsCorrectResult ( string cidr1 , string cidr2 , bool expected )
    {
        var range1 = new NetRangeV6 ( cidr1 );
        var range2 = new NetRangeV6 ( cidr2 );
        Assert.Equal ( expected , range1.OverlapsWith ( range2 ) );
        Assert.Equal ( expected , range2.OverlapsWith ( range1 ) );
    }
    #endregion

    #region Subnet/Supernet Tests
    [ Theory ]
    [ InlineData ( "2001:db8::/64" , "2001:db8::/32" , true ) ]
    [ InlineData ( "2001:db8::/64" , "2001:db8::/64" , true ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db8::/64" , false ) ]
    public void IsSubnetOf_ReturnsCorrectResult ( string subnet , string supernet , bool expected )
    {
        var subRange = new NetRangeV6 ( subnet );
        var superRange = new NetRangeV6 ( supernet );
        Assert.Equal ( expected , subRange.IsSubnetOf ( superRange ) );
    }
    #endregion

    #region Subnet Generation Tests
    [ Fact ]
    public void GetSubnets_ValidPrefix_GeneratesCorrectSubnets()
    {
        var range = new NetRangeV6 ( "2001:db8::/62" );
        var subnets = range.GetSubnets ( 64 ).ToList();
        Assert.Equal ( 4 , subnets.Count );
        Assert.Equal ( new NetRangeV6 ( "2001:db8::/64" ) , subnets[0] );
        Assert.Equal ( new NetRangeV6 ( "2001:db8:0:1::/64" ) , subnets[1] );
        Assert.Equal ( new NetRangeV6 ( "2001:db8:0:2::/64" ) , subnets[2] );
        Assert.Equal ( new NetRangeV6 ( "2001:db8:0:3::/64" ) , subnets[3] );
    }

    [ Theory ]
    [ InlineData ( 32 ) ]
    [ InlineData ( 31 ) ]
    [ InlineData ( 129 ) ]
    public void GetSubnets_InvalidPrefix_ThrowsArgumentOutOfRangeException ( int newPrefix )
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Throws< ArgumentOutOfRangeException > ( () => range.GetSubnets ( newPrefix ).ToList() );
    }

    [ Fact ]
    public void GetSubnets_TooManySubnets_ThrowsArgumentException()
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Throws< ArgumentException > ( () => range.GetSubnets ( 96 ).ToList() );
    }

    [ Fact ]
    public void GetSupernet_ValidPrefix_GeneratesCorrectSupernet()
    {
        var range = new NetRangeV6 ( "2001:db8::/64" );
        var supernet = range.GetSupernet ( 32 );
        Assert.Equal ( new NetRangeV6 ( "2001:db8::/32" ) , supernet );
    }
    #endregion

    #region Edge Case Tests
    [ Fact ]
    public void EdgeCases_ZeroPrefix_HandledCorrectly()
    {
        var range = new NetRangeV6 ( "::/0" );
        Assert.Equal ( 0 , range.CidrPrefix );
        Assert.Equal ( IPAddress.IPv6Any , range.NetworkAddress );
        Assert.Equal ( BigInteger.Pow ( 2 , 128 ) , range.TotalAddresses );
    }

    [ Fact ]
    public void EdgeCases_MaxPrefix_HandledCorrectly()
    {
        var range = new NetRangeV6 ( "2001:db8::1/128" );
        Assert.Equal ( 128 , range.CidrPrefix );
        Assert.True ( range.IsHost );
        Assert.Equal ( 1 , (int) range.TotalAddresses );
    }
    #endregion

    #region Parsing Tests
    [ Theory ]
    [ InlineData ( "2001:db8::/32" , true ) ]
    [ InlineData ( "::/0" , true ) ]
    [ InlineData ( "::1/128" , true ) ]
    public void TryParse_ValidCidr_ReturnsTrue ( string cidr , bool expected )
    {
        var result = NetRangeV6.TryParse ( cidr , out var range );
        Assert.Equal ( expected , result );

        if ( expected ) { Assert.NotEqual ( default , range ); }
    }

    [ Theory ]
    [ InlineData ( null ) ]
    [ InlineData ( "" ) ]
    [ InlineData ( "2001:db8::" ) ]
    [ InlineData ( "2001:db8::/" ) ]
    [ InlineData ( "/64" ) ]
    [ InlineData ( "2001:db8::/129" ) ]
    [ InlineData ( "192.168.1.0/24" ) ]
    public void TryParse_InvalidCidr_ReturnsFalse ( string cidr )
    {
        var result = NetRangeV6.TryParse ( cidr , out var range );
        Assert.False ( result );
        Assert.Equal ( default , range );
    }
    #endregion

    #region ToString Tests
    [ Fact ]
    public void ToString_ReturnsCorrectFormat()
    {
        var range = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Equal ( "2001:db8::/32" , range.ToString() );
    }
    #endregion
}
