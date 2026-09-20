using System.Xml;

namespace Assets.Scripts.OpenNat;

internal class AddPortMappingResponseMessage : ResponseMessageBase
{
	public AddPortMappingResponseMessage(XmlDocument response, string serviceType)
		: base(response, serviceType, "AddPortMappingResponseMessage")
	{
	}
}
