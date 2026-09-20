using System;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using Objects.Items;
using Sound;
using UnityEngine;

namespace Objects.Electrical;

public class AudioSequencer : LogicUnitBase
{
	private StepData[] _stepData = new StepData[MaxSteps];

	private byte _stepUnitUpdateFlags;

	public StepUnit[] StepUnits;

	private long[] _savedInputIds;

	private long _savedOutputId;

	private static readonly int NumberOfSteps = 8;

	public Knob SpeedKnob;

	public Knob AttackKnob;

	public Knob ReleaseKnob;

	private static readonly int MaxAttackSetting = 5000;

	private IAudioInput _audioOutput;

	public static float DriftTolerance = 0.5f;

	private double _targetAudioTime;

	public const float RenderBufferLookaheadDuration = 0.8f;

	public static readonly int MaxSteps = 256;

	public const int StepsPerBeat = 4;

	public const int MinBpm = 15;

	public const int MaxBpm = 180;

	private int _bpm = 60;

	private int _attack;

	private int _release;

	private int _lastRenderedStep;

	private double _lastRenderedStepEndTime;

	private double _lastBufferedDspTime;

	private double _audioTimeOnSettingChanged;

	public const float SecondsInMinute = 60f;

	private readonly List<int> _stepRenderList = new List<int>();

	public SequencerCartridge CurrentCartridge;

	public static readonly string[] SpeedModeStrings = new string[5] { "Whole Note", "Half Note", "Quarter Note", "Eighth Note", "Sixteenth Note" };

	private int _lastManualClipsDataHash;

	private PooledAudioSource _lastManualAudio;

	public static readonly double PlaybackBuffer = 0.10000000149011612;

	private static readonly int DeviceStepUnitHash = Animator.StringToHash("DeviceStepUnit");

	private long[] _stepNetIds;

	private int CurrentRealTimeStep => (int)(Math.Truncate(RealTimePlayHeadPosition) % (double)MaxSteps) / NoteLengthMultiplier() % NumberOfSteps;

