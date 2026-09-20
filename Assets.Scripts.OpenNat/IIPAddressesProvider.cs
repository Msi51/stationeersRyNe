using System.Collections.Generic;
using System.Net;

namespace Assets.Scripts.OpenNat;

internal interface IIPAddressesProvider
{
	IEnumerable<IPAddress> DnsAddresses();

	IEnumerable<IPAddress> GatewayAddresses();

	IEnumerable<IPAddress> UnicastAddresses();
}
