using System.Net;
using System.Numerics;
using NetRangeManager.Models;

namespace UnitTest;

public partial class NetRangeV6Tests
{
    [ Fact ]
    public void Constructor_ShouldCalculatePropertiesCorrectly()
    {
        const string cidr = "2001:db8:acad::/48";
        var expectedNetworkAddress = IPAddress.Parse ( "2001:db8:acad::" );
        var expectedLastAddress = IPAddress.Parse ( "2001:db8:acad:ffff:ffff:ffff:ffff:ffff" );
        var expectedTotalAddresses = BigInteger.Pow ( 2 , 128 - 48 );
        var range = new NetRangeV6 ( cidr );
        Assert.Equal ( expectedNetworkAddress , range.NetworkAddress );
        Assert.Equal ( 48 , range.CidrPrefix );
        Assert.Equal ( expectedLastAddress , range.LastAddressInRange );
        Assert.Equal ( expectedTotalAddresses , range.TotalAddresses );
        Assert.False ( range.IsHost );
    }

    [ Theory ]
    [ InlineData ( "2001:db8:acad:1:ffff:ffff:ffff:ffff" , true ) ]
    [ InlineData ( "2001:db8:beef::1" , false ) ]
    [ InlineData ( "2001:db8:acad::" , true ) ]
    [ InlineData ( "2001:db8:acad:ffff:ffff:ffff:ffff:ffff" , true ) ]
    public void Contains_ShouldReturnExpectedResult ( string ipAddressToTest , bool expectedResult )
    {
        var range = new NetRangeV6 ( "2001:db8:acad::/48" );
        var ipAddress = IPAddress.Parse ( ipAddressToTest );
        var actualResult = range.Contains ( ipAddress );
        Assert.Equal ( expectedResult , actualResult );
    }

    [ Theory ]
    [ InlineData ( "2001:db8::/32" , "2001:db8:acad::/48" , true , true , true ) ]
    [ InlineData ( "2001:db8:acad::/48" , "2001:db8::/32" , true , false , false ) ]
    [ InlineData ( "2001:db8::/32" , "2001:db9::/32" , false , false , false ) ]
    public void RelationshipTests_ShouldReturnExpectedResults ( string rangeACidr , string rangeBCidr , bool shouldOverlap , bool bShouldBeSubnetOfA , bool aShouldBeSupernetOfB )
    {
        var rangeA = new NetRangeV6 ( rangeACidr );
        var rangeB = new NetRangeV6 ( rangeBCidr );
        var actualOverlap = rangeA.OverlapsWith ( rangeB );
        var actualIsSubnet = rangeB.IsSubnetOf ( rangeA );
        var actualIsSupernet = rangeA.IsSupernetOf ( rangeB );
        Assert.Equal ( shouldOverlap , actualOverlap );
        Assert.Equal ( bShouldBeSubnetOfA , actualIsSubnet );
        Assert.Equal ( aShouldBeSupernetOfB , actualIsSupernet );
    }
}
