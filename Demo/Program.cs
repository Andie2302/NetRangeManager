using System.Net;
using NetRangeManager.Models;

Console.WriteLine("--- NetRangeManager Demo ---");
Console.WriteLine();
var network = new NetRangeV4("192.168.1.0/24");
Console.WriteLine($"Network created: {network.NetworkAddress}/{network.CidrPrefix}");
Console.WriteLine($"Broadcast address: {network.LastAddressInRange}");
Console.WriteLine($"First usable IP: {network.FirstUsableAddress}");
Console.WriteLine($"Number of addresses: {network.TotalAddresses}");
Console.WriteLine("------------------------------------");
Console.WriteLine();
Console.WriteLine("Testing IPs with the Contains() method:");
var ipInside = IPAddress.Parse("192.168.1.150");
var ipOutside = IPAddress.Parse("10.0.0.5");
var ipIsNetworkAddress = network.NetworkAddress;
var ipIsBroadcastAddress = network.LastAddressInRange;
Console.WriteLine($"Is {ipInside} in the range? ---> {network.Contains(ipInside)}");
Console.WriteLine($"Is {ipOutside} in the range? ---> {network.Contains(ipOutside)}");
Console.WriteLine($"Is {ipIsNetworkAddress} in the range? ---> {network.Contains(ipIsNetworkAddress)}");
Console.WriteLine($"Is {ipIsBroadcastAddress} in the range? ---> {network.Contains(ipIsBroadcastAddress)}");
Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Testing OverlapsWith(), IsSubnetOf() and IsSupernetOf():");
Console.WriteLine();
var largeRange = new NetRangeV4("10.0.0.0/16");
var smallerRangeInside = new NetRangeV4("10.0.10.0/24");
var overlappingRange = new NetRangeV4("10.0.255.0/24");
var separateRange = new NetRangeV4("172.16.0.0/16");
Console.WriteLine("--- OverlapsWith ---");
Console.WriteLine($"Does '{largeRange}' overlap with '{smallerRangeInside}'? ---> {largeRange.OverlapsWith(smallerRangeInside)}");
Console.WriteLine($"Does '{largeRange}' overlap with '{overlappingRange}'? ---> {largeRange.OverlapsWith(overlappingRange)}");
Console.WriteLine($"Does '{largeRange}' overlap with '{separateRange}'? ---> {largeRange.OverlapsWith(separateRange)}");
Console.WriteLine();
Console.WriteLine("--- IsSubnetOf ---");
Console.WriteLine($"Is '{smallerRangeInside}' a subnet of '{largeRange}'? ---> {smallerRangeInside.IsSubnetOf(largeRange)}");
Console.WriteLine($"Is '{largeRange}' a subnet of '{smallerRangeInside}'? ---> {largeRange.IsSubnetOf(smallerRangeInside)}");
Console.WriteLine($"Is '{overlappingRange}' a subnet of '{largeRange}'? ---> {overlappingRange.IsSubnetOf(largeRange)}");
Console.WriteLine();
Console.WriteLine("--- IsSupernetOf ---");
Console.WriteLine($"Is '{largeRange}' a supernet of '{smallerRangeInside}'? ---> {largeRange.IsSupernetOf(smallerRangeInside)}");
Console.WriteLine($"Is '{smallerRangeInside}' a supernet of '{largeRange}'? ---> {smallerRangeInside.IsSupernetOf(largeRange)}");
Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Testing CompareTo() by sorting:");
Console.WriteLine();
var ranges = new List<NetRangeV4> { new("192.168.1.128/25"), new("10.0.0.0/8"), new("192.168.1.0/24"), new("10.0.0.0/16") };
Console.WriteLine("Unsorted list:");

foreach (var range in ranges) { Console.WriteLine($"- {range}"); }

ranges.Sort();
Console.WriteLine("\nSorted list:");

foreach (var range in ranges) { Console.WriteLine($"- {range}"); }

Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Testing GetSubnets():");
Console.WriteLine();
var networkToSubnet = new NetRangeV4("172.16.10.0/24");
Console.WriteLine($"Splitting network {networkToSubnet} into /26 subnets:");
var subnets = networkToSubnet.GetSubnets(26);

foreach (var subnet in subnets) { Console.WriteLine($"- {subnet} (First IP: {subnet.FirstUsableAddress}, Last IP: {subnet.LastUsableAddress})"); }

Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("--- NetRangeManager IPv6 Demo ---");
Console.WriteLine("====================================");
Console.WriteLine();
var ipv6Network = new NetRangeV6("2001:db8:acad::/48");
Console.WriteLine($"IPv6 network created: {ipv6Network}");
Console.WriteLine($"First address: {ipv6Network.FirstUsableAddress}");
Console.WriteLine($"Last address: {ipv6Network.LastAddressInRange}");
Console.WriteLine($"Number of addresses: {ipv6Network.TotalAddresses:N0}");
Console.WriteLine("------------------------------------");
Console.WriteLine();
Console.WriteLine("Testing IPv6 IPs with the Contains() method:");
var ipv6Inside = IPAddress.Parse("2001:db8:acad:1:ffff:ffff:ffff:ffff");
var ipv6Outside = IPAddress.Parse("2001:db8:beef::1");
Console.WriteLine($"Is {ipv6Inside} in the range? ---> {ipv6Network.Contains(ipv6Inside)}");
Console.WriteLine($"Is {ipv6Outside} in the range? ---> {ipv6Network.Contains(ipv6Outside)}");
Console.WriteLine();
Console.WriteLine("Testing OverlapsWith() and IsSubnetOf() for IPv6:");
var ipv6Subnet = new NetRangeV6("2001:db8:acad:dead::/64");
var ipv6Separate = new NetRangeV6("2a03:2880:f12f::/64");
Console.WriteLine($"Does '{ipv6Network}' overlap with '{ipv6Subnet}'? ---> {ipv6Network.OverlapsWith(ipv6Subnet)}");
Console.WriteLine($"Is '{ipv6Subnet}' a subnet of '{ipv6Network}'? ---> {ipv6Subnet.IsSubnetOf(ipv6Network)}");
Console.WriteLine($"Is '{ipv6Network}' a supernet of '{ipv6Subnet}'? ---> {ipv6Network.IsSupernetOf(ipv6Subnet)}");
Console.WriteLine($"Does '{ipv6Network}' overlap with '{ipv6Separate}'? ---> {ipv6Network.OverlapsWith(ipv6Separate)}");
Console.WriteLine();
Console.WriteLine("Testing CompareTo() by sorting an IPv6 list:");
var ipv6Ranges = new List<NetRangeV6> { new("2001:db8:2::/48"), new("2001:db8:1::/48"), new("2001:db8:1:1::/64"), new("2001:db8:1::/56") };
ipv6Ranges.Sort();
Console.WriteLine("Sorted IPv6 list:");

foreach (var range in ipv6Ranges) { Console.WriteLine($"- {range}"); }

Console.WriteLine();
Console.WriteLine("Testing GetSubnets() for IPv6:");
var ipv6NetworkToSubnet = new NetRangeV6("fd00::/8");
Console.WriteLine($"Splitting network {ipv6NetworkToSubnet} into /12 subnets (first 5):");
var ipv6Subnets = ipv6NetworkToSubnet.GetSubnets(12);

foreach (var subnet in ipv6Subnets.Take(5)) { Console.WriteLine($"- {subnet}"); }