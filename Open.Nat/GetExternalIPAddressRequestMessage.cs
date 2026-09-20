using System.Collections.Generic;

namespace Open.Nat;

internal class GetExternalIPAddressRequestMessage : RequestMessageBase
{
	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object>();
	}
}
