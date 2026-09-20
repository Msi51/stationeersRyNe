using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Util;

namespace Assets.Scripts.UI;

public class StationSuitProperties
{
	public readonly string MovementSpeed;

	public readonly string CoolantTemperatureRange;

	public readonly string OperatingTemperatureRange;

	public readonly string MaxOperatingTemperatureRange;

	public StationSuitProperties()
	{
	}

	public StationSuitProperties(SuitBase suitBase)
	{
		if (!(suitBase == null))
		{
			MovementSpeed = (suitBase.MovementSpeedMultiplier * 100f).ToStringPercent("red");
			string arg = RocketMath.KelvinToCelsius(suitBase.MinCoolantTemperatureK).ToStringPrefix("°C", "#008AE6");
			string arg2 = RocketMath.KelvinToCelsius(suitBase.MaxCoolantTemperatureK).ToStringPrefix("°C", "red");
			CoolantTemperatureRange = GameStrings.SuitOperatingRangeText.AsString(arg, arg2);
			string arg3 = suitBase.SuitConvectionData.MinimumTemperatureC.ToStringPrefix("°C", "#008AE6");
			string arg4 = suitBase.SuitConvectionData.OperatingColdTemperatureC.ToStringPrefix("°C", "green");
			string arg5 = suitBase.SuitConvectionData.OperatingHotTemperatureC.ToStringPrefix("°C", "green");
			string arg6 = suitBase.SuitConvectionData.MaximumTemperatureC.ToStringPrefix("°C", "orange");
			OperatingTemperatureRange = GameStrings.SuitOperatingRangeText.AsString(arg4, arg5);
			MaxOperatingTemperatureRange = GameStrings.SuitOperatingRangeText.AsString(arg3, arg6);
		}
	}
}
