using System.Xml;

namespace Open.Nat;

internal class AddPortMappingResponseMessage : ResponseMessageBase
{
	public AddPortMappingResponseMessage(XmlDocument response, string serviceType, string typeName)
		: base(response, serviceType, typeName)
	{
	}
}
