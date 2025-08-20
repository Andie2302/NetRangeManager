using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeCommonTests
{
    [ Fact ]
    public void HashCode_SameRanges_ProduceSameHashCode()
    {
        var range1 = new NetRangeV4 ( "192.168.1.0/24" );
        var range2 = new NetRangeV4 ( "192.168.1.0/24" );
        Assert.Equal ( range1.GetHashCode() , range2.GetHashCode() );
        var rangeV6_1 = new NetRangeV6 ( "2001:db8::/32" );
        var rangeV6_2 = new NetRangeV6 ( "2001:db8::/32" );
        Assert.Equal ( rangeV6_1.GetHashCode() , rangeV6_2.GetHashCode() );
    }

    [ Fact ]
    public void Collections_CanBeUsedInHashSets()
    {
        var set = new HashSet< NetRangeV4 > { new( "192.168.1.0/24" ) , new( "192.168.1.0/24" ) , new( "192.168.2.0/24" ) };
        Assert.Equal ( 2 , set.Count );
        var setV6 = new HashSet< NetRangeV6 > { new( "2001:db8::/32" ) , new( "2001:db8::/32" ) , new( "2001:db8:1::/32" ) };
        Assert.Equal ( 2 , setV6.Count );
    }

    [ Fact ]
    public void Collections_CanBeSorted()
    {
        var ranges = new List< NetRangeV4 > { new( "192.168.2.0/24" ) , new( "192.168.1.0/24" ) , new( "192.168.1.0/25" ) };
        ranges.Sort();
        Assert.Equal ( new NetRangeV4 ( "192.168.1.0/24" ) , ranges[0] );
        Assert.Equal ( new NetRangeV4 ( "192.168.1.0/25" ) , ranges[1] );
        Assert.Equal ( new NetRangeV4 ( "192.168.2.0/24" ) , ranges[2] );
    }
}
