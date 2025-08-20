using System.Numerics;
using System.Net;
using System.Net.Sockets;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

/// <summary>
/// Repräsentiert einen IPv6-Netzwerkbereich in CIDR-Notation.
/// </summary>
public readonly partial record struct NetRangeV6 : INetRange<NetRangeV6>
{
    // --- Private Felder ---
    private readonly BigInteger _networkAddressBigInt;
    private readonly BigInteger _lastAddressBigInt;

    // --- Konstruktoren ---
    /// <summary>
    /// Initialisiert eine neue Instanz der NetRangeV6-Struktur aus einer CIDR-Notation.
    /// </summary>
    /// <param name="cidr">Die CIDR-Notation (z.B. "2001:db8::/32").</param>
    /// <exception cref="ArgumentException">Wird geworfen, wenn die CIDR-Notation ungültig ist.</exception>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn cidr null ist.</exception>
    public NetRangeV6(string cidr)
    {
        if(cidr is null) {
            throw new ArgumentNullException(nameof(cidr));
        }

        if (!TryParse(cidr, out this))
        {
            throw new ArgumentException($"Ungültige IPv6 CIDR-Notation: '{cidr}'", nameof(cidr));
        }
    }

    /// <summary>
    /// Initialisiert eine neue Instanz der NetRangeV6-Struktur aus einer IP-Adresse und einem Präfix.
    /// </summary>
    /// <param name="ip">Die IPv6-Adresse.</param>
    /// <param name="prefix">Das CIDR-Präfix (0-128).</param>
    /// <exception cref="ArgumentNullException">Wird geworfen, wenn ip null ist.</exception>
    /// <exception cref="ArgumentException">Wird geworfen, wenn ip keine IPv6-Adresse ist.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Wird geworfen, wenn prefix nicht zwischen 0 und 128 liegt.</exception>
    public NetRangeV6(IPAddress ip, int prefix)
    {
        // --- Validierung ---
        if(ip is null) {
            throw new ArgumentNullException(nameof(ip));
        }
        
        if (ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            throw new ArgumentException("Nur IPv6-Adressen werden unterstützt.", nameof(ip));
        }
        
        if (prefix is < 0 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), prefix,
                "IPv6-Präfix muss zwischen 0 und 128 liegen.");
        }

        // --- Berechnung ---
        CidrPrefix = prefix;
        var ipBigInt = ToBigInteger(ip);
        
        // Maske berechnen - Vorsicht bei Edge Cases
        BigInteger mask;
        if (prefix == 0)
        {
            mask = BigInteger.Zero;
        }
        else if (prefix == 128)
        {
            mask = (BigInteger.One << 128) - 1;
        }
        else
        {
            mask = (BigInteger.One << 128) - 1 & ~((BigInteger.One << 128 - prefix) - 1);
        }

        _networkAddressBigInt = ipBigInt & mask;
        _lastAddressBigInt = _networkAddressBigInt | ~mask;

        NetworkAddress = ToIpAddress(_networkAddressBigInt);
        TotalAddresses = BigInteger.Pow(2, 128 - prefix);
        IsHost = prefix == 128;
    }

    // --- Private Hilfsmethoden ---
    private static BigInteger ToBigInteger(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        
        // Sicherstellen, dass wir 16 Bytes haben
        if (bytes.Length != 16)
        {
            throw new ArgumentException("IPv6-Adresse muss genau 16 Bytes haben.");
        }
        
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        // Null-Byte hinzufügen um sicherzustellen, dass BigInteger als positiv interpretiert wird
        var bytesWithPadding = new byte[bytes.Length + 1];
        bytes.CopyTo(bytesWithPadding, 0);
        
        return new BigInteger(bytesWithPadding);
    }

    private static IPAddress ToIpAddress(BigInteger addressValue)
    {
        // Negative Werte abfangen (sollten nicht vorkommen, aber Sicherheit)
        if (addressValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(addressValue),
                "IPv6-Adresse kann nicht negativ sein.");
        }

        var bytes = addressValue.ToByteArray();
        var ipBytes = new byte[16];

        // Bytes kopieren, aber nur bis zu 16 Bytes
        var bytesToCopy = Math.Min(bytes.Length, 16);
        Array.Copy(bytes, ipBytes, bytesToCopy);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ipBytes);
        }

        return new IPAddress(ipBytes);
    }

    // --- Öffentliche Eigenschaften ---
    /// <summary>
    /// Die Netzwerkadresse (erste Adresse im Bereich).
    /// </summary>
    public IPAddress NetworkAddress { get; }

    /// <summary>
    /// Das CIDR-Präfix (0-128).
    /// </summary>
    public int CidrPrefix { get; }

    /// <summary>
    /// Die erste nutzbare Host-Adresse im Bereich (bei IPv6 immer die Netzwerkadresse).
    /// </summary>
    public IPAddress FirstUsableAddress => NetworkAddress;

    /// <summary>
    /// Die letzte nutzbare Host-Adresse im Bereich (bei IPv6 die letzte Adresse).
    /// </summary>
    public IPAddress LastUsableAddress => LastAddressInRange;

    /// <summary>
    /// Die absolut letzte Adresse im Bereich.
    /// </summary>
    public IPAddress LastAddressInRange => ToIpAddress(_lastAddressBigInt);

    /// <summary>
    /// Die Gesamtzahl der IP-Adressen im Bereich.
    /// </summary>
    public BigInteger TotalAddresses { get; }

    /// <summary>
    /// Gibt an, ob es sich um einen Host-Bereich handelt (/128).
    /// </summary>
    public bool IsHost { get; }

    /// <summary>
    /// Gibt an, ob es sich um eine Loopback-Adresse handelt (::1/128).
    /// </summary>
    public bool IsLoopback => IsHost && NetworkAddress.Equals(IPAddress.IPv6Loopback);

    /// <summary>
    /// Gibt an, ob es sich um einen Link-Local-Bereich handelt (fe80::/10).
    /// </summary>
    public bool IsLinkLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();
            return bytes[0] == 0xFE && (bytes[1] & 0xC0) == 0x80;
        }
    }

    /// <summary>
    /// Gibt an, ob es sich um einen Unique Local Address-Bereich handelt (fc00::/7).
    /// </summary>
    public bool IsUniqueLocal
    {
        get
        {
            var bytes = NetworkAddress.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }
    }

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

        // IPv4-Adressen können per definitionem nicht in IPv6-Bereichen enthalten sein
        if (ipAddress.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var ipBigInt = ToBigInteger(ipAddress);
        return ipBigInt >= _networkAddressBigInt && ipBigInt <= _lastAddressBigInt;
    }

    /// <summary>
    /// Überprüft, ob dieser Netzwerkbereich mit einem anderen Bereich überlappt.
    /// </summary>
    /// <param name="other">Der andere zu vergleichende Netzwerkbereich.</param>
    /// <returns><c>true</c>, wenn eine Überlappung besteht, andernfalls <c>false</c>.</returns>
    public bool OverlapsWith(NetRangeV6 other) =>
        _networkAddressBigInt <= other._lastAddressBigInt &&
        _lastAddressBigInt >= other._networkAddressBigInt;

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Subnetz des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Supernet-Bereich.</param>
    /// <returns><c>true</c>, wenn dieser Bereich vollständig im anderen enthalten ist.</returns>
    public bool IsSubnetOf(NetRangeV6 other) =>
        _networkAddressBigInt >= other._networkAddressBigInt &&
        _lastAddressBigInt <= other._lastAddressBigInt;

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Supernet des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Subnet-Bereich.</param>
    /// <returns><c>true</c>, wenn der andere Bereich vollständig in diesem enthalten ist.</returns>
    public bool IsSupernetOf(NetRangeV6 other) => other.IsSubnetOf(this);

    /// <summary>
    /// Teilt das aktuelle Netzwerk in kleinere Subnetze mit dem angegebenen neuen Präfix auf.
    /// </summary>
    /// <param name="newPrefix">Das neue CIDR-Präfix für die Subnetze. Muss größer als das aktuelle sein.</param>
    /// <returns>Eine Aufzählung der resultierenden Subnetze.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Wird geworfen, wenn newPrefix ungültig ist.</exception>
    public IEnumerable<NetRangeV6> GetSubnets(int newPrefix)
    {
        if (newPrefix <= CidrPrefix || newPrefix > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix,
                $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 128 sein.");
        }

        // Vorsicht bei sehr großen Subnetanzahlen
        var prefixDifference = newPrefix - CidrPrefix;
        if (prefixDifference > 63) // 2^63 ist bereits sehr groß
        {
            throw new ArgumentException("Zu viele Subnetze würden generiert. Maximaler Präfix-Unterschied: 63");
        }

        var subnetSize = BigInteger.Pow(2, 128 - newPrefix);
        var currentAddress = _networkAddressBigInt;

        while (currentAddress <= _lastAddressBigInt)
        {
            yield return new NetRangeV6(ToIpAddress(currentAddress), newPrefix);

            // Overflow-Schutz
            var maxValue = BigInteger.Pow(2, 128) - 1;
            if (currentAddress > maxValue - subnetSize)
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
    public NetRangeV6 GetSupernet(int newPrefix)
    {
        if (newPrefix >= CidrPrefix || newPrefix < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), newPrefix,
                $"Neues Präfix muss kleiner als {CidrPrefix} und größer/gleich 0 sein.");
        }

        BigInteger newMask;
        if (newPrefix == 0)
        {
            newMask = BigInteger.Zero;
        }
        else
        {
            newMask = (BigInteger.One << 128) - 1 & ~((BigInteger.One << 128 - newPrefix) - 1);
        }

        var newNetworkAddress = _networkAddressBigInt & newMask;
        return new NetRangeV6(ToIpAddress(newNetworkAddress), newPrefix);
    }

    // --- Vergleichsmethoden ---
    public int CompareTo(NetRangeV6 other)
    {
        var networkComparison = _networkAddressBigInt.CompareTo(other._networkAddressBigInt);
        return networkComparison == 0 ? CidrPrefix.CompareTo(other.CidrPrefix) : networkComparison;
    }

    public bool Equals(NetRangeV6 other) =>
        CidrPrefix == other.CidrPrefix && _networkAddressBigInt.Equals(other._networkAddressBigInt);

    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";

    // --- Parsing-Methoden ---
    /// <summary>
    /// Versucht, eine CIDR-Notation zu parsen und eine NetRangeV6-Instanz zu erstellen.
    /// </summary>
    /// <param name="cidr">Die CIDR-Notation.</param>
    /// <param name="result">Die resultierende NetRangeV6-Instanz.</param>
    /// <returns><c>true</c>, wenn das Parsen erfolgreich war, andernfalls <c>false</c>.</returns>
    public static bool TryParse(string? cidr, out NetRangeV6 result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(cidr))
        {
            return false;
        }

        var parts = cidr.Split('/');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!IPAddress.TryParse(parts[0], out var ip) ||
            ip.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var prefix) || prefix is < 0 or > 128)
        {
            return false;
        }

        try
        {
            result = new NetRangeV6(ip, prefix);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

// --- Plattformspezifische GetHashCode-Implementierung ---
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
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + CidrPrefix;
            hash = hash * 31 + _networkAddressBigInt.GetHashCode();
            return hash;
        }
    }
}
#endif