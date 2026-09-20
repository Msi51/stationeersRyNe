using TMPro;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class ToxicGasText : MonoBehaviour
{
	[SerializeField]
	private TextMeshProUGUI GasNameValue;

	[SerializeField]
	private TextMeshProUGUI PartialPressureLimitValue;

	[SerializeField]
	private TextMeshProUGUI PartialPressurePresentValue;

	public void Apply(PlantAnalyserCartridge.ToxicGasInfo info)
	{
		GasNameValue.text = info.GasName;
		PartialPressureLimitValue.text = info.PartialPressureLimit;
		PartialPressurePresentValue.text = info.PartialPressurePresent;
	}
}
