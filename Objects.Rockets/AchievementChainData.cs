using System.Collections.Generic;
using System.Xml.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

namespace Objects.Rockets;

public class AchievementChainData : AchievementData
{
	private static List<AchievementChainData> _achievementChainData = new List<AchievementChainData>();

	[XmlElement("PointOfInterest")]
	public List<SerializedId> PointsOfInterest = new List<SerializedId>();

	[XmlIgnore]
	private bool _isCompleted;

	[XmlIgnore]
	private Achievements.Kind _achievementEnum;

	public bool IsCompleted()
	{
		foreach (SerializedId item in PointsOfInterest)
		{
			if (!PointOfInterestManager.IsDiscovered(item))
			{
				return false;
			}
		}
		return true;
	}

	public void Assess()
	{
		if (!_isCompleted && IsCompleted())
		{
			_isCompleted = true;
			Execute();
		}
	}

	public void Initialize(ModAbout mod)
	{
		_achievementEnum = this;
		_achievementChainData.Add(this);
	}

	public static void InitializeOnStart()
	{
		foreach (AchievementChainData achievementChainDatum in _achievementChainData)
		{
			achievementChainDatum._isCompleted = Achievements.Check(achievementChainDatum._achievementEnum);
		}
	}

	public static void CheckAll()
	{
		foreach (AchievementChainData achievementChainDatum in _achievementChainData)
		{
			if (!achievementChainDatum._isCompleted)
			{
				achievementChainDatum.Assess();
			}
		}
	}

	public static void CheckOnDiscovered(PointOfInterest poi)
	{
		foreach (AchievementChainData achievementChainDatum in _achievementChainData)
		{
			if (achievementChainDatum._isCompleted)
			{
				continue;
			}
			foreach (SerializedId item in achievementChainDatum.PointsOfInterest)
			{
				if (item.Id == poi.Id)
				{
					achievementChainDatum.Assess();
				}
			}
		}
	}
}
