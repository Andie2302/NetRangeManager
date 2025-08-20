using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

public readonly partial record struct NetRangeV6 : INetRange<NetRangeV6>
{
    // --- Private Felder ---
    private readonly BigInteger _networkAddressBigInt;
    private readonly BigInteger _lastAddressBigInt;

    // --- Konstruktoren ---
    public NetRangeV6(string cidr)
    {
        var parts = cidr.Split('/');
        if (!IPAddress.TryParse(parts[0], out var ip) || !int.TryParse(parts[1], out var prefix))
        {
            throw new ArgumentException("Ungültige IPv6 CIDR-Notation.", nameof(cidr));
        }
        // Validiere die IP-Adresse und das Präfix im zweiten Konstruktor
        this = new NetRangeV6(ip, prefix);
    }

    public NetRangeV6(IPAddress ip, int prefix)
    {
        if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            throw new ArgumentException("Nur IPv6-Adressen werden unterstützt.", nameof(ip));
        }
        if (prefix is < 0 or > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(prefix), "Präfix für IPv6 muss zwischen 0 und 128 liegen.");
        }

        CidrPrefix = prefix;

        var ipBigInt = ToBigInteger(ip);
        var mask = CidrPrefix == 0 ? BigInteger.Zero : (BigInteger.One << 128 - CidrPrefix) - 1;
        mask = ~mask;

        _networkAddressBigInt = ipBigInt & mask;
        _lastAddressBigInt = _networkAddressBigInt | ~mask;

        NetworkAddress = ToIpAddress(_networkAddressBigInt);
        TotalAddresses = BigInteger.Pow(2, 128 - CidrPrefix);
        IsHost = CidrPrefix == 128;
    }

    // --- Private Hilfsmethoden ---
    private static BigInteger ToBigInteger(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian) { Array.Reverse(bytes); }
        return new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());
    }

    private static IPAddress ToIpAddress(BigInteger addressValue)
    {
        var bytes = addressValue.ToByteArray();
        var ipBytes = new byte[16];
        var bytesToCopy = Math.Min(bytes.Length, 16);
        Array.Copy(bytes, ipBytes, bytesToCopy);
        if (BitConverter.IsLittleEndian) { Array.Reverse(ipBytes); }
        return new IPAddress(ipBytes);
    }

    // --- Öffentliche Eigenschaften ---
    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => NetworkAddress;
    public IPAddress LastUsableAddress => LastAddressInRange;
    public IPAddress LastAddressInRange => ToIpAddress(_lastAddressBigInt);
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    // --- Interface-Methoden ---
    public bool Contains(IPAddress ipAddress)
    {
        var ipBigInt = ToBigInteger(ipAddress);
        return ipBigInt >= _networkAddressBigInt && ipBigInt <= _lastAddressBigInt;
    }

    public bool OverlapsWith(NetRangeV6 other)
    {
        return _networkAddressBigInt <= other._lastAddressBigInt && _lastAddressBigInt >= other._networkAddressBigInt;
    }

    public bool IsSubnetOf(NetRangeV6 other)
    {
        return _networkAddressBigInt >= other._networkAddressBigInt && _lastAddressBigInt <= other._lastAddressBigInt;
    }

    public bool IsSupernetOf(NetRangeV6 other) => other.IsSubnetOf(this);

    public IEnumerable<NetRangeV6> GetSubnets(int newPrefix)
    {
        if (newPrefix <= CidrPrefix || newPrefix > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(newPrefix), $"Neues Präfix muss größer als {CidrPrefix} und kleiner/gleich 128 sein.");
        }
        
        var subnetSize = BigInteger.Pow(2, 128 - newPrefix);
        var lastAddress = _lastAddressBigInt;
        var currentAddress = _networkAddressBigInt;

        while (currentAddress <= lastAddress)
        {
            yield return new NetRangeV6(ToIpAddress(currentAddress), newPrefix);
            if (currentAddress > BigInteger.Pow(2, 128) - subnetSize) break; // Überlaufschutz
            currentAddress += subnetSize;
        }
    }

    public int CompareTo(NetRangeV6 other)
    {
        var networkComparison = _networkAddressBigInt.CompareTo(other._networkAddressBigInt);
        return networkComparison == 0 ? CidrPrefix.CompareTo(other.CidrPrefix) : networkComparison;
    }
    
    public bool Equals(NetRangeV6 other) => CidrPrefix == other.CidrPrefix && _networkAddressBigInt.Equals(other._networkAddressBigInt);
    
    public override string ToString() => $"{NetworkAddress}/{CidrPrefix}";
}

// --- Plattformspezifische Implementierung für GetHashCode ---
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
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
            return CidrPrefix * 397 ^ _networkAddressBigInt.GetHashCode();
        }
    }
}
#endif