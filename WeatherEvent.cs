using System;
using System.Xml.Serialization;
using Assets.Scripts;
using Trading;
using UnityEngine;

public class WeatherEvent : DataCollection
{
	private const int DEFAULT_FIRST_STORM_DELAY = 7;

	[XmlAttribute("FirstStormDelay")]
	public int FirstStormDelayDays = 7;

	[XmlAttribute("WindSound")]
	public bool WindSound = true;

	[XmlAttribute("ActiveInOrbit")]
	public bool ActiveInOrbit;

	[XmlElement("ParticleId")]
	public StringReference ParticleEffectId;

	[XmlElement("CoolDown")]
	public IntRangeData CoolDownDays;

	[XmlElement("StartDelay")]
	public FloatRangeData EventStartDelaySeconds;

	[XmlElement("Duration")]
	public FloatRangeData EventDurationSeconds;

	[XmlElement("StormEffect")]
	public StormEffectData StormEffect;

	[XmlElement("Fog")]
	public FogData Fog;

	[XmlElement("TemperatureOffset", typeof(GlobalTemperatureFloatOffset))]
	[XmlElement("TemperatureOffsetCurve", typeof(GlobalTemperatureCurveOffset))]
	public GlobalTemperatureOffsetData TemperatureOffset;

	[XmlElement("SolarRatio")]
	public FloatReference SolarRatio;

	[XmlElement("WindStrength")]
	public FloatReference WindStrength;

	[XmlElement("DamageMultiplier")]
	public FloatReference WeatherDamageMultiplier;

	[XmlElement("MovementSpeedMultiplier")]
	public FloatReference MovementSpeedMultiplier;

	[XmlElement("DirectionalLight")]
	public DirectionalLightData DirectionalLight;

	[XmlElement("SolarStormCameraEffect")]
	public FloatReference SolarStormCameraEffect;

	[XmlElement("Shell1Sound")]
	public StringReference Shell1Sound;

	[XmlElement("Shell2Sound")]
	public StringReference Shell2Sound;

	[XmlElement("Shell3Sound")]
	public StringReference Shell3Sound;

	[XmlElement("Shell4Sound")]
	public StringReference Shell4Sound;

	[XmlIgnore]
	private int Shell1SoundHash;

	[XmlIgnore]
	private int Shell2SoundHash;

	[XmlIgnore]
	private int Shell3SoundHash;

	[XmlIgnore]
	private int Shell4SoundHash;

	public string ToTooltip()
	{
		return $"<color=lightblue>{Name}</color>";
	}

	public override void Initialize(ModAbout mod)
	{
		if (IsValid())
		{
			DataCollection.Register(this, mod);
		}
		Shell1SoundHash = Animator.StringToHash(Shell1Sound);
		Shell2SoundHash = Animator.StringToHash(Shell2Sound);
		Shell3SoundHash = Animator.StringToHash(Shell3Sound);
		Shell4SoundHash = Animator.StringToHash(Shell4Sound);
	}

	public override bool IsValid()
	{
		if (!string.IsNullOrEmpty(Id) && CoolDownDays != null && EventStartDelaySeconds != null && EventDurationSeconds != null && TemperatureOffset != null && SolarRatio != null && WindStrength != null)
		{
			return base.IsValid();
		}
		return false;
	}

	public int GetShellSound(int shellIndex)
	{
		return shellIndex switch
		{
			0 => Shell1SoundHash, 
			1 => Shell2SoundHash, 
			2 => Shell3SoundHash, 
			3 => Shell4SoundHash, 
			_ => 0, 
		};
	}

	public int GetCoolDown(System.Random random)
	{
		return CoolDownDays.GenerateValue(random);
	}

	public float GetRandomWeatherStartTime(System.Random random)
	{
		return EventStartDelaySeconds.GenerateValue(random) + GameManager.GameTime;
	}

	public float GetRandomWeatherDuration(System.Random random)
	{
		return EventDurationSeconds.GenerateValue(random);
	}
}
