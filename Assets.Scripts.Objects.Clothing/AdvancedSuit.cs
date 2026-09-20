using System;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Networking;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Clothing;

public class AdvancedSuit : Suit, ICircuitHolder, IDensePoolable, ITransmitable, ILogicable, IReferencable, IEvaluable, ISetable, ISoundAlert, IMemoryReadable, IMemory, IMemoryWritable
{
	private List<ILogicable> _logicList = new List<ILogicable>(20);

	private const float EXPLOSION_FORCE = 200f;

	private const float EXPLOSION_RADIUS = 2.3f;

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	[ByteArraySync]
	private int _setting = 1;

	public ulong LastEditedBy { get; set; }

	public override float HygieneReductionMultiplier => 1.5f;

	public byte SoundVolume
	{
		get
		{
			return _soundVolume;
		}
		set
		{
			_soundVolume = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
			if (_playingAudio != null)
			{
				_playingAudio.GameAudioSource.SetVolumeMultiplier(AudioManager.Find((SoundAlert)SoundAlert).NameHash, (float)(int)SoundVolume / 100f);
			}
		}
	}

	public byte SoundAlert
	{
		get
		{
			return _soundAlert;
		}
		set
		{
			_soundAlert = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			if (!GameManager.IsBatchMode)
			{
				WaitThenPlay().Forget();
			}
		}
	}

	public Slot ChipSlot => Slots[3];

	public virtual GasFilter Filter4 => FilterSlot4.Get<GasFilter>();

	public override Slot FilterSlot1 => Slots[4];

	public override Slot FilterSlot2 => Slots[5];

	public override Slot FilterSlot3 => Slots[6];

	public Slot FilterSlot4 => Slots[7];

	public override bool HasFilters
	{
		get
		{
			if (!base.HasFilters)
			{
				return FilterSlot4.Contains<GasFilter>();
			}
			return true;
		}
	}

	public double Setting
	{
		get
		{
			return _setting;
		}
		set
		{
			if ((int)value != _setting)
			{
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
				_setting = (int)value;
			}
		}
	}

	public void HasPut()
	{
	}

