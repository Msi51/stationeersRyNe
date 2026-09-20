using System;
using System.Collections.Generic;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Objects.Electrical;
using Sound;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LfoVolume : LogicUnitBase, IAudioInput, ILogicable, IReferencable, IEvaluable
{
	public Knob IntensityKnob;

	public Knob SpeedKnob;

	public Knob WaveformKnob;

	public AnimationCurve SquareWave;

	public AnimationCurve SawWave;

	public AnimationCurve TriangleWave;

	public AnimationCurve ReverseSawWave;

	public AnimationCurve SineWave;

	private float _lfoVolume;

	private double _targetAudioTime;

	private double _audioTimeOnSettingChanged;

	private int _intensity = 100;

	private int _bpm = 60;

	private int _waveform;

	private readonly List<PooledAudioSource> _inputAudioSources = new List<PooledAudioSource>();

	private static readonly int MaxIntensity = 100;

	private IAudioInput _audioOutput;

	private static readonly int MaxWaveforms = 4;

	private IAudioInput _nextAudioOutput;

	private long _savedOutputId;

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
			if (!GameManager.IsBatchMode && setting > Setting)
			{
				_audioTimeOnSettingChanged = AudioSequencer.BufferedDspTime;
			}
			else if (!GameManager.IsBatchMode)
			{
				double bufferedDspTime = AudioSequencer.BufferedDspTime;
				double num = Setting - setting;
				double num2 = bufferedDspTime - _targetAudioTime;
				double num3 = _audioTimeOnSettingChanged + num;
				if (Math.Abs(num - num2) < (double)AudioSequencer.DriftTolerance && Math.Abs(num3 - bufferedDspTime) < (double)AudioSequencer.DriftTolerance)
				{
					_audioTimeOnSettingChanged = num3;
				}
				else
				{
					_audioTimeOnSettingChanged = AudioSequencer.BufferedDspTime;
				}
				_targetAudioTime = bufferedDspTime;
			}
		}
	}

	private double RealTimePlayHeadPosition => (double)((float)Bpm / 60f * 4f) * (Setting + AudioSequencer.BufferedDspTime - _audioTimeOnSettingChanged);

	[ByteArraySync]
	private int Intensity
	{
		get
		{
			return _intensity;
		}
		set
		{
			_intensity = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 16384;
			}
			IntensityKnob.SetKnob(Intensity, MaxIntensity).Forget();
		}
	}

	[ByteArraySync]
	private int Bpm
	{
		get
		{
			return _bpm;
		}
		set
		{
			_bpm = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 1024;
			}
			SpeedKnob.SetKnob(Mode, ModeStrings.Length - 1).Forget();
		}
	}

	[ByteArraySync]
	private int Waveform
	{
		get
		{
			return _waveform;
		}
		set
		{
			_waveform = value;
			if (NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 32768;
			}
			WaveformKnob.SetKnob(Waveform, MaxWaveforms).Forget();
		}
	}

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

	public override string[] ModeStrings => AudioSequencer.SpeedModeStrings;

	protected override bool IsOperable
	{
		get
		{
			if (AudioOutput == null || !base.InputNetwork1DevicesSorted.Contains(AudioOutput))
			{
				if (Error == 0)
				{
					OnServer.Interact(base.InteractError, 1);
				}
				return false;
			}
			for (_nextAudioOutput = AudioOutput; _nextAudioOutput != null; _nextAudioOutput = _nextAudioOutput.AudioOutput)
			{
				if (_nextAudioOutput.GetAsThing.ReferenceId == base.ReferenceId)
				{
					if (Error == 0)
					{
						OnServer.Interact(base.InteractError, 1);
					}
					_nextAudioOutput = null;
					return false;
				}
			}
			if (Error == 1)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return true;
		}
	}

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			SpeedKnob.Initialize(this);
			IntensityKnob.Initialize(this);
			WaveformKnob.Initialize(this);
			SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
			IntensityKnob.SetKnob(Intensity, MaxIntensity).Forget();
			WaveformKnob.SetKnob(Waveform, MaxWaveforms).Forget();
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteByte((byte)Intensity);
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			writer.WriteByte((byte)Bpm);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteByte((byte)Waveform);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteInt64(AudioOutput?.ReferenceId ?? 0);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			Intensity = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(1024u, networkUpdateType))
		{
			Bpm = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			Waveform = reader.ReadByte();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			long num = reader.ReadInt64();
			if (num != 0L)
			{
				AudioOutput = Referencable.Find<IAudioInput>(num);
			}
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteByte((byte)Intensity);
		writer.WriteByte((byte)Bpm);
		writer.WriteByte((byte)Waveform);
		writer.WriteInt64(AudioOutput?.ReferenceId ?? 0);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		Intensity = reader.ReadByte();
		Bpm = reader.ReadByte();
		Waveform = reader.ReadByte();
		_savedOutputId = reader.ReadInt64();
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (GameManager.IsBatchMode)
		{
			return;
		}
		if (!OnOff || !Powered || Error == 1 || AudioSettings.dspTime > _audioTimeOnSettingChanged + 2.0)
		{
			if (_inputAudioSources.Count > 0)
			{
				_inputAudioSources.Clear();
			}
			return;
		}
		double dspTime = AudioSettings.dspTime;
		_lfoVolume = GetLfoVolume();
		for (int num = _inputAudioSources.Count - 1; num >= 0; num--)
		{
			if (_inputAudioSources[num]?.GameAudioSource == null || !_inputAudioSources[num].GameAudioSource.isPlaying || _inputAudioSources[num].GameAudioSource.ScheduledEndTime < dspTime)
			{
				_inputAudioSources.RemoveAt(num);
			}
			else
			{
				_inputAudioSources[num].GameAudioSource.SetEffectVolumeMultiplier(_lfoVolume);
			}
		}
	}

	public override void OnPowerTick()
	{
		base.OnPowerTick();
		_ = IsOperable;
	}

	public PooledAudioSource InputAudioScheduled(int clipsDataHash, double startTime, double endTime, float volumeMultiplier = 1f, float pitchMultiplier = 1f, int attack = 0, int release = 0)
	{
		PooledAudioSource pooledAudioSource = AudioOutput.InputAudioScheduled(clipsDataHash, startTime, endTime, volumeMultiplier, pitchMultiplier, attack, release);
		if (!_inputAudioSources.Contains(pooledAudioSource))
		{
			_inputAudioSources.Add(pooledAudioSource);
		}
		return pooledAudioSource;
	}

	public PooledAudioSource InputAudio(int clipsDataHash, float volumeMultiplier = 1f, float pitchMultiplier = 1f)
	{
		return AudioOutput.InputAudioScheduled(clipsDataHash, volumeMultiplier, pitchMultiplier);
	}

	public float GetLfoVolume()
	{
		return Waveform switch
		{
			0 => AssessCurve(SquareWave), 
			1 => AssessCurve(SawWave), 
			2 => AssessCurve(TriangleWave), 
			3 => AssessCurve(ReverseSawWave), 
			4 => AssessCurve(SineWave), 
			_ => AssessCurve(SineWave), 
		};
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

	private float AssessCurve(AnimationCurve curve)
	{
		if (Bpm == 0)
		{
			return 0f;
		}
		int num = NoteLengthMultiplier();
		float t = curve.Evaluate(Mathf.Clamp01((float)(RealTimePlayHeadPosition % (double)num) / (float)num));
		return Mathf.Lerp(1f - Mathf.Clamp01((float)Intensity / (float)MaxIntensity), 1f, t);
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		if (interactable == base.InteractMode)
		{
			SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
		}
		base.OnInteractableUpdated(interactable);
	}

	public override string GetContextualName(Interactable interactable)
	{
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			return InterfaceStrings.Intensity;
		case InteractableType.Button2:
			return InterfaceStrings.Intensity;
		case InteractableType.Button5:
			return InterfaceStrings.Waveform;
		case InteractableType.Button6:
			return InterfaceStrings.Waveform;
		case InteractableType.Button7:
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
		empty += $"{InterfaceStrings.LogicTypeTime} <color=green>{Setting:F}";
		empty += "\n";
		empty += $"<color=white>{InterfaceStrings.LogicTypeBpm} <color=green>{Bpm}";
		empty += "\n";
		empty += $"<color=white>{InterfaceStrings.Volume} <color=green>{_lfoVolume:F}";
		result.State = empty;
		return result;
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
		switch (interactable.Action)
		{
		case InteractableType.Button1:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalIntensity, StringManager.Get(Intensity));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Max(Intensity - ((!interaction.AltKey) ? 1 : 10), 0);
			IntensityKnob.PlayKnobSound(num, increase: false, MaxIntensity);
			IntensityKnob.SetKnob(num, MaxIntensity).Forget();
			if (GameManager.RunSimulation)
			{
				Intensity = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button2:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalIntensity, StringManager.Get(Intensity));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Min(Intensity + ((!interaction.AltKey) ? 1 : 10), MaxIntensity);
			IntensityKnob.PlayKnobSound(num, increase: true, MaxIntensity);
			IntensityKnob.SetKnob(num, MaxIntensity).Forget();
			if (GameManager.RunSimulation)
			{
				Intensity = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button3:
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
		case InteractableType.Button4:
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
		case InteractableType.Button5:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalWaveForm, StringManager.Get(Waveform));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Max(Waveform - 1, 0);
			WaveformKnob.PlayKnobSound(num, increase: false, MaxWaveforms);
			WaveformKnob.SetKnob(num, MaxWaveforms).Forget();
			if (GameManager.RunSimulation)
			{
				Waveform = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button6:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalWaveForm, StringManager.Get(Waveform));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Min(Waveform + 1, MaxWaveforms);
			WaveformKnob.PlayKnobSound(num, increase: true, MaxWaveforms);
			WaveformKnob.SetKnob(num, MaxWaveforms).Forget();
			if (GameManager.RunSimulation)
			{
				Waveform = num;
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
		case InteractableType.Button7:
		{
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			IAudioInput nextAudioInput = Logicable.GetNextAudioInput(this, AudioOutput, base.InputNetwork1DevicesSorted, interaction.AltKey);
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
		ThingSaveData savedData = new LfoVolumeSaveData();
		InitialiseSaveData(ref savedData);
		return savedData;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is LfoVolumeSaveData lfoVolumeSaveData)
		{
			lfoVolumeSaveData.Bpm = Bpm;
			lfoVolumeSaveData.OutputId = AudioOutput?.ReferenceId ?? 0;
			lfoVolumeSaveData.Intensity = Intensity;
			lfoVolumeSaveData.Waveform = Waveform;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is LfoVolumeSaveData lfoVolumeSaveData)
		{
			Bpm = lfoVolumeSaveData.Bpm;
			_savedOutputId = lfoVolumeSaveData.OutputId;
			Intensity = lfoVolumeSaveData.Intensity;
			Waveform = lfoVolumeSaveData.Waveform;
		}
		SpeedKnob.SetKnob(base.InteractMode.State, ModeStrings.Length - 1).Forget();
		IntensityKnob.SetKnob(Intensity, MaxIntensity).Forget();
		WaveformKnob.SetKnob(Waveform, MaxWaveforms).Forget();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		AudioOutput = Referencable.Find<Thing>(_savedOutputId) as IAudioInput;
	}
}
