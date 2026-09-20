using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace Assets.Scripts.Objects.Pipes;

public class DeviceAtmospherics : Device, ISmartRotatable
{
	[SerializeField]
	private float outputSetting = 5f;

	[FormerlySerializedAs("PressurePerTick")]
	[SerializeField]
	protected float pressurePerTick = 101.325f;

	public float MinSetting;

	public float MaxSetting = 10f;

	public SettingWheel SettingWheel;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	public const int CHECK_CONNECTIONS_WAIT = 100;

	private bool _checkScheduled;

	[ByteArraySync]
	public float OutputSetting
	{
		get
		{
			return outputSetting;
		}
		set
		{
			float @float = Mathf.Clamp(value, MinSetting, MaxSetting);
			if (!RocketMath.Approximately(outputSetting, @float))
			{
				outputSetting = @float;
				OnOutputSettingChanged();
			}
		}
	}

	public PressurekPa PressurePerTick => new PressurekPa(pressurePerTick);

	public virtual float WheelSettingIncrement => 10f;

	public virtual float WheelAltSettingIncrement => 1f;

	public virtual bool HasValidConnections => true;

	protected override bool IsOperable
	{
		get
		{
			if (Error == 1)
			{
				if (!HasPipeNetwork || !base.HasOpenGrid)
				{
					return false;
				}
				if (GameManager.RunSimulation)
				{
					OnServer.Interact(base.InteractError, 0);
				}
				return true;
			}
			if (HasPipeNetwork && base.HasOpenGrid)
			{
				return true;
			}
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			return false;
		}
	}

	public virtual bool HasPipeNetwork => ConnectedPipeNetworks.Count > 0;

	public override void Awake()
	{
		base.Awake();
		if (!(SettingWheel == null))
		{
			SettingWheel.Awake();
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (!HasValidConnections && !_checkScheduled)
		{
			CheckConnectionsFromThread().Forget();
		}
	}

	private async UniTaskVoid CheckConnectionsFromThread()
	{
		_checkScheduled = true;
		await UniTask.Delay(100);
		await UniTask.SwitchToMainThread();
		if (!base.IsBeingDestroyed)
		{
			CheckConnections();
			_checkScheduled = false;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(OutputSetting);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			OutputSetting = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(OutputSetting);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		OutputSetting = reader.ReadSingle();
	}

	protected void CheckWheel()
	{
		if (!(SettingWheel == null))
		{
			SettingWheel.CheckWheel().Forget();
		}
	}

	public virtual void OnOutputSettingChanged()
	{
		if (NetworkManager.IsServer)
		{
			base.NetworkUpdateFlags |= 256;
		}
		CheckWheel();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting || logicType - 23 <= LogicType.Power)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => OutputSetting, 
			LogicType.Maximum => MaxSetting, 
			LogicType.Ratio => OutputSetting / MaxSetting, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (logicType == LogicType.Setting)
		{
			OutputSetting = (float)value;
			if (SettingWheel != null)
			{
				SettingWheel.ThreadedCheck();
			}
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new DeviceAtmosphericSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override string GetStationpediaCategory()
	{
		return Localization.GetInterface(StationpediaCategoryStrings.AtmosDevices);
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is DeviceAtmosphericSaveData deviceAtmosphericSaveData)
		{
			OutputSetting = Mathf.Clamp(deviceAtmosphericSaveData.OutputSetting, MinSetting, MaxSetting);
		}
		if ((bool)SettingWheel)
		{
			SettingWheel.OnDeserialize();
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is DeviceAtmosphericSaveData deviceAtmosphericSaveData)
		{
			deviceAtmosphericSaveData.OutputSetting = OutputSetting;
		}
	}

	public void MoveToEqualize(Atmosphere inputAtmos, Atmosphere outputAtmos)
	{
		AtmosphereHelper.MoveToEqualize(inputAtmos, outputAtmos, PressurePerTick, AtmosphereHelper.MatterState.All);
	}

	public void MoveToEqualizeGases(Atmosphere inputAtmos, Atmosphere outputAtmos)
	{
		AtmosphereHelper.MoveToEqualize(inputAtmos, outputAtmos, PressurePerTick, AtmosphereHelper.MatterState.Gas);
	}

	public void MoveToEqualizeLiquids(Atmosphere inputAtmos, Atmosphere outputAtmos)
	{
		AtmosphereHelper.MoveToEqualize(inputAtmos, outputAtmos, PressurePerTick, AtmosphereHelper.MatterState.Liquid);
	}

	public void MoveTargetPressure(Atmosphere inputAtmos, Atmosphere outputAtmos)
	{
		if (inputAtmos == null || outputAtmos == null)
		{
			return;
		}
		Atmosphere atmosphere = ((inputAtmos.Volume > outputAtmos.Volume) ? inputAtmos : outputAtmos);
		PressurekPa pressurekPa = RocketMath.Min(PressurePerTick, PressurePerTick - outputAtmos.PressureGassesAndLiquids);
		if (pressurekPa > PressurekPa.Zero)
		{
			MoleQuantity moleQuantity = IdealGas.Quantity(pressurekPa, RocketMath.Min(inputAtmos.Volume, outputAtmos.Volume), atmosphere.Temperature);
			if (!(moleQuantity <= MoleQuantity.Zero))
			{
				GasMixture gasMixture = inputAtmos.Remove(moleQuantity, outputAtmos.AllowedMatterState);
				outputAtmos.Add(gasMixture);
			}
		}
	}

	public override void InitializeDevice()
	{
		base.InitializeDevice();
		base.InitializeDevice();
		if ((bool)SettingWheel)
		{
			SettingWheel.SetLastAngle();
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		if ((bool)SettingWheel)
		{
			SettingWheel.SetRotation();
		}
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
}
