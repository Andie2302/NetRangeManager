using System.Net;
using System.Numerics;

namespace NetRangeManager.Interfaces;

/// <summary>
/// Definiert einen allgemeinen Vertrag für einen IP-Adressbereich (sowohl IPv4 als auch IPv6).
/// </summary>
/// <typeparam name="TNetRange">Der konkrete Typ des IP-Bereichs, der dieses Interface implementiert.</typeparam>
public interface INetRange<TNetRange> : IComparable<TNetRange>, IEquatable<TNetRange> 
    where TNetRange : INetRange<TNetRange>
{
    // --- Eigenschaften ---
    IPAddress NetworkAddress { get; }
    int CidrPrefix { get; }
    IPAddress FirstUsableAddress { get; }
    IPAddress LastAddressInRange { get; }
    BigInteger TotalAddresses { get; }
    bool IsHost { get; }

    // --- Methoden ---
    bool Contains(IPAddress ipAddress);
    bool OverlapsWith(TNetRange other);
    bool IsSubnetOf(TNetRange other);
    bool IsSupernetOf(TNetRange other);

    /// <summary>
    /// Teilt das aktuelle Netzwerk in kleinere Subnetze mit dem angegebenen neuen Präfix auf.
    /// </summary>
    /// <param name="newPrefix">Das neue CIDR-Präfix für die Subnetze. Muss größer als das aktuelle sein.</param>
    /// <returns>Eine Aufzählung der resultierenden Subnetze.</returns>
    IEnumerable<TNetRange> GetSubnets(int newPrefix);
}
