using System;
using System.Linq;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Sound;
using Assets.Scripts.Util;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class Speaker : LogicUnitBase, IAirlockDevice, IAudioInput, ILogicable, IReferencable, IEvaluable
{
	private float _volume = 50f;

	public static readonly string[] modeStrings = Enum.GetNames(typeof(SoundAlert));

	public static readonly int[] ModeHashes = modeStrings.Select(Animator.StringToHash).ToArray();

	private static readonly Vector3 SpeakerAudioOffset = new Vector3(0f, 0f, 0.4f);

	private const float AUDIBLE_SQUARE_DISTANCE = 1700f;

	public float Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			if (Volume != value && Mode <= ModeHashes.Length && Mode >= 0)
			{
				_volume = value;
				SetSourceVolume(ModeHashes[Mode], _volume / 50f);
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 256;
				}
			}
		}
	}

	public override string[] ModeStrings => modeStrings;

	public IAudioInput AudioOutput
	{
		get
		{
			return null;
		}
		set
		{
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			writer.WriteSingle(Volume);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(256u, networkUpdateType))
		{
			Volume = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(Volume);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Volume = reader.ReadSingle();
	}

	public override CanConstructInfo CanConstruct()
	{
		CanMountResult canMountResult = CanMountOnWall();
		if (canMountResult.result != WallMountResult.Valid)
		{
			return CanConstructInfo.InvalidPlacement(canMountResult.ResultMessage());
		}
		return CanConstructInfo.ValidPlacement;
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button1)
		{
			return EnumCollections.SpeakerSounds.GetNameFromValue(Mode);
		}
		if (interactable.Action == InteractableType.Button2)
		{
			return "Volume " + StringManager.Get((int)Volume) + "%";
		}
		return base.GetContextualName(interactable);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable.Action == InteractableType.Button1 || interactable.Action == InteractableType.Button2)
		{
			DelayedActionInstance delayedActionInstance = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			if (interactable.Action == InteractableType.Button1)
			{
				string arg = (interaction.AltKey ? modeStrings[Mathf.Clamp(Mode - 1, 0, modeStrings.Length - 1)] : modeStrings[Mathf.Clamp(Mode + 1, 0, modeStrings.Length - 1)]);
				delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, arg);
				if (!KeyManager.GetButton(KeyMap.QuantityModifier))
				{
					delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					int mode = Mode;
					mode += ((!interaction.AltKey) ? 1 : (-1));
					mode = Mathf.Clamp(mode, 0, ModeStrings.Length - 1);
					OnServer.Interact(base.InteractMode, mode);
				}
				return DelayedActionInstance.Success(interactable.ContextualName);
			}
			if (interactable.Action == InteractableType.Button2)
			{
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				if (GameManager.RunSimulation)
				{
					if (interaction.AltKey && Volume > 1f)
					{
						Volume = Mathf.Max(Volume - 10f, 1f);
					}
					else if (!interaction.AltKey && Volume < 100f)
					{
						Volume = Mathf.Min(Volume + 10f, 100f);
					}
				}
				return delayedActionInstance.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Volume || logicType == LogicType.SoundAlert)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Volume => Volume, 
			LogicType.SoundAlert => Mode, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Volume:
			Volume = (int)(byte)Mathf.Clamp((int)value, 1, 200);
			break;
		case LogicType.SoundAlert:
			OnServer.Interact(base.InteractMode, Mathf.Clamp((int)value, 0, EnumCollections.SpeakerSounds.Length - 1));
			break;
		}
	}

	public PooledAudioSource InputAudioScheduled(int clipsDataHash, double startTime, double endTime, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		if (InventoryManager.ParentHuman != null && Vector3.SqrMagnitude(base.Position - InventoryManager.ParentHuman.Position) < 1700f)
		{
			return Singleton<AudioManager>.Instance.PlayScheduledAudioClipsData(this, clipsDataHash, startTime, endTime, SpeakerAudioOffset, volumeMultiplier, pitchMultiplier, attack, release);
		}
		return null;
	}

	public PooledAudioSource InputAudio(int clipsDataHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		return Singleton<AudioManager>.Instance.PlayAudioClipsData(this, clipsDataHash, SpeakerAudioOffset, null, volumeMultiplier, pitchMultiplier);
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new SpeakerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is SpeakerSaveData speakerSaveData)
		{
			Volume = speakerSaveData.Volume;
		}
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is SpeakerSaveData speakerSaveData)
		{
			speakerSaveData.Volume = Volume;
		}
	}
}
