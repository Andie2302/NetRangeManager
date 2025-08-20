using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

/// <summary>
/// Represents a range of IPv4 addresses defined by a network address and a CIDR prefix.
/// This is an immutable struct.
/// </summary>
public readonly partial record struct NetRangeV4 : INetRange<NetRangeV4>
{
    private readonly uint _networkAddressUInt;
    private readonly uint _broadcastAddressUInt;
    private readonly IPAddress? _firstUsableAddressCache;
    private readonly IPAddress? _lastUsableAddressCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetRangeV4"/> struct from a CIDR string.
    /// </summary>
    /// <param name="cidr">The network range in CIDR notation (e.g., "192.168.1.0/24").</param>
    /// <exception cref="ArgumentNullException">Thrown if cidr is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the CIDR notation is invalid.</exception>
    public NetRangeV4(string cidr)
    {
        if (cidr is null) { throw new ArgumentNullException(nameof(cidr)); }

        if (!TryParse(cidr, out this))
        {
            throw new ArgumentException($"Invalid IPv4 CIDR notation: '{cidr}'", nameof(cidr));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetRangeV4"/> struct from an IP address and a prefix.
    /// </summary>
    /// <param name="ip">The IP address. It will be adjusted to the network address of the range.</param>
    /// <param name="prefix">The CIDR prefix length (0-32).</param>
    /// <exception cref="ArgumentNullException">Thrown if ip is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided IP address is not an IPv4 address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the prefix is not between 0 and 32.</exception>
    public NetRangeV4(IPAddress ip, int prefix)
    {
        if (ip is null) { throw new ArgumentNullException(nameof(ip)); }

        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Only IPv4 addresses are supported.", nameof(ip));
        }

        if (prefix is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), prefix, "IPv4 prefix must be between 0 and 32.");
        }

        CidrPrefix = prefix;
        var ipUInt = ToUInt32(ip);
        var mask = prefix == 0 ? 0u : 0xFFFFFFFFu << 32 - prefix;
        _networkAddressUInt = ipUInt & mask;
        _broadcastAddressUInt = _networkAddressUInt | ~mask;
        NetworkAddress = ToIpAddress(_networkAddressUInt);
        TotalAddresses = BigInteger.Pow(2, 32 - prefix);
        IsHost = prefix == 32;
        _firstUsableAddressCache = prefix >= 31 ? NetworkAddress : ToIpAddress(_networkAddressUInt + 1);
        _lastUsableAddressCache = prefix >= 31 ? NetworkAddress : ToIpAddress(_broadcastAddressUInt - 1);
    }

    /// <summary>
    /// Converts an IPAddress to its 32-bit unsigned integer representation.
    /// </summary>
    private static uint ToUInt32(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Converts a 32-bit unsigned integer to its IPAddress representation.
    /// </summary>
    private static IPAddress ToIpAddress(uint addressValue)
    {
        var bytes = BitConverter.GetBytes(addressValue);
        if (BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
        return new IPAddress(bytes);
    }

    /// <inheritdoc />
    public IPAddress NetworkAddress { get; }

    /// <inheritdoc />
    public int CidrPrefix { get; }

    /// <inheritdoc />
    public IPAddress FirstUsableAddress => _firstUsableAddressCache ?? NetworkAddress;

    /// <inheritdoc />
    public IPAddress LastUsableAddress => _lastUsableAddressCache ?? NetworkAddress;

    /// <inheritdoc />
    public IPAddress LastAddressInRange => ToIpAddress(_broadcastAddressUInt);

    /// <inheritdoc />
    public BigInteger TotalAddresses { get; }

    /// <inheritdoc />
    public bool IsHost { get; }

    /// <summary>
    /// Gets a value indicating whether this network range is a private range according to RFC 1918.
    /// </summary>
    public bool IsPrivateRange => IsRfc1918Private();

    /// <summary>
    /// Gets a value indicating whether this network range is a loopback range (127.0.0.0/8).
    /// </summary>
    public bool IsLoopback => (_networkAddressUInt & 0xFF000000u) == 0x7F000000u;

    /// <inheritdoc />
    public bool Contains(IPAddress ipAddress)
    {
        if (ipAddress is null) { throw new ArgumentNullException(nameof(ipAddress)); }

        if (ipAddress.AddressFamily != AddressFamily.InterNetwork) { return false; }

        var ipUInt = ToUInt32(ipAddress);
        return ipUInt >= _networkAddressUInt && ipUInt <= _broadcastAddressUInt;
    }

    /// <inheritdoc />
    public bool OverlapsWith(NetRangeV4 other) => _networkAddressUInt <= other._broadcastAddressUInt && _broadcastAddressUInt >= other._networkAddressUInt;

    /// <inheritdoc />
    public bool IsSubnetOf(NetRangeV4 other) => _networkAddressUInt >= other._networkAddressUInt && _broadcastAddressUInt <= other._broadcastAddressUInt;

    /// <inheritdoc />
    public bool IsSupernetOf(NetRangeV4 other) => other.IsSubnetOf(this);

    /// <inheritdoc />
    public IEnumerable<NetRangeV4> GetSubnets(int newPrefix)
    {
        if (newPrefix <= CidrPrefix || newPrefix > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix, $"New prefix must be larger than {CidrPrefix} and less than or equal to 32.");
        }

        var subnetSize = 1u << 32 - newPrefix;
        var currentAddress = _networkAddressUInt;

        while (currentAddress <= _broadcastAddressUInt)
        {
            yield return new NetRangeV4(ToIpAddress(currentAddress), newPrefix);
            if (currentAddress > uint.MaxValue - subnetSize) { break; }
            currentAddress += subnetSize;
        }
    }

    /// <summary>
    /// Calculates the supernet of this network range with a smaller prefix.
    /// </summary>
    /// <param name="newPrefix">The new, smaller CIDR prefix for the supernet.</param>
    /// <returns>A new <see cref="NetRangeV4"/> instance representing the supernet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the new prefix is not smaller than the current prefix or is invalid.</exception>
    public NetRangeV4 GetSupernet(int newPrefix)
    {
        if (newPrefix >= CidrPrefix || newPrefix < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix, $"New prefix must be smaller than {CidrPrefix} and greater than or equal to 0.");
        }

        var newMask = newPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - newPrefix;
        var newNetworkAddress = _networkAddressUInt & newMask;
        return new NetRangeV4(ToIpAddress(newNetworkAddress), newPrefix);
    }

    /// <inheritdoc />
    public int CompareTo(NetRangeV4 other)
    {
        var networkComparison = _networkAddressUInt.CompareTo(other._networkAddressUInt);
        return networkComparison == 0 ? CidrPrefix.CompareTo(other.CidrPrefix) : networkComparison;
    }

    /// <inheritdoc />
    public bool Equals(NetRangeV4 other) => CidrPrefix == other.CidrPrefix && _networkAddressUInt == other._networkAddressUInt;

    /// <summary>
    /// Returns the string representation of the network range in CIDR notation.
    /// </summary>
    /// <returns>The CIDR string (e.g., "192.168.1.0/24").</returns>
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

    /// <summary>
    /// Tries to parse a string in CIDR notation into a <see cref="NetRangeV4"/>.
    /// </summary>
    /// <param name="cidr">The CIDR string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed NetRangeV4, if the parsing succeeded, or a default value if the parsing failed.</param>
    /// <returns>True if the string was successfully parsed; otherwise, false.</returns>
    public static bool TryParse(string? cidr, out NetRangeV4 result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(cidr)) { return false; }

        var parts = cidr?.Split('/');
        if (parts is not { Length: 2 }) { return false; }

        if (!IPAddress.TryParse(parts[0], out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) { return false; }

        if (!int.TryParse(parts[1], out var prefix) || prefix is < 0 or > 32) { return false; }

        try
        {
            result = new NetRangeV4(ip, prefix);
            return true;
        }
        catch { return false; }
    }

    private bool IsRfc1918Private()
    {
        // 10.0.0.0/8
        if ((_networkAddressUInt & 0xFF000000u) == 0x0A000000u) { return true; }
        // 172.16.0.0/12
        if ((_networkAddressUInt & 0xFFF00000u) == 0xAC100000u) { return true; }
        // 192.168.0.0/16
        if ((_networkAddressUInt & 0xFFFF0000u) == 0xC0A80000u) { return true; }

        return false;
    }
}

#if NET5_0_OR_GREATER
public readonly partial record struct NetRangeV4
{
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(CidrPrefix, _networkAddressUInt);
}
#else
public readonly partial record struct NetRangeV4
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + CidrPrefix;
            hash = hash * 31 + (int)_networkAddressUInt;
            return hash;
        }
    }
}
#endif