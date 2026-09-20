using System.Collections.Generic;

namespace Open.Nat;

internal class GetSpecificPortMappingEntryRequestMessage : RequestMessageBase
{
	private readonly int _externalPort;

	private readonly Protocol _protocol;

	public GetSpecificPortMappingEntryRequestMessage(Protocol protocol, int externalPort)
	{
		_protocol = protocol;
		_externalPort = externalPort;
	}

	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object>
		{
			{
				"NewRemoteHost",
				string.Empty
			},
			{ "NewExternalPort", _externalPort },
			{
				"NewProtocol",
				(_protocol == Protocol.Tcp) ? "TCP" : "UDP"
			}
		};
	}
}
