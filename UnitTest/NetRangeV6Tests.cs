using System.Net;
using System.Numerics;
using NetRangeManager.Models;

namespace UnitTest;

public class NetRangeV6Tests
{
    [Fact]
    public void Constructor_ShouldCalculatePropertiesCorrectly()
    {
        // ARRANGE
        var cidr = "2001:db8:acad::/48";
        var expectedNetworkAddress = IPAddress.Parse("2001:db8:acad::");
        var expectedLastAddress = IPAddress.Parse("2001:db8:acad:ffff:ffff:ffff:ffff:ffff");
        var expectedTotalAddresses = BigInteger.Pow(2, 128 - 48);

        // ACT
        var range = new NetRangeV6(cidr);

        // ASSERT
        Assert.Equal(expectedNetworkAddress, range.NetworkAddress);
        Assert.Equal(48, range.CidrPrefix);
        Assert.Equal(expectedLastAddress, range.LastAddressInRange);
        Assert.Equal(expectedTotalAddresses, range.TotalAddresses);
        Assert.False(range.IsHost);
    }

    [Theory]
    [InlineData("2001:db8:acad:1:ffff:ffff:ffff:ffff", true)] // Innerhalb
    [InlineData("2001:db8:beef::1", false)]                   // Außerhalb
    [InlineData("2001:db8:acad::", true)]                     // Untere Grenze
    [InlineData("2001:db8:acad:ffff:ffff:ffff:ffff:ffff", true)] // Obere Grenze
    public void Contains_ShouldReturnExpectedResult(string ipAddressToTest, bool expectedResult)
    {
        // ARRANGE
        var range = new NetRangeV6("2001:db8:acad::/48");
        var ipAddress = IPAddress.Parse(ipAddressToTest);

        // ACT
        var actualResult = range.Contains(ipAddress);

        // ASSERT
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData("2001:db8::/32", "2001:db8:acad::/48", true, true, true)] // Subnetz
    [InlineData("2001:db8:acad::/48", "2001:db8::/32", true, false, false)] // Supernetz
    [InlineData("2001:db8::/32", "2001:db9::/32", false, false, false)] // Getrennt
    public void RelationshipTests_ShouldReturnExpectedResults(
        string rangeACidr, string rangeBCidr,
        bool shouldOverlap, bool bShouldBeSubnetOfA, bool aShouldBeSupernetOfB)
    {
        // ARRANGE
        var rangeA = new NetRangeV6(rangeACidr);
        var rangeB = new NetRangeV6(rangeBCidr);

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