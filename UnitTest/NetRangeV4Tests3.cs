using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public partial class NetRangeV4Tests3
{
    #region Konstruktor Tests
    [ Fact ]
    public void Constructor_ValidCidr_CreatesCorrectInstance()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.0" ) , range.NetworkAddress );
        Assert.Equal ( 24 , range.CidrPrefix );
        Assert.Equal ( 256 , (int) range.TotalAddresses );
        Assert.False ( range.IsHost );
    }

    [ Fact ]
    public void Constructor_ValidIpAndPrefix_CreatesCorrectInstance()
    {
        var ip = IPAddress.Parse ( "10.0.0.0" );
        var range = new NetRangeV4 ( ip , 8 );
        Assert.Equal ( ip , range.NetworkAddress );
        Assert.Equal ( 8 , range.CidrPrefix );
        Assert.Equal ( 16777216 , (int) range.TotalAddresses );
        Assert.False ( range.IsHost );
    }

    [ Fact ]
    public void Constructor_HostAddress_MarksAsHost()
    {
        var range = new NetRangeV4 ( "192.168.1.100/32" );
        Assert.True ( range.IsHost );
        Assert.Equal ( 1 , (int) range.TotalAddresses );
        Assert.Equal ( range.NetworkAddress , range.FirstUsableAddress );
        Assert.Equal ( range.NetworkAddress , range.LastUsableAddress );
    }

    [ Theory ]
    [ InlineData ( null ) ]
    [ InlineData ( "" ) ]
    [ InlineData ( " " ) ]
    public void Constructor_NullOrEmptyCidr_ThrowsArgumentNullException ( string cidr ) { Assert.Throws< ArgumentNullException > ( () => new NetRangeV4 ( cidr ) ); }

    [ Theory ]
    [ InlineData ( "192.168.1.0" ) ]
    [ InlineData ( "192.168.1.0/" ) ]
    [ InlineData ( "/24" ) ]
    [ InlineData ( "192.168.1.0/33" ) ]
    [ InlineData ( "192.168.1.0/-1" ) ]
    [ InlineData ( "256.1.1.1/24" ) ]
    [ InlineData ( "2001:db8::/64" ) ]
    public void Constructor_InvalidCidr_ThrowsArgumentException ( string cidr ) { Assert.Throws< ArgumentException > ( () => new NetRangeV4 ( cidr ) ); }

    [ Fact ]
    public void Constructor_NullIpAddress_ThrowsArgumentNullException() { Assert.Throws< ArgumentNullException > ( () => new NetRangeV4 ( null! , 24 ) ); }

    [ Fact ]
    public void Constructor_IPv6Address_ThrowsArgumentException()
    {
        var ipv6 = IPAddress.Parse ( "2001:db8::1" );
        Assert.Throws< ArgumentException > ( () => new NetRangeV4 ( ipv6 , 24 ) );
    }

    [ Theory ]
    [ InlineData ( -1 ) ]
    [ InlineData ( 33 ) ]
    public void Constructor_InvalidPrefix_ThrowsArgumentOutOfRangeException ( int prefix )
    {
        var ip = IPAddress.Parse ( "192.168.1.0" );
        Assert.Throws< ArgumentOutOfRangeException > ( () => new NetRangeV4 ( ip , prefix ) );
    }
    #endregion

    #region Property Tests
    [ Fact ]
    public void Properties_StandardNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.0" ) , range.NetworkAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.1" ) , range.FirstUsableAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.254" ) , range.LastUsableAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.255" ) , range.LastAddressInRange );
    }

    [ Fact ]
    public void Properties_PointToPointNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV4 ( "192.168.1.0/31" );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.0" ) , range.NetworkAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.0" ) , range.FirstUsableAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.0" ) , range.LastUsableAddress );
        Assert.Equal ( IPAddress.Parse ( "192.168.1.1" ) , range.LastAddressInRange );
    }

    [ Theory ]
    [ InlineData ( "10.0.0.0/8" , true ) ]
    [ InlineData ( "172.16.0.0/12" , true ) ]
    [ InlineData ( "192.168.0.0/16" , true ) ]
    [ InlineData ( "172.15.0.0/16" , false ) ]
    [ InlineData ( "8.8.8.8/32" , false ) ]
    public void IsPrivateRange_DetectsRFC1918Networks ( string cidr , bool expected )
    {
        var range = new NetRangeV4 ( cidr );
        Assert.Equal ( expected , range.IsPrivateRange );
    }

    [ Theory ]
    [ InlineData ( "127.0.0.1/32" , true ) ]
    [ InlineData ( "127.1.1.1/32" , true ) ]
    [ InlineData ( "192.168.1.1/32" , false ) ]
    public void IsLoopback_DetectsLoopbackAddresses ( string cidr , bool expected )
    {
        var range = new NetRangeV4 ( cidr );
        Assert.Equal ( expected , range.IsLoopback );
    }
    #endregion

    #region Contains Tests
    [ Theory ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.100" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.0" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.255" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.2.1" , false ) ]
    [ InlineData ( "10.0.0.0/8" , "10.255.255.255" , true ) ]
    [ InlineData ( "10.0.0.0/8" , "11.0.0.1" , false ) ]
    public void Contains_IPv4Address_ReturnsCorrectResult ( string cidr , string testIp , bool expected )
    {
        var range = new NetRangeV4 ( cidr );
        var ip = IPAddress.Parse ( testIp );
        Assert.Equal ( expected , range.Contains ( ip ) );
    }

    [ Fact ]
    public void Contains_IPv6Address_ReturnsFalse()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        var ipv6 = IPAddress.Parse ( "2001:db8::1" );
        Assert.False ( range.Contains ( ipv6 ) );
    }

    [ Fact ]
    public void Contains_NullAddress_ThrowsArgumentNullException()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Throws< ArgumentNullException > ( () => range.Contains ( null! ) );
    }
    #endregion

    #region Overlap Tests
    [ Theory ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.0/25" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.128/25" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.2.0/24" , false ) ]
    [ InlineData ( "10.0.0.0/8" , "192.168.1.0/24" , false ) ]
    [ InlineData ( "192.168.0.0/16" , "192.168.1.0/24" , true ) ]
    public void OverlapsWith_ReturnsCorrectResult ( string cidr1 , string cidr2 , bool expected )
    {
        var range1 = new NetRangeV4 ( cidr1 );
        var range2 = new NetRangeV4 ( cidr2 );
        Assert.Equal ( expected , range1.OverlapsWith ( range2 ) );
        Assert.Equal ( expected , range2.OverlapsWith ( range1 ) );
    }
    #endregion

    #region Subnet/Supernet Tests
    [ Theory ]
    [ InlineData ( "192.168.1.0/24" , "192.168.0.0/16" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.0/24" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.0/25" , false ) ]
    [ InlineData ( "192.168.1.0/24" , "10.0.0.0/8" , false ) ]
    public void IsSubnetOf_ReturnsCorrectResult ( string subnet , string supernet , bool expected )
    {
        var subRange = new NetRangeV4 ( subnet );
        var superRange = new NetRangeV4 ( supernet );
        Assert.Equal ( expected , subRange.IsSubnetOf ( superRange ) );
    }

    [ Theory ]
    [ InlineData ( "192.168.0.0/16" , "192.168.1.0/24" , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.1.0/24" , true ) ]
    [ InlineData ( "192.168.1.0/25" , "192.168.1.0/24" , false ) ]
    public void IsSupernetOf_ReturnsCorrectResult ( string supernet , string subnet , bool expected )
    {
        var superRange = new NetRangeV4 ( supernet );
        var subRange = new NetRangeV4 ( subnet );
        Assert.Equal ( expected , superRange.IsSupernetOf ( subRange ) );
    }
    #endregion

    #region Subnet Generation Tests
    [ Fact ]
    public void GetSubnets_ValidPrefix_GeneratesCorrectSubnets()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        var subnets = range.GetSubnets ( 26 ).ToList();
        Assert.Equal ( 4 , subnets.Count );
        Assert.Equal ( new NetRangeV4 ( "192.168.1.0/26" ) , subnets[0] );
        Assert.Equal ( new NetRangeV4 ( "192.168.1.64/26" ) , subnets[1] );
        Assert.Equal ( new NetRangeV4 ( "192.168.1.128/26" ) , subnets[2] );
        Assert.Equal ( new NetRangeV4 ( "192.168.1.192/26" ) , subnets[3] );
    }

    [ Theory ]
    [ InlineData ( 24 ) ]
    [ InlineData ( 23 ) ]
    [ InlineData ( 33 ) ]
    public void GetSubnets_InvalidPrefix_ThrowsArgumentOutOfRangeException ( int newPrefix )
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Throws< ArgumentOutOfRangeException > ( () => range.GetSubnets ( newPrefix ).ToList() );
    }

    [ Fact ]
    public void GetSupernet_ValidPrefix_GeneratesCorrectSupernet()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        var supernet = range.GetSupernet ( 16 );
        Assert.Equal ( new NetRangeV4 ( "192.168.0.0/16" ) , supernet );
    }

    [ Theory ]
    [ InlineData ( 24 ) ]
    [ InlineData ( 25 ) ]
    [ InlineData ( -1 ) ]
    public void GetSupernet_InvalidPrefix_ThrowsArgumentOutOfRangeException ( int newPrefix )
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Throws< ArgumentOutOfRangeException > ( () => range.GetSupernet ( newPrefix ) );
    }
    #endregion

    #region Parsing Tests
    [ Theory ]
    [ InlineData ( "192.168.1.0/24" , true ) ]
    [ InlineData ( "10.0.0.0/8" , true ) ]
    [ InlineData ( "255.255.255.255/32" , true ) ]
    [ InlineData ( "0.0.0.0/0" , true ) ]
    public void TryParse_ValidCidr_ReturnsTrue ( string cidr , bool expected )
    {
        var result = NetRangeV4.TryParse ( cidr , out var range );
        Assert.Equal ( expected , result );

        if ( expected ) { Assert.NotEqual ( default , range ); }
    }

    [ Theory ]
    [ InlineData ( null ) ]
    [ InlineData ( "" ) ]
    [ InlineData ( "192.168.1.0" ) ]
    [ InlineData ( "192.168.1.0/" ) ]
    [ InlineData ( "/24" ) ]
    [ InlineData ( "192.168.1.0/33" ) ]
    [ InlineData ( "256.1.1.1/24" ) ]
    [ InlineData ( "2001:db8::/64" ) ]
    public void TryParse_InvalidCidr_ReturnsFalse ( string cidr )
    {
        var result = NetRangeV4.TryParse ( cidr , out var range );
        Assert.False ( result );
        Assert.Equal ( default , range );
    }
    #endregion

    #region Comparison Tests
    [ Fact ]
    public void CompareTo_SameNetwork_ComparesByPrefix()
    {
        var range1 = new NetRangeV4 ( "192.168.1.0/24" );
        var range2 = new NetRangeV4 ( "192.168.1.0/25" );
        Assert.True ( range1.CompareTo ( range2 ) < 0 );
        Assert.True ( range2.CompareTo ( range1 ) > 0 );
    }

    [ Fact ]
    public void CompareTo_DifferentNetworks_ComparesByAddress()
    {
        var range1 = new NetRangeV4 ( "192.168.1.0/24" );
        var range2 = new NetRangeV4 ( "192.168.2.0/24" );
        Assert.True ( range1.CompareTo ( range2 ) < 0 );
        Assert.True ( range2.CompareTo ( range1 ) > 0 );
    }

    [ Fact ]
    public void Equals_SameRange_ReturnsTrue()
    {
        var range1 = new NetRangeV4 ( "192.168.1.0/24" );
        var range2 = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.True ( range1.Equals ( range2 ) );
        Assert.True ( range1 == range2 );
    }

    [ Fact ]
    public void Equals_DifferentRange_ReturnsFalse()
    {
        var range1 = new NetRangeV4 ( "192.168.1.0/24" );
        var range2 = new NetRangeV4 ( "192.168.1.0/25" );
        Assert.False ( range1.Equals ( range2 ) );
        Assert.False ( range1 == range2 );
    }
    #endregion

    #region ToString Tests
    [ Fact ]
    public void ToString_ReturnsCorrectFormat()
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Equal ( "192.168.1.0/24" , range.ToString() );
    }
    #endregion
}
