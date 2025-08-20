using System.Net;
using System.Net.Sockets;
using System.Numerics;
using NetRangeManager.Models;
using Xunit;

namespace NetRangeManager.Tests;

public class NetRangeV4Tests
{
    #region Konstruktor Tests

    [Fact]
    public void Constructor_ValidCidr_CreatesCorrectInstance()
    {
        var range = new NetRangeV4("192.168.1.0/24");

        Assert.Equal(IPAddress.Parse("192.168.1.0"), range.NetworkAddress);
        Assert.Equal(24, range.CidrPrefix);
        Assert.Equal(256, (int)range.TotalAddresses);
        Assert.False(range.IsHost);
    }

    [Fact]
    public void Constructor_ValidIpAndPrefix_CreatesCorrectInstance()
    {
        var ip = IPAddress.Parse("10.0.0.0");
        var range = new NetRangeV4(ip, 8);

        Assert.Equal(ip, range.NetworkAddress);
        Assert.Equal(8, range.CidrPrefix);
        Assert.Equal(16777216, (int)range.TotalAddresses); // 2^24
        Assert.False(range.IsHost);
    }

    [Fact]
    public void Constructor_HostAddress_MarksAsHost()
    {
        var range = new NetRangeV4("192.168.1.100/32");

        Assert.True(range.IsHost);
        Assert.Equal(1, (int)range.TotalAddresses);
        Assert.Equal(range.NetworkAddress, range.FirstUsableAddress);
        Assert.Equal(range.NetworkAddress, range.LastUsableAddress);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NullOrEmptyCidr_ThrowsArgumentNullException(string cidr)
    {
        Assert.Throws<ArgumentNullException>(() => new NetRangeV4(cidr));
    }

    [Theory]
    [InlineData("192.168.1.0")]
    [InlineData("192.168.1.0/")]
    [InlineData("/24")]
    [InlineData("192.168.1.0/33")]
    [InlineData("192.168.1.0/-1")]
    [InlineData("256.1.1.1/24")]
    [InlineData("2001:db8::/64")] // IPv6 in IPv4 constructor
    public void Constructor_InvalidCidr_ThrowsArgumentException(string cidr)
    {
        Assert.Throws<ArgumentException>(() => new NetRangeV4(cidr));
    }

    [Fact]
    public void Constructor_NullIpAddress_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NetRangeV4(null!, 24));
    }

    [Fact]
    public void Constructor_IPv6Address_ThrowsArgumentException()
    {
        var ipv6 = IPAddress.Parse("2001:db8::1");
        Assert.Throws<ArgumentException>(() => new NetRangeV4(ipv6, 24));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(33)]
    public void Constructor_InvalidPrefix_ThrowsArgumentOutOfRangeException(int prefix)
    {
        var ip = IPAddress.Parse("192.168.1.0");
        Assert.Throws<ArgumentOutOfRangeException>(() => new NetRangeV4(ip, prefix));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_StandardNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV4("192.168.1.0/24");

        Assert.Equal(IPAddress.Parse("192.168.1.0"), range.NetworkAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.1"), range.FirstUsableAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.254"), range.LastUsableAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.255"), range.LastAddressInRange);
    }

    [Fact]
    public void Properties_PointToPointNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV4("192.168.1.0/31");

        Assert.Equal(IPAddress.Parse("192.168.1.0"), range.NetworkAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.0"), range.FirstUsableAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.0"), range.LastUsableAddress);
        Assert.Equal(IPAddress.Parse("192.168.1.1"), range.LastAddressInRange);
    }

    [Theory]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("172.16.0.0/12", true)]
    [InlineData("192.168.0.0/16", true)]
    [InlineData("172.15.0.0/16", false)]
    [InlineData("8.8.8.8/32", false)]
    public void IsPrivateRange_DetectsRFC1918Networks(string cidr, bool expected)
    {
        var range = new NetRangeV4(cidr);
        Assert.Equal(expected, range.IsPrivateRange);
    }

    [Theory]
    [InlineData("127.0.0.1/32", true)]
    [InlineData("127.1.1.1/32", true)]
    [InlineData("192.168.1.1/32", false)]
    public void IsLoopback_DetectsLoopbackAddresses(string cidr, bool expected)
    {
        var range = new NetRangeV4(cidr);
        Assert.Equal(expected, range.IsLoopback);
    }

    #endregion

    #region Contains Tests

    [Theory]
    [InlineData("192.168.1.0/24", "192.168.1.100", true)]
    [InlineData("192.168.1.0/24", "192.168.1.0", true)]
    [InlineData("192.168.1.0/24", "192.168.1.255", true)]
    [InlineData("192.168.1.0/24", "192.168.2.1", false)]
    [InlineData("10.0.0.0/8", "10.255.255.255", true)]
    [InlineData("10.0.0.0/8", "11.0.0.1", false)]
    public void Contains_IPv4Address_ReturnsCorrectResult(string cidr, string testIp, bool expected)
    {
        var range = new NetRangeV4(cidr);
        var ip = IPAddress.Parse(testIp);

        Assert.Equal(expected, range.Contains(ip));
    }

    [Fact]
    public void Contains_IPv6Address_ReturnsFalse()
    {
        var range = new NetRangeV4("192.168.1.0/24");
        var ipv6 = IPAddress.Parse("2001:db8::1");

        Assert.False(range.Contains(ipv6));
    }

    [Fact]
    public void Contains_NullAddress_ThrowsArgumentNullException()
    {
        var range = new NetRangeV4("192.168.1.0/24");
        Assert.Throws<ArgumentNullException>(() => range.Contains(null!));
    }

    #endregion

    #region Overlap Tests

    [Theory]
    [InlineData("192.168.1.0/24", "192.168.1.0/25", true)]
    [InlineData("192.168.1.0/24", "192.168.1.128/25", true)]
    [InlineData("192.168.1.0/24", "192.168.2.0/24", false)]
    [InlineData("10.0.0.0/8", "192.168.1.0/24", false)]
    [InlineData("192.168.0.0/16", "192.168.1.0/24", true)]
    public void OverlapsWith_ReturnsCorrectResult(string cidr1, string cidr2, bool expected)
    {
        var range1 = new NetRangeV4(cidr1);
        var range2 = new NetRangeV4(cidr2);

        Assert.Equal(expected, range1.OverlapsWith(range2));
        Assert.Equal(expected, range2.OverlapsWith(range1)); // Should be symmetric
    }

    #endregion

    #region Subnet/Supernet Tests

    [Theory]
    [InlineData("192.168.1.0/24", "192.168.0.0/16", true)]
    [InlineData("192.168.1.0/24", "192.168.1.0/24", true)]
    [InlineData("192.168.1.0/24", "192.168.1.0/25", false)]
    [InlineData("192.168.1.0/24", "10.0.0.0/8", false)]
    public void IsSubnetOf_ReturnsCorrectResult(string subnet, string supernet, bool expected)
    {
        var subRange = new NetRangeV4(subnet);
        var superRange = new NetRangeV4(supernet);

        Assert.Equal(expected, subRange.IsSubnetOf(superRange));
    }

    [Theory]
    [InlineData("192.168.0.0/16", "192.168.1.0/24", true)]
    [InlineData("192.168.1.0/24", "192.168.1.0/24", true)]
    [InlineData("192.168.1.0/25", "192.168.1.0/24", false)]
    public void IsSupernetOf_ReturnsCorrectResult(string supernet, string subnet, bool expected)
    {
        var superRange = new NetRangeV4(supernet);
        var subRange = new NetRangeV4(subnet);

        Assert.Equal(expected, superRange.IsSupernetOf(subRange));
    }

    #endregion

    #region Subnet Generation Tests

    [Fact]
    public void GetSubnets_ValidPrefix_GeneratesCorrectSubnets()
    {
        var range = new NetRangeV4("192.168.1.0/24");
        var subnets = range.GetSubnets(26).ToList();

        Assert.Equal(4, subnets.Count);
        Assert.Equal(new NetRangeV4("192.168.1.0/26"), subnets[0]);
        Assert.Equal(new NetRangeV4("192.168.1.64/26"), subnets[1]);
        Assert.Equal(new NetRangeV4("192.168.1.128/26"), subnets[2]);
        Assert.Equal(new NetRangeV4("192.168.1.192/26"), subnets[3]);
    }

    [Theory]
    [InlineData(24)] // Same as current
    [InlineData(23)] // Smaller than current
    [InlineData(33)] // Invalid
    public void GetSubnets_InvalidPrefix_ThrowsArgumentOutOfRangeException(int newPrefix)
    {
        var range = new NetRangeV4("192.168.1.0/24");
        Assert.Throws<ArgumentOutOfRangeException>(() => range.GetSubnets(newPrefix).ToList());
    }

    [Fact]
    public void GetSupernet_ValidPrefix_GeneratesCorrectSupernet()
    {
        var range = new NetRangeV4("192.168.1.0/24");
        var supernet = range.GetSupernet(16);

        Assert.Equal(new NetRangeV4("192.168.0.0/16"), supernet);
    }

    [Theory]
    [InlineData(24)] // Same as current
    [InlineData(25)] // Larger than current
    [InlineData(-1)] // Invalid
    public void GetSupernet_InvalidPrefix_ThrowsArgumentOutOfRangeException(int newPrefix)
    {
        var range = new NetRangeV4("192.168.1.0/24");
        Assert.Throws<ArgumentOutOfRangeException>(() => range.GetSupernet(newPrefix));
    }

    #endregion

    #region Parsing Tests

    [Theory]
    [InlineData("192.168.1.0/24", true)]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("255.255.255.255/32", true)]
    [InlineData("0.0.0.0/0", true)]
    public void TryParse_ValidCidr_ReturnsTrue(string cidr, bool expected)
    {
        var result = NetRangeV4.TryParse(cidr, out var range);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.NotEqual(default, range);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("192.168.1.0")]
    [InlineData("192.168.1.0/")]
    [InlineData("/24")]
    [InlineData("192.168.1.0/33")]
    [InlineData("256.1.1.1/24")]
    [InlineData("2001:db8::/64")]
    public void TryParse_InvalidCidr_ReturnsFalse(string cidr)
    {
        var result = NetRangeV4.TryParse(cidr, out var range);

        Assert.False(result);
        Assert.Equal(default, range);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void CompareTo_SameNetwork_ComparesByPrefix()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.1.0/25");

        Assert.True(range1.CompareTo(range2) < 0);
        Assert.True(range2.CompareTo(range1) > 0);
    }

    [Fact]
    public void CompareTo_DifferentNetworks_ComparesByAddress()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.2.0/24");

        Assert.True(range1.CompareTo(range2) < 0);
        Assert.True(range2.CompareTo(range1) > 0);
    }

    [Fact]
    public void Equals_SameRange_ReturnsTrue()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.1.0/24");

        Assert.True(range1.Equals(range2));
        Assert.True(range1 == range2);
    }

    [Fact]
    public void Equals_DifferentRange_ReturnsFalse()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.1.0/25");

        Assert.False(range1.Equals(range2));
        Assert.False(range1 == range2);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var range = new NetRangeV4("192.168.1.0/24");
        Assert.Equal("192.168.1.0/24", range.ToString());
    }

    #endregion
}

