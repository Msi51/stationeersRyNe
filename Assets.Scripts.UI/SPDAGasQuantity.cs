using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine.UI;

namespace Assets.Scripts.UI;

public class SPDAGasQuantity : UserInterfaceBase
{
	public TextMeshProUGUI Quantity;

	public Image GasIcon;

	public void Populate(Chemistry.GasType gasType, MoleQuantity quantity, TemperatureKelvin temperature)
	{
		Stationpedia.TryGetGasThumbnail(gasType, out var thumbnail);
		GasIcon.sprite = thumbnail;
		Quantity.text = " x " + quantity.ToDouble().ToStringRounded().AsColor("lightblue");
	}
}
