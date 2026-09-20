using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.UI.ImGuiUi;

public class GlobalAtmosphereData
{
	[XmlElement("Temperature")]
	public AnimationCurveData SolarAngleTemperatureCurveData = new AnimationCurveData();

	[XmlElement("SolarRadiationTemperature")]
	public GlobalTemperatureCurveOffset SolarRadiationTemperatureOffset = new GlobalTemperatureCurveOffset();

	[XmlElement("GHGTemperatureOffset")]
	public GlobalTemperatureCurveOffset GHGTemperatureOffset = new GlobalTemperatureCurveOffset();

	[XmlElement("DensityOffset")]
	public GlobalTemperatureCurveOffset DensityOffset = new GlobalTemperatureCurveOffset();

	[XmlElement("Gas")]
	public GlobalGasMixData GlobalGasMixData = new GlobalGasMixData();

	[XmlElement("Volume")]
	public DoubleReference Volume = new DoubleReference
	{
		Value = 40000000000.0
	};

	public MoleQuantity TotalMolesGasses()
	{
		MoleQuantity zero = MoleQuantity.Zero;
		foreach (GlobalMoleData globalMoleData in GlobalGasMixData.GlobalMoleDatas)
		{
			zero += new MoleQuantity(globalMoleData.Quantity);
		}
		return zero;
	}

	public VolumeLitres GetVolume()
	{
		return new VolumeLitres(Volume.Value);
	}

	public TemperatureKelvin GetSolarAngleTemperature(float solarAngle)
	{
		return new TemperatureKelvin(SolarAngleTemperatureCurveData.Curve?.Evaluate(solarAngle) ?? 0f);
	}

	public TemperatureKelvin GetSolarDistanceTemperatureOffset(float solarAngle, float solarEnergyPercent)
	{
		return new TemperatureKelvin(SolarRadiationTemperatureOffset.GetOffset(solarAngle, solarEnergyPercent));
	}

	public TemperatureKelvin GetGHGTemperatureOffset(float solarAngle, float ghgIndex)
	{
		return new TemperatureKelvin(GHGTemperatureOffset.GetOffset(solarAngle, ghgIndex));
	}

	public TemperatureKelvin GetDensityOffset(float solarAngle, double density)
	{
		return new TemperatureKelvin(DensityOffset.GetOffset(solarAngle, (float)density));
	}

	public void Init()
	{
		SolarAngleTemperatureCurveData.Init();
		SolarRadiationTemperatureOffset.Init();
		GHGTemperatureOffset.Init();
		DensityOffset.Init();
		GlobalGasMixData.Init();
	}
}
