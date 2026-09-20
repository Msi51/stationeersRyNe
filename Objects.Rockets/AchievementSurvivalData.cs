using System.Xml.Serialization;

namespace Objects.Rockets;

public class AchievementSurvivalData : AchievementData
{
	[XmlAttribute("DaysLived")]
	public int DaysLived;

	[XmlAttribute("Difficulty")]
	public string Difficuly;

	[XmlIgnore]
	private bool _isCompleted;

	public void Evaluate(ushort daysLived, string currentDifficulty)
	{
		if (!_isCompleted && daysLived >= DaysLived && (string.IsNullOrEmpty(Difficuly) || currentDifficulty == Difficuly))
		{
			_isCompleted = true;
			Execute();
		}
	}
}
