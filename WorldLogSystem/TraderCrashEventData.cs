using System.Xml.Serialization;
using Assets.Scripts.Util;

namespace WorldLogSystem;

public class TraderCrashEventData : WorldEventData
{
	[XmlElement("PadId")]
	public long TargetPadId;

	[XmlElement("CollisionObjectId")]
	public long CollisionObjectId;

	[XmlElement("Position")]
	public Float3 ShuttlePosition;

	public override WorldEvent ToEvent()
	{
		return new TraderCrashEvent
		{
			Text = Text,
			DateTime = DateTime,
			TargetPadId = TargetPadId,
			CollisionObjectId = CollisionObjectId,
			ShuttlePosition = ShuttlePosition.ToVector3()
		};
	}
}
