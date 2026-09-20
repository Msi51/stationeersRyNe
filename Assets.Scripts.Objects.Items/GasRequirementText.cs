using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class GasRequirementText : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI GasNameValue;

	[SerializeField]
	private TextMeshProUGUI PercentRequiredValue;

	[SerializeField]
	private TextMeshProUGUI PercentPresentValue;

	public void Apply(PlantAnalyserCartridge.GasRequirementInfo info)
	{
		GasNameValue.text = info.GasName;
		PercentRequiredValue.text = info.PercentRequired;
		PercentPresentValue.text = info.PercentPresent;
	}
}
