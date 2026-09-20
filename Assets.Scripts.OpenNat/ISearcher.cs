using System.Net;

namespace Assets.Scripts.OpenNat;

internal interface ISearcher
{
	void Search();

	NatDevice AnalyseReceivedResponse(IPAddress localAddress, byte[] response, IPEndPoint endpoint);
}
