using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI.HelperHints.Extensions;
using Assets.Scripts.Util;
using UnityEngine;

namespace Assets.Scripts.Objects;

public class LargeExtendableRadiator : RadiatorRotatable
{
	[Tooltip("Numbers Greater than 1 reduce panel radiation effectiveness when in sunlight. Panel radiation is multiplied by 1 / value when in direct sunlight.")]
	[SerializeField]
	private float _solarRadiationRetardationDenominator = 24f;

	private float SolarRadiationModulator
	{
		get
		{
			if (!IsHeatedByAtmosphere())
			{
				return _solarRadiationRetardationDenominator;
			}
			return 1f;
		}
	}

	public override float ConvectionFactor
	{
		get
		{
			if (base.SourcePrefab == this)
			{
				return 0.02f;
			}
			if (!IsOpen || IsBroken)
			{
				return 0f;
			}
			return 0.02f;
		}
	}

	public override float RadiationFactor
	{
		get
		{
			if (base.SourcePrefab == this)
			{
				return 2f;
			}
			if (!IsOpen || IsBroken)
			{
				return 0f;
			}
			return Mathf.Lerp(SolarRadiationModulator, 1f, base.HeatingEfficiency) / SolarRadiationModulator + 1f;
		}
	}

	public override float SolarHeatingFactor
	{
		get
		{
			if (!IsOpen || IsBroken)
			{
				return 0f;
			}
			return base.HeatingEfficiency * SolarHeatingScale;
		}
	}

	public override double Horizontal
	{
		get
		{
			return base.Horizontal;
		}
		set
		{
			base.Horizontal = value;
			if ((bool)_panelRotation)
			{
				_panelRotation.localRotation = Quaternion.Euler(0f, (float)(Horizontal * base.MaximumHorizontal), 0f);
			}
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(100f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(20f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	private bool IsHeatedByAtmosphere()
	{
		Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
		if (atmosphere == null)
		{
			return false;
		}
		if (atmosphere.Temperature > base.InternalAtmosphere.Temperature)
		{
			return true;
		}
		return false;
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Vertical)
		{
			return false;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Vertical)
		{
			return false;
		}
		return base.CanLogicWrite(logicType);
	}

	public override string PanelInfo()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(GameStrings.RadiatorHeatingEfficiency.AsString(((IsOpen && !IsBroken) ? Mathf.Round(base.HeatingEfficiency * 100f) : 0f).ToStringPercent("yellow")));
		stringBuilder.Newline();
		stringBuilder.Append(GameStrings.ThingHealth.AsString((100f - DamageState.TotalRatio * 100f).ToStringPercent(base.DamageColor)));
		return stringBuilder.ToString();
	}

	public override void OnAnimationStart()
	{
		base.OnAnimationStart();
	}

	public override void OnAnimationStop()
	{
		base.OnAnimationStop();
	}
}
