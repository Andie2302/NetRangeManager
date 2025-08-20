using System.Net;
using NetRangeManager.Models;

Console.WriteLine ( "--- NetRangeManager Demo ---" );
Console.WriteLine();
var network = new NetRangeV4 ( "192.168.1.0/24" );
Console.WriteLine ( $"Netzwerk erstellt: {network.NetworkAddress}/{network.CidrPrefix}" );
Console.WriteLine ( $"Broadcast-Adresse: {network.LastAddressInRange}" );
Console.WriteLine ( $"Erste nutzbare IP: {network.FirstUsableAddress}" );
Console.WriteLine ( $"Anzahl Adressen: {network.TotalAddresses}" );
Console.WriteLine ( "------------------------------------" );
Console.WriteLine();
Console.WriteLine ( "Teste IPs mit der Contains()-Methode:" );
var ipInside = IPAddress.Parse ( "192.168.1.150" );
var ipOutside = IPAddress.Parse ( "10.0.0.5" );
var ipIsNetworkAddress = network.NetworkAddress;
var ipIsBroadcastAddress = network.LastAddressInRange;
Console.WriteLine ( $"Liegt {ipInside} im Bereich? ---> {network.Contains ( ipInside )}" );
Console.WriteLine ( $"Liegt {ipOutside} im Bereich? ---> {network.Contains ( ipOutside )}" );
Console.WriteLine ( $"Liegt {ipIsNetworkAddress} im Bereich? ---> {network.Contains ( ipIsNetworkAddress )}" );
Console.WriteLine ( $"Liegt {ipIsBroadcastAddress} im Bereich? ---> {network.Contains ( ipIsBroadcastAddress )}" );
Console.WriteLine();
Console.WriteLine ( "------------------------------------" );
Console.WriteLine ( "Teste OverlapsWith() und IsSubnetOf():" );
Console.WriteLine();
var grosserBereich = new NetRangeV4 ( "10.0.0.0/16" );
var kleinerBereichDarin = new NetRangeV4 ( "10.0.10.0/24" );
var ueberlappenderBereich = new NetRangeV4 ( "10.0.255.0/24" );
var separaterBereich = new NetRangeV4 ( "172.16.0.0/16" );
Console.WriteLine ( "--- OverlapsWith ---" );
Console.WriteLine ( $"Überlappt '{grosserBereich}' mit '{kleinerBereichDarin}'? ---> {grosserBereich.OverlapsWith ( kleinerBereichDarin )}" );
Console.WriteLine ( $"Überlappt '{grosserBereich}' mit '{ueberlappenderBereich}'? ---> {grosserBereich.OverlapsWith ( ueberlappenderBereich )}" );
Console.WriteLine ( $"Überlappt '{grosserBereich}' mit '{separaterBereich}'? ---> {grosserBereich.OverlapsWith ( separaterBereich )}" );
Console.WriteLine();
Console.WriteLine ( "--- IsSubnetOf ---" );
Console.WriteLine ( $"Ist '{kleinerBereichDarin}' ein Subnetz von '{grosserBereich}'? ---> {kleinerBereichDarin.IsSubnetOf ( grosserBereich )}" );
Console.WriteLine ( $"Ist '{grosserBereich}' ein Subnetz von '{kleinerBereichDarin}'? ---> {grosserBereich.IsSubnetOf ( kleinerBereichDarin )}" );
Console.WriteLine ( $"Ist '{ueberlappenderBereich}' ein Subnetz von '{grosserBereich}'? ---> {ueberlappenderBereich.IsSubnetOf ( grosserBereich )}" );
Console.WriteLine();
Console.WriteLine ( "--- IsSupernetOf ---" );
Console.WriteLine ( $"Ist '{grosserBereich}' ein Supernetz von '{kleinerBereichDarin}'? ---> {grosserBereich.IsSupernetOf ( kleinerBereichDarin )}" );
Console.WriteLine ( $"Ist '{kleinerBereichDarin}' ein Supernetz von '{grosserBereich}'? ---> {kleinerBereichDarin.IsSupernetOf ( grosserBereich )}" );
Console.WriteLine();
Console.WriteLine ( "------------------------------------" );
Console.WriteLine ( "Teste CompareTo() durch Sortieren:" );
Console.WriteLine();
var ranges = new List< NetRangeV4 > { new( "192.168.1.128/25" ) , new( "10.0.0.0/8" ) , new( "192.168.1.0/24" ) , new( "10.0.0.0/16" ) };
Console.WriteLine ( "Unsortierte Liste:" );

