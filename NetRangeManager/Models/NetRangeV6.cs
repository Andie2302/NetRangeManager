using System.Numerics;
using System.Net;
using NetRangeManager.Interfaces;

namespace NetRangeManager.Models;

public readonly partial record struct NetRangeV6 : INetRange<NetRangeV6>
{
    public bool Equals(NetRangeV6 other)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(NetRangeV6 other)
    {
        throw new NotImplementedException();
    }

    public IPAddress NetworkAddress { get; }
    public int CidrPrefix { get; }
    public IPAddress FirstUsableAddress { get; }
    public IPAddress LastUsableAddress { get; }
    public IPAddress LastAddressInRange { get; }
    public BigInteger TotalAddresses { get; }
    public bool IsHost { get; }

    public bool Contains(IPAddress ipAddress)
    {
        throw new NotImplementedException();
    }

    public bool OverlapsWith(NetRangeV6 other)
    {
        throw new NotImplementedException();
    }

    public bool IsSubnetOf(NetRangeV6 other)
    {
        throw new NotImplementedException();
    }

    public bool IsSupernetOf(NetRangeV6 other) => other.IsSubnetOf(this);
    
    public IEnumerable<NetRangeV6> GetSubnets(int newPrefix)
    {
        throw new NotImplementedException();
    }
}
