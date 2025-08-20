using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeValidationTests
{
    [ Theory ]
    [ InlineData ( "192.168.1.0/24" ) ]
    [ InlineData ( "10.0.0.0/8" ) ]
    [ InlineData ( "172.16.0.0/12" ) ]
    [ InlineData ( "0.0.0.0/0" ) ]
    [ InlineData ( "255.255.255.255/32" ) ]
    public void Validation_WellKnownIPv4Networks_ParseCorrectly ( string cidr )
    {
        var parseSuccess = NetRangeV4.TryParse ( cidr , out var range );
        Assert.True ( parseSuccess );
        var constructorRange = new NetRangeV4 ( cidr );
        Assert.Equal ( range , constructorRange );
    }

    [ Theory ]
    [ InlineData ( "2001:db8::/32" ) ]
    [ InlineData ( "fe80::/10" ) ]
    [ InlineData ( "fc00::/7" ) ]
    [ InlineData ( "::/0" ) ]
    [ InlineData ( "::1/128" ) ]
    public void Validation_WellKnownIPv6Networks_ParseCorrectly ( string cidr )
    {
        var parseSuccess = NetRangeV6.TryParse ( cidr , out var range );
        Assert.True ( parseSuccess );
        var constructorRange = new NetRangeV6 ( cidr );
        Assert.Equal ( range , constructorRange );
    }

    [ Fact ]
    public void Validation_ConsistencyBetweenConstructorAndTryParse()
    {
        var testCases = new[] { "192.168.1.0/24" , "10.0.0.0/8" , "127.0.0.1/32" };

        foreach ( var cidr in testCases ) {
            var success = NetRangeV4.TryParse ( cidr , out var parsedRange );
            Assert.True ( success );
            var constructedRange = new NetRangeV4 ( cidr );
            Assert.Equal ( parsedRange.NetworkAddress , constructedRange.NetworkAddress );
            Assert.Equal ( parsedRange.CidrPrefix , constructedRange.CidrPrefix );
            Assert.Equal ( parsedRange.TotalAddresses , constructedRange.TotalAddresses );
        }
    }
}