public class NetRangeV6Tests
{
    #region Konstruktor Tests

    [Fact]
    public void Constructor_ValidCidr_CreatesCorrectInstance()
    {
        var range = new NetRangeV6("2001:db8::/32");

        Assert.Equal(IPAddress.Parse("2001:db8::"), range.NetworkAddress);
        Assert.Equal(32, range.CidrPrefix);
        Assert.Equal(BigInteger.Pow(2, 96), range.TotalAddresses);
        Assert.False(range.IsHost);
    }

    [Fact]
    public void Constructor_ValidIpAndPrefix_CreatesCorrectInstance()
    {
        var ip = IPAddress.Parse("2001:db8::1");
        var range = new NetRangeV6(ip, 64);

        Assert.Equal(IPAddress.Parse("2001:db8::"), range.NetworkAddress);
        Assert.Equal(64, range.CidrPrefix);
        Assert.False(range.IsHost);
    }

    [Fact]
    public void Constructor_HostAddress_MarksAsHost()
    {
        var range = new NetRangeV6("2001:db8::1/128");

        Assert.True(range.IsHost);
        Assert.Equal(1, (int)range.TotalAddresses);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_NullOrEmptyCidr_ThrowsArgumentNullException(string cidr)
    {
        Assert.Throws<ArgumentNullException>(() => new NetRangeV6(cidr));
    }

    [Theory]
    [InlineData("2001:db8::")]
    [InlineData("2001:db8::/")]
    [InlineData("/64")]
    [InlineData("2001:db8::/129")]
    [InlineData("2001:db8::/-1")]
    [InlineData("192.168.1.0/24")] // IPv4 in IPv6 constructor
    public void Constructor_InvalidCidr_ThrowsArgumentException(string cidr)
    {
        Assert.Throws<ArgumentException>(() => new NetRangeV6(cidr));
    }

    [Fact]
    public void Constructor_NullIpAddress_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NetRangeV6(null!, 64));
    }

    [Fact]
    public void Constructor_IPv4Address_ThrowsArgumentException()
    {
        var ipv4 = IPAddress.Parse("192.168.1.1");
        Assert.Throws<ArgumentException>(() => new NetRangeV6(ipv4, 64));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(129)]
    public void Constructor_InvalidPrefix_ThrowsArgumentOutOfRangeException(int prefix)
    {
        var ip = IPAddress.Parse("2001:db8::1");
        Assert.Throws<ArgumentOutOfRangeException>(() => new NetRangeV6(ip, prefix));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_StandardNetwork_CalculatedCorrectly()
    {
        var range = new NetRangeV6("2001:db8::/64");

        Assert.Equal(IPAddress.Parse("2001:db8::"), range.NetworkAddress);
        Assert.Equal(IPAddress.Parse("2001:db8::"), range.FirstUsableAddress);
        Assert.Equal(IPAddress.Parse("2001:db8::ffff:ffff:ffff:ffff"), range.LastUsableAddress);
        Assert.Equal(range.LastUsableAddress, range.LastAddressInRange);
    }

    [Fact]
    public void IsLoopback_DetectsLoopbackAddress()
    {
        var loopback = new NetRangeV6("::1/128");
        var notLoopback = new NetRangeV6("2001:db8::1/128");

        Assert.True(loopback.IsLoopback);
        Assert.False(notLoopback.IsLoopback);
    }

    [Theory]
    [InlineData("fe80::/10", true)]
    [InlineData("fe80:1234::/64", true)]
    [InlineData("2001:db8::/32", false)]
    public void IsLinkLocal_DetectsLinkLocalAddresses(string cidr, bool expected)
    {
        var range = new NetRangeV6(cidr);
        Assert.Equal(expected, range.IsLinkLocal);
    }

    [Theory]
    [InlineData("fc00::/7", true)]
    [InlineData("fd00:1234::/32", true)]
    [InlineData("2001:db8::/32", false)]
    public void IsUniqueLocal_DetectsUniqueLocalAddresses(string cidr, bool expected)
    {
        var range = new NetRangeV6(cidr);
        Assert.Equal(expected, range.IsUniqueLocal);
    }

    #endregion

    #region Contains Tests

    [Theory]
    [InlineData("2001:db8::/32", "2001:db8::1", true)]
    [InlineData("2001:db8::/32", "2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [InlineData("2001:db8::/32", "2001:db9::1", false)]
    [InlineData("2001:db8::/64", "2001:db8::1", true)]
    [InlineData("2001:db8::/64", "2001:db8:0:1::1", false)]
    public void Contains_IPv6Address_ReturnsCorrectResult(string cidr, string testIp, bool expected)
    {
        var range = new NetRangeV6(cidr);
        var ip = IPAddress.Parse(testIp);

        Assert.Equal(expected, range.Contains(ip));
    }

    [Fact]
    public void Contains_IPv4Address_ReturnsFalse()
    {
        var range = new NetRangeV6("2001:db8::/32");
        var ipv4 = IPAddress.Parse("192.168.1.1");

        Assert.False(range.Contains(ipv4));
    }

    [Fact]
    public void Contains_NullAddress_ThrowsArgumentNullException()
    {
        var range = new NetRangeV6("2001:db8::/32");
        Assert.Throws<ArgumentNullException>(() => range.Contains(null!));
    }

    #endregion

    #region Overlap Tests

    [Theory]
    [InlineData("2001:db8::/32", "2001:db8::/64", true)]
    [InlineData("2001:db8::/32", "2001:db8:1::/64", true)]
    [InlineData("2001:db8::/32", "2001:db9::/32", false)]
    public void OverlapsWith_ReturnsCorrectResult(string cidr1, string cidr2, bool expected)
    {
        var range1 = new NetRangeV6(cidr1);
        var range2 = new NetRangeV6(cidr2);

        Assert.Equal(expected, range1.OverlapsWith(range2));
        Assert.Equal(expected, range2.OverlapsWith(range1));
    }

    #endregion

    #region Subnet/Supernet Tests

    [Theory]
    [InlineData("2001:db8::/64", "2001:db8::/32", true)]
    [InlineData("2001:db8::/64", "2001:db8::/64", true)]
    [InlineData("2001:db8::/32", "2001:db8::/64", false)]
    public void IsSubnetOf_ReturnsCorrectResult(string subnet, string supernet, bool expected)
    {
        var subRange = new NetRangeV6(subnet);
        var superRange = new NetRangeV6(supernet);

        Assert.Equal(expected, subRange.IsSubnetOf(superRange));
    }

    #endregion

    #region Subnet Generation Tests

    [Fact]
    public void GetSubnets_ValidPrefix_GeneratesCorrectSubnets()
    {
        var range = new NetRangeV6("2001:db8::/62");
        var subnets = range.GetSubnets(64).ToList();

        Assert.Equal(4, subnets.Count);
        Assert.Equal(new NetRangeV6("2001:db8::/64"), subnets[0]);
        Assert.Equal(new NetRangeV6("2001:db8:0:1::/64"), subnets[1]);
        Assert.Equal(new NetRangeV6("2001:db8:0:2::/64"), subnets[2]);
        Assert.Equal(new NetRangeV6("2001:db8:0:3::/64"), subnets[3]);
    }

    [Theory]
    [InlineData(32)] // Same as current
    [InlineData(31)] // Smaller than current
    [InlineData(129)] // Invalid
    public void GetSubnets_InvalidPrefix_ThrowsArgumentOutOfRangeException(int newPrefix)
    {
        var range = new NetRangeV6("2001:db8::/32");
        Assert.Throws<ArgumentOutOfRangeException>(() => range.GetSubnets(newPrefix).ToList());
    }

    [Fact]
    public void GetSubnets_TooManySubnets_ThrowsArgumentException()
    {
        var range = new NetRangeV6("2001:db8::/32");
        Assert.Throws<ArgumentException>(() => range.GetSubnets(96).ToList()); // 2^64 subnets
    }

    [Fact]
    public void GetSupernet_ValidPrefix_GeneratesCorrectSupernet()
    {
        var range = new NetRangeV6("2001:db8::/64");
        var supernet = range.GetSupernet(32);

        Assert.Equal(new NetRangeV6("2001:db8::/32"), supernet);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void EdgeCases_ZeroPrefix_HandledCorrectly()
    {
        var range = new NetRangeV6("::/0");

        Assert.Equal(0, range.CidrPrefix);
        Assert.Equal(IPAddress.IPv6Any, range.NetworkAddress);
        Assert.Equal(BigInteger.Pow(2, 128), range.TotalAddresses);
    }

    [Fact]
    public void EdgeCases_MaxPrefix_HandledCorrectly()
    {
        var range = new NetRangeV6("2001:db8::1/128");

        Assert.Equal(128, range.CidrPrefix);
        Assert.True(range.IsHost);
        Assert.Equal(1, (int)range.TotalAddresses);
    }

    #endregion

    #region Parsing Tests

    [Theory]
    [InlineData("2001:db8::/32", true)]
    [InlineData("::/0", true)]
    [InlineData("::1/128", true)]
    public void TryParse_ValidCidr_ReturnsTrue(string cidr, bool expected)
    {
        var result = NetRangeV6.TryParse(cidr, out var range);

        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.NotEqual(default, range);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("2001:db8::")]
    [InlineData("2001:db8::/")]
    [InlineData("/64")]
    [InlineData("2001:db8::/129")]
    [InlineData("192.168.1.0/24")]
    public void TryParse_InvalidCidr_ReturnsFalse(string cidr)
    {
        var result = NetRangeV6.TryParse(cidr, out var range);

        Assert.False(result);
        Assert.Equal(default, range);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        var range = new NetRangeV6("2001:db8::/32");
        Assert.Equal("2001:db8::/32", range.ToString());
    }

    #endregion
}

public class NetRangeCommonTests
{
    [Fact]
    public void HashCode_SameRanges_ProduceSameHashCode()
    {
        var range1 = new NetRangeV4("192.168.1.0/24");
        var range2 = new NetRangeV4("192.168.1.0/24");

        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());

        var rangeV6_1 = new NetRangeV6("2001:db8::/32");
        var rangeV6_2 = new NetRangeV6("2001:db8::/32");

        Assert.Equal(rangeV6_1.GetHashCode(), rangeV6_2.GetHashCode());
    }

    [Fact]
    public void Collections_CanBeUsedInHashSets()
    {
        var set = new HashSet<NetRangeV4>
        {
            new("192.168.1.0/24"),
            new("192.168.1.0/24"), // Duplicate
            new("192.168.2.0/24")
        };

        Assert.Equal(2, set.Count);

        var setV6 = new HashSet<NetRangeV6>
        {
            new("2001:db8::/32"),
            new("2001:db8::/32"), // Duplicate
            new("2001:db8:1::/32")
        };

        Assert.Equal(2, setV6.Count);
    }

    [Fact]
    public void Collections_CanBeSorted()
    {
        var ranges = new List<NetRangeV4>
        {
            new("192.168.2.0/24"),
            new("192.168.1.0/24"),
            new("192.168.1.0/25")
        };

        ranges.Sort();

        Assert.Equal(new NetRangeV4("192.168.1.0/24"), ranges[0]);
        Assert.Equal(new NetRangeV4("192.168.1.0/25"), ranges[1]);
        Assert.Equal(new NetRangeV4("192.168.2.0/24"), ranges[2]);
    }
}

public class NetRangePerformanceTests
{
    [Fact]
    public void Performance_LargeSubnetGeneration_CompletesInReasonableTime()
    {
        var range = new NetRangeV4("10.0.0.0/8");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var subnets = range.GetSubnets(16).Take(100).ToList();

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should complete in less than 1 second
        Assert.Equal(100, subnets.Count);
    }

    [Fact]
    public void Performance_ContainsCheck_FastForManyAddresses()
    {
        var range = new NetRangeV4("192.168.0.0/16");
        var addresses = new[]
        {
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.100.100"),
            IPAddress.Parse("192.168.255.255"),
            IPAddress.Parse("10.0.0.1"), // Outside range
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            foreach (var addr in addresses)
            {
                range.Contains(addr);
            }
        }

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1000); // Should be very fast
    }
}

public class NetRangeIntegrationTests
{
    [Fact]
    public void Integration_NetworkSubdivision_WorksCorrectly()
    {
        // Start with a /24 network
        var mainNetwork = new NetRangeV4("192.168.1.0/24");

        // Subdivide into /26 subnets
        var subnets = mainNetwork.GetSubnets(26).ToList();

        Assert.Equal(4, subnets.Count);

        // Verify each subnet is properly contained in the main network
        foreach (var subnet in subnets)
        {
            Assert.True(subnet.IsSubnetOf(mainNetwork));
            Assert.True(mainNetwork.IsSupernetOf(subnet));
        }

        // Verify subnets don't overlap with each other
        for (int i = 0; i < subnets.Count; i++)
        {
            for (int j = i + 1; j < subnets.Count; j++)
            {
                Assert.False(subnets[i].OverlapsWith(subnets[j]));
            }
        }

        // Verify we can create a supernet
        var supernet = mainNetwork.GetSupernet(16);
        Assert.Equal(new NetRangeV4("192.168.0.0/16"), supernet);
        Assert.True(mainNetwork.IsSubnetOf(supernet));
    }

    [Fact]
    public void Integration_IPv6NetworkOperations_WorkCorrectly()
    {
        // Start with a /48 network (typical ISP allocation)
        var ispNetwork = new NetRangeV6("2001:db8::/48");

        // Create customer networks (/56)
        var customerNetworks = ispNetwork.GetSubnets(56).Take(10).ToList();

        Assert.Equal(10, customerNetworks.Count);

        // Each customer can create /64 subnets
        var firstCustomer = customerNetworks[0];
        var customerSubnets = firstCustomer.GetSubnets(64).Take(5).ToList();

        Assert.Equal(5, customerSubnets.Count);

        // Verify hierarchy
        foreach (var subnet in customerSubnets)
        {
            Assert.True(subnet.IsSubnetOf(firstCustomer));
            Assert.True(subnet.IsSubnetOf(ispNetwork));
        }
    }

    [Fact]
    public void Integration_MixedAddressFamilies_HandleCorrectly()
    {
        var ipv4Range = new NetRangeV4("192.168.1.0/24");
        var ipv6Range = new NetRangeV6("2001:db8::/64");

        var ipv4Address = IPAddress.Parse("192.168.1.100");
        var ipv6Address = IPAddress.Parse("2001:db8::1");

        // IPv4 range should contain IPv4 address but not IPv6
        Assert.True(ipv4Range.Contains(ipv4Address));
        Assert.False(ipv4Range.Contains(ipv6Address));

        // IPv6 range should contain IPv6 address but not IPv4
        Assert.True(ipv6Range.Contains(ipv6Address));
        Assert.False(ipv6Range.Contains(ipv4Address));
    }
}

public class NetRangeEdgeCaseTests
{
    [Fact]
    public void EdgeCase_SingleHostNetworks_WorkCorrectly()
    {
        var ipv4Host = new NetRangeV4("192.168.1.100/32");
        var ipv6Host = new NetRangeV6("2001:db8::1/128");

        Assert.True(ipv4Host.IsHost);
        Assert.True(ipv6Host.IsHost);

        Assert.Equal(ipv4Host.NetworkAddress, ipv4Host.FirstUsableAddress);
        Assert.Equal(ipv4Host.NetworkAddress, ipv4Host.LastUsableAddress);

        Assert.Equal(ipv6Host.NetworkAddress, ipv6Host.FirstUsableAddress);
        Assert.Equal(ipv6Host.NetworkAddress, ipv6Host.LastUsableAddress);

        // Host networks cannot be subdivided
        Assert.Throws<ArgumentOutOfRangeException>(() => ipv4Host.GetSubnets(33).ToList());
        Assert.Throws<ArgumentOutOfRangeException>(() => ipv6Host.GetSubnets(129).ToList());
    }

    [Fact]
    public void EdgeCase_ZeroPrefixNetworks_WorkCorrectly()
    {
        var ipv4All = new NetRangeV4("0.0.0.0/0");
        var ipv6All = new NetRangeV6("::/0");

        Assert.Equal(0, ipv4All.CidrPrefix);
        Assert.Equal(0, ipv6All.CidrPrefix);

        Assert.Equal(IPAddress.Any, ipv4All.NetworkAddress);
        Assert.Equal(IPAddress.IPv6Any, ipv6All.NetworkAddress);

        // Should contain any address of the same family
        Assert.True(ipv4All.Contains(IPAddress.Parse("192.168.1.1")));
        Assert.True(ipv4All.Contains(IPAddress.Parse("8.8.8.8")));
        Assert.False(ipv4All.Contains(IPAddress.Parse("2001:db8::1")));

        Assert.True(ipv6All.Contains(IPAddress.Parse("2001:db8::1")));
        Assert.True(ipv6All.Contains(IPAddress.IPv6Loopback));
        Assert.False(ipv6All.Contains(IPAddress.Parse("192.168.1.1")));
    }

    [Fact]
    public void EdgeCase_Point2PointNetworks_WorkCorrectly()
    {
        var p2p = new NetRangeV4("192.168.1.0/31");

        Assert.Equal(31, p2p.CidrPrefix);
        Assert.Equal(2, (int)p2p.TotalAddresses);
        Assert.False(p2p.IsHost);

        // In /31 networks, both addresses are usable (RFC 3021)
        Assert.Equal(p2p.NetworkAddress, p2p.FirstUsableAddress);
        Assert.Equal(p2p.NetworkAddress, p2p.LastUsableAddress);

        Assert.True(p2p.Contains(IPAddress.Parse("192.168.1.0")));
        Assert.True(p2p.Contains(IPAddress.Parse("192.168.1.1")));
        Assert.False(p2p.Contains(IPAddress.Parse("192.168.1.2")));
    }

    [Fact]
    public void EdgeCase_LargeIPv6Subnets_DontCauseOverflow()
    {
        var range = new NetRangeV6("2001:db8::/64");

        // This should work without overflow
        var subnets = range.GetSubnets(128).Take(1000).ToList();

        Assert.Equal(1000, subnets.Count);
        Assert.All(subnets, s => Assert.True(s.IsHost));
    }

    [Fact]
    public void EdgeCase_BoundaryAddresses_HandledCorrectly()
    {
        var range = new NetRangeV4("192.168.1.0/24");

        // Test boundary addresses
        Assert.True(range.Contains(IPAddress.Parse("192.168.1.0")));   // Network address
        Assert.True(range.Contains(IPAddress.Parse("192.168.1.255"))); // Broadcast address
        Assert.False(range.Contains(IPAddress.Parse("192.168.0.255"))); // Just before
        Assert.False(range.Contains(IPAddress.Parse("192.168.2.0")));   // Just after
    }
}

public class NetRangeValidationTests
{
    [Theory]
    [InlineData("192.168.1.0/24")]
    [InlineData("10.0.0.0/8")]
    [InlineData("172.16.0.0/12")]
    [InlineData("0.0.0.0/0")]
    [InlineData("255.255.255.255/32")]
    public void Validation_WellKnownIPv4Networks_ParseCorrectly(string cidr)
    {
        var parseSuccess = NetRangeV4.TryParse(cidr, out var range);
        Assert.True(parseSuccess);

        var constructorRange = new NetRangeV4(cidr);
        Assert.Equal(range, constructorRange);
    }

    [Theory]
    [InlineData("2001:db8::/32")]
    [InlineData("fe80::/10")]
    [InlineData("fc00::/7")]
    [InlineData("::/0")]
    [InlineData("::1/128")]
    public void Validation_WellKnownIPv6Networks_ParseCorrectly(string cidr)
    {
        var parseSuccess = NetRangeV6.TryParse(cidr, out var range);
        Assert.True(parseSuccess);

        var constructorRange = new NetRangeV6(cidr);
        Assert.Equal(range, constructorRange);
    }

    [Fact]
    public void Validation_ConsistencyBetweenConstructorAndTryParse()
    {
        var testCases = new[]
        {
            "192.168.1.0/24",
            "10.0.0.0/8",
            "127.0.0.1/32"
        };

        foreach (var cidr in testCases)
        {
            var success = NetRangeV4.TryParse(cidr, out var parsedRange);
            Assert.True(success);

            var constructedRange = new NetRangeV4(cidr);

            Assert.Equal(parsedRange.NetworkAddress, constructedRange.NetworkAddress);
            Assert.Equal(parsedRange.CidrPrefix, constructedRange.CidrPrefix);
            Assert.Equal(parsedRange.TotalAddresses, constructedRange.TotalAddresses);
        }
    }
}

// Test project file would also be needed:
/*
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.0">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\NetRangeManager\NetRangeManager.csproj" />
  </ItemGroup>

</Project>
*/