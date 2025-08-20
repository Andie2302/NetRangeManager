using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

public readonly partial record struct NetRangeV4 : INetRange< NetRangeV4 >
{
    private readonly uint _networkAddressUInt;
    private readonly uint _broadcastAddressUInt;
    private readonly IPAddress? _firstUsableAddressCache;
    private readonly IPAddress? _lastUsableAddressCache;

    public NetRangeV4 ( string cidr )
    {
        if ( cidr is null ) { throw new ArgumentNullException ( nameof ( cidr ) ); }

        if ( !TryParse ( cidr , out this ) ) { throw new ArgumentException ( $"Ungültige IPv4 CIDR-Notation: '{cidr}'" , nameof ( cidr ) ); }
    }

    public NetRangeV4 ( IPAddress ip , int prefix )
    {
        if ( ip is null ) { throw new ArgumentNullException ( nameof ( ip ) ); }

        if ( ip.AddressFamily != AddressFamily.InterNetwork ) { throw new ArgumentException ( "Nur IPv4-Adressen werden unterstützt." , nameof ( ip ) ); }

        if ( prefix is < 0 or > 32 ) { throw new ArgumentOutOfRangeException ( nameof ( prefix ) , prefix , "IPv4-Präfix muss zwischen 0 und 32 liegen." ); }

        CidrPrefix = prefix;
        var ipUInt = ToUInt32 ( ip );
        var mask = prefix == 0 ? 0u : 0xFFFFFFFFu << 32 - prefix;
        _networkAddressUInt = ipUInt & mask;
        _broadcastAddressUInt = _networkAddressUInt | ~mask;
        NetworkAddress = ToIpAddress ( _networkAddressUInt );
        TotalAddresses = BigInteger.Pow ( 2 , 32 - prefix );
        IsHost = prefix == 32;
        _firstUsableAddressCache = prefix >= 31 ? NetworkAddress : ToIpAddress ( _networkAddressUInt + 1 );
        _lastUsableAddressCache = prefix >= 31 ? NetworkAddress : ToIpAddress ( _broadcastAddressUInt - 1 );
    }

    private static uint ToUInt32 ( IPAddress ipAddress )
    {
        var bytes = ipAddress.GetAddressBytes();

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        return BitConverter.ToUInt32 ( bytes , 0 );
    }

    private static IPAddress ToIpAddress ( uint addressValue )
    {
        var bytes = BitConverter.GetBytes ( addressValue );

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        return new IPAddress ( bytes );
    }

    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => _firstUsableAddressCache ?? NetworkAddress;
    public IPAddress LastUsableAddress => _lastUsableAddressCache ?? NetworkAddress;
    public IPAddress LastAddressInRange => ToIpAddress ( _broadcastAddressUInt );
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }
    public bool IsPrivateRange => IsRfc1918Private();
    public bool IsLoopback => ( _networkAddressUInt & 0xFF000000u ) == 0x7F000000u;

    public bool Contains ( IPAddress ipAddress )
    {
        if ( ipAddress is null ) { throw new ArgumentNullException ( nameof ( ipAddress ) ); }

        if ( ipAddress.AddressFamily != AddressFamily.InterNetwork ) { return false; }

        var ipUInt = ToUInt32 ( ipAddress );

        return ipUInt >= _networkAddressUInt && ipUInt <= _broadcastAddressUInt;
    }

    public bool OverlapsWith ( NetRangeV4 other ) => _networkAddressUInt <= other._broadcastAddressUInt && _broadcastAddressUInt >= other._networkAddressUInt;
    public bool IsSubnetOf ( NetRangeV4 other ) => _networkAddressUInt >= other._networkAddressUInt && _broadcastAddressUInt <= other._broadcastAddressUInt;
    public bool IsSupernetOf ( NetRangeV4 other ) => other.IsSubnetOf ( this );

    public IEnumerable< NetRangeV4 > GetSubnets ( int newPrefix )
    {
        if ( newPrefix <= CidrPrefix || newPrefix > 32 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 32 sein." ); }

        var subnetSize = 1u << 32 - newPrefix;
        var currentAddress = _networkAddressUInt;

        while ( currentAddress <= _broadcastAddressUInt ) {
            yield return new NetRangeV4 ( ToIpAddress ( currentAddress ) , newPrefix );

            if ( currentAddress > uint.MaxValue - subnetSize ) { break; }

            currentAddress += subnetSize;
        }
    }

    public NetRangeV4 GetSupernet ( int newPrefix )
    {
        if ( newPrefix >= CidrPrefix || newPrefix < 0 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"Neues Präfix muss kleiner als {CidrPrefix} und größer/gleich 0 sein." ); }

        var newMask = newPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - newPrefix;
        var newNetworkAddress = _networkAddressUInt & newMask;

        return new NetRangeV4 ( ToIpAddress ( newNetworkAddress ) , newPrefix );
    }

    public int CompareTo ( NetRangeV4 other )
    {
        var networkComparison = _networkAddressUInt.CompareTo ( other._networkAddressUInt );

        return networkComparison == 0 ? CidrPrefix.CompareTo ( other.CidrPrefix ) : networkComparison;
    }

    public bool Equals ( NetRangeV4 other ) => CidrPrefix == other.CidrPrefix && _networkAddressUInt == other._networkAddressUInt;
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

    public static bool TryParse ( string? cidr , out NetRangeV4 result )
    {
        result = default;

        if ( string.IsNullOrWhiteSpace ( cidr ) ) { return false; }

        var parts = cidr?.Split ( '/' );

        if ( parts is not
            {
                Length: 2
            } ) { return false; }

        if ( !IPAddress.TryParse ( parts[0] , out var ip ) || ip.AddressFamily != AddressFamily.InterNetwork ) { return false; }

        if ( !int.TryParse ( parts[1] , out var prefix ) || prefix is < 0 or > 32 ) { return false; }

        try {
            result = new NetRangeV4 ( ip , prefix );

            return true;
        }
        catch { return false; }
    }

    private bool IsRfc1918Private()
    {
        if ( ( _networkAddressUInt & 0xFF000000u ) == 0x0A000000u ) { return true; }

        if ( ( _networkAddressUInt & 0xFFF00000u ) == 0xAC100000u ) { return true; }

        if ( ( _networkAddressUInt & 0xFFFF0000u ) == 0xC0A80000u ) { return true; }

        return false;
    }
}

#if NET5_0_OR_GREATER
public readonly partial record struct NetRangeV4
{
    public override int GetHashCode() => HashCode.Combine(CidrPrefix, _networkAddressUInt);
}
#else
public readonly partial record struct NetRangeV4
{
    public override int GetHashCode()
    {
        unchecked {
            var hash = 17;
            hash = hash * 31 + CidrPrefix;
            hash = hash * 31 + (int) _networkAddressUInt;

            return hash;
        }
    }
}
#endif
