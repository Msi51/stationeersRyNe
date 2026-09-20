using System.Collections.Generic;
using System.Net;

namespace Open.Nat;

internal interface IIPAddressesProvider
{
	IEnumerable<IPAddress> DnsAddresses();

	IEnumerable<IPAddress> GatewayAddresses();

	IEnumerable<IPAddress> UnicastAddresses();
}
