using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Open.Nat;

internal interface ISearcher
{
	void Search(CancellationToken cancellationToken);

	IEnumerable<NatDevice> Receive();

	NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint);
}
