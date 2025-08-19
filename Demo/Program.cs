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

Console.WriteLine();
Console.WriteLine("--- Demo Ende ---");
