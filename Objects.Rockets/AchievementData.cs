using System;
using System.Xml.Serialization;
using Assets.Scripts.Util;

namespace Objects.Rockets;

public class AchievementData
{
	[XmlAttribute("Id")]
	public string Id;

	[XmlAttribute("SendToAll")]
	public bool SendToAll;

	public void Execute()
	{
		Achievements.Achieve(Id, SendToAll);
	}

	public static implicit operator Achievements.Kind(AchievementData achievementData)
	{
		return Enum.Parse<Achievements.Kind>(achievementData.Id);
	}
}
