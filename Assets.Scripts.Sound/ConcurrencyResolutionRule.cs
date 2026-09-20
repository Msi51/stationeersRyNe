using System.Xml.Serialization;

namespace Assets.Scripts.Sound;

public enum ConcurrencyResolutionRule
{
	[XmlEnum("StopOldest")]
	StopOldest,
	[XmlEnum("StopFarthest")]
	StopFarthest,
	[XmlEnum("StopOldestInRange")]
	StopInaudibleThenOldest,
	[XmlEnum("StopNew")]
	StopNew,
	[XmlEnum("StopLowestPriorityThenNew")]
	StopLowPriorityThenNew,
	[XmlEnum("StopLowestPriorityThenOld")]
	StopLowPriorityThenOld,
	[XmlEnum("StopInaudibleThenNew")]
	StopInaudibleThenNew
}
