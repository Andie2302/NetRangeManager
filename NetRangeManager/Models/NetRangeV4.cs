using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

// Hauptdefinition der Klasse
public readonly partial record struct NetRangeV4 : INetRange< NetRangeV4 >
{
    // --- Private Felder ---
    private readonly uint _networkAddressUInt;
    private readonly uint _broadcastAddressUInt;

    // --- Konstruktor ---
    public NetRangeV4 ( string cidr )
    {
        var parts = cidr.Split ( '/' );
        var ip = IPAddress.Parse ( parts[0] );
        CidrPrefix = int.Parse ( parts[1] );
        var ipUInt = ToUInt32 ( ip );
        var mask = CidrPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - CidrPrefix;
        _networkAddressUInt = ipUInt & mask;
        _broadcastAddressUInt = _networkAddressUInt | ~mask;
        NetworkAddress = ToIpAddress ( _networkAddressUInt );
        TotalAddresses = BigInteger.Pow ( 2 , 32 - CidrPrefix );
        IsHost = CidrPrefix == 32;
    }

    // --- Private Hilfsmethoden ---
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

    // --- Öffentliche Eigenschaften ---
    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => CidrPrefix >= 31 ? NetworkAddress : ToIpAddress ( _networkAddressUInt + 1 );
    public IPAddress LastAddressInRange => ToIpAddress ( _broadcastAddressUInt );
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    // --- Interface-Methoden ---

    public bool Contains(IPAddress ipAddress)
    {
        var ipUInt = ToUInt32(ipAddress);
        return ipUInt >= _networkAddressUInt && ipUInt <= _broadcastAddressUInt;
    }


    // NEUE IMPLEMENTIERUNG 1:
    public bool OverlapsWith(NetRangeV4 other)
    {
        // Zwei Bereiche überschneiden sich, wenn der Anfang des einen VOR dem Ende des anderen liegt
        // UND das Ende des einen NACH dem Anfang des anderen liegt.
        return _networkAddressUInt <= other._broadcastAddressUInt && _broadcastAddressUInt >= other._networkAddressUInt;
    }

    // NEUE IMPLEMENTIERUNG 2:
    public bool IsSubnetOf(NetRangeV4 other)
    {
        // Ein Bereich ist ein Subnetz, wenn sein Anfang größer/gleich dem Anfang des anderen ist
        // UND sein Ende kleiner/gleich dem Ende des anderen ist.
        return _networkAddressUInt >= other._networkAddressUInt && _broadcastAddressUInt <= other._broadcastAddressUInt;
    }

    public bool IsSupernetOf(NetRangeV4 other) => other.IsSubnetOf(this);
    public IEnumerable<NetRangeV4> GetSubnets(int newPrefix) => throw new NotImplementedException();
    public int CompareTo(NetRangeV4 other) => throw new NotImplementedException();
    public bool Equals(NetRangeV4 other) => CidrPrefix == other.CidrPrefix && _networkAddressUInt == other._networkAddressUInt;
}

// --- Plattformspezifische Implementierung für GetHashCode ---
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
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
            var hashCode = 397;
            hashCode = hashCode * 397 ^ CidrPrefix;
            hashCode = hashCode * 397 ^ (int) _networkAddressUInt;

            return hashCode;
        }
    }
}
#endif
