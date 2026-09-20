using System.Xml.Serialization;
using Assets.Scripts;
using Objects.Rockets;

namespace UnityEngine;

[XmlRoot("PointOfInterest")]
public class PointOfInterest : DataCollection
{
	[XmlElement("Region")]
	public Region Region;

	[XmlElement("Achievement")]
	public AchievementData Achievement;

	[XmlElement("Description")]
	public LocalizedStringReference Description;

	public override void Initialize(ModAbout mod)
	{
		PointOfInterestManager.AddPointOfInterest(this);
	}

	public void OnDiscovered()
	{
		Achievement?.Execute();
	}
}