	public IAudioInput AudioOutput
	{
		get
		{
			return _audioOutput;
		}
		set
		{
			_audioOutput = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
			}
		}
	}

	public override double Setting
	{
		get
		{
			return base.Setting;
		}
		set
		{
			double setting = Setting;
			base.Setting = value;
			if (setting > Setting)
			{
				_lastRenderedStep = 0;
				if (!GameManager.IsBatchMode)
				{
					_audioTimeOnSettingChanged = BufferedDspTime;
					ScheduleStepAudio().Forget();
				}
			}
			else if (!GameManager.IsBatchMode)
			{
				double bufferedDspTime = BufferedDspTime;
				double num = Setting - setting;
				double num2 = bufferedDspTime - _targetAudioTime;
				double num3 = _audioTimeOnSettingChanged + num;
				if (Math.Abs(num - num2) < (double)DriftTolerance && Math.Abs(num3 - bufferedDspTime) < (double)DriftTolerance)
				{
					_audioTimeOnSettingChanged = num3;
				}
				else
				{
					_audioTimeOnSettingChanged = BufferedDspTime;
				}
				_targetAudioTime = bufferedDspTime;
				ScheduleStepAudio().Forget();
			}
		}
	}

	[ByteArraySync]
	public int Bpm
	{
		get
		{
			return _bpm;
		}
		set
		{
			if (_bpm != value)
			{
				_bpm = value;
				_lastRenderedStep = CurrentStep;
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 1024;
				}
			}
		}
	}

	[ByteArraySync]
	public int Attack
	{
		get
		{
			return _attack;
		}
		set
		{
			if (_attack != value)
			{
				_attack = value;
				AttackKnob.SetKnob(value, MaxAttackSetting).Forget();
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 2048;
				}
			}
		}
	}

	[ByteArraySync]
	public int Release
	{
		get
		{
			return _release;
		}
		set
		{
			if (_release != value)
			{
				_release = value;
				ReleaseKnob.SetKnob(value, MaxAttackSetting).Forget();
				if (NetworkManager.IsServer)
				{
					base.NetworkUpdateFlags |= 4096;
				}
			}
		}
	}

	public static double BufferedDspTime => AudioSettings.dspTime + PlaybackBuffer;

	private double PlayHeadPosition => (double)((float)Bpm / 60f * 4f) * Setting;

	private double RealTimePlayHeadPosition => (double)((float)Bpm / 60f * 4f) * (Setting + BufferedDspTime - _audioTimeOnSettingChanged);

	private int CurrentStep => Convert.ToInt32(Math.Truncate(PlayHeadPosition));

	private double RenderRange => (float)Bpm / 60f * 4f * 0.8f;

	private int StepRenderTarget => Convert.ToInt32(Math.Truncate(PlayHeadPosition + RenderRange));

	public override int LogicOnHash => 0;

	public override int LogicOffHash => 0;

	public override string[] ModeStrings => SpeedModeStrings;

	public long[] StepUnitIds
	{
		get
		{
			if (_stepNetIds == null)
			{
				_stepNetIds = new long[NumberOfSteps];
			}
			for (int i = 0; i < StepUnits.Length; i++)
			{
				_stepNetIds[i] = ((StepUnits[i] == null) ? NetworkThing.Invalid : StepUnits[i].NetworkId);
			}
			return _stepNetIds;
		}
	}

	protected override bool IsOperable
	{
		get
		{
			if (CurrentCartridge == null)
			{
				if (Error == 0)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			StepUnit[] stepUnits = StepUnits;
			foreach (StepUnit item in stepUnits)
			{
				if (base.InputNetwork1 == null || !base.InputNetwork1.DataDeviceList.Contains(item))
				{
					if (Error == 0)
					{
						OnServer.Interact(base.InteractError, 1);
					}
					return false;
				}
			}
			if (AudioOutput == null || !base.OutputNetwork1DevicesSorted.Contains(AudioOutput))
			{
				if (Error == 0)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			if (Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return true;
		}
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.IsBatchMode || StepUnits == null || !OnOff || !Powered || Activate == 0 || Error == 1 || AudioSettings.dspTime > _audioTimeOnSettingChanged + 1.0)
		{
			return;
		}
		int currentRealTimeStep = CurrentRealTimeStep;
		if (currentRealTimeStep >= StepUnits.Length || StepUnits[currentRealTimeStep] == null || StepUnits[currentRealTimeStep].IsCurrentStep)
		{
			return;
		}
		for (int i = 0; i < StepUnits.Length; i++)
		{
			if (i != currentRealTimeStep)
			{
				StepUnits[i].IsCurrentStep = false;
			}
		}
		StepUnits[currentRealTimeStep].IsCurrentStep = true;
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteByte((byte)Bpm);
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			writer.WriteInt16((short)Attack);
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteInt16((short)Release);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteInt64(AudioOutput?.ReferenceId ?? 0);
			writer.WriteByte(_stepUnitUpdateFlags);
			if ((1 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[0]?.ReferenceId ?? 0);
			}
			if ((2 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[1]?.ReferenceId ?? 0);
			}
			if ((4 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[2]?.ReferenceId ?? 0);
			}
			if ((8 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[3]?.ReferenceId ?? 0);
			}
			if ((0x10 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[4]?.ReferenceId ?? 0);
			}
			if ((0x20 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[5]?.ReferenceId ?? 0);
			}
			if ((0x40 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[6]?.ReferenceId ?? 0);
			}
			if ((0x80 & _stepUnitUpdateFlags) != 0)
			{
				writer.WriteInt64(StepUnits[7]?.ReferenceId ?? 0);
			}
		}
		_stepUnitUpdateFlags = 0;
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Bpm = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(2048u, networkUpdateType))
		{
			Attack = reader.ReadInt16();
		}
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			Release = reader.ReadInt16();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			AudioOutput = Thing.Find<IAudioInput>(reader.ReadInt64());
			byte b = reader.ReadByte();
			if ((1 & b) != 0)
			{
				ReadStepUnit(0, reader);
			}
			if ((2 & b) != 0)
			{
				ReadStepUnit(1, reader);
			}
			if ((4 & b) != 0)
			{
				ReadStepUnit(2, reader);
			}
			if ((8 & b) != 0)
			{
				ReadStepUnit(3, reader);
			}
			if ((0x10 & b) != 0)
			{
				ReadStepUnit(4, reader);
			}
			if ((0x20 & b) != 0)
			{
				ReadStepUnit(5, reader);
			}
			if ((0x40 & b) != 0)
			{
				ReadStepUnit(6, reader);
			}
			if ((0x80 & b) != 0)
			{
				ReadStepUnit(7, reader);
			}
		}
	}

	private void ReadStepUnit(int index, RocketBinaryReader reader)
	{
		if (StepUnits[index] != null)
		{
			StepUnits[0].OnPlayStepManual -= PlayStep;
		}
		StepUnit stepUnit = Thing.Find<StepUnit>(reader.ReadInt64());
		if ((object)stepUnit != null)
		{
			StepUnits[index] = stepUnit;
			StepUnits[index].OnPlayStepManual += PlayStep;
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)Bpm);
		writer.WriteInt16((short)Attack);
		writer.WriteInt16((short)Release);
		writer.WriteInt64(AudioOutput?.ReferenceId ?? 0);
		StepUnit[] stepUnits = StepUnits;
		for (int i = 0; i < stepUnits.Length; i++)
		{
			writer.WriteInt64(stepUnits[i]?.ReferenceId ?? 0);
		}
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Bpm = reader.ReadByte();
		Attack = reader.ReadInt16();
		Release = reader.ReadInt16();
		_savedOutputId = reader.ReadInt64();
		_savedInputIds = new long[NumberOfSteps];
		for (int i = 0; i < _savedInputIds.Length; i++)
		{
			_savedInputIds[i] = reader.ReadInt64();
		}
	}

	private double StepTimeInternal(int step)
	{
		return (float)step * 60f / (float)Bpm / 4f;
	}

	private double StepLength()
	{
		return 60f / (float)Bpm / 4f * (float)NoteLengthMultiplier();
	}

	private double StepTime(int step)
	{
		return StepTimeInternal(step) - Setting + _audioTimeOnSettingChanged;
	}

	private double StepEndTime(int step)
	{
		return StepTimeInternal(step + NoteLengthMultiplier()) - Setting + _audioTimeOnSettingChanged;
	}

	private int NoteLengthMultiplier()
	{
		return Mode switch
		{
			0 => 16, 
			1 => 8, 
			2 => 4, 
			3 => 2, 
			4 => 1, 
			_ => 1, 
		};
	}

	public override void Awake()
	{
		base.Awake();
		SpeedKnob.Initialize(this);
		AttackKnob.Initialize(this);
		ReleaseKnob.Initialize(this);
		SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
		AttackKnob.SetKnob(Attack, MaxAttackSetting).Forget();
		ReleaseKnob.SetKnob(Release, MaxAttackSetting).Forget();
		_lastRenderedStep = CurrentStep;
	}

	public override void OnThreadUpdate()
	{
		base.OnThreadUpdate();
		if (!GameManager.IsBatchMode && !GameManager.RunSimulation && OnOff && Powered && Activate != 0)
		{
			UpdateStepData();
			UpdateStepRenderList();
		}
	}

	private async UniTaskVoid ScheduleStepAudio()
	{
		if (GameManager.IsBatchMode || !OnOff || !Powered || !IsOperable || ((Activate == 0) | !IsOperable))
		{
			return;
		}
		await UniTask.SwitchToThreadPool();
		UpdateStepData();
		UpdateStepRenderList();
		await UniTask.SwitchToMainThread();
		float num = ((_stepRenderList.Count > 0) ? (1f / (float)_stepRenderList.Count) : 1f);
		foreach (int stepRender in _stepRenderList)
		{
			int num2 = stepRender % MaxSteps;
			if (_stepData[num2].Enabled)
			{
				SampleData sampleData = CurrentCartridge.GetSampleData(_stepData[num2].Pitch);
				if (sampleData != null)
				{
					int clipsDataNameHash = sampleData.ClipsDataNameHash;
					float pitchMultiplier = sampleData.PitchMultiplier;
					float volumeMultiplier = (float)_stepData[num2].Velocity / (float)StepUnit.MaxMidiValue;
					double num3 = StepTime(stepRender);
					double num4 = StepEndTime(stepRender);
					_lastRenderedStep = stepRender;
					_lastRenderedStepEndTime = num4;
					if (num3 <= BufferedDspTime)
					{
						_audioTimeOnSettingChanged = Mathf.Lerp((float)_audioTimeOnSettingChanged, (float)_targetAudioTime, num * DriftTolerance);
						continue;
					}
					AudioOutput.InputAudioScheduled(clipsDataNameHash, num3, num4, volumeMultiplier, pitchMultiplier, Attack, Release);
				}
			}
			_audioTimeOnSettingChanged = Mathf.Lerp((float)_audioTimeOnSettingChanged, (float)_targetAudioTime, num * DriftTolerance);
		}
		_stepRenderList.Clear();
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		if (interactable == base.InteractMode)
		{
			SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
		}
		InteractableType action = interactable.Action;
		if (action == InteractableType.Activate || action == InteractableType.Powered)
		{
			base.ActivateButton.MaterialChanger.ChangeState((!Powered) ? ((Activate == 1) ? Defines.Animator.On : Defines.Animator.Off) : ((Activate == 1) ? Defines.Animator.OnPowered : Defines.Animator.Off));
		}
		base.OnInteractableUpdated(interactable);
	}

	private void UpdateStepData()
	{
		Array.Clear(_stepData, 0, _stepData.Length);
		for (int i = 0; i < StepUnits.Length; i++)
		{
			if (!(StepUnits[i] == null))
			{
				for (int j = i * NoteLengthMultiplier(); j < MaxSteps; j += StepUnits.Length * NoteLengthMultiplier())
				{
					_stepData[j].SetData(StepUnits[i].StepData());
				}
			}
		}
	}

	public void PlayStep(StepData stepData)
	{
		if (GameManager.IsBatchMode || !Powered || !OnOff || !IsOperable)
		{
			return;
		}
		SampleData sampleData = CurrentCartridge.GetSampleData(stepData.Pitch);
		if (sampleData != null)
		{
			int clipsDataNameHash = sampleData.ClipsDataNameHash;
			if (_lastManualAudio != null && _lastManualAudio.GameAudioSource.isPlaying)
			{
				_lastManualAudio.Stop(_lastManualClipsDataHash);
			}
			double bufferedDspTime = BufferedDspTime;
			double endTime = BufferedDspTime + StepLength();
			_lastManualAudio = AudioOutput.InputAudioScheduled(clipsDataNameHash, bufferedDspTime, endTime, Mathf.Lerp(0f, 1f, (float)stepData.Velocity / (float)StepUnit.MaxMidiValue), sampleData.PitchMultiplier, Attack, Release);
			_lastManualClipsDataHash = clipsDataNameHash;
		}
	}

	private void UpdateStepRenderList()
	{
		int currentStep = CurrentStep;
		int renderTarget = StepRenderTarget;
		_stepRenderList.RemoveAll((int x) => x < currentStep || x > renderTarget + 1);
		if (_lastRenderedStep > renderTarget)
		{
			_lastRenderedStep = currentStep;
		}
		for (int num = currentStep; num <= renderTarget; num++)
		{
			if (!_stepRenderList.Contains(num) && num > _lastRenderedStep)
			{
				_stepRenderList.Add(num);
			}
		}
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		if (interactable == null)
		{
			return null;
		}
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		int num = 0;
		string empty = string.Empty;
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			return SetStepDevice(delayedActionInstance, interaction, 0, doAction);
		case InteractableType.Button2:
			return SetStepDevice(delayedActionInstance, interaction, 1, doAction);
		case InteractableType.Button3:
			return SetStepDevice(delayedActionInstance, interaction, 2, doAction);
		case InteractableType.Button4:
			return SetStepDevice(delayedActionInstance, interaction, 3, doAction);
		case InteractableType.Button5:
			return SetStepDevice(delayedActionInstance, interaction, 4, doAction);
		case InteractableType.Button6:
			return SetStepDevice(delayedActionInstance, interaction, 5, doAction);
		case InteractableType.Button7:
			return SetStepDevice(delayedActionInstance, interaction, 6, doAction);
		case InteractableType.Button8:
			return SetStepDevice(delayedActionInstance, interaction, 7, doAction);
		case InteractableType.Button9:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalAttack, StringManager.Get(Attack));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Clamp(Attack - (interaction.AltKey ? 1000 : 100), 0, MaxAttackSetting);
			AttackKnob.PlayKnobSound(num, increase: false, MaxAttackSetting);
			AttackKnob.SetKnob(num, MaxAttackSetting).Forget();
			if (GameManager.RunSimulation)
			{
				Attack = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button10:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalAttack, StringManager.Get(Attack));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Clamp(Attack + (interaction.AltKey ? 1000 : 100), 0, MaxAttackSetting);
			AttackKnob.PlayKnobSound(num, increase: true, MaxAttackSetting);
			AttackKnob.SetKnob(num, MaxAttackSetting).Forget();
			if (GameManager.RunSimulation)
			{
				Attack = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button11:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalRelease, StringManager.Get(Release));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Clamp(Release - (interaction.AltKey ? 1000 : 100), 0, MaxAttackSetting);
			ReleaseKnob.PlayKnobSound(num, increase: false, MaxAttackSetting);
			ReleaseKnob.SetKnob(num, MaxAttackSetting).Forget();
			if (GameManager.RunSimulation)
			{
				Release = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button12:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalRelease, StringManager.Get(Release));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Clamp(Release + (interaction.AltKey ? 1000 : 100), 0, MaxAttackSetting);
			ReleaseKnob.PlayKnobSound(num, increase: true, MaxAttackSetting);
			ReleaseKnob.SetKnob(num, MaxAttackSetting).Forget();
			if (GameManager.RunSimulation)
			{
				Release = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button13:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalSpeed, ModeStrings[Mode]);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SpeedKnob.PlayKnobSound(Mode, increase: false, ModeStrings.Length - 1);
			SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractMode, Mathf.Max(Mode - 1, 0));
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button14:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalSpeed, ModeStrings[Mode]);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			SpeedKnob.PlayKnobSound(Mode, increase: true, ModeStrings.Length - 1);
			SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
			if (GameManager.RunSimulation)
			{
				OnServer.Interact(base.InteractMode, Mathf.Min(Mode + 1, ModeStrings.Length - 1));
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button15:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			empty = ((CurrentCartridge == null) ? InterfaceStrings.NoSoundCartridge : CurrentCartridge.ModeStrings[CurrentCartridge.Mode]);
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalCurrentInstrument, empty);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation && CurrentCartridge != null)
			{
				OnServer.Interact(CurrentCartridge.InteractMode, Mathf.Max(CurrentCartridge.Mode - 1, 0));
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button16:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			empty = ((CurrentCartridge == null) ? InterfaceStrings.NoSoundCartridge : CurrentCartridge.ModeStrings[CurrentCartridge.Mode]);
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalCurrentInstrument, empty);
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation && CurrentCartridge != null)
			{
				OnServer.Interact(CurrentCartridge.InteractMode, Mathf.Min(CurrentCartridge.Mode + 1, CurrentCartridge.ModeStrings.Length - 1));
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Activate:
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			if (GameManager.RunSimulation)
			{
				int state = (interactable.State = ((Activate != 1) ? 1 : 0));
				OnServer.Interact(interactable, state);
			}
			return delayedActionInstance.Succeed();
		case InteractableType.Button17:
		{
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			IAudioInput nextAudioInput = Logicable.GetNextAudioInput(this, AudioOutput, base.OutputNetwork1DevicesSorted, interaction.AltKey);
			if (nextAudioInput == null)
			{
				return delayedActionInstance.Fail(GameStrings.NoAudioReceiverDevices);
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextAudioInput.ToTooltip());
			if (!KeyManager.GetButton(KeyMap.QuantityModifier))
			{
				delayedActionInstance.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
			if (GameManager.RunSimulation)
			{
				AudioOutput = nextAudioInput;
			}
			return delayedActionInstance.Succeed();
		}
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	private DelayedActionInstance SetStepDevice(DelayedActionInstance result, Interaction interaction, int stepIndex, bool doAction)
	{
		if (!(interaction.SourceSlot.Occupant is Screwdriver))
		{
			return result.Fail(GameStrings.RequiresScrewdriver);
		}
		StepUnit nextValidReadable = Logicable.GetNextValidReadable(this, StepUnits[stepIndex], base.InputNetwork1DevicesSorted, interaction.AltKey);
		if (!nextValidReadable)
		{
			return result.Fail(GameStrings.LogicNoReadableDevices);
		}
		result.AppendStateMessage(GameStrings.GlobalChangeSettingTo, nextValidReadable.ToTooltip());
		if (!KeyManager.GetButton(KeyMap.QuantityModifier))
		{
			result.ExtendedMessage = InterfaceStrings.HoldForPreviousObject;
		}
		if (!doAction)
		{
			return result.Succeed();
		}
		PlayPooledAudioSound(Defines.Sounds.ScrewdriverSound, Vector3.zero);
		if (GameManager.RunSimulation)
		{
			if (StepUnits[stepIndex] != null)
			{
				StepUnits[stepIndex].OnPlayStepManual -= PlayStep;
			}
			StepUnits[stepIndex] = nextValidReadable;
			StepUnits[stepIndex].OnPlayStepManual += PlayStep;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 8192;
				switch (stepIndex)
				{
				case 0:
					_stepUnitUpdateFlags |= 1;
					break;
				case 1:
					_stepUnitUpdateFlags |= 2;
					break;
				case 2:
					_stepUnitUpdateFlags |= 4;
					break;
				case 3:
					_stepUnitUpdateFlags |= 8;
					break;
				case 4:
					_stepUnitUpdateFlags |= 16;
					break;
				case 5:
					_stepUnitUpdateFlags |= 32;
					break;
				case 6:
					_stepUnitUpdateFlags |= 64;
					break;
				case 7:
					_stepUnitUpdateFlags |= 128;
					break;
				}
			}
		}
		return result.Succeed();
	}

	public override string GetContextualName(Interactable interactable)
	{
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			if (!(StepUnits[0] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[0].DisplayName;
		case InteractableType.Button2:
			if (!(StepUnits[1] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[1].DisplayName;
		case InteractableType.Button3:
			if (!(StepUnits[2] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[2].DisplayName;
		case InteractableType.Button4:
			if (!(StepUnits[3] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[3].DisplayName;
		case InteractableType.Button5:
			if (!(StepUnits[4] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[4].DisplayName;
		case InteractableType.Button6:
			if (!(StepUnits[5] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[5].DisplayName;
		case InteractableType.Button7:
			if (!(StepUnits[6] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[6].DisplayName;
		case InteractableType.Button8:
			if (!(StepUnits[7] != null))
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return StepUnits[7].DisplayName;
		case InteractableType.Button9:
			return InterfaceStrings.Attack;
		case InteractableType.Button10:
			return InterfaceStrings.Attack;
		case InteractableType.Button11:
			return InterfaceStrings.Release;
		case InteractableType.Button12:
			return InterfaceStrings.Release;
		case InteractableType.Button17:
			if (AudioOutput == null)
			{
				return InterfaceStrings.LogicNoDevice;
			}
			return AudioOutput.DisplayName;
		default:
			return base.GetContextualName(interactable);
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		PassiveTooltip result = new PassiveTooltip(true);
		result.Title = DisplayName;
		foreach (Connection openEnd in OpenEnds)
		{
			if (hitCollider == openEnd.Collider && hitCollider != null)
			{
				return result.Populate(openEnd);
			}
		}
		string empty = string.Empty;
		int currentRealTimeStep = CurrentRealTimeStep;
		empty += $"{InterfaceStrings.LogicTypeTime} <color=green>{Setting:F}";
		empty += "\n";
		empty += $"<color=white>{InterfaceStrings.LogicTypeBpm} <color=green>{Bpm}";
		if (OnOff && Activate == 1 && AudioSettings.dspTime < _audioTimeOnSettingChanged + 1.0)
		{
			string text = ((StepUnits[currentRealTimeStep] != null) ? StepUnit.MidiNotePitchStrings[StepUnits[currentRealTimeStep].Mode] : InterfaceStrings.NoNote);
			empty += "\n";
			empty = empty + "<color=white>" + InterfaceStrings.Pitch + " <color=green>" + text;
		}
		result.State = empty;
		return result;
	}

	public override void OnChildEnterInventory(DynamicThing newChild)
	{
		if (newChild is SequencerCartridge currentCartridge)
		{
			CurrentCartridge = currentCartridge;
		}
		base.OnChildEnterInventory(newChild);
	}

	public override void OnChildExitInventory(DynamicThing previousChild)
	{
		CurrentCartridge = null;
		base.OnChildExitInventory(previousChild);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		StepUnit[] stepUnits = StepUnits;
		foreach (StepUnit stepUnit in stepUnits)
		{
			if (!(stepUnit == null))
			{
				stepUnit.OnPlayStepManual -= PlayStep;
			}
		}
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Bpm => true, 
			LogicType.Time => true, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		switch (logicType)
		{
		case LogicType.Bpm:
			Bpm = (int)Mathf.Clamp((float)value, 15f, 180f);
			_lastRenderedStep = CurrentStep;
			break;
		case LogicType.Time:
			Setting = value;
			break;
		}
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Bpm => true, 
			LogicType.Time => true, 
			_ => base.CanLogicRead(logicType), 
		};
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Bpm => Bpm, 
			LogicType.Time => Setting, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData = new StepSequencerSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (!(savedData is StepSequencerSaveData stepSequencerSaveData))
		{
			return;
		}
		stepSequencerSaveData.Attack = Attack;
		stepSequencerSaveData.Release = Release;
		stepSequencerSaveData.Bpm = Bpm;
		stepSequencerSaveData.OutputId = ((AudioOutput != null) ? AudioOutput.ReferenceId : 0);
		if (_savedInputIds == null)
		{
			_savedInputIds = new long[NumberOfSteps];
		}
		for (int i = 0; i < StepUnits.Length; i++)
		{
			if (StepUnits[i] != null)
			{
				_savedInputIds[i] = StepUnits[i].ReferenceId;
			}
			else
			{
				_savedInputIds[i] = 0L;
			}
		}
		stepSequencerSaveData.InputIds = _savedInputIds;
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is StepSequencerSaveData stepSequencerSaveData)
		{
			Attack = stepSequencerSaveData.Attack;
			Release = stepSequencerSaveData.Release;
			Bpm = stepSequencerSaveData.Bpm;
			_savedOutputId = stepSequencerSaveData.OutputId;
			_savedInputIds = stepSequencerSaveData.InputIds;
		}
	}

	public override void OnRegistered(Cell cell)
	{
		base.OnRegistered(cell);
		StepUnits = new StepUnit[NumberOfSteps];
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		AudioOutput = Thing.Find<IAudioInput>(_savedOutputId);
		SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
		AttackKnob.SetKnob(Attack, MaxAttackSetting).Forget();
		ReleaseKnob.SetKnob(Release, MaxAttackSetting).Forget();
		if (_savedInputIds == null)
		{
			return;
		}
		for (int i = 0; i < _savedInputIds.Length; i++)
		{
			StepUnit stepUnit = Thing.Find<StepUnit>(_savedInputIds[i]);
			if (stepUnit != null)
			{
				StepUnits[i] = stepUnit;
				StepUnits[i].OnPlayStepManual += PlayStep;
			}
		}
	}
}
