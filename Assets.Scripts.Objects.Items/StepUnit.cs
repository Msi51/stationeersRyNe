using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Electrical;
using UnityEngine;

namespace Assets.Scripts.Objects.Items;

public class StepUnit : SmallDevice, ISmartRotatable
{
	public delegate void StepOperation(StepData stepData);

	public Knob PitchKnob;

	public Knob VolumeKnob;

	private int _volume = 64;

	public static readonly int MaxMidiValue = 127;

	public static readonly string[] MidiNotePitchStrings = new string[128]
	{
		"C-2", "C#-2", "D-2", "D#-2", "E-2", "F-2", "F#-2", "G-2", "G#-2", "A-2",
		"A#-2", "B-2", "C-1", "C#-1", "D-1", "D#-1", "E-1", "F-1", "F#-1", "G-1",
		"G#-1", "A-1", "A#-1", "B-1", "C0", "C#0", "D0", "D#0", "E0", "F0",
		"F#0", "G0", "G#0", "A0", "A#0", "B0", "C1", "C#1", "D1", "D#1",
		"E1", "F1", "F#1", "G1", "G#1", "A1", "A#1", "B1", "C2", "C#2",
		"D2", "D#2", "E2", "F2", "F#2", "G2", "G#2", "A2", "A#2", "B2",
		"C3", "C#3", "D3", "D#3", "E3", "F3", "F#3", "G3", "G#3", "A3",
		"A#3", "B3", "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4",
		"G#4", "A4", "A#4", "B4", "C5", "C#5", "D5", "D#5", "E5", "F5",
		"F#5", "G5 ", "G#5", "A5", "A#5", "B5", "C6", "C#6", "D6", "D#6",
		"E6", "F6", "F#6", "G6", "G#6", "A6", "A#6", "B6", "C7", "C#7",
		"D7", "D#7", "E7", "F7", "F#7", "G7", "G#7", "A7", "A#7", "B7",
		"C8", "C#8", "D8", "D#8", "E8", "F8", "F#8", "G8"
	};

	public static readonly int OctaveSemitones = 12;

	private static readonly int IsCurrentHash = Animator.StringToHash("Current");

	private static readonly int IsIdleHash = Animator.StringToHash("Idle");

	private bool _isCurrentStep;

	[Header("ISmartRotation")]
	public SmartRotate.ConnectionType ConnectionType = SmartRotate.ConnectionType.Exhaustive;

	public int[] OpenEndsPermutation = new int[6] { 0, 1, 2, 3, 4, 5 };

	[ByteArraySync]
	public int Volume
	{
		get
		{
			return _volume;
		}
		set
		{
			_volume = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
		}
	}

	public override string[] ModeStrings => MidiNotePitchStrings;

	public bool IsCurrentStep
	{
		get
		{
			return _isCurrentStep;
		}
		set
		{
			if (value != _isCurrentStep)
			{
				base.ActivateButton.MaterialChanger.ChangeState(value ? IsCurrentHash : IsIdleHash);
			}
			_isCurrentStep = value;
		}
	}

	public event StepOperation OnPlayStepManual = delegate
	{
	};

	public StepData StepData()
	{
		return new StepData
		{
			Enabled = OnOff,
			Pitch = (sbyte)Mathf.Clamp(Mode, 0, MaxMidiValue),
			Velocity = (sbyte)Mathf.Clamp(Volume, 0, MaxMidiValue)
		};
	}

	public override void Awake()
	{
		base.Awake();
		PitchKnob.Initialize(this);
		VolumeKnob.Initialize(this);
		PitchKnob.SetKnob(Mode, ModeStrings.Length - 1).Forget();
		VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte((byte)Volume);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Volume = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)Volume);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Volume = reader.ReadByte();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		if (interactable == base.InteractMode)
		{
			PitchKnob.SetKnob(base.InteractMode.State, MaxMidiValue).Forget();
		}
		if (interactable == base.InteractActivate && Activate > 0)
		{
			this.OnPlayStepManual(StepData());
		}
		base.OnInteractableUpdated(interactable);
	}

	private async UniTaskVoid ResetActivateButton()
	{
		await UniTask.Delay(500);
		OnServer.Interact(this, InteractableType.Activate, 0);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		switch (interactable.Action)
		{
		case InteractableType.Activate:
			if (!doAction || interactable.State == 1)
			{
				return delayedActionInstance.Succeed();
			}
			interactable.State = 1;
			this.OnPlayStepManual(StepData());
			if (GameManager.RunSimulation)
			{
				ResetActivateButton().Forget();
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button1:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalPitch, ModeStrings[Mode]);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			PitchKnob.PlayKnobSound(Mode, increase: false, ModeStrings.Length - 1);
			Mode = Mathf.Max(Mode - ((!interaction.AltKey) ? 1 : OctaveSemitones), 0);
			PitchKnob.SetKnob(Mode, ModeStrings.Length - 1).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalPitch, ModeStrings[Mode]);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			PitchKnob.PlayKnobSound(Mode, increase: true, ModeStrings.Length - 1);
			Mode = Mathf.Min(Mode + ((!interaction.AltKey) ? 1 : OctaveSemitones), MaxMidiValue);
			PitchKnob.SetKnob(Mode, ModeStrings.Length - 1).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button3:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalVolume, StringManager.Get(Volume));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			VolumeKnob.PlayKnobSound(Volume, increase: false, MaxMidiValue);
			Volume = Mathf.Max(Volume - ((!interaction.AltKey) ? 1 : 10), 0);
			VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button4:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalVolume, StringManager.Get(Volume));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			VolumeKnob.PlayKnobSound(Volume, increase: true, MaxMidiValue);
			Volume = Mathf.Min(Volume + ((!interaction.AltKey) ? 1 : 10), MaxMidiValue);
			VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			return InterfaceStrings.Pitch;
		}
		return base.GetContextualName(interactable);
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Volume)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Volume)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		if (logicType == LogicType.Volume)
		{
			return Volume;
		}
		return base.GetLogicValue(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Volume:
			Volume = Mathf.Clamp((int)value, 0, MaxMidiValue);
			VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
			break;
		case LogicType.Activate:
			if (value > 0.0)
			{
				this.OnPlayStepManual(StepData());
				ResetActivateButton().Forget();
			}
			break;
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StepUnitSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is StepUnitSaveData stepUnitSaveData)
		{
			stepUnitSaveData.Volume = Volume;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StepUnitSaveData stepUnitSaveData)
		{
			Volume = stepUnitSaveData.Volume;
		}
		VolumeKnob.SetKnob(Volume, MaxMidiValue).Forget();
		PitchKnob.SetKnob(Mode, ModeStrings.Length - 1).Forget();
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
