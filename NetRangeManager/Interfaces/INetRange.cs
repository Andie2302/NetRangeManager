using System.Numerics;
using System.Net;

namespace NetRangeManager.Interfaces;

public interface INetRange < TNetRange > : IComparable< TNetRange > , IEquatable< TNetRange > where TNetRange : INetRange< TNetRange >
{
    IPAddress NetworkAddress { get; }
    int CidrPrefix { get; }
    IPAddress FirstUsableAddress { get; }
    IPAddress LastUsableAddress { get; }
    IPAddress LastAddressInRange { get; }
    BigInteger TotalAddresses { get; }
    bool IsHost { get; }
    bool Contains ( IPAddress ipAddress );
    bool OverlapsWith ( TNetRange other );
    bool IsSubnetOf ( TNetRange other );
    bool IsSupernetOf ( TNetRange other );
    IEnumerable< TNetRange > GetSubnets ( int newPrefix );
}
