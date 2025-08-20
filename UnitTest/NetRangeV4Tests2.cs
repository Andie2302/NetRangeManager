using System.Net;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeV4Tests2
{
    [Fact]
    public void Constructor_ShouldCalculatePropertiesCorrectly_ForClassC()
    {
        // ... (dein bestehender, erfolgreicher Test)
        // ARRANGE
        var cidr = "192.168.1.0/24";
        var expectedNetworkAddress = IPAddress.Parse("192.168.1.0");
        var expectedFirstUsable = IPAddress.Parse("192.168.1.1");
        var expectedLastUsable = IPAddress.Parse("192.168.1.254");
        var expectedBroadcast = IPAddress.Parse("192.168.1.255");

        // ACT
        var range = new NetRangeV4(cidr);

        // ASSERT
        Assert.Equal(expectedNetworkAddress, range.NetworkAddress);
        Assert.Equal(24, range.CidrPrefix);
        Assert.Equal(expectedFirstUsable, range.FirstUsableAddress);
        Assert.Equal(expectedLastUsable, range.LastUsableAddress);
        Assert.Equal(expectedBroadcast, range.LastAddressInRange);
        Assert.Equal(256, range.TotalAddresses);
        Assert.False(range.IsHost);
    }

    // HIER IST DER NEUE TEST:
    [Theory] // Sagt xUnit, dass dies eine parametrisierte Testmethode ist.
    [InlineData("192.168.1.150", true)]  // Fall 1: IP ist mitten im Bereich
    [InlineData("10.0.0.5", false)]      // Fall 2: IP ist komplett außerhalb
    [InlineData("192.168.1.0", true)]    // Fall 3: IP ist die Netzwerkadresse (Grenzwert)
    [InlineData("192.168.1.255", true)]  // Fall 4: IP ist die Broadcast-Adresse (Grenzwert)
    [InlineData("192.168.0.255", false)] // Fall 5: IP ist knapp davor (Grenzwert)
    [InlineData("192.168.2.0", false)]   // Fall 6: IP ist knapp danach (Grenzwert)
    public void Contains_ShouldReturnExpectedResult(string ipAddressToTest, bool expectedResult)
    {
        // ARRANGE
        var range = new NetRangeV4("192.168.1.0/24");
        var ipAddress = IPAddress.Parse(ipAddressToTest);

        // ACT
        var actualResult = range.Contains(ipAddress);

        // ASSERT
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData("10.0.0.0/16", "10.0.10.0/24", true, true, true)]       // Szenario 1: Subnetz liegt komplett innerhalb
    [InlineData("10.0.10.0/24", "10.0.0.0/16", true, false, false)]      // Szenario 2: Umgekehrt, Supernetz
    [InlineData("10.0.0.0/16", "10.0.255.0/24", true, true, true)]       // Szenario 3: Subnetz liegt am Rand, aber noch drin
    [InlineData("10.0.0.0/23", "10.0.1.128/25", true, true, true)]       // Szenario 4: Ein weiteres klares Subnetz
    [InlineData("192.168.1.0/24", "192.168.2.0/24", false, false, false)]// Szenario 5: Komplett getrennte Netze
    [InlineData("172.16.0.0/24", "172.16.0.128/25", true, true, true)]   // Szenario 6: Ein Subnetz
    public void RelationshipTests_ShouldReturnExpectedResults(
        string rangeACidr, string rangeBCidr,
        bool shouldOverlap, bool bShouldBeSubnetOfA, bool aShouldBeSupernetOfB)
    {
        // ARRANGE
        var rangeA = new NetRangeV4(rangeACidr);
        var rangeB = new NetRangeV4(rangeBCidr);

        // ACT
        var actualOverlap = rangeA.OverlapsWith(rangeB);
        var actualIsSubnet = rangeB.IsSubnetOf(rangeA);
        var actualIsSupernet = rangeA.IsSupernetOf(rangeB);

        // ASSERT
        Assert.Equal(shouldOverlap, actualOverlap);
        Assert.Equal(bShouldBeSubnetOfA, actualIsSubnet);
        Assert.Equal(aShouldBeSupernetOfB, actualIsSupernet);
    }

}