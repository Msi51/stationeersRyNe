using System.Xml;

namespace Assets.Scripts.OpenNat;

internal class DeletePortMappingResponseMessage : ResponseMessageBase
{
	public DeletePortMappingResponseMessage(XmlDocument response, string serviceType)
		: base(response, serviceType, "DeletePortMappingResponseMessage")
	{
	}
}
