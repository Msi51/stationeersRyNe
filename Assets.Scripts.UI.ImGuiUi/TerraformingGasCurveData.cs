using System.Xml.Serialization;
using Assets.Scripts.Atmospherics;

namespace Assets.Scripts.UI.ImGuiUi;

public class TerraformingGasCurveData : AnimationCurveData
{
	[XmlAttribute("Gas")]
	public Chemistry.GasType GasType;

	public TerraformingGasCurveData()
	{
	}

	public TerraformingGasCurveData(Chemistry.GasType type)
	{
		GasType = type;
	}

	public override void Init()
	{
		Id = GasType.ToString();
		base.Init();
		Register();
	}

	private void Register()
	{
		if (GasType != Chemistry.GasType.Undefined)
		{
			TerraForming.TerraformingGasCurves.TryAdd(GasType, this);
		}
	}
}
