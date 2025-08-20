using System.Net;
using NetRangeManager.Models; // Wichtig: Hier binden wir unsere NetRangeV4-Klasse ein!

Console.WriteLine("--- NetRangeManager Demo ---");
Console.WriteLine();

// 1. Erstellen wir einen neuen IP-Bereich
var network = new NetRangeV4("192.168.1.0/24");

Console.WriteLine($"Netzwerk erstellt: {network.NetworkAddress}/{network.CidrPrefix}");
Console.WriteLine($"Broadcast-Adresse: {network.LastAddressInRange}");
Console.WriteLine($"Erste nutzbare IP: {network.FirstUsableAddress}");
Console.WriteLine($"Anzahl Adressen: {network.TotalAddresses}");
Console.WriteLine("------------------------------------");
Console.WriteLine();

// 2. Testen wir die 'Contains'-Methode
Console.WriteLine("Teste IPs mit der Contains()-Methode:");

var ipInside = IPAddress.Parse("192.168.1.150");
var ipOutside = IPAddress.Parse("10.0.0.5");
var ipIsNetworkAddress = network.NetworkAddress;
var ipIsBroadcastAddress = network.LastAddressInRange;

Console.WriteLine($"Liegt {ipInside} im Bereich? ---> {network.Contains(ipInside)}"); // Erwartet: True
Console.WriteLine($"Liegt {ipOutside} im Bereich? ---> {network.Contains(ipOutside)}"); // Erwartet: False
Console.WriteLine($"Liegt {ipIsNetworkAddress} im Bereich? ---> {network.Contains(ipIsNetworkAddress)}"); // Erwartet: True
Console.WriteLine($"Liegt {ipIsBroadcastAddress} im Bereich? ---> {network.Contains(ipIsBroadcastAddress)}"); // Erwartet: True


// 3. Testen wir die 'OverlapsWith' und 'IsSubnetOf' Methoden
Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Teste OverlapsWith() und IsSubnetOf():");
Console.WriteLine();

// Test-Szenarien definieren
var grosserBereich = new NetRangeV4("10.0.0.0/16");      // Ein großes /16 Netz
var kleinerBereichDarin = new NetRangeV4("10.0.10.0/24"); // Ein kleines /24 Netz, das komplett darin liegt
var ueberlappenderBereich = new NetRangeV4("10.0.255.0/24"); // Ein Netz, das am Ende des großen Bereichs überlappt
var separaterBereich = new NetRangeV4("172.16.0.0/16");     // Ein komplett anderes Netz

// --- OverlapsWith Tests ---
Console.WriteLine("--- OverlapsWith ---");
// Erwartet: True, da der kleine Bereich im großen liegt
Console.WriteLine($"Überlappt '{grosserBereich}' mit '{kleinerBereichDarin}'? ---> {grosserBereich.OverlapsWith(kleinerBereichDarin)}");
// Erwartet: True, da sie sich am Rand überschneiden (10.0.255.0 bis 10.0.255.255)
Console.WriteLine($"Überlappt '{grosserBereich}' mit '{ueberlappenderBereich}'? ---> {grosserBereich.OverlapsWith(ueberlappenderBereich)}");
// Erwartet: False, da es komplett andere Netze sind
Console.WriteLine($"Überlappt '{grosserBereich}' mit '{separaterBereich}'? ---> {grosserBereich.OverlapsWith(separaterBereich)}");
Console.WriteLine();

// --- IsSubnetOf Tests ---
Console.WriteLine("--- IsSubnetOf ---");
// Erwartet: True, da 10.0.10.0/24 komplett in 10.0.0.0/16 enthalten ist
Console.WriteLine($"Ist '{kleinerBereichDarin}' ein Subnetz von '{grosserBereich}'? ---> {kleinerBereichDarin.IsSubnetOf(grosserBereich)}");
// Erwartet: False, da der große Bereich nicht im kleinen liegen kann
Console.WriteLine($"Ist '{grosserBereich}' ein Subnetz von '{kleinerBereichDarin}'? ---> {grosserBereich.IsSubnetOf(kleinerBereichDarin)}");
// Erwartet: False, da sie sich nur überschneiden, aber nicht komplett enthalten sind
Console.WriteLine($"Ist '{ueberlappenderBereich}' ein Subnetz von '{grosserBereich}'? ---> {ueberlappenderBereich.IsSubnetOf(grosserBereich)}");
Console.WriteLine();

// --- IsSupernetOf Tests (nutzt die IsSubnetOf-Logik) ---
Console.WriteLine("--- IsSupernetOf ---");
// Erwartet: True, da 10.0.0.0/16 den kleinen Bereich komplett enthält
Console.WriteLine($"Ist '{grosserBereich}' ein Supernetz von '{kleinerBereichDarin}'? ---> {grosserBereich.IsSupernetOf(kleinerBereichDarin)}");
// Erwartet: False, da der kleine Bereich nicht den großen enthalten kann
Console.WriteLine($"Ist '{kleinerBereichDarin}' ein Supernetz von '{grosserBereich}'? ---> {kleinerBereichDarin.IsSupernetOf(grosserBereich)}");


// 4. Testen wir die 'CompareTo' Methode durch Sortieren einer Liste
Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Teste CompareTo() durch Sortieren:");
Console.WriteLine();

