using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeV4Tests2
{
    [ Fact ]
    public void Constructor_ShouldCalculatePropertiesCorrectly_ForClassC()
    {
        const string cidr = "192.168.1.0/24";
        var expectedNetworkAddress = IPAddress.Parse ( "192.168.1.0" );
        var expectedFirstUsable = IPAddress.Parse ( "192.168.1.1" );
        var expectedLastUsable = IPAddress.Parse ( "192.168.1.254" );
        var expectedBroadcast = IPAddress.Parse ( "192.168.1.255" );
        var range = new NetRangeV4 ( cidr );
        Assert.Equal ( expectedNetworkAddress , range.NetworkAddress );
        Assert.Equal ( 24 , range.CidrPrefix );
        Assert.Equal ( expectedFirstUsable , range.FirstUsableAddress );
        Assert.Equal ( expectedLastUsable , range.LastUsableAddress );
        Assert.Equal ( expectedBroadcast , range.LastAddressInRange );
        Assert.Equal ( 256 , range.TotalAddresses );
        Assert.False ( range.IsHost );
    }

    [ Theory ]
    [ InlineData ( "192.168.1.150" , true ) ]
    [ InlineData ( "10.0.0.5" , false ) ]
    [ InlineData ( "192.168.1.0" , true ) ]
    [ InlineData ( "192.168.1.255" , true ) ]
    [ InlineData ( "192.168.0.255" , false ) ]
    [ InlineData ( "192.168.2.0" , false ) ]
    public void Contains_ShouldReturnExpectedResult ( string ipAddressToTest , bool expectedResult )
    {
        var range = new NetRangeV4 ( "192.168.1.0/24" );
        var ipAddress = IPAddress.Parse ( ipAddressToTest );
        var actualResult = range.Contains ( ipAddress );
        Assert.Equal ( expectedResult , actualResult );
    }

    [ Theory ]
    [ InlineData ( "10.0.0.0/16" , "10.0.10.0/24" , true , true , true ) ]
    [ InlineData ( "10.0.10.0/24" , "10.0.0.0/16" , true , false , false ) ]
    [ InlineData ( "10.0.0.0/16" , "10.0.255.0/24" , true , true , true ) ]
    [ InlineData ( "10.0.0.0/23" , "10.0.1.128/25" , true , true , true ) ]
    [ InlineData ( "192.168.1.0/24" , "192.168.2.0/24" , false , false , false ) ]
    [ InlineData ( "172.16.0.0/24" , "172.16.0.128/25" , true , true , true ) ]
    public void RelationshipTests_ShouldReturnExpectedResults ( string rangeACidr , string rangeBCidr , bool shouldOverlap , bool bShouldBeSubnetOfA , bool aShouldBeSupernetOfB )
    {
        var rangeA = new NetRangeV4 ( rangeACidr );
        var rangeB = new NetRangeV4 ( rangeBCidr );
        var actualOverlap = rangeA.OverlapsWith ( rangeB );
        var actualIsSubnet = rangeB.IsSubnetOf ( rangeA );
        var actualIsSupernet = rangeA.IsSupernetOf ( rangeB );
        Assert.Equal ( shouldOverlap , actualOverlap );
        Assert.Equal ( bShouldBeSubnetOfA , actualIsSubnet );
        Assert.Equal ( aShouldBeSupernetOfB , actualIsSupernet );
    }
}
