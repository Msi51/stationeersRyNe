using System;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;
using Weather;

namespace Objects;

public class WindTurbineGenerator : Device, ISmartRotatable, IPowered, IDensePoolable, IReferencable, IEvaluable, IPowerGenerator
{
	private new const float SHADOW_DISTANCE = 10f;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public static int SpeedState = Animator.StringToHash("Speed");

	public const float NoiseFrequency = 0.01f;

	public const float WindDirectionFrequency = 0.005f;

	private static OpenSimplexNoise PowerGenerationSimplexNoise = new OpenSimplexNoise(42L);

	private static readonly float MinVolume = 0.5f;

	public float MinDistance = 12f;

	public float MaxDistance = 20f;

	[SerializeField]
	private Transform bladesTransform;

	[SerializeField]
	private Vector3 bladesRotationVector = Vector3.up;

	private float _generationRate;

	private static readonly PressurekPa _maxPressure = new PressurekPa(25.0);

	private static readonly PressurekPa _minPressure = new PressurekPa(5.0);

	private float _generatedPower;

	private float _turbineRotationSpeed;

	[SerializeField]
	private Transform headRotator;

	private const float TURBINE_ROTATION_SPEED = 0.5f;

	private static readonly float DegreesRotationPerSecond = 720f;

	private static readonly int TurbineOperatingHash = Animator.StringToHash("TurbineOperating");

	private GameAudioSource _turbineAudio;

	private bool _isPlayingSound;

	public virtual float MAXPowerOutput => 500f;

	public virtual float MaxPowerOutputStorm => 1000f;

	public virtual float NoiseIntensity => 10f;

	public virtual float WeatherUtilisationMultiplier => 3f;

	public static float WindStrength { get; private set; }

	public static Vector3 WindDirection { get; private set; }

	public override float AudioDistanceSquared => 80f;

	private bool IsPlayingSound
	{
		get
		{
			return _isPlayingSound;
		}
		set
		{
			if (value != _isPlayingSound)
			{
				_isPlayingSound = value;
				if (_isPlayingSound)
				{
					PlaySound(TurbineOperatingHash, Mathf.Lerp(MinVolume, 1f, _turbineRotationSpeed), _turbineRotationSpeed);
					_turbineAudio = GetAudioSource(GetAudioEvent(TurbineOperatingHash).Channel);
				}
				else
				{
					StopSound(TurbineOperatingHash);
				}
			}
		}
	}

	protected override float GetRenderMaxDistanceSquared()
	{
		return Mathf.Pow(40f * OcclusionManager.RenderDistanceMultiplier, 2f);
	}

	protected override float GetShadowMaxDistanceSquared()
	{
		return Mathf.Pow(10f * Settings.CurrentData.ThingShadowDistanceMultiplier, 2f);
	}

	public float GetMaxPowerGenerated()
	{
		return MAXPowerOutput;
	}

	public override void Awake()
	{
		base.Awake();
		bladesTransform.Rotate(bladesRotationVector, UnityEngine.Random.Range(0f, 360f));
	}

	public float CalculateGenerationRate()
	{
		if (!IsOperable || GetRoom() != null || !base.IsStructureCompleted)
		{
			return 0f;
		}
		if (!base.HasOpenGrid)
		{
			return 0f;
		}
		PressurekPa worldAtmospherePressureClamped = GetWorldAtmospherePressureClamped();
		if (worldAtmospherePressureClamped <= PressurekPa.Zero)
		{
			return 0f;
		}
		int num;
		float num2;
		if (WeatherManager.CurrentWeatherEvent != null && WeatherManager.IsWeatherEventRunning)
		{
			num = ((WeatherManager.CurrentWeatherEvent.StormEffect != null) ? 1 : 0);
			if (num != 0)
			{
				num2 = (float)WeatherManager.CurrentWeatherEvent.WindStrength * WeatherUtilisationMultiplier;
				goto IL_0084;
			}
		}
		else
		{
			num = 0;
		}
		num2 = 1f;
		goto IL_0084;
		IL_0084:
		float num3 = num2;
		float max = ((num != 0) ? MaxPowerOutputStorm : MAXPowerOutput);
		float min = ((num != 0) ? ((float)WeatherManager.CurrentWeatherEvent.WindStrength * (MaxPowerOutputStorm / 100f)) : 0f);
		return Mathf.Clamp(worldAtmospherePressureClamped.ToFloat() * WindStrength * num3 * NoiseIntensity, min, max);
	}

	public static void UpdateWind()
	{
		WindStrength = GetNoise(1f);
		WindDirection = GetWindDirection();
	}

	public static float GetNoise(float noiseIntensity)
	{
		return Mathf.Abs(PowerGenerationSimplexNoise.Evaluate(0f, NetworkTime.time * 0.01f) * noiseIntensity);
	}

