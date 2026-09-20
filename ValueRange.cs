using System;
using System.Xml.Serialization;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;

[Serializable]
[XmlRoot("Range")]
public class ValueRange
{
	[XmlAttribute]
	public float Minimum;

	[XmlAttribute]
	public float Maximum;

	public static ValueRange zero = new ValueRange();

	public ValueRange()
	{
	}

	public ValueRange(float min)
	{
		Minimum = min;
		Maximum = min;
	}

	public ValueRange(float min, float max)
	{
		Minimum = min;
		Maximum = max;
	}

	public ValueRange(GlobalAtmosphereData globalAtmosphereData, float range, float step, ValueRange solarRadiation)
	{
		float num = float.PositiveInfinity;
		float num2 = float.NegativeInfinity;
		GlobalGasMix globalGasMix = GlobalGasMix.Create(globalAtmosphereData);
		TerraForming.GetGhgIndex(globalGasMix);
		IdealGas.GetMilliMolesPerLitre(globalGasMix.Volume, globalGasMix.TotalQuantityGas());
		for (float num3 = 0f; num3 < range; num3 += step)
		{
			float solarEnergyPercentClamped = OrbitalSimulation.System.GetSolarEnergyPercentClamped(solarRadiation, solarRadiation.Maximum);
			float num4 = globalGasMix.GetGlobalGasMixTemperature(globalAtmosphereData, num3, solarEnergyPercentClamped).ToFloat();
			if (num4 > num2)
			{
				num2 = num4;
			}
			if (num4 < num)
			{
				num = num4;
			}
			float solarEnergyPercentClamped2 = OrbitalSimulation.System.GetSolarEnergyPercentClamped(solarRadiation, solarRadiation.Minimum);
			num4 = globalGasMix.GetGlobalGasMixTemperature(globalAtmosphereData, num3, solarEnergyPercentClamped2).ToFloat();
			if (num4 > num2)
			{
				num2 = num4;
			}
			if (num4 < num)
			{
				num = num4;
			}
		}
		Minimum = num;
		Maximum = num2;
	}

	public override string ToString()
	{
		return $"ValueRange({Minimum},{Maximum})";
	}
}
