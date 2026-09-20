using System.Collections.Generic;

namespace Assets.Scripts.OpenNat;

internal abstract class RequestMessageBase
{
	public abstract IDictionary<string, object> ToXml();
}
