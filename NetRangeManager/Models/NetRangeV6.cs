using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

public readonly partial record struct NetRangeV6 : INetRange<NetRangeV6>
{
    // --- Private Felder ---
    private readonly BigInteger _networkAddressBigInt;
    private readonly BigInteger _lastAddressBigInt;

    // --- Konstruktor ---
    public NetRangeV6(string cidr)
    {
        var parts = cidr.Split('/');
        if (!IPAddress.TryParse(parts[0], out var ip) || !int.TryParse(parts[1], out var prefix))
        {
            throw new ArgumentException("Ungültige IPv6 CIDR-Notation.", nameof(cidr));
        }

        CidrPrefix = prefix;

        // Konvertiere die IP in einen BigInteger.
        var ipBigInt = ToBigInteger(ip);

        // Berechne die 128-Bit-Maske.
        var mask = CidrPrefix == 0 ? BigInteger.Zero : (BigInteger.One << 128 - CidrPrefix) - 1;
        mask = ~mask;

        // Berechne die erste und letzte Adresse des Bereichs.
        _networkAddressBigInt = ipBigInt & mask;
        _lastAddressBigInt = _networkAddressBigInt | ~mask;

        // Fülle die öffentlichen Eigenschaften.
        NetworkAddress = ToIpAddress(_networkAddressBigInt);
        TotalAddresses = BigInteger.Pow(2, 128 - CidrPrefix);
        IsHost = CidrPrefix == 128;
    }
    
    // --- Private Hilfsmethoden ---
    private static BigInteger ToBigInteger(IPAddress ipAddress)
    {
        var bytes = ipAddress.GetAddressBytes();
        // BigInteger erwartet Little-Endian, GetAddressBytes liefert Big-Endian.
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        // Das extra Null-Byte am Ende stellt sicher, dass die Zahl als positiv interpretiert wird.
        return new BigInteger(bytes.Concat(new byte[] { 0 }).ToArray());
    }

    private static IPAddress ToIpAddress(BigInteger addressValue)
    {
        var bytes = addressValue.ToByteArray();
        var ipBytes = new byte[16]; // IPv6 hat 16 Bytes
        
        // Kopiere die Bytes und fülle bei Bedarf mit Nullen auf.
        var bytesToCopy = Math.Min(bytes.Length, 16);
        Array.Copy(bytes, ipBytes, bytesToCopy);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ipBytes);
        }
        return new IPAddress(ipBytes);
    }


    // --- Öffentliche Eigenschaften ---
    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress => NetworkAddress; // Bei IPv6 gibt es kein Konzept wie bei v4
    public IPAddress LastUsableAddress => LastAddressInRange;  // Dito
    public IPAddress LastAddressInRange => ToIpAddress(_lastAddressBigInt);
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    // --- Interface-Methoden (noch nicht implementiert) ---
    public bool Contains(IPAddress ipAddress) => throw new NotImplementedException();
    public bool OverlapsWith(NetRangeV6 other) => throw new NotImplementedException();
    public bool IsSubnetOf(NetRangeV6 other) => throw new NotImplementedException();
    public bool IsSupernetOf(NetRangeV6 other) => other.IsSubnetOf(this);
    public IEnumerable<NetRangeV6> GetSubnets(int newPrefix) => throw new NotImplementedException();
    public int CompareTo(NetRangeV6 other) => throw new NotImplementedException();
    
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