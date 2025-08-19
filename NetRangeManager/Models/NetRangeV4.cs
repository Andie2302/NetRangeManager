using System.Numerics;
using System.Net;

namespace NetRangeManager.Models;

public readonly record struct NetRangeV4 : INetRange< NetRangeV4 >
{
    public bool Equals ( NetRangeV4 other ) { throw new NotImplementedException(); }
    public int CompareTo ( NetRangeV4 other ) { throw new NotImplementedException(); }
    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress { get; }
    public IPAddress LastAddressInRange { get; }
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }
    public bool Contains ( IPAddress ipAddress ) { throw new NotImplementedException(); }
    public bool OverlapsWith ( NetRangeV4 other ) { throw new NotImplementedException(); }
    public bool IsSubnetOf ( NetRangeV4 other ) { throw new NotImplementedException(); }
    public bool IsSupernetOf ( NetRangeV4 other ) => other.IsSubnetOf ( this );
    public IEnumerable< NetRangeV4 > GetSubnets ( int newPrefix ) { throw new NotImplementedException(); }
}
