using System.Numerics;
using System.Net;

namespace NetRangeManager.Interfaces;

/// <summary>
/// Definiert einen allgemeinen Vertrag für einen IP-Adressbereich (sowohl IPv4 als auch IPv6).
/// </summary>
/// <typeparam name="TNetRange">Der konkrete Typ des IP-Bereichs, der dieses Interface implementiert.</typeparam>
public interface INetRange<TNetRange> : IComparable<TNetRange>, IEquatable<TNetRange>
    where TNetRange : INetRange<TNetRange>
{
    /// <summary>
    /// Ruft die erste Adresse des Netzwerkbereichs ab (Netzwerkadresse).
    /// </summary>
    IPAddress NetworkAddress { get; }

    /// <summary>
    /// Ruft das CIDR-Präfix ab, das die Größe des Netzwerks bestimmt (z.B. 24 für /24).
    /// </summary>
    int CidrPrefix { get; }

    /// <summary>
    /// Ruft die erste nutzbare Host-Adresse im Bereich ab.
    /// </summary>
    IPAddress FirstUsableAddress { get; }

    /// <summary>
    /// Ruft die letzte nutzbare Host-Adresse im Bereich ab.
    /// </summary>
    IPAddress LastUsableAddress { get; }

    /// <summary>
    /// Ruft die absolut letzte Adresse im Bereich ab (Broadcast-Adresse bei IPv4).
    /// </summary>
    IPAddress LastAddressInRange { get; }

    /// <summary>
    /// Ruft die Gesamtzahl der IP-Adressen im Bereich ab.
    /// </summary>
    BigInteger TotalAddresses { get; }

    /// <summary>
    /// Gibt an, ob der Bereich nur eine einzelne Host-Adresse repräsentiert (/32 bei IPv4, /128 bei IPv6).
    /// </summary>
    bool IsHost { get; }

    /// <summary>
    /// Überprüft, ob die angegebene IP-Adresse in diesem Netzwerkbereich enthalten ist.
    /// </summary>
    /// <param name="ipAddress">Die zu überprüfende IP-Adresse.</param>
    /// <returns><c>true</c>, wenn die Adresse im Bereich liegt, andernfalls <c>false</c>.</returns>
    bool Contains(IPAddress ipAddress);

    /// <summary>
    /// Überprüft, ob dieser Netzwerkbereich mit einem anderen Bereich überlappt.
    /// </summary>
    /// <param name="other">Der andere zu vergleichende Netzwerkbereich.</param>
    /// <returns><c>true</c>, wenn eine Überlappung besteht, andernfalls <c>false</c>.</returns>
    bool OverlapsWith(TNetRange other);

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Subnetz des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Supernet-Bereich.</param>
    /// <returns><c>true</c>, wenn dieser Bereich vollständig im anderen enthalten ist.</returns>
    bool IsSubnetOf(TNetRange other);

    /// <summary>
    /// Überprüft, ob dieser Bereich ein Supernet des anderen angegebenen Bereichs ist.
    /// </summary>
    /// <param name="other">Der potenzielle Subnet-Bereich.</param>
    /// <returns><c>true</c>, wenn der andere Bereich vollständig in diesem enthalten ist.</returns>
    bool IsSupernetOf(TNetRange other);

    /// <summary>
    /// Teilt das aktuelle Netzwerk in kleinere Subnetze mit dem angegebenen neuen Präfix auf.
    /// </summary>
    /// <param name="newPrefix">Das neue CIDR-Präfix für die Subnetze. Muss größer als das aktuelle sein.</param>
    /// <returns>Eine Aufzählung der resultierenden Subnetze.</returns>
    IEnumerable<TNetRange> GetSubnets(int newPrefix);
}