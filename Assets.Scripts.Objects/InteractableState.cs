using System.Xml.Serialization;

namespace Assets.Scripts.Objects;

[XmlRoot]
public class InteractableState
{
	[XmlElement]
	public string StateName;

	[XmlElement]
	public int State;
}
