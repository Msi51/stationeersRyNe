using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Electrical;

public class TurbineGenerator : Device, ISmartRotatable
{
	public static int SpeedState = Animator.StringToHash("Speed");

	private static readonly float PressureSpeedThreshold = 0.001f;

	private static readonly int TurbineOperatingHash = Animator.StringToHash("TurbineOperating");

	private static readonly int TurbineOperatingSlowHash = Animator.StringToHash("TurbineOperatingSlow");

	[FormerlySerializedAs("OperatingSlowSoundCurve")]
	public AnimationCurve OperatingSlowVolumeCurve;

	public AnimationCurve OperatingSlowPitchCurve;

	public WorldGrid ForwardGrid;

	public WorldGrid BackwardGrid;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	private bool _shouldPlaySound;

	private float _pressureSpeed;

	private float _generatedPower;

	public static float MaxOutputPower = 90f;

	public static PressurekPa MaxProcessing = Chemistry.OneAtmosphere / 2.0;

	public bool ShouldPlaySound
	{
		get
		{
			return _shouldPlaySound;
		}
		set
		{
			if (value != ShouldPlaySound)
			{
				_shouldPlaySound = value;
				if (_shouldPlaySound)
				{
					PlaySound(TurbineOperatingHash);
					PlaySound(TurbineOperatingSlowHash);
				}
				else
				{
					StopSound(TurbineOperatingHash);
					StopSound(TurbineOperatingSlowHash);
				}
			}
		}
	}

	public float PressureSpeed
	{
		get
		{
			return _pressureSpeed;
		}
		set
		{
			if (NetworkManager.IsServer && NetworkServer.HasClients() && !RocketMath.Approximately(PressureSpeed, value))
			{
				base.NetworkUpdateFlags |= 512;
			}
			_pressureSpeed = value;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		ForwardGrid = new WorldGrid(base.ThingTransformPosition + ThingTransform.forward);
		BackwardGrid = new WorldGrid(base.ThingTransformPosition - ThingTransform.forward);
	}

	public override void UpdateEachFrame()
	{
		if (WorldManager.IsGamePaused)
		{
			return;
		}
		base.UpdateEachFrame();
		if (!IsOccluded)
		{
			BaseAnimator.SetFloat(SpeedState, PressureSpeed);
			ShouldPlaySound = PressureSpeed >= PressureSpeedThreshold;
			if (ShouldPlaySound)
			{
				GetAudioEvent(TurbineOperatingHash).SetVolumeAndPitch(PressureSpeed, PressureSpeed);
				GetAudioEvent(TurbineOperatingSlowHash).SetVolumeAndPitch(OperatingSlowVolumeCurve.Evaluate(PressureSpeed), OperatingSlowPitchCurve.Evaluate(PressureSpeed));
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(PressureSpeed);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		PressureSpeed = reader.ReadSingle();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteSingle(PressureSpeed);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			PressureSpeed = reader.ReadSingle();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (base.GridController.CanContainAtmos(ForwardGrid) && base.GridController.CanContainAtmos(BackwardGrid))
		{
			Atmosphere atmosphere = base.AtmosphericsController.CloneGlobalAtmosphere(ForwardGrid, 0L);
			Atmosphere atmosphere2 = base.AtmosphericsController.CloneGlobalAtmosphere(BackwardGrid, 0L);
			PressurekPa obj = atmosphere?.PressureGassesAndLiquids ?? PressurekPa.Zero;
			PressurekPa pressurekPa = atmosphere2?.PressureGassesAndLiquids ?? PressurekPa.Zero;
			PressurekPa pressurekPa2 = RocketMath.Min(RocketMath.Abs(obj - pressurekPa), MaxProcessing);
			float num = (pressurekPa2 / MaxProcessing).ToFloat();
			_generatedPower = num * MaxOutputPower;
			if (Mathf.Abs(num - PressureSpeed) > PressureSpeedThreshold)
			{
				PressureSpeed = Mathf.Clamp01(num);
			}
			if (pressurekPa2 > PressurekPa.Zero)
			{
				AtmosphereHelper.EqualizeBothWays(atmosphere, atmosphere2, AtmosphereHelper.MatterState.All, Chemistry.OneAtmosphere, MoleQuantity.MaxValue);
			}
		}
	}

	public override float GetGeneratedPower(CableNetwork cableNetwork)
	{
		if (cableNetwork != base.PowerCableNetwork)
		{
			return 0f;
		}
		return _generatedPower;
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
