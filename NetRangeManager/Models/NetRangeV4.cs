using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

// Hauptdefinition der Klasse
public readonly partial record struct NetRangeV4 : INetRange< NetRangeV4 >
{
    // --- Private Felder ---
    private readonly uint _networkAddressUInt;
    private readonly uint _broadcastAddressUInt; // Dieses Feld machen wir wieder private!

    // --- Konstruktoren ---
    public NetRangeV4 ( string cidr )
    {
        if ( !TryParse ( cidr , out this ) ) { throw new ArgumentException ( "Ungültige CIDR-Notation." , nameof ( cidr ) ); }
    }

    public NetRangeV4 ( IPAddress ip , int prefix )
    {
        // --- Defensive Prüfungen ---
        if ( ip is null ) { throw new ArgumentNullException ( nameof ( ip ) ); }

        if ( ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ) { throw new ArgumentException ( "Nur IPv4-Adressen werden unterstützt." , nameof ( ip ) ); }

        if ( prefix is < 0 or > 32 ) { throw new ArgumentOutOfRangeException ( nameof ( prefix ) , "Präfix für IPv4 muss zwischen 0 und 32 liegen." ); }

        // --- Ende der Prüfungen ---
        CidrPrefix = prefix;
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

    // HIER IST DIE KORREKTUR:
    public IPAddress LastUsableAddress => CidrPrefix >= 31 ? NetworkAddress : ToIpAddress ( _broadcastAddressUInt - 1 );
    public IPAddress LastAddressInRange => ToIpAddress ( _broadcastAddressUInt );
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    // ... (Der Rest der Klasse bleibt unverändert)
    /// <summary>
    /// Überprüft, ob die angegebene IP-Adresse in diesem Netzwerkbereich enthalten ist.
    /// </summary>
    /// <param name="ipAddress">Die zu überprüfende IP-Adresse. Darf nicht null sein.</param>
    /// <returns><c>true</c>, wenn die Adresse im Bereich liegt, andernfalls <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn <paramref name="ipAddress"/> null ist.</exception>
    /// <exception cref="ArgumentException">Wird geworfen, wenn eine IPv6-Adresse an eine IPv4-Range übergeben wird.</exception>
    public bool Contains ( IPAddress ipAddress )
    {
        // --- Defensive Programmierung ---
        if ( ipAddress is null ) {
            throw new ArgumentNullException ( nameof ( ipAddress ) );
        }

        if ( ipAddress.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ) {
            // Wir geben false zurück, anstatt eine Exception zu werfen.
            // Das ist oft benutzerfreundlicher, da eine IPv6-Adresse per definitionem
            // nicht in einem IPv4-Bereich enthalten sein kann.
            return false;
        }

        // --- Ende der Prüfungen ---
        var ipUInt = ToUInt32 ( ipAddress );

        return ipUInt >= _networkAddressUInt && ipUInt <= _broadcastAddressUInt;
    }

    public bool OverlapsWith ( NetRangeV4 other ) => _networkAddressUInt <= other._broadcastAddressUInt && _broadcastAddressUInt >= other._networkAddressUInt;
    public bool IsSubnetOf ( NetRangeV4 other ) => _networkAddressUInt >= other._networkAddressUInt && _broadcastAddressUInt <= other._broadcastAddressUInt;
    public bool IsSupernetOf ( NetRangeV4 other ) => other.IsSubnetOf ( this );

    public IEnumerable< NetRangeV4 > GetSubnets ( int newPrefix )
    {
        if ( newPrefix <= CidrPrefix || newPrefix > 32 ) { throw new ArgumentOutOfRangeException ( nameof ( newPrefix ) , $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 32 sein." ); }

        var subnetSize = 1u << 32 - newPrefix;
        var lastAddress = _broadcastAddressUInt;
        var currentAddress = _networkAddressUInt;

        while ( currentAddress <= lastAddress ) {
            yield return new NetRangeV4 ( ToIpAddress ( currentAddress ) , newPrefix );

            if ( currentAddress > uint.MaxValue - subnetSize ) { break; }

            currentAddress += subnetSize;
        }
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

        if ( !IPAddress.TryParse ( parts[0] , out var ip ) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ) { return false; }

        if ( !int.TryParse ( parts[1] , out var prefix ) ) { return false; }

        // Wir können jetzt sicher sein, dass die Eingabe gültig ist.
        result = new NetRangeV4 ( ip , prefix );

        return true;
    }
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
