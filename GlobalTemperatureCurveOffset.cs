using System;
using System.Xml.Serialization;
using Assets.Scripts.UI.ImGuiUi;
using Assets.Scripts.Util;
using UnityEngine;

public class GlobalTemperatureCurveOffset : GlobalTemperatureOffsetData
{
	[XmlElement("Day")]
	public AnimationCurveData DayCurveOffsetData = new AnimationCurveData();

	[XmlElement("Night")]
	public AnimationCurveData NightCurveOffsetData = new AnimationCurveData();

	public override float GetOffset(float solarAngleDegrees)
	{
		throw new NotImplementedException();
	}

	public override void Init()
	{
		base.Init();
		DayCurveOffsetData?.Init();
		NightCurveOffsetData?.Init();
	}

	public override float GetOffset(float solarAngleDegrees, float evaluateX)
	{
		if (solarAngleDegrees < 70f)
		{
			return (DayCurveOffsetData?.Curve?.Evaluate(evaluateX)).GetValueOrDefault();
		}
		if (solarAngleDegrees > 110f)
		{
			return (NightCurveOffsetData?.Curve?.Evaluate(evaluateX)).GetValueOrDefault();
		}
		float valueOrDefault = (DayCurveOffsetData?.Curve?.Evaluate(evaluateX)).GetValueOrDefault();
		float valueOrDefault2 = (NightCurveOffsetData?.Curve?.Evaluate(evaluateX)).GetValueOrDefault();
		float num = RocketMath.MapToScaleClamp(70f, 110f, 0f, 1f, solarAngleDegrees);
		num = ((!(num < 0.5f)) ? Mathf.Sin(MathF.PI * num / 2f) : (1f - Mathf.Cos(MathF.PI * num / 2f)));
		return Mathf.Lerp(valueOrDefault, valueOrDefault2, num);
	}
}
