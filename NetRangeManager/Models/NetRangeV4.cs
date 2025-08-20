using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

/// <summary>
/// Repräsentiert einen IPv4-Netzwerkbereich in CIDR-Notation.
/// </summary>
public readonly partial record struct NetRangeV4 : INetRange<NetRangeV4>
{
    // --- Private Felder ---
    private readonly uint _networkAddressUInt;
    private readonly uint _broadcastAddressUInt;

    // --- Cached Properties für Performance ---
    private readonly IPAddress? _firstUsableAddressCache;
    private readonly IPAddress? _lastUsableAddressCache;

    // --- Konstruktoren ---
    /// <summary>
    /// Initialisiert eine neue Instanz der NetRangeV4-Struktur aus einer CIDR-Notation.
    /// </summary>
    /// <param name="cidr">Die CIDR-Notation (z.B. "192.168.1.0/24").</param>
    /// <exception cref="ArgumentException">Wird geworfen, wenn die CIDR-Notation ungültig ist.</exception>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn cidr null ist.</exception>
    public NetRangeV4(string cidr)
    {
        if(cidr is null) {
            throw new ArgumentNullException(nameof(cidr));
        }

        if (!TryParse(cidr, out this))
        {
            throw new ArgumentException($"Ungültige IPv4 CIDR-Notation: '{cidr}'", nameof(cidr));
        }
    }

    /// <summary>
    /// Initialisiert eine neue Instanz der NetRangeV4-Struktur aus einer IP-Adresse und einem Präfix.
    /// </summary>
    /// <param name="ip">Die IPv4-Adresse.</param>
    /// <param name="prefix">Das CIDR-Präfix (0-32).</param>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn ip null ist.</exception>
    /// <exception cref="ArgumentException">Wird geworfen, wenn ip keine IPv4-Adresse ist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Wird geworfen, wenn prefix nicht zwischen 0 und 32 liegt.</exception>
    public NetRangeV4(IPAddress ip, int prefix)
    {
        // --- Validierung ---
        if(ip is null) {
            throw new ArgumentNullException(nameof(ip));
        }

       if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Nur IPv4-Adressen werden unterstützt.", nameof(ip));
        }

        if (prefix is < 0 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), prefix,
                "IPv4-Präfix muss zwischen 0 und 32 liegen.");
        }

        // --- Berechnung ---
        CidrPrefix = prefix;
        var ipUInt = ToUInt32(ip);
        var mask = prefix == 0 ? 0u : 0xFFFFFFFFu << 32 - prefix;

        _networkAddressUInt = ipUInt & mask;
        _broadcastAddressUInt = _networkAddressUInt | ~mask;

        NetworkAddress = ToIpAddress(_networkAddressUInt);
        TotalAddresses = BigInteger.Pow(2, 32 - prefix);
        IsHost = prefix == 32;

        // --- Caching für Performance ---
        _firstUsableAddressCache = prefix >= 31
            ? NetworkAddress
            : ToIpAddress(_networkAddressUInt + 1);

        _lastUsableAddressCache = prefix >= 31
            ? NetworkAddress
            : ToIpAddress(_broadcastAddressUInt - 1);
    }

    // --- Private Hilfsmethoden ---
    private static uint ToUInt32(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static IPAddress ToIpAddress(uint addressValue)
    {
        var bytes = BitConverter.GetBytes(addressValue);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        return new IPAddress(bytes);
    }

    // --- Öffentliche Eigenschaften ---
    /// <summary>
    /// Die Netzwerkadresse (erste Adresse im Bereich).
    /// </summary>
    public IPAddress NetworkAddress { get; }

    /// <summary>
    /// Das CIDR-Präfix (0-32).
    /// </summary>
    public int CidrPrefix { get; }

    /// <summary>
    /// Die erste nutzbare Host-Adresse im Bereich.
    /// Bei /31 und /32 Netzen entspricht dies der Netzwerkadresse.
    /// </summary>
    public IPAddress FirstUsableAddress => _firstUsableAddressCache ?? NetworkAddress;

    /// <summary>
    /// Die letzte nutzbare Host-Adresse im Bereich.
    /// Bei /31 und /32 Netzen entspricht dies der Netzwerkadresse.
    /// </summary>
    public IPAddress LastUsableAddress => _lastUsableAddressCache ?? NetworkAddress;

    /// <summary>
    /// Die absolut letzte Adresse im Bereich (Broadcast-Adresse).
    /// </summary>
    public IPAddress LastAddressInRange => ToIpAddress(_broadcastAddressUInt);

    /// <summary>
    /// Die Gesamtzahl der IP-Adressen im Bereich.
    /// </summary>
    public BigInteger TotalAddresses { get; }

    /// <summary>
    /// Gibt an, ob es sich um einen Host-Bereich handelt (/32).
    /// </summary>
    public bool IsHost { get; }

    /// <summary>
    /// Gibt an, ob es sich um einen privaten IP-Bereich nach RFC 1918 handelt.
    /// </summary>
    public bool IsPrivateRange => IsRfc1918Private();

    /// <summary>
    /// Gibt an, ob es sich um eine Loopback-Adresse handelt (127.0.0.0/8).
    /// </summary>
    public bool IsLoopback => (_networkAddressUInt & 0xFF000000u) == 0x7F000000u;

    // --- Interface-Implementierung ---
    /// <summary>
    /// Überprüft, ob die angegebene IP-Adresse in diesem Netzwerkbereich enthalten ist.
    /// </summary>
    /// <param name="ipAddress">Die zu überprüfende IP-Adresse.</param>
    /// <returns><c>true</c>, wenn die Adresse im Bereich liegt, andernfalls <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn <paramref name="ipAddress"/> null ist.</exception>
    public bool Contains(IPAddress ipAddress)
    {
        if(ipAddress is null) {
            throw new ArgumentNullException(nameof(ipAddress));
        }

        // IPv6-Adressen können per Definition nicht in IPv4-Bereichen enthalten sein
        if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var ipUInt = ToUInt32(ipAddress);
        return ipUInt >= _networkAddressUInt && ipUInt <= _broadcastAddressUInt;
    }

    /// <summary>
    /// Überprüft, ob dieser Netzwerkbereich mit einem anderen Bereich überlappt.
    /// </summary>
    /// <param name="other">Der andere zu vergleichende Netzwerkbereich.</param>
    /// <returns><c>true</c>, wenn eine Überlappung besteht, andernfalls <c>false</c>.</returns>
    public bool OverlapsWith(NetRangeV4 other) =>
        _networkAddressUInt <= other._broadcastAddressUInt &&
        _broadcastAddressUInt >= other._networkAddressUInt;

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Subnetz des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Supernet-Bereich.</param>
    /// <returns><c>true</c>, wenn dieser Bereich vollständig im anderen enthalten ist.</returns>
    public bool IsSubnetOf(NetRangeV4 other) =>
        _networkAddressUInt >= other._networkAddressUInt &&
        _broadcastAddressUInt <= other._broadcastAddressUInt;

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Supernet des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Subnet-Bereich.</param>
    /// <returns><c>true</c>, wenn der andere Bereich vollständig in diesem enthalten ist.</returns>
    public bool IsSupernetOf(NetRangeV4 other) => other.IsSubnetOf(this);

    /// <summary>
    /// Teilt das aktuelle Netzwerk in kleinere Subnetze mit dem angegebenen neuen Präfix auf.
    /// </summary>
    /// <param name="newPrefix">Das neue CIDR-Präfix für die Subnetze. Muss größer als das aktuelle sein.</param>
    /// <returns>Eine Aufzählung der resultierenden Subnetze.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Wird geworfen, wenn newPrefix ungültig ist.</exception>
    public IEnumerable<NetRangeV4> GetSubnets(int newPrefix)
    {
        if (newPrefix <= CidrPrefix || newPrefix > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix,
                $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 32 sein.");
        }

        var subnetSize = 1u << 32 - newPrefix;
        var currentAddress = _networkAddressUInt;

        while (currentAddress <= _broadcastAddressUInt)
        {
            yield return new NetRangeV4(ToIpAddress(currentAddress), newPrefix);

            // Overflow-Schutz
            if (currentAddress > uint.MaxValue - subnetSize)
            {
                break;
            }

            currentAddress += subnetSize;
        }
    }

    /// <summary>
    /// Erstellt ein Supernet mit dem angegebenen kleineren Präfix.
    /// </summary>
    /// <param name="newPrefix">Das neue CIDR-Präfix. Muss kleiner als das aktuelle sein.</param>
    /// <returns>Das resultierende Supernet.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Wird geworfen, wenn newPrefix ungültig ist.</exception>
    public NetRangeV4 GetSupernet(int newPrefix)
    {
        if (newPrefix >= CidrPrefix || newPrefix < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix,
                $"Neues Präfix muss kleiner als {CidrPrefix} und größer/gleich 0 sein.");
        }

        var newMask = newPrefix == 0 ? 0u : 0xFFFFFFFFu << 32 - newPrefix;
        var newNetworkAddress = _networkAddressUInt & newMask;

        return new NetRangeV4(ToIpAddress(newNetworkAddress), newPrefix);
    }

    // --- Vergleichsmethoden ---
    public int CompareTo(NetRangeV4 other)
    {
        var networkComparison = _networkAddressUInt.CompareTo(other._networkAddressUInt);
        return networkComparison == 0 ? CidrPrefix.CompareTo(other.CidrPrefix) : networkComparison;
    }

    public bool Equals(NetRangeV4 other) =>
        CidrPrefix == other.CidrPrefix && _networkAddressUInt == other._networkAddressUInt;

    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

    // --- Parsing-Methoden ---
    /// <summary>
    /// Versucht, eine CIDR-Notation zu parsen und eine NetRangeV4-Instanz zu erstellen.
    /// </summary>
    /// <param name="cidr">Die CIDR-Notation.</param>
    /// <param name="result">Die resultierende NetRangeV4-Instanz.</param>
    /// <returns><c>true</c>, wenn das Parsen erfolgreich war, andernfalls <c>false</c>.</returns>
    public static bool TryParse(string? cidr, out NetRangeV4 result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(cidr))
        {
            return false;
        }

        var parts = cidr?.Split('/');
        if (parts is not
            {
                Length: 2
            })
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var ip) ||
            ip.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var prefix) || prefix is < 0 or > 32)
        {
            return false;
        }

        try
        {
            result = new NetRangeV4(ip, prefix);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // --- Private Hilfsmethoden ---
    private bool IsRfc1918Private()
    {
        // 10.0.0.0/8
        if ( ( _networkAddressUInt & 0xFF000000u ) == 0x0A000000u ) { return true; }

        if ( ( _networkAddressUInt & 0xFFF00000u ) == 0xAC100000u ) { return true; }

        // 192.168.0.0/16
        if ( ( _networkAddressUInt & 0xFFFF0000u ) == 0xC0A80000u ) { return true; }

        return false;

        // 172.16.0.0/12
    }
}

// --- Plattformspezifische GetHashCode-Implementierung ---
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