using System.Collections.Generic;

namespace Assets.Scripts.OpenNat;

internal class GetExternalIPAddressRequestMessage : RequestMessageBase
{
	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object>();
	}
}
