using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

public readonly partial record struct NetRangeV6 : INetRange< NetRangeV6 >
{
    private readonly BigInteger _networkAddressBigInt;
    private readonly BigInteger _lastAddressBigInt;

    public NetRangeV6 ( string cidr )
    {
        if ( cidr is null ) { throw new ArgumentNullException ( nameof ( cidr ) ); }

        if ( !TryParse ( cidr , out this ) ) { throw new ArgumentException ( $"Ungültige IPv6 CIDR-Notation: '{cidr}'" , nameof ( cidr ) ); }
    }

    public NetRangeV6 ( IPAddress ip , int prefix )
    {
        if ( ip is null ) { throw new ArgumentNullException ( nameof ( ip ) ); }

        if ( ip.AddressFamily != AddressFamily.InterNetworkV6 ) { throw new ArgumentException ( "Nur IPv6-Adressen werden unterstützt." , nameof ( ip ) ); }

        if ( prefix is < 0 or > 128 ) { throw new ArgumentOutOfRangeException ( nameof ( prefix ) , prefix , "IPv6-Präfix muss zwischen 0 und 128 liegen." ); }

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

    private static BigInteger ToBigInteger ( IPAddress ipAddress )
    {
        var bytes = ipAddress.GetAddressBytes();

        if ( bytes.Length != 16 ) { throw new ArgumentException ( "IPv6-Adresse muss genau 16 Bytes haben." ); }

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        var bytesWithPadding = new byte[ bytes.Length + 1 ];
        bytes.CopyTo ( bytesWithPadding , 0 );

        return new BigInteger ( bytesWithPadding );
    }

    private static IPAddress ToIpAddress ( BigInteger addressValue )
    {
        if ( addressValue < 0 ) { throw new ArgumentOutOfRangeException ( nameof ( addressValue ) , "IPv6-Adresse kann nicht negativ sein." ); }

        var bytes = addressValue.ToByteArray();
        var ipBytes = new byte[ 16 ];
        var bytesToCopy = Math.Min ( bytes.Length , 16 );
        Array.Copy ( bytes , ipBytes , bytesToCopy );

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( ipBytes ); }

        return new IPAddress ( ipBytes );
    }

    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => NetworkAddress;
    public IPAddress LastUsableAddress => LastAddressInRange;
    public IPAddress LastAddressInRange => ToIpAddress ( _lastAddressBigInt );
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }
    public bool IsLoopback => IsHost && NetworkAddress.Equals ( IPAddress.IPv6Loopback );
    public bool IsLinkLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();

            return bytes[0] == 0xFE && ( bytes[1] & 0xC0 ) == 0x80;
        }
    }
    public bool IsUniqueLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();

            return ( bytes[0] & 0xFE ) == 0xFC;
        }
    }

    public bool Contains ( IPAddress ipAddress )
    {
        if ( ipAddress is null ) { throw new ArgumentNullException ( nameof ( ipAddress ) ); }

        if ( ipAddress.AddressFamily != AddressFamily.InterNetworkV6 ) { return false; }

        var ipBigInt = ToBigInteger ( ipAddress );

        return ipBigInt >= _networkAddressBigInt && ipBigInt <= _lastAddressBigInt;
    }

    public bool OverlapsWith ( NetRangeV6 other ) => _networkAddressBigInt <= other._lastAddressBigInt && _lastAddressBigInt >= other._networkAddressBigInt;
    public bool IsSubnetOf ( NetRangeV6 other ) => _networkAddressBigInt >= other._networkAddressBigInt && _lastAddressBigInt <= other._lastAddressBigInt;
    public bool IsSupernetOf ( NetRangeV6 other ) => other.IsSubnetOf ( this );

    public IEnumerable< NetRangeV6 > GetSubnets ( int newPrefix )
    {
        if ( newPrefix <= CidrPrefix || newPrefix > 128 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 128 sein." ); }

        var prefixDifference = newPrefix - CidrPrefix;

        if ( prefixDifference > 63 ) { throw new ArgumentException ( "Zu viele Subnetze würden generiert. Maximaler Präfix-Unterschied: 63" ); }

        var subnetSize = BigInteger.Pow ( 2 , 128 - newPrefix );
        var currentAddress = _networkAddressBigInt;

        while ( currentAddress <= _lastAddressBigInt ) {
            yield return new NetRangeV6 ( ToIpAddress ( currentAddress ) , newPrefix );

            var maxValue = BigInteger.Pow ( 2 , 128 ) - 1;

            if ( currentAddress > maxValue - subnetSize ) { break; }

            currentAddress += subnetSize;
        }
    }

    public NetRangeV6 GetSupernet ( int newPrefix )
    {
        if ( newPrefix >= CidrPrefix || newPrefix < 0 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , newPrefix , $"Neues Präfix muss kleiner als {CidrPrefix} und größer/gleich 0 sein." ); }

        BigInteger newMask;

        if ( newPrefix == 0 ) { newMask = BigInteger.Zero; }
        else { newMask = ( BigInteger.One << 128 ) - 1 & ~( ( BigInteger.One << 128 - newPrefix ) - 1 ); }

        var newNetworkAddress = _networkAddressBigInt & newMask;

        return new NetRangeV6 ( ToIpAddress ( newNetworkAddress ) , newPrefix );
    }

    public int CompareTo ( NetRangeV6 other )
    {
        var networkComparison = _networkAddressBigInt.CompareTo ( other._networkAddressBigInt );

        return networkComparison == 0 ? CidrPrefix.CompareTo ( other.CidrPrefix ) : networkComparison;
    }

    public bool Equals ( NetRangeV6 other ) => CidrPrefix == other.CidrPrefix && _networkAddressBigInt.Equals ( other._networkAddressBigInt );
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

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
    public override int GetHashCode() => HashCode.Combine(CidrPrefix, _networkAddressBigInt);
}
#else
public readonly partial record struct NetRangeV6
{
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
