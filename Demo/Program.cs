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