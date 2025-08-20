using System.Numerics;
using System.Net;

namespace NetRangeManager.Interfaces;

/// <summary>
/// Defines a generic interface for a network range (either IPv4 or IPv6).
/// </summary>
/// <typeparam name="TNetRange">The specific type of the network range implementing this interface.</typeparam>
public interface INetRange<TNetRange> : IComparable<TNetRange>, IEquatable<TNetRange> where TNetRange : INetRange<TNetRange>
{
    /// <summary>
    /// Gets the network address of the range.
    /// </summary>
    IPAddress? NetworkAddress { get; }

    /// <summary>
    /// Gets the CIDR prefix length of the network range.
    /// </summary>
    int CidrPrefix { get; }

    /// <summary>
    /// Gets the first usable IP address in the network range.
    /// </summary>
    IPAddress? FirstUsableAddress { get; }

    /// <summary>
    /// Gets the last usable IP address in the network range.
    /// </summary>
    IPAddress? LastUsableAddress { get; }

    /// <summary>
    /// Gets the last address in the network range (e.g., the broadcast address for IPv4).
    /// </summary>
    IPAddress? LastAddressInRange { get; }

    /// <summary>
    /// Gets the total number of IP addresses in this network range.
    /// </summary>
    BigInteger TotalAddresses { get; }

    /// <summary>
    /// Gets a value indicating whether this network range represents a single host address.
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Determines whether the specified IP address is within this network range.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if the IP address is contained in the range; otherwise, false.</returns>
    bool Contains(IPAddress? ipAddress);

    /// <summary>
    /// Determines whether this network range overlaps with another network range.
    /// </summary>
    /// <param name="other">The other network range to check for overlap.</param>
    /// <returns>True if the ranges overlap; otherwise, false.</returns>
    bool OverlapsWith(TNetRange other);

    /// <summary>
    /// Determines whether this network range is a subnet of another network range.
    /// </summary>
    /// <param name="other">The potential supernet.</param>
    /// <returns>True if this range is a subnet of the other range; otherwise, false.</returns>
    bool IsSubnetOf(TNetRange other);

    /// <summary>
    /// Determines whether this network range is a supernet of another network range.
    /// </summary>
    /// <param name="other">The potential subnet.</param>
    /// <returns>True if this range is a supernet of the other range; otherwise, false.</returns>
    bool IsSupernetOf(TNetRange other);

    /// <summary>
    /// Divides the network range into smaller subnets of a specified prefix length.
    /// </summary>
    /// <param name="newPrefix">The new CIDR prefix for the subnets. Must be larger than the current prefix.</param>
    /// <returns>An enumerable collection of subnets.</returns>
    IEnumerable<TNetRange> GetSubnets(int newPrefix);
}