using System.Collections.Generic;
using System.Text;
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

namespace Assets.Scripts.Objects;

public class SurvivalToolbelt : ToolBelt, ILogicable, IReferencable, IEvaluable, IBatteryPowered, IPowered, IDensePoolable, ICircuitHolder, ISoundAlert, ITransmitable
{
	[Header("Power Tool")]
	public float UsedPowerPassive = 1f;

	private float ExplosionForce = 200f;

	private float ExplosionRadius = 2.3f;

	private readonly DensePoolReference<ICircuitHolder> _circuitHolderPool = new DensePoolReference<ICircuitHolder>(CircuitHolders.AllCircuitHolders);

	private List<ILogicable> _logicList = new List<ILogicable>(20);

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	public BatteryCell Battery => BatterySlot.Get<BatteryCell>();

	public Slot BatterySlot { get; private set; }

	public Slot ChipSlot { get; private set; }

	private ProgrammableChip ProgrammableChip => ChipSlot.Get<ProgrammableChip>();

	public bool IsOperable
	{
		get
		{
			if ((bool)Battery)
			{
				return !Battery.IsEmpty;
			}
			return false;
		}
	}

	public ulong LastEditedBy { get; set; }

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
			if ((bool)_playingAudio)
			{
				int clipDataNameHash = AudioManager.Find((SoundAlert)SoundAlert)?.NameHash ?? 0;
				_playingAudio.GameAudioSource?.SetVolumeMultiplier(clipDataNameHash, (float)(int)SoundVolume / 100f);
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

	public override void Awake()
	{
		base.Awake();
		BatterySlot = Slots.Find((Slot s) => s.Type == Slot.Class.Battery);
		ChipSlot = Slots.Find((Slot s) => s.Type == Slot.Class.ProgrammableChip);
		if (IsCursor)
		{
			return;
		}
		ElectricityManager.Register(this);
		foreach (Slot slot in Slots)
		{
			_ = slot;
			_logicList.Add(null);
		}
		if ((object)this != null)
		{
			CircuitHolders.Register(this);
		}
	}

	public override bool OnAddToPool(object densePool, int slot)
	{
		if (_circuitHolderPool.CanAddToPool(densePool))
		{
			return _circuitHolderPool.AddToPool(densePool, slot);
		}
		return base.OnAddToPool(densePool, slot);
	}

	public override void OnRemoveFromPool(object densePool)
	{
		base.OnRemoveFromPool(densePool);
		_circuitHolderPool.OnRemovedFrom(densePool);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (GameManager.GameState != GameState.None && !IsCursor)
		{
			ElectricityManager.Deregister(this);
			if ((object)this != null)
			{
				CircuitHolders.Deregister(this);
			}
		}
	}

	public void Recharge(float amount)
	{
		if ((bool)Battery)
		{
			Battery.PowerStored += amount;
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		if (OnOff && Powered && (object)Battery != null)
		{
			Battery.PowerStored -= UsedPowerPassive;
		}
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		ProgrammableChip programmableChip = ProgrammableChip;
		if ((bool)programmableChip)
		{
			extendedText.Append(programmableChip.GetErrorCode());
		}
		return extendedText;
	}

	public void ClearError()
	{
	}

	public void RaiseError(int state)
	{
	}

	public List<LogicBinding> GetLogicBindings()
	{
		List<LogicBinding> list = new List<LogicBinding>();
		list.Add(new LogicBinding("TOOLBELT"));
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			if (slot.Type != Slot.Class.None)
			{
				string text = EnumCollections.SlotClasses.GetName(slot.Type);
				text = text.Replace(" ", "_").ToUpper();
				list.Add(new LogicBinding(i, text));
			}
		}
		return list;
	}

	public ILogicable GetLogicableFromIndex(int deviceIndex, int networkIndex = int.MinValue)
	{
		if (deviceIndex == int.MaxValue)
		{
			return this;
		}
		if (deviceIndex >= Slots.Count)
		{
			return null;
		}
		Slot slot = Slots[deviceIndex];
		if (slot.Type == Slot.Class.None)
		{
			return null;
		}
		if (slot.Occupant is ILogicable result)
		{
			return result;
		}
		return null;
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

	public List<ILogicable> GetBatchOutput()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			if (slot.Type != Slot.Class.None)
			{
				_logicList[i] = slot.Get<ILogicable>();
			}
		}
		return _logicList;
	}

	public bool IsValidIndex(int index)
	{
		if (index != int.MaxValue)
		{
			if (index >= 0)
			{
				return index < Slots.Count;
			}
			return false;
		}
		return true;
	}

	public void SetDeviceLabel(int index, string label)
	{
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
		global::Explosion.Explode(ExplosionForce, base.Position, ExplosionRadius);
		OnServer.Destroy(this);
	}

	public void SetSourceCode(string sourceCode)
	{
		if ((bool)ProgrammableChip)
		{
			ProgrammableChip.SetSourceCode(sourceCode, this);
			ProgrammableChip.SendUpdate();
		}
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && (bool)Battery && !Battery.IsEmpty && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && !(ProgrammableChip == null) && !(Battery == null) && !ProgrammableChip.CompilationError)
		{
			Battery.PowerStored -= 2.5f;
			ProgrammableChip.Execute(128);
		}
	}

	public void HasPut()
	{
	}

	public string GetSourceCode()
	{
		if (!ProgrammableChip)
		{
			return "";
		}
		return ProgrammableChip.GetSourceCode();
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
			_playingAudio = Thing.PlayPooledAudioSound(this, Singleton<AudioManager>.Instance.GetChannelData(Defines.SoundChannel.Small));
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
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
		writer.WriteByte(SoundVolume);
		writer.WriteByte(SoundAlert);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		SoundVolume = reader.ReadByte();
		SoundAlert = reader.ReadByte();
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Volume:
		case LogicType.SoundAlert:
		case LogicType.EntityState:
		case LogicType.HealthDamage:
		case LogicType.StunDamage:
			return true;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Volume:
			SoundVolume = (byte)Mathf.Clamp((int)value, 0, 100);
			break;
		case LogicType.SoundAlert:
			SoundAlert = (byte)Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1);
			break;
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.Volume:
			return (int)SoundVolume;
		case LogicType.SoundAlert:
			return (int)SoundAlert;
		case LogicType.EntityState:
			if (!(base.ParentSlot?.Parent is Entity entity3))
			{
				return -1.0;
			}
			return (int)entity3.State;
		case LogicType.HealthDamage:
			if (!(base.ParentSlot?.Parent is Entity entity2))
			{
				return -1.0;
			}
			return (int)entity2.DamageState.Total;
		case LogicType.StunDamage:
			if (!(base.ParentSlot?.Parent is Entity entity))
			{
				return -1.0;
			}
			return (int)entity.DamageState.Stun;
		default:
			return base.GetLogicValue(logicType);
		}
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}
}
