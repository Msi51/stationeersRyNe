using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class AdvancedTablet : Tablet, ICircuitHolder, IDensePoolable, ISoundAlert, ITransmitable, ILogicable, IReferencable, IEvaluable
{
	private float ExplosionForce = 200f;

	private float ExplosionRadius = 2.3f;

	private List<ILogicable> _logicList = new List<ILogicable>(20);

	public int CartridgeCount;

	public List<Slot> CartridgeSlots = new List<Slot>();

	private byte _soundVolume = 50;

	private byte _soundAlert;

	private PooledAudioSource _playingAudio;

	public ulong LastEditedBy { get; set; }

	public Slot ChipSlot => Slots[3];

	private ProgrammableChip ProgrammableChip => ChipSlot.Occupant as ProgrammableChip;

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

	public void HasPut()
	{
	}

	public List<ILogicable> GetBatchOutput()
	{
		for (int i = 0; i < Slots.Count; i++)
		{
			Slot slot = Slots[i];
			_logicList[i] = slot.Occupant as ILogicable;
		}
		return _logicList;
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
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
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
		return logicType switch
		{
			LogicType.Volume => (int)SoundVolume, 
			LogicType.SoundAlert => (int)SoundAlert, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public void OnTransmitterCreated()
	{
		Transmitters.AllTransmitters.Add(this);
	}

	public override void Awake()
	{
		base.Awake();
		for (int i = 0; i < Slots.Count; i++)
		{
			if (Slots[i].Type == Slot.Class.Cartridge)
			{
				CartridgeSlots.Add(Slots[i]);
			}
		}
		if (GameManager.GameState == GameState.None)
		{
			return;
		}
		foreach (Slot slot in Slots)
		{
			_ = slot;
			_logicList.Add(null);
		}
	}

	public override void OnDestroy()
	{
		if (!Singleton<GameManager>.IsQuitting)
		{
			base.OnDestroy();
		}
	}

	public void Execute()
	{
		if (GameManager.RunSimulation && !IsCursor && (bool)base.Battery && !base.Battery.IsEmpty && GameManager.GameState == GameState.Running && !WorldManager.IsGamePaused && !(ProgrammableChip == null) && !(base.Battery == null) && !ProgrammableChip.CompilationError)
		{
			base.Battery.PowerStored -= 2.5f;
			ProgrammableChip.Execute(128);
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
			new LogicBinding("TABLET"),
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
			0 => RootParentHuman?.HelmetSlot.Get<ILogicable>(), 
			1 => RootParentHuman?.BackpackSlot.Get<ILogicable>(), 
			2 => RootParentHuman?.ToolbeltSlot.Get<ILogicable>(), 
			3 => RootParentHuman?.GlassesSlot.Get<ILogicable>(), 
			4 => RootParentHuman?.LeftHandSlot.Get<ILogicable>(), 
			5 => RootParentHuman?.RightHandSlot.Get<ILogicable>(), 
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
		if ((bool)ProgrammableChip)
		{
			ProgrammableChip.SetSourceCode(sourceCode, this);
			ProgrammableChip.SendUpdate();
		}
	}

	public string GetSourceCode()
	{
		if (!ProgrammableChip)
		{
			return "";
		}
		return ProgrammableChip.GetSourceCode();
	}

	public override StringBuilder GetExtendedText()
	{
		StringBuilder extendedText = base.GetExtendedText();
		Slot slot = CartridgeSlots[1];
		if (slot.IsNotEmpty())
		{
			extendedText.AppendLine(GameStrings.SlotContainsItem.AsString(slot.ToTooltip(), slot.Get().ToTooltip()));
		}
		ProgrammableChip programmableChip = ProgrammableChip;
		if ((bool)programmableChip)
		{
			extendedText.Append(programmableChip.GetErrorCode());
		}
		return extendedText;
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Activate)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success((Activate == 0) ? "Activate" : "Deactivate");
			}
			Activate = ((Activate != 1) ? 1 : 0);
			return DelayedActionInstance.Success("Activate");
		}
		if (interactable.Action == InteractableType.Button1)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success("Set");
			}
			int num = Mode + 1;
			if (num >= CartridgeSlots.Count)
			{
				num = 0;
			}
			Thing.Interact(base.InteractMode, num);
			GetCartridge();
		}
		if (interactable.Action == InteractableType.Button2)
		{
			if (!doAction)
			{
				return DelayedActionInstance.Success("Set");
			}
			int num2 = Mode - 1;
			if (num2 < 0)
			{
				num2 = CartridgeSlots.Count - 1;
			}
			Thing.Interact(base.InteractMode, num2);
			GetCartridge();
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	private void GetCartridge()
	{
		RemoveScreen(Cartridge);
		if (CartridgeSlots.Count > 0 && Mode >= 0 && Mode < CartridgeSlots.Count)
		{
			if ((bool)CartridgeSlots[Mode].Occupant)
			{
				Cartridge = (Cartridge)CartridgeSlots[Mode].Occupant;
			}
			else
			{
				Cartridge = null;
			}
		}
		else
		{
			Cartridge = null;
		}
		TransferScreen();
		InputHandling();
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (GameManager.GameState != GameState.None)
		{
			_ = savedData is AdvancedTabletSaveData;
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if ((!OnOff || !Powered) && (bool)_playingAudio)
		{
			_playingAudio.Stop(immediate: true);
			_playingAudio = null;
		}
		else if (OnOff && Powered && !_playingAudio && SoundAlert != 0 && !GameManager.IsBatchMode)
		{
			WaitThenPlay().Forget();
		}
		if (interactable.Action == InteractableType.Mode)
		{
			GetCartridge();
		}
	}

	public override void CheckError()
	{
		if (!GameManager.RunSimulation)
		{
			return;
		}
		bool flag = true;
		foreach (Slot slot in Slots)
		{
			Slot.Class type = slot.Type;
			if (type == Slot.Class.ProgrammableChip || type == Slot.Class.Cartridge)
			{
				flag = false;
				break;
			}
		}
		if (flag && Error == 0)
		{
			OnServer.Interact(base.InteractError, 1);
		}
		else if (!flag && Error == 1)
		{
			OnServer.Interact(base.InteractError, 0);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new AdvancedTabletSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		_ = savedData is AdvancedTabletSaveData;
	}

	public override void ValidateOnLoad(int currentSaveVersion)
	{
		if (Mode < 0 || Mode >= CartridgeSlots.Count)
		{
			OnServer.Interact(base.InteractMode, 0);
		}
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		GetCartridge();
	}
}
