using System.Xml.Serialization;
using Assets.Scripts.Util;
using UnityEngine;

public class GlobalTemperatureFloatOffset : GlobalTemperatureOffsetData
{
	[XmlElement("Day")]
	public FloatReference FloatOffsetDayData = new FloatReference();

	[XmlElement("Night")]
	public FloatReference FloatOffsetNightData = new FloatReference();

	public override float GetOffset(float solarAngleDegrees)
	{
		FloatReference floatOffsetDayData;
		if (solarAngleDegrees < 70f)
		{
			floatOffsetDayData = FloatOffsetDayData;
			if (floatOffsetDayData == null)
			{
				return 0f;
			}
			return floatOffsetDayData;
		}
		if (solarAngleDegrees > 110f)
		{
			floatOffsetDayData = FloatOffsetNightData;
			if (floatOffsetDayData == null)
			{
				return 0f;
			}
			return floatOffsetDayData;
		}
		float t = RocketMath.MapToScaleClamp(70f, 110f, 0f, 1f, solarAngleDegrees);
		floatOffsetDayData = FloatOffsetDayData;
		float a = ((floatOffsetDayData != null) ? ((float)floatOffsetDayData) : 0f);
		floatOffsetDayData = FloatOffsetNightData;
		return Mathf.Lerp(a, (floatOffsetDayData != null) ? ((float)floatOffsetDayData) : 0f, t);
	}

	public override float GetOffset(float solarAngleDegrees, float evaluateX)
	{
		return GetOffset(solarAngleDegrees);
	}
}
