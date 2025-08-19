using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

// Hauptdefinition der Klasse
public readonly partial record struct NetRangeV4 : INetRange< NetRangeV4 >
{
    // --- Private Felder ---
    private readonly uint _networkAddressUInt;
    public readonly uint BroadcastAddressUInt;
    private readonly IPAddress _lastUsableAddress;

    // --- Konstruktor ---
    public NetRangeV4 ( string cidr )
    {
        var parts = cidr.Split ( '/' );
        var ip = IPAddress.Parse ( parts[0] );
        CidrPrefix = int.Parse ( parts[1] );
        var ipUInt = ToUInt32 ( ip );
        var mask = CidrPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - CidrPrefix;
        _networkAddressUInt = ipUInt & mask;
        BroadcastAddressUInt = _networkAddressUInt | ~mask;
        NetworkAddress = ToIpAddress ( _networkAddressUInt );
        TotalAddresses = BigInteger.Pow ( 2 , 32 - CidrPrefix );
        IsHost = CidrPrefix == 32;
    }

    public NetRangeV4(IPAddress ip, int prefix)
    {
        CidrPrefix = prefix;
        var ipUInt = ToUInt32(ip);
        var mask = CidrPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - CidrPrefix;

        _networkAddressUInt = ipUInt & mask;
        BroadcastAddressUInt = _networkAddressUInt | ~mask;

        NetworkAddress = ToIpAddress(_networkAddressUInt);
        TotalAddresses = BigInteger.Pow(2, 32 - CidrPrefix);
        IsHost = CidrPrefix == 32;
    }

    // --- Private Hilfsmethoden ---
    private static uint ToUInt32 ( IPAddress ipAddress )
    {
        var bytes = ipAddress.GetAddressBytes();

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        return BitConverter.ToUInt32 ( bytes , 0 );
    }

    public static IPAddress ToIpAddress ( uint addressValue )
    {
        var bytes = BitConverter.GetBytes ( addressValue );

        if ( BitConverter.IsLittleEndian ) { Array.Reverse ( bytes ); }

        return new IPAddress ( bytes );
    }

    // --- Öffentliche Eigenschaften ---
    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => CidrPrefix >= 31 ? NetworkAddress : ToIpAddress ( _networkAddressUInt + 1 );
    public IPAddress LastUsableAddress => _lastUsableAddress;
    public IPAddress LastAddressInRange => ToIpAddress ( BroadcastAddressUInt );
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    // --- Interface-Methoden ---

    public bool Contains(IPAddress ipAddress)
    {
        var ipUInt = ToUInt32(ipAddress);
        return ipUInt >= _networkAddressUInt && ipUInt <= BroadcastAddressUInt;
    }


    // NEUE IMPLEMENTIERUNG 1:
    public bool OverlapsWith(NetRangeV4 other)
    {
        // Zwei Bereiche überschneiden sich, wenn der Anfang des einen VOR dem Ende des anderen liegt
        // UND das Ende des einen NACH dem Anfang des anderen liegt.
        return _networkAddressUInt <= other.BroadcastAddressUInt && BroadcastAddressUInt >= other._networkAddressUInt;
    }

    // NEUE IMPLEMENTIERUNG 2:
    public bool IsSubnetOf(NetRangeV4 other)
    {
        // Ein Bereich ist ein Subnetz, wenn sein Anfang größer/gleich dem Anfang des anderen ist
        // UND sein Ende kleiner/gleich dem Ende des anderen ist.
        return _networkAddressUInt >= other._networkAddressUInt && BroadcastAddressUInt <= other.BroadcastAddressUInt;
    }

    public bool IsSupernetOf(NetRangeV4 other) => other.IsSubnetOf(this);
    public IEnumerable<NetRangeV4> GetSubnets(int newPrefix)
    {
        // Validierung: Das neue Präfix muss größer (also das Netz kleiner) sein.
        if (newPrefix <= CidrPrefix || newPrefix > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 32 sein.");
        }

        // Die Größe (Anzahl der Adressen) eines einzelnen neuen Subnetzes.
        var subnetSize = 1u << 32 - newPrefix;

        // Die letzte mögliche Adresse in unserem ursprünglichen Bereich.
        var lastAddress = BroadcastAddressUInt;

        // Wir starten beim Anfang unseres aktuellen Netzes.
        var currentAddress = _networkAddressUInt;

        while (currentAddress <= lastAddress)
        {
            // Erzeuge ein neues NetRangeV4-Objekt für das aktuelle Subnetz.
            yield return new NetRangeV4(ToIpAddress(currentAddress), newPrefix);

            // Springe zum Anfang des nächsten Subnetzes.
            currentAddress += subnetSize;
        }
    }    public int CompareTo(NetRangeV4 other)
    {
        // Vergleiche zuerst die numerischen Werte der Netzwerkadressen.
        var networkComparison = _networkAddressUInt.CompareTo(other._networkAddressUInt);

        // Wenn die Netzwerkadressen gleich sind, ist das Netz mit dem
        // GRÖSSEREN Präfix (also das kleinere Netz) "größer" in der Sortierung.
        // Das ist eine gängige Konvention.
        return networkComparison == 0 ? CidrPrefix.CompareTo(other.CidrPrefix) : networkComparison;
    }
    public bool Equals(NetRangeV4 other) => CidrPrefix == other.CidrPrefix && _networkAddressUInt == other._networkAddressUInt;
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";
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