foreach ( var range in ranges ) { Console.WriteLine ( $"- {range}" ); }

ranges.Sort();
Console.WriteLine ( "\nSortierte Liste:" );

foreach ( var range in ranges ) { Console.WriteLine ( $"- {range}" ); }

Console.WriteLine();
Console.WriteLine ( "------------------------------------" );
Console.WriteLine ( "Teste GetSubnets():" );
Console.WriteLine();
var netzwerkZumAufteilen = new NetRangeV4 ( "172.16.10.0/24" );
Console.WriteLine ( $"Teile das Netzwerk {netzwerkZumAufteilen} in /26-Subnetze auf:" );
var subnetze = netzwerkZumAufteilen.GetSubnets ( 26 );

foreach ( var subnetz in subnetze ) { Console.WriteLine ( $"- {subnetz} (Erste IP: {subnetz.FirstUsableAddress}, Letzte IP: {subnetz.LastUsableAddress})" ); }

Console.WriteLine();
Console.WriteLine ( "====================================" );
Console.WriteLine ( "--- NetRangeManager Demo für IPv6 ---" );
Console.WriteLine ( "====================================" );
Console.WriteLine();
var ipv6Network = new NetRangeV6 ( "2001:db8:acad::/48" );
Console.WriteLine ( $"IPv6-Netzwerk erstellt: {ipv6Network}" );
Console.WriteLine ( $"Erste Adresse: {ipv6Network.FirstUsableAddress}" );
Console.WriteLine ( $"Letzte Adresse: {ipv6Network.LastAddressInRange}" );
Console.WriteLine ( $"Anzahl Adressen: {ipv6Network.TotalAddresses:N0}" );
Console.WriteLine ( "------------------------------------" );
Console.WriteLine();
Console.WriteLine ( "Teste IPv6-IPs mit der Contains()-Methode:" );
var ipv6Inside = IPAddress.Parse ( "2001:db8:acad:1:ffff:ffff:ffff:ffff" );
var ipv6Outside = IPAddress.Parse ( "2001:db8:beef::1" );
Console.WriteLine ( $"Liegt {ipv6Inside} im Bereich? ---> {ipv6Network.Contains ( ipv6Inside )}" );
Console.WriteLine ( $"Liegt {ipv6Outside} im Bereich? ---> {ipv6Network.Contains ( ipv6Outside )}" );
Console.WriteLine();
Console.WriteLine ( "Teste OverlapsWith() und IsSubnetOf() für IPv6:" );
var ipv6Subnet = new NetRangeV6 ( "2001:db8:acad:dead::/64" );
var ipv6Separate = new NetRangeV6 ( "2a03:2880:f12f::/64" );
Console.WriteLine ( $"Überlappt '{ipv6Network}' mit '{ipv6Subnet}'? ---> {ipv6Network.OverlapsWith ( ipv6Subnet )}" );
Console.WriteLine ( $"Ist '{ipv6Subnet}' ein Subnetz von '{ipv6Network}'? ---> {ipv6Subnet.IsSubnetOf ( ipv6Network )}" );
Console.WriteLine ( $"Ist '{ipv6Network}' ein Supernetz von '{ipv6Subnet}'? ---> {ipv6Network.IsSupernetOf ( ipv6Subnet )}" );
Console.WriteLine ( $"Überlappt '{ipv6Network}' mit '{ipv6Separate}'? ---> {ipv6Network.OverlapsWith ( ipv6Separate )}" );
Console.WriteLine();
Console.WriteLine ( "Teste CompareTo() durch Sortieren einer IPv6-Liste:" );
var ipv6Ranges = new List< NetRangeV6 > { new( "2001:db8:2::/48" ) , new( "2001:db8:1::/48" ) , new( "2001:db8:1:1::/64" ) , new( "2001:db8:1::/56" ) };
ipv6Ranges.Sort();
Console.WriteLine ( "Sortierte IPv6-Liste:" );

foreach ( var range in ipv6Ranges ) { Console.WriteLine ( $"- {range}" ); }

Console.WriteLine();
Console.WriteLine ( "Teste GetSubnets() für IPv6:" );
var ipv6NetzwerkZumAufteilen = new NetRangeV6 ( "fd00::/8" );
Console.WriteLine ( $"Teile das Netzwerk {ipv6NetzwerkZumAufteilen} in /12-Subnetze auf (die ersten 5):" );
var ipv6Subnetze = ipv6NetzwerkZumAufteilen.GetSubnets ( 12 );

foreach ( var subnetz in ipv6Subnetze.Take ( 5 ) ) { Console.WriteLine ( $"- {subnetz}" ); }