	public List<ILogicable> GetBatchOutput()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			_logicList[i] = GetSlot(i).Get<ILogicable>();
		}
		return _logicList;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		base.OnChildEnterInventory(newChild);
		if (GameManager.GameState == GameState.Running && ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			occupant.Reset();
			ClearError();
		}
	}

	public async UniTask HaltAndCatchFire()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		if (GameManager.IsThread)
		{
			await UniTask.SwitchToMainThread();
			if (ThingTransform.GetCancellationTokenOnDestroy().IsCancellationRequested)
			{
				return;
			}
		}
		base.InternalAtmosphere.Sparked = true;
		global::Explosion.Explode(200f, base.Position, 2.3f);
		OnServer.Destroy(this);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (!Powered && _playingAudio != null)
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		else if (Powered && _playingAudio == null && SoundAlert != 0 && !GameManager.IsBatchMode)
		{
			WaitThenPlay().Forget();
		}
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}

	private async UniTaskVoid WaitThenPlay()
	{
		if (!GameManager.IsMainThread)
		{
			await UniTask.SwitchToMainThread();
		}
		if (_playingAudio != null && PooledAudioSources.Contains(_playingAudio))
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		if (SoundAlert != 0)
		{
			_playingAudio = Thing.PlayPooledAudioSound(this, Singleton<AudioManager>.Instance.GetChannelData(Defines.SoundChannel.Medium));
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		foreach (Slot slot in Slots)
		{
			_ = slot;
			_logicList.Add(null);
		}
		ElectricityManager.Register(this);
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
			ElectricityManager.Deregister(this);
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteInt32(_setting);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteByte(SoundVolume);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte(SoundAlert);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			_setting = reader.ReadInt32();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			SoundVolume = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			SoundAlert = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteInt32(_setting);
		writer.WriteByte(SoundVolume);
		writer.WriteByte(SoundAlert);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_setting = reader.ReadInt32();
		SoundVolume = reader.ReadByte();
		SoundAlert = reader.ReadByte();
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && base.BatterySlot.Contains<BatteryCell>(out var occupant) && !occupant.IsEmpty && ChipSlot.Contains<ProgrammableChip>(out var occupant2))
		{
			occupant.PowerStored -= 2.5f;
			if (!occupant2.CompilationError)
			{
				occupant2.Execute(128);
			}
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.PressureExternal:
		case LogicType.Setting:
		case LogicType.Volume:
		case LogicType.PressureSetting:
		case LogicType.TemperatureSetting:
		case LogicType.TemperatureExternal:
		case LogicType.Filtration:
		case LogicType.AirRelease:
		case LogicType.PositionX:
		case LogicType.PositionY:
		case LogicType.PositionZ:
		case LogicType.VelocityMagnitude:
		case LogicType.VelocityRelativeX:
		case LogicType.VelocityRelativeY:
		case LogicType.VelocityRelativeZ:
		case LogicType.SoundAlert:
		case LogicType.ForwardX:
		case LogicType.ForwardY:
		case LogicType.ForwardZ:
		case LogicType.Orientation:
		case LogicType.VelocityX:
		case LogicType.VelocityY:
		case LogicType.VelocityZ:
		case LogicType.EntityState:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Error:
		case LogicType.Setting:
		case LogicType.Volume:
		case LogicType.PressureSetting:
		case LogicType.TemperatureSetting:
		case LogicType.Filtration:
		case LogicType.AirRelease:
		case LogicType.SoundAlert:
			return true;
		default:
			return base.CanLogicWrite(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.EntityState:
			if ((object)base.ParentSlot?.Parent == null || base.ParentSlot.Type != Slot.Class.Suit)
			{
				return -1.0;
			}
			return ((int?)base.ParentEntity?.State) ?? (-1);
		case LogicType.TemperatureExternal:
			return base.WorldAtmosphere?.Temperature.ToDouble() ?? 0.0;
		case LogicType.PressureExternal:
			return base.WorldAtmosphere?.PressureGassesAndLiquids.ToDouble() ?? 0.0;
		case LogicType.PressureSetting:
			return base.OutputSetting;
		case LogicType.TemperatureSetting:
			return base.OutputTemperature.ToDouble();
		case LogicType.AirRelease:
			return Importing;
		case LogicType.Filtration:
			return Exporting;
		case LogicType.PositionX:
			return base.Position.x;
		case LogicType.PositionY:
			return base.Position.y;
		case LogicType.PositionZ:
			return base.Position.z;
		case LogicType.VelocityMagnitude:
			return base.VelocityMagnitude;
		case LogicType.VelocityRelativeX:
			return RelativeVelocity.x;
		case LogicType.VelocityRelativeY:
			return RelativeVelocity.y;
		case LogicType.VelocityRelativeZ:
			return RelativeVelocity.z;
		case LogicType.VelocityX:
			return base.Velocity.x;
		case LogicType.VelocityY:
			return base.Velocity.y;
		case LogicType.VelocityZ:
			return base.Velocity.z;
		case LogicType.ForwardX:
			return Forward.x;
		case LogicType.ForwardY:
			return Forward.y;
		case LogicType.ForwardZ:
			return Forward.z;
		case LogicType.Orientation:
			return Orientation;
		case LogicType.Setting:
			return Setting;
		case LogicType.Volume:
			return (int)SoundVolume;
		case LogicType.SoundAlert:
			return (int)SoundAlert;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.PressureSetting:
			base.OutputSetting = (float)value;
			break;
		case LogicType.TemperatureSetting:
			base.OutputTemperature = new TemperatureKelvin(value);
			break;
		case LogicType.Error:
			OnServer.Interact(base.InteractError, (int)value);
			break;
		case LogicType.AirRelease:
			OnServer.Interact(base.InteractImport, (int)value);
			break;
		case LogicType.Filtration:
			OnServer.Interact(base.InteractExport, (int)value);
			break;
		case LogicType.Setting:
			Setting = value;
			break;
		case LogicType.Volume:
			SoundVolume = (byte)Mathf.Clamp((int)value, 1, 100);
			break;
		case LogicType.SoundAlert:
			SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1);
			break;
		}
	}

	public void ClearError()
	{
	}

	public void RaiseError(int state)
	{
	}

	public List<LogicBinding> GetLogicBindings()
	{
		return new List<LogicBinding>
		{
			new LogicBinding("SUIT"),
			new LogicBinding(0, "HELMET"),
			new LogicBinding(1, "BACKPACK"),
			new LogicBinding(2, "TOOLBELT"),
			new LogicBinding(3, "GLASSES"),
			new LogicBinding(4, "LEFT_HAND"),
			new LogicBinding(5, "RIGHT_HAND")
		};
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		return deviceIndex switch
		{
			int.MaxValue => this, 
			0 => base.ParentEntity?.HelmetSlot.Get<ILogicable>(), 
			1 => base.ParentEntity?.BackpackSlot.Get<ILogicable>(), 
			2 => base.ParentEntity?.ToolbeltSlot.Get<ILogicable>(), 
			3 => base.ParentEntity?.GlassesSlot.Get<ILogicable>(), 
			4 => base.ParentEntity?.LeftHandSlot.Get<ILogicable>(), 
			5 => base.ParentEntity?.RightHandSlot.Get<ILogicable>(), 
			_ => null, 
		};
	}

	public ILogicable GetLogicableFromId(int deviceId, int networkIndex = int.MinValue)
	{
		if (deviceId == 0L)
		{
			return null;
		}
		ILogicable logicable = Referencable.Find<ILogicable>(deviceId);
		if (logicable == null)
		{
			return null;
		}
		foreach (Slot slot in Slots)
		{
			if (slot.Occupant == logicable)
			{
				return logicable;
			}
		}
		return null;
	}

	public bool IsValidIndex(int index)
	{
		switch (index)
		{
		case 0:
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case int.MaxValue:
			return true;
		default:
			return false;
		}
	}

	public void SetDeviceLabel(int index, string label)
	{
	}

	public void SetSourceCode(string sourceCode)
	{
		if (ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			occupant.SetSourceCode(sourceCode, this);
			occupant.SendUpdate();
		}
	}

	public string GetSourceCode()
	{
		AsciiString? asciiString = ChipSlot.Get<ProgrammableChip>()?.GetSourceCode();
		if (!asciiString.HasValue)
		{
			return string.Empty;
		}
		return asciiString.GetValueOrDefault();
	}

	public double ReadMemory(int address)
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		return occupant.ReadMemory(address);
	}

	public void WriteMemory(int address, double value)
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		occupant.WriteMemory(address, value);
	}

	public void ClearMemory()
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			throw new NullReferenceException();
		}
		occupant.ClearMemory();
	}

	public int GetStackSize()
	{
		if (!ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			return 0;
		}
		return occupant?.GetStackSize() ?? 0;
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		if (ChipSlot.Contains<ProgrammableChip>(out var occupant))
		{
			extendedText.Append(occupant.GetErrorCode());
		}
		return extendedText;
	}
}
