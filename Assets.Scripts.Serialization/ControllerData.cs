using System.Xml.Serialization;
using Assets.Scripts.UI;

namespace Assets.Scripts.Serialization;

[XmlRoot]
public class ControllerData
{
	public string ControllerName;

	public Controller Controller;

	public ControllerAxis Axis;
}
