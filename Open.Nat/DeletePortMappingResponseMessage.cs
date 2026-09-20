using System.Xml;

namespace Open.Nat;

internal class DeletePortMappingResponseMessage : ResponseMessageBase
{
	public DeletePortMappingResponseMessage(XmlDocument response, string serviceType, string typeName)
		: base(response, serviceType, typeName)
	{
	}
}
