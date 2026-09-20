using System.Collections.Generic;

namespace Assets.Scripts.OpenNat;

internal class GetGenericPortMappingEntry : RequestMessageBase
{
	private readonly int _index;

	public GetGenericPortMappingEntry(int index)
	{
		_index = index;
	}

	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object> { { "NewPortMappingIndex", _index } };
	}
}
