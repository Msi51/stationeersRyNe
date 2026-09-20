using System.Collections.Generic;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using TMPro;
using UnityEngine;
using Util;

namespace Assets.Scripts.UI;

public class SPDACombustionItem : UserInterfaceBase
{
	public SPDAGasQuantity Fuel;

	public SPDAGasQuantity Oxidiser;

	private List<SPDAGasQuantity> _outputs = new List<SPDAGasQuantity>(4);

	[SerializeField]
	private SPDAGasQuantity gasQuantityPrefab;

	[SerializeField]
	private Transform outputGasParent;

	[SerializeField]
	private TextMeshProUGUI autoIgnitionValue;

	[SerializeField]
	private TextMeshProUGUI outputTemperature;

	public void Populate(Chemistry.GasType fuel, Chemistry.GasType oxidiser, CombustionResult combustionResult)
	{
		TemperatureKelvin temperature = Chemistry.Temperature.TwentyDegrees;
		if (Mole.MatterState(fuel) == AtmosphereHelper.MatterState.Liquid)
		{
			temperature = Mole.BoilingPoint(fuel);
		}
		TemperatureKelvin temperature2 = Chemistry.Temperature.TwentyDegrees;
		if (Mole.MatterState(oxidiser) == AtmosphereHelper.MatterState.Liquid)
		{
			temperature2 = Mole.BoilingPoint(oxidiser);
		}
		Mole fuel2 = new Mole(fuel, combustionResult.FuelMoleCount, IdealGas.Energy(temperature, Mole.SpecificHeat(fuel), combustionResult.FuelMoleCount));
		Mole oxidiser2 = new Mole(oxidiser, combustionResult.OxidiserMoleCount, IdealGas.Energy(temperature2, Mole.SpecificHeat(oxidiser), combustionResult.OxidiserMoleCount));
		MoleEnergy combustionEnergy;
		MoleQuantity burnedFuel;
		float cleanBurnRatio;
		GasMixture gasMixture = combustionResult.RunCombustion(fuel2, oxidiser2, 1.0, out combustionEnergy, out burnedFuel, out cleanBurnRatio);
		Fuel.Populate(fuel, combustionResult.FuelMoleCount, temperature);
		Oxidiser.Populate(oxidiser, combustionResult.OxidiserMoleCount, temperature2);
		ClearChildren();
		CombustionValue[] outputs = combustionResult.Outputs;
		for (int i = 0; i < outputs.Length; i++)
		{
			CombustionValue combustionValue = outputs[i];
			SPDAGasQuantity sPDAGasQuantity = Object.Instantiate(gasQuantityPrefab, outputGasParent);
			_outputs.Add(sPDAGasQuantity);
			sPDAGasQuantity.Populate(combustionValue.GasType, combustionValue.Quantity, gasMixture.Temperature);
		}
		TemperatureKelvin temperatureKelvin = fuel2.AutoIgnitionTemperature + oxidiser2.AutoIgnitionOffset;
		autoIgnitionValue.text = (temperatureKelvin - Chemistry.Temperature.ZeroDegrees).ToDouble().ToStringPrefix("°C", "orange");
		string color = ((gasMixture.Temperature > Chemistry.Temperature.FiftyDegrees) ? "orange" : "lightblue");
		outputTemperature.text = Mathf.Round((gasMixture.Temperature - Chemistry.Temperature.ZeroDegrees).ToFloat()).ToStringPrefix("°C", color);
	}

	public void Clear()
	{
		ClearChildren();
	}

	private void ClearChildren()
	{
		for (int num = _outputs.Count - 1; num >= 0; num--)
		{
			_outputs[num]?.GameObject.DestroyGameObject();
			_outputs.RemoveAt(num);
		}
	}
}
