using Assets.Scripts.Localization2;
using Assets.Scripts.UI;
using TMPro;
using UnityEngine;

public class StationpediaRocketPanel : MonoBehaviour
{
	public GameObject ConstructableInRocketsitem;

	public TextMeshProUGUI ConstructableInRocketsValue;

	public GameObject RocketMassContribution;

	public TextMeshProUGUI RocketMassContributionValue;

	public GameObject EngineForce;

	public TextMeshProUGUI EngineForceValue;

	public GameObject EngineEfficiency;

	public TextMeshProUGUI EngineEfficiencyValue;

	public GameObject EngineExhaustVelocity;

	public TextMeshProUGUI EngineExhaustVelocityValue;

	public bool Show(StationpediaPage page)
	{
		return !string.IsNullOrEmpty(page.PlaceableInRocket);
	}

	public void SetRocketPanelValues(StationpediaPage page)
	{
		bool flag = !string.IsNullOrEmpty(page.PlaceableInRocket);
		bool flag2 = !string.IsNullOrEmpty(page.RocketMass);
		ConstructableInRocketsitem.SetActive(flag);
		RocketMassContribution.SetActive(flag2);
		if (flag)
		{
			ConstructableInRocketsValue.text = GameStrings.ConstructableInRocketsTrue;
		}
		if (flag2)
		{
			RocketMassContributionValue.text = page.RocketMass;
		}
		bool flag3 = !string.IsNullOrEmpty(page.RocketEngineForce) && !string.IsNullOrEmpty(page.RocketEngineEfficiency) && !string.IsNullOrEmpty(page.RocketEngineExhaustVelocity);
		EngineForce.SetActive(flag3);
		EngineEfficiency.SetActive(flag3);
		EngineExhaustVelocity.SetActive(flag3);
		if (flag3)
		{
			EngineForceValue.text = page.RocketEngineForce;
			EngineEfficiencyValue.text = page.RocketEngineEfficiency;
			EngineExhaustVelocityValue.text = page.RocketEngineExhaustVelocity;
		}
	}
}
