using System;
using System.Xml.Serialization;
using Assets.Scripts.Networking;
using Objects.Rockets.Mining;

namespace Objects.Rockets.Scanning;

public abstract class SpaceMapNodeActionData : INetworkNullable
{
	[XmlAttribute("Repeating")]
	public bool IsRepeating = true;

	[XmlAttribute("Difficulty")]
	public int Difficulty = 100;

	[XmlElement("Achievement")]
	public AchievementData Achievement;

	public abstract RocketAction ToInstance(SpaceMapNode node);

	public SpaceMapNodeActionData()
	{
	}

	public SpaceMapNodeActionData(RocketBinaryReader reader)
	{
		ushort difficulty = reader.ReadUInt16();
		bool isRepeating = reader.ReadBoolean();
		Difficulty = difficulty;
		IsRepeating = isRepeating;
	}

	public virtual void Write(RocketBinaryWriter writer)
	{
		writer.WriteUInt16((ushort)Difficulty);
		writer.WriteBoolean(IsRepeating);
	}

	public virtual void Initialize(ModAbout mod)
	{
	}

	public static void ValidateActionDatas(SpaceMapNodeData data)
	{
		ChartData chartData = null;
		DiscoverSiteData discoverSiteData = null;
		SpaceMapNodeMineData spaceMapNodeMineData = null;
		SurveyData surveyData = null;
		DeployData deployData = null;
		SurfaceScanData surfaceScanData = null;
		foreach (SpaceMapNodeActionData availableAction in data.AvailableActions)
		{
			if (!(availableAction is ChartData chartData2))
			{
				if (!(availableAction is SurfaceScanData surfaceScanData2))
				{
					if (!(availableAction is DiscoverSiteData discoverSiteData2))
					{
						if (!(availableAction is SpaceMapNodeMineData spaceMapNodeMineData2))
						{
							if (!(availableAction is SurveyData surveyData2))
							{
								if (!(availableAction is DeployData deployData2))
								{
									throw new ArgumentOutOfRangeException("actionData");
								}
								deployData = deployData2;
							}
							else
							{
								surveyData = surveyData2;
							}
						}
						else
						{
							spaceMapNodeMineData = spaceMapNodeMineData2;
						}
					}
					else
					{
						discoverSiteData = discoverSiteData2;
					}
				}
				else
				{
					surfaceScanData = surfaceScanData2;
				}
			}
			else
			{
				chartData = chartData2;
			}
		}
		if (data.Children.Count > 0 && chartData == null)
		{
			data.AvailableActions.Add(new ChartData());
		}
		if (data.DepositData != null)
		{
			if (spaceMapNodeMineData == null && deployData == null)
			{
				data.AvailableActions.Add(new SpaceMapNodeMineData());
			}
			if (surveyData == null)
			{
				data.AvailableActions.Add(new SurveyData());
			}
		}
		if (surfaceScanData != null)
		{
			data.AvailableActions.Add(new SurfaceScanData());
		}
		if (discoverSiteData == null)
		{
			return;
		}
		foreach (SpaceMapNodeData siteGeneration in discoverSiteData.SiteGenerations)
		{
			SurveyData surveyData3 = null;
			foreach (SpaceMapNodeActionData availableAction2 in siteGeneration.AvailableActions)
			{
				if (availableAction2 is SurveyData surveyData4)
				{
					surveyData3 = surveyData4;
				}
			}
			if (surveyData3 == null)
			{
				siteGeneration.AvailableActions.Add(new SurveyData
				{
					Difficulty = discoverSiteData.Difficulty
				});
			}
		}
	}
}