var ranges = new List<NetRangeV4>
{
    new("192.168.1.128/25"),
    new("10.0.0.0/8"),
    new("192.168.1.0/24"),
    new("10.0.0.0/16") // Gleiche Start-IP, aber kleineres Netz
};

Console.WriteLine("Unsortierte Liste:");
foreach (var range in ranges)
{
    Console.WriteLine($"- {range}");
}

// Die .Sort()-Methode verwendet jetzt automatisch unsere CompareTo-Implementierung!
ranges.Sort();

Console.WriteLine("\nSortierte Liste:");
foreach (var range in ranges)
{
    Console.WriteLine($"- {range}");
}

// 5. Testen wir die 'GetSubnets' Methode
Console.WriteLine();
Console.WriteLine("------------------------------------");
Console.WriteLine("Teste GetSubnets():");
Console.WriteLine();

var netzwerkZumAufteilen = new NetRangeV4("172.16.10.0/24");
Console.WriteLine($"Teile das Netzwerk {netzwerkZumAufteilen} in /26-Subnetze auf:");

var subnetze = netzwerkZumAufteilen.GetSubnets(26);

foreach (var subnetz in subnetze)
{
    Console.WriteLine($"- {subnetz} (Erste IP: {subnetz.FirstUsableAddress}, Letzte IP: {subnetz.LastUsableAddress})");
}

// 6. Testen wir die komplette NetRangeV6 Klasse
Console.WriteLine();
Console.WriteLine("====================================");
Console.WriteLine("--- NetRangeManager Demo für IPv6 ---");
Console.WriteLine("====================================");
Console.WriteLine();

// Erstellen eines IPv6-Netzwerks
var ipv6Network = new NetRangeV6("2001:db8:acad::/48");
Console.WriteLine($"IPv6-Netzwerk erstellt: {ipv6Network}");
Console.WriteLine($"Erste Adresse: {ipv6Network.FirstUsableAddress}");
Console.WriteLine($"Letzte Adresse: {ipv6Network.LastAddressInRange}");
Console.WriteLine($"Anzahl Adressen: {ipv6Network.TotalAddresses:N0}"); // :N0 formatiert die Zahl mit Tausendertrennzeichen
Console.WriteLine("------------------------------------");
Console.WriteLine();

// Testen der Contains-Methode
Console.WriteLine("Teste IPv6-IPs mit der Contains()-Methode:");
var ipv6Inside = IPAddress.Parse("2001:db8:acad:1:ffff:ffff:ffff:ffff");
var ipv6Outside = IPAddress.Parse("2001:db8:beef::1");
Console.WriteLine($"Liegt {ipv6Inside} im Bereich? ---> {ipv6Network.Contains(ipv6Inside)}"); // Erwartet: True
Console.WriteLine($"Liegt {ipv6Outside} im Bereich? ---> {ipv6Network.Contains(ipv6Outside)}"); // Erwartet: False
Console.WriteLine();

// Testen von OverlapsWith und IsSubnetOf
Console.WriteLine("Teste OverlapsWith() und IsSubnetOf() für IPv6:");
var ipv6Subnet = new NetRangeV6("2001:db8:acad:dead::/64");
var ipv6Separate = new NetRangeV6("2a03:2880:f12f::/64"); // Ein Facebook-Netzwerk
Console.WriteLine($"Überlappt '{ipv6Network}' mit '{ipv6Subnet}'? ---> {ipv6Network.OverlapsWith(ipv6Subnet)}"); // Erwartet: True
Console.WriteLine($"Ist '{ipv6Subnet}' ein Subnetz von '{ipv6Network}'? ---> {ipv6Subnet.IsSubnetOf(ipv6Network)}"); // Erwartet: True
Console.WriteLine($"Ist '{ipv6Network}' ein Supernetz von '{ipv6Subnet}'? ---> {ipv6Network.IsSupernetOf(ipv6Subnet)}"); // Erwartet: True
Console.WriteLine($"Überlappt '{ipv6Network}' mit '{ipv6Separate}'? ---> {ipv6Network.OverlapsWith(ipv6Separate)}"); // Erwartet: False
Console.WriteLine();

// Testen der Sortierung
Console.WriteLine("Teste CompareTo() durch Sortieren einer IPv6-Liste:");
var ipv6Ranges = new List<NetRangeV6>
{
    new("2001:db8:2::/48"),
    new("2001:db8:1::/48"),
    new("2001:db8:1:1::/64"),
    new("2001:db8:1::/56")
};
ipv6Ranges.Sort();
Console.WriteLine("Sortierte IPv6-Liste:");
foreach (var range in ipv6Ranges)
{
    Console.WriteLine($"- {range}");
}
Console.WriteLine();

// Testen von GetSubnets
Console.WriteLine("Teste GetSubnets() für IPv6:");
var ipv6NetzwerkZumAufteilen = new NetRangeV6("fd00::/8");
Console.WriteLine($"Teile das Netzwerk {ipv6NetzwerkZumAufteilen} in /12-Subnetze auf (die ersten 5):");
var ipv6Subnetze = ipv6NetzwerkZumAufteilen.GetSubnets(12);
foreach (var subnetz in ipv6Subnetze.Take(5))
{
    Console.WriteLine($"- {subnetz}");
}

