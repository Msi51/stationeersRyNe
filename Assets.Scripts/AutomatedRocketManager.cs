using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts;

public class AutomatedRocketManager : MonoBehaviour
{
	private static int minRoll = 10;

	private static int maxRoll = 30;

	public static List<PlanetCollectableData> GeneratePlanetData()
	{
		List<PlanetCollectableData> list = new List<PlanetCollectableData>();
		for (int i = 1; i < 6; i++)
		{
			PlanetCollectableData planetCollectableData = new PlanetCollectableData();
			planetCollectableData.CollectType = (CollectableType)i;
			planetCollectableData.Quantity = Random.Range(minRoll, maxRoll);
			list.Add(planetCollectableData);
		}
		return list;
	}
}
