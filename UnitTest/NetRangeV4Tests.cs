using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public partial class NetRangeV4Tests
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
}
