using System.Collections.Generic;

namespace Open.Nat;

internal abstract class RequestMessageBase
{
	public abstract IDictionary<string, object> ToXml();
}