	public static Vector3 GetWindDirection()
	{
		float value = PowerGenerationSimplexNoise.Evaluate(0f, NetworkTime.time * 0.005f);
		float num = RocketMath.MapToScale(-1f, 1f, -180f, 180f, value);
		Vector3 result = new Vector3(Mathf.Cos(num * (MathF.PI / 180f)), 0f, Mathf.Sin(num * (MathF.PI / 180f)));
		if (WeatherManager.IsWeatherEventRunning)
		{
			return WeatherManager.StormDirectionVector * -1f;
		}
		return result;
	}

	public PressurekPa GetWorldAtmospherePressure()
	{
		if (GetRoom() != null)
		{
			return PressurekPa.Zero;
		}
		return AtmosphericsController.ReadonlyGlobalAtmosphere(base.WorldGrid.Value).PressureGassesAndLiquids;
	}

	public PressurekPa GetWorldAtmospherePressureClamped()
	{
		PressurekPa worldAtmospherePressure = GetWorldAtmospherePressure();
		if (worldAtmospherePressure < PressurekPa.One)
		{
			return PressurekPa.Zero;
		}
		return RocketMath.Clamp(worldAtmospherePressure, _minPressure, _maxPressure);
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork || base.PowerCableNetwork == null)
		{
			_generatedPower = 0f;
			return _generatedPower;
		}
		_generatedPower = CalculateGenerationRate();
		return _generatedPower;
	}

	private float NormalizeValues(float val, float max, float min)
	{
		return Mathf.Clamp01((val - min) / (max - min));
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		if (_generationRate > 0f && base.IsStructureCompleted)
		{
			if (IsOccluded)
			{
				IsPlayingSound = false;
				return;
			}
			float num = (InventoryManager.Parent ? Vector3.SqrMagnitude(InventoryManager.Parent.Position - base.Position) : float.MaxValue);
			IsPlayingSound = num < AudioDistanceSquared;
			if ((bool)_turbineAudio?.AudioSource && !_turbineAudio.PausedForConcurrency && !_turbineAudio.AudioSource.isVirtual)
			{
				_turbineAudio.SetVolumeMultiplier(TurbineOperatingHash, Mathf.Lerp(MinVolume, 1f, _turbineRotationSpeed));
				_turbineAudio.SetPitchMultiplier(TurbineOperatingHash, _turbineRotationSpeed);
				_turbineAudio.maxDistance = Mathf.Lerp(MinDistance, MaxDistance, _turbineRotationSpeed);
			}
		}
		else
		{
			IsPlayingSound = false;
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		if (_generationRate > 0f && base.IsStructureCompleted)
		{
			if (!IsOccluded)
			{
				float value = NormalizeValues(_generationRate, MAXPowerOutput, 0f);
				SetTurbineRotationSpeed(Mathf.Clamp(value, 0f, MAXPowerOutput));
				if ((bool)headRotator)
				{
					SetTurbineDirection(WindDirection);
				}
			}
		}
		else
		{
			SetTurbineRotationSpeed(0f);
		}
	}

	private void SetTurbineDirection(Vector3 forward)
	{
		Quaternion b = Quaternion.LookRotation(forward, Vector3.up);
		Quaternion rotation = Quaternion.Lerp(headRotator.rotation, b, GameManager.DeltaTime * 0.5f);
		headRotator.rotation = rotation;
	}

	private void SetTurbineRotationSpeed(float speed)
	{
		if ((bool)BaseAnimator)
		{
			BaseAnimator.SetFloat(SpeedState, speed);
		}
		else if ((bool)bladesTransform && speed > 0f)
		{
			bladesTransform.Rotate(bladesRotationVector, DegreesRotationPerSecond * GameManager.DeltaTime * speed);
		}
		_turbineRotationSpeed = speed;
	}

	public override CanConstructInfo CanConstruct()
	{
		WorldGrid worldGrid = new WorldGrid(base.ThingTransformPosition - ThingTransform.up * GridSize);
		Structure structure = base.GridController.Get<Structure>(worldGrid);
		if (!structure || ((bool)structure && !structure.AllowMounting))
		{
			return CanConstructInfo.InvalidPlacement(GameStrings.PlacementRequiresFrame.DisplayString);
		}
		return base.CanConstruct();
	}

	public SmartRotate.ConnectionType GetConnectionType()
	{
		return ConnectionType;
	}

	public void SetOpenEndsPermutation(int[] permutation)
	{
		OpenEndsPermutation = (int[])permutation.Clone();
	}

	public void SetConnectionType(SmartRotate.ConnectionType connectionType)
	{
		ConnectionType = connectionType;
	}

	public int[] GetOpenEndsPermutation()
	{
		return (int[])OpenEndsPermutation.Clone();
	}

	public override void OnDestroy()
	{
		IsPlayingSound = false;
		base.OnDestroy();
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.IsBatchMode)
		{
			_generationRate = CalculateGenerationRate();
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.PowerGeneration)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.PowerGeneration)
		{
			return _generatedPower;
		}
		return base.GetLogicValue(logicType);
	}
}
