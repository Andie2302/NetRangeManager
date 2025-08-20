using System.Net;
using NetRangeManager.Models; // Wichtig: Wir binden unsere Klasse hier ein!

namespace UnitTest;

public partial class NetRangeV4Tests // Wir benennen die Klasse um, damit klar ist, was wir testen.
{
    [Fact] // Ein [Fact] ist ein einzelner, einfacher Testfall in xUnit.
    public void Constructor_ShouldCalculatePropertiesCorrectly_ForClassC()
    {
        // ARRANGE: Wir bereiten alles vor, was wir für den Test brauchen.
        var cidr = "192.168.1.0/24";
        var expectedNetworkAddress = IPAddress.Parse("192.168.1.0");
        var expectedFirstUsable = IPAddress.Parse("192.168.1.1");
        var expectedLastUsable = IPAddress.Parse("192.168.1.254");
        var expectedBroadcast = IPAddress.Parse("192.168.1.255");

        // ACT: Wir führen die Aktion aus, die wir testen wollen.
        var range = new NetRangeV4(cidr);

        // ASSERT: Wir überprüfen, ob das Ergebnis unseren Erwartungen entspricht.
        Assert.Equal(expectedNetworkAddress, range.NetworkAddress);
        Assert.Equal(24, range.CidrPrefix);
        Assert.Equal(expectedFirstUsable, range.FirstUsableAddress);
        Assert.Equal(expectedLastUsable, range.LastUsableAddress);
        Assert.Equal(expectedBroadcast, range.LastAddressInRange);
        Assert.Equal(256, range.TotalAddresses);
        Assert.False(range.IsHost); // Ein /24 ist kein Host-Netz.
    }
}
