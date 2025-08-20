using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

/// <summary>
/// Represents a range of IPv6 addresses defined by a network address and a CIDR prefix.
/// This is an immutable struct.
/// </summary>
public readonly partial record struct NetRangeV6 : INetRange< NetRangeV6 >
{
    private readonly BigInteger _networkAddressBigInt;
    private readonly BigInteger _lastAddressBigInt;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetRangeV6"/> struct from a CIDR string.
    /// </summary>
    /// <param name="cidr">The network range in CIDR notation (e.g., "2001:db8::/32").</param>
    /// <exception cref="ArgumentNullException">Thrown if cidr is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the CIDR notation is invalid.</exception>
    public NetRangeV6 ( string cidr )
    {
        if ( cidr is null ) { throw new ArgumentNullException ( nameof ( cidr ) ); }

        if ( !TryParse ( cidr , out this ) ) { throw new ArgumentException ( $"Invalid IPv6 CIDR notation: '{cidr}'" , nameof ( cidr ) ); }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetRangeV6"/> struct from an IP address and a prefix.
    /// </summary>
    /// <param name="ip">The IP address. It will be adjusted to the network address of the range.</param>
    /// <param name="prefix">The CIDR prefix length (0-128).</param>
    /// <exception cref="ArgumentNullException">Thrown if ip is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the provided IP address is not an IPv6 address.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the prefix is not between 0 and 128.</exception>
    public NetRangeV6 ( IPAddress? ip , int prefix )
    {
        if ( ip is null ) { throw new ArgumentNullException ( nameof ( ip ) ); }

        if ( ip.AddressFamily != AddressFamily.InterNetworkV6 ) { throw new ArgumentException ( "Only IPv6 addresses are supported." , nameof ( ip ) ); }

        if ( prefix is < 0 or > 128 ) { throw new ArgumentOutOfRangeException ( nameof ( prefix ) , prefix , "IPv6 prefix must be between 0 and 128." ); }

        CidrPrefix = prefix;
        var ipBigInt = ToBigInteger ( ip );
        BigInteger mask;

        if ( prefix == 0 ) { mask = BigInteger.Zero; }
        else {
            if ( prefix == 128 ) { mask = ( BigInteger.One << 128 ) - 1; }
            else { mask = ( BigInteger.One << 128 ) - 1 & ~( ( BigInteger.One << 128 - prefix ) - 1 ); }
        }

        _networkAddressBigInt = ipBigInt & mask;
        _lastAddressBigInt = _networkAddressBigInt | ~mask;
        NetworkAddress = ToIpAddress ( _networkAddressBigInt );
        TotalAddresses = BigInteger.Pow ( 2 , 128 - prefix );
        IsHost = prefix == 128;
    }

    /// <summary>
    /// Converts an IPAddress to its BigInteger representation.
    /// </summary>
    private static BigInteger ToBigInteger ( IPAddress ipAddress )
    {
        var bytes = ipAddress.GetAddressBytes();

        // BigInteger erwartet Little-Endian, GetAddressBytes gibt Big-Endian zurück
        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        // Wichtig: BigInteger als positive Zahl erstellen, indem ein Null-Byte angehängt wird
        return new BigInteger ( bytes.Concat ( new byte[] { 0 } ).ToArray() );
    }

    /// <summary>
    /// Converts a BigInteger to its IPAddress representation.
    /// </summary>
    private static IPAddress ToIpAddress ( BigInteger addressValue )
    {
        if ( addressValue < 0 ) { throw new ArgumentOutOfRangeException ( nameof ( addressValue ) , "IPv6 address cannot be negative." ); }

        var bytes = addressValue.ToByteArray();
        var ipBytes = new byte[ 16 ]; // IPv6 hat 16 Bytes

        // Kopiere die Bytes. Wenn die Zahl kleiner ist, werden die restlichen Bytes zu 0.
        // Wenn die Zahl größer ist (durch das angehängte Null-Byte), wird das letzte Byte ignoriert.
        var bytesToCopy = Math.Min ( bytes.Length , 16 );
        Array.Copy ( bytes , ipBytes , bytesToCopy );

        // Zurück zu Big-Endian für die IPAddress-Klasse
        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( ipBytes ); }

        return new IPAddress ( ipBytes );
    }

    /// <inheritdoc />
    public IPAddress? NetworkAddress { get; }
    /// <inheritdoc />
    public int CidrPrefix { get; }
    /// <inheritdoc />
    public IPAddress? FirstUsableAddress => NetworkAddress;
    /// <inheritdoc />
    public IPAddress? LastUsableAddress => LastAddressInRange;
    /// <inheritdoc />
    public IPAddress? LastAddressInRange => ToIpAddress ( _lastAddressBigInt );
    /// <inheritdoc />
    public BigInteger TotalAddresses { get; }
    /// <inheritdoc />
    public bool IsHost { get; }
    /// <summary>
    /// Gets a value indicating whether this is the loopback address (::1/128).
    /// </summary>
    public bool IsLoopback => IsHost && NetworkAddress.Equals ( IPAddress.IPv6Loopback );
    /// <summary>
    /// Gets a value indicating whether this is a link-local address (fe80::/10).
    /// </summary>
    public bool IsLinkLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();

            return bytes[0] == 0xFE && ( bytes[1] & 0xC0 ) == 0x80;
        }
    }
    /// <summary>
    /// Gets a value indicating whether this is a unique local address (fc00::/7).
    /// </summary>
    public bool IsUniqueLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();

            return ( bytes[0] & 0xFE ) == 0xFC;
        }
    }

    /// <inheritdoc />
    public bool Contains ( IPAddress? ipAddress )
    {
        if ( ipAddress is null ) { throw new ArgumentNullException ( nameof ( ipAddress ) ); }

        if ( ipAddress.AddressFamily != AddressFamily.InterNetworkV6 ) { return false; }

        var ipBigInt = ToBigInteger ( ipAddress );

        return ipBigInt >= _networkAddressBigInt && ipBigInt <= _lastAddressBigInt;
    }

    /// <inheritdoc />
    public bool OverlapsWith ( NetRangeV6 other ) => _networkAddressBigInt <= other._lastAddressBigInt && _lastAddressBigInt >= other._networkAddressBigInt;

    /// <inheritdoc />
    public bool IsSubnetOf ( NetRangeV6 other ) => _networkAddressBigInt >= other._networkAddressBigInt && _lastAddressBigInt <= other._lastAddressBigInt;

    /// <inheritdoc />
    public bool IsSupernetOf ( NetRangeV6 other ) => other.IsSubnetOf ( this );

    /// <inheritdoc />
    public IEnumerable< NetRangeV6 > GetSubnets ( int newPrefix )
    {
        if ( newPrefix <= CidrPrefix || newPrefix > 128 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"New prefix must be larger than {CidrPrefix} and less than or equal to 128." ); }

        var prefixDifference = newPrefix - CidrPrefix;

        if ( prefixDifference > 63 ) {
            // Prevents generating an excessive number of subnets (more than 2^63).
            throw new ArgumentException ( "Too many subnets would be generated. Maximum prefix difference is 63." , nameof ( newPrefix ) );
        }

        var subnetSize = BigInteger.Pow ( 2 , 128 - newPrefix );
        var currentAddress = _networkAddressBigInt;
        var maxValue = ( BigInteger.One << 128 ) - 1;

        while ( currentAddress <= _lastAddressBigInt ) {
            yield return new NetRangeV6 ( ToIpAddress ( currentAddress ) , newPrefix );

            if ( currentAddress > maxValue - subnetSize ) { break; } // Prevent overflow

            currentAddress += subnetSize;
        }
    }

    /// <summary>
    /// Calculates the supernet of this network range with a smaller prefix.
    /// </summary>
    /// <param name="newPrefix">The new, smaller CIDR prefix for the supernet.</param>
    /// <returns>A new <see cref="NetRangeV6"/> instance representing the supernet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the new prefix is not smaller than the current prefix or is invalid.</exception>
    public NetRangeV6 GetSupernet ( int newPrefix )
    {
        if ( newPrefix >= CidrPrefix || newPrefix < 0 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"New prefix must be smaller than {CidrPrefix} and greater than or equal to 0." ); }

        BigInteger newMask;

        if ( newPrefix == 0 ) { newMask = BigInteger.Zero; }
        else { newMask = ( BigInteger.One << 128 ) - 1 & ~( ( BigInteger.One << 128 - newPrefix ) - 1 ); }

        var newNetworkAddress = _networkAddressBigInt & newMask;

        return new NetRangeV6 ( ToIpAddress ( newNetworkAddress ) , newPrefix );
    }

    /// <inheritdoc />
    public int CompareTo ( NetRangeV6 other )
    {
        var networkComparison = _networkAddressBigInt.CompareTo ( other._networkAddressBigInt );

        return networkComparison == 0 ? CidrPrefix.CompareTo ( other.CidrPrefix ) : networkComparison;
    }

    /// <inheritdoc />
    public bool Equals ( NetRangeV6 other ) => CidrPrefix == other.CidrPrefix && _networkAddressBigInt.Equals ( other._networkAddressBigInt );

    /// <summary>
    /// Returns the string representation of the network range in CIDR notation.
    /// </summary>
    /// <returns>The CIDR string (e.g., "2001:db8::/32").</returns>
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

    /// <summary>
    /// Tries to parse a string in CIDR notation into a <see cref="NetRangeV6"/>.
    /// </summary>
    /// <param name="cidr">The CIDR string to parse.</param>
    /// <param name="result">When this method returns, contains the parsed NetRangeV6, if the parsing succeeded, or a default value if the parsing failed.</param>
    /// <returns>True if the string was successfully parsed; otherwise, false.</returns>
    public static bool TryParse ( string? cidr , out NetRangeV6 result )
    {
        result = default;

        if ( string.IsNullOrWhiteSpace ( cidr ) ) { return false; }

        var parts = cidr?.Split ( '/' );

        if ( parts is not
            {
                Length: 2
            } ) { return false; }

        if ( !IPAddress.TryParse ( parts[0] , out var ip ) || ip.AddressFamily != AddressFamily.InterNetworkV6 ) { return false; }

        if ( !int.TryParse ( parts[1] , out var prefix ) || prefix is < 0 or > 128 ) { return false; }

        try {
            result = new NetRangeV6 ( ip , prefix );

            return true;
        }
        catch { return false; }
    }
}

#if NET5_0_OR_GREATER
public readonly partial record struct NetRangeV6
{
    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(CidrPrefix, _networkAddressBigInt);
}
#else
public readonly partial record struct NetRangeV6
{
    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked {
            var hash = 17;
            hash = hash * 31 + CidrPrefix;
            hash = hash * 31 + _networkAddressBigInt.GetHashCode();

            return hash;
        }
    }
}
#endif
