using Assets.Scripts.Atmospherics;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class CombustionDeepMiner : DeepMiner, IInternalCombustion, IReferencable, IEvaluable
{
	private static readonly VolumeLitres Volume = new VolumeLitres(2.0);

	[SerializeField]
	private Dial dial;

	[SerializeField]
	private Transform throttleLever;

	[SerializeField]
	private Transform combustionLever;

	private InternalCombustion _internalCombustion;

	private const float STRESS_WOBBLE_THRESHOLD = 40f;

	private const float WOBBLE_RANGE = 60f;

	private GameAudioEvent _gear2Audio;

	private GameAudioEvent _gear4Audio;

	private GameAudioEvent _stressAudio;

	private GameAudioEvent _maxStressAudio;

	private GameAudioEvent _motorAudio;

	private object _rpmStressLock = new object();

	protected override Slot ProgrammableChipSlot => Slots[1];

	public Transform ThrottleLever => throttleLever;

	public Transform CombustionLever => combustionLever;

	public override float Rpm => _internalCombustion.Rpm;

	public override float ShaftWobbleFactor => Mathf.Max(0.003f, GearsWobbleFactor);

	public override float GearsWobbleFactor => 0.02f * (Mathf.Max(0f, _internalCombustion.Stress - 40f) / 60f) * Mathf.Clamp01((Rpm - 20f) / 100f);

	protected override bool IsOperable
	{
		get
		{
			bool flag = (bool)base.ProgrammableChip && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = base.IsInputValid && base.IsOutputValid && !flag && CanMine() && base.IsStructureCompleted;
			if (Error == 0 && !flag2)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && flag2)
			{
				OnServer.Interact(base.InteractError, 0);
			}
			return flag2;
		}
	}

	public override bool HasReadableAtmosphere => true;

	public override void Awake()
	{
		base.Awake();
		if (!IsCursor)
		{
			_internalCombustion = new InternalCombustion(this);
		}
		dial?.Init();
		_gear1Audio = GetAudioEvent(Animator.StringToHash("GearTwo"));
		_gear3Audio = GetAudioEvent(Animator.StringToHash("GearFour"));
		_stressAudio = GetAudioEvent(Animator.StringToHash("Stress"));
		_maxStressAudio = GetAudioEvent(Animator.StringToHash("MaxStress"));
		_motorAudio = GetAudioEvent(Animator.StringToHash("MotorRunning"));
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded && _internalCombustion != null)
		{
			dial.UpdatePosition(_internalCombustion.Stress, Time.deltaTime);
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		HandleStressAnimSound(deltaTime);
		HandleMotorSound(deltaTime);
		if (!IsOccluded && base.IsStructureCompleted)
		{
			float pitch = RpmToAudioPitch();
			float volumeMultiplier = RpmToAudioVolume();
			_gear2Audio?.SetVolumeAndPitch(volumeMultiplier, pitch);
			_gear4Audio?.SetVolumeAndPitch(volumeMultiplier, pitch);
		}
	}

	private void HandleMotorSound(float deltaTime)
	{
		if (IsOccluded || Rpm < 2f || !base.IsStructureCompleted || !OnOff || !Powered)
		{
			_motorAudio.UpdatePlayState(shouldPlay: false);
			return;
		}
		float num = 0f;
		float num2 = 1f;
		num = ((!(Rpm < 30f)) ? RocketMath.MapToScale(50f, 800f, 0.5f, 1.1f, Rpm) : RocketMath.MapToScale(2f, 30f, 0f, 0.5f, Rpm));
		num2 = RocketMath.MapToScale(0f, 1000f, 0.25f, 2f, Rpm);
		if (!_motorAudio.IsPlaying)
		{
			_motorAudio.Trigger(num, num2);
		}
		_motorAudio.LerpVolumeAndPitch(num, num2, deltaTime);
	}

	private void HandleStressAnimSound(float deltaTime)
	{
		if (IsOccluded || !base.IsStructureCompleted)
		{
			_stressAudio.UpdatePlayState(shouldPlay: false);
			_maxStressAudio.UpdatePlayState(shouldPlay: false);
			return;
		}
		float num = 0f;
		float num2 = 1f;
		if (_internalCombustion.Stress > 40f)
		{
			float t = Mathf.Clamp01((_internalCombustion.Stress - 50f) / 50f);
			num = Mathf.Lerp(0.3f, 1f, t);
			num2 = Mathf.Lerp(0.75f, 1.5f, Mathf.Clamp01(_internalCombustion.Rpm / 600f));
			if (_internalCombustion.Rpm < 10f)
			{
				num = 0f;
			}
			if (!_stressAudio.IsPlaying)
			{
				_stressAudio.Trigger(num, num2);
			}
			_stressAudio.LerpVolumeAndPitch(num, num2, deltaTime);
		}
		else if (_stressAudio.IsPlaying)
		{
			_stressAudio.Stop();
		}
		if (_internalCombustion.Stress < 90f && _maxStressAudio.IsPlaying)
		{
			_maxStressAudio.Stop();
		}
		else if (_internalCombustion.Stress > 95f && !_maxStressAudio.IsPlaying)
		{
			_maxStressAudio.Trigger();
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	public override void AssessError()
	{
		if (GameManager.RunSimulation)
		{
			bool flag = (bool)base.ProgrammableChip && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = base.IsInputValid && base.IsOutputValid && !flag && CanMine() && base.IsStructureCompleted;
			if (Error == 0 && !flag2)
			{
				OnServer.Interact(base.InteractError, 1);
			}
			else if (Error == 1 && flag2)
			{
				OnServer.Interact(base.InteractError, 0);
			}
		}
	}

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		if (_internalCombustion == null)
		{
			return;
		}
		lock (_rpmStressLock)
		{
			if (!IsOperable)
			{
				_internalCombustion.HandleShutDown();
				return;
			}
			if (!base.IsStructureCompleted)
			{
				_internalCombustion.Rpm = 0f;
				return;
			}
			_internalCombustion.ManualCombust();
			_internalCombustion.SpeedTick();
			if (!OnOff || !Powered || Error == 1)
			{
				_internalCombustion.HandleShutDown();
			}
			_internalCombustion.HandleGasOutput(base.PressurePerTick, InputNetwork, OutputNetwork);
			_internalCombustion.HandleGasInput(InputNetwork);
			if (OnOff && Powered)
			{
				base.InternalAtmosphere.Sparked = true;
			}
		}
	}

	public override PassiveTooltip GetPassiveTooltip(Collider hitCollider)
	{
		if (hitCollider == dial.Collider)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			if (_internalCombustion.Stress < 50f)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + _internalCombustion.Stress.ToStringPercent("green"));
			}
			else if (_internalCombustion.Stress < 80f)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + _internalCombustion.Stress.ToStringPercent("yellow"));
			}
			else
			{
				Tooltip.ToolTipStringBuilder.AppendLine(InterfaceStrings.Stress + " " + _internalCombustion.Stress.ToStringPercent("red"));
			}
			Tooltip.ToolTipStringBuilder.AppendLine(_internalCombustion.Rpm.ToStringPrefix("RPM", "yellow"));
			AtmosphericsManager.DisplayBasicAtmosphere(base.InternalAtmosphere, Tooltip.ToolTipStringBuilder);
			result.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result;
		}
		return base.GetPassiveTooltip(hitCollider);
	}

	public override DelayedActionInstance InteractWith(Interactable interactable, Interaction interaction, bool doAction = true)
	{
		DelayedActionInstance delayedActionInstance = new DelayedActionInstance
		{
			Duration = 0f,
			ActionMessage = interactable.ContextualName
		};
		float num = 0f;
		switch (interactable.Action)
		{
		case InteractableType.Button4:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalThrottle, StringManager.Get(_internalCombustion.Throttle));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Round(Mathf.Max(_internalCombustion.Throttle - 10f, 0f));
			if (GameManager.RunSimulation)
			{
				_internalCombustion.Throttle = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button5:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalThrottle, StringManager.Get(_internalCombustion.Throttle));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Round(Mathf.Min(_internalCombustion.Throttle + 10f, 100f));
			if (GameManager.RunSimulation)
			{
				_internalCombustion.Throttle = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button6:
			delayedActionInstance.ActionMessage = GameStrings.GlobalDecrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalCombustionLimiter, StringManager.Get(_internalCombustion.CombustionLimiter));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Round(Mathf.Max(_internalCombustion.CombustionLimiter - 10f, 0f));
			if (GameManager.RunSimulation)
			{
				_internalCombustion.CombustionLimiter = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		case InteractableType.Button7:
			delayedActionInstance.ActionMessage = GameStrings.GlobalIncrease.AsString();
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalCombustionLimiter, StringManager.Get(_internalCombustion.CombustionLimiter));
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			num = Mathf.Round(Mathf.Min(_internalCombustion.CombustionLimiter + 10f, 100f));
			if (GameManager.RunSimulation)
			{
				_internalCombustion.CombustionLimiter = num;
			}
			return DelayedActionInstance.Success(interactable.ContextualName);
		default:
			return base.InteractWith(interactable, interaction, doAction);
		}
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new CombustionDeepMinerSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CombustionDeepMinerSaveData combustionDeepMinerSaveData && _internalCombustion != null)
		{
			combustionDeepMinerSaveData.Throttle = _internalCombustion.Throttle;
			combustionDeepMinerSaveData.CombustionLimiter = _internalCombustion.CombustionLimiter;
			combustionDeepMinerSaveData.Rpm = _internalCombustion.Rpm;
			combustionDeepMinerSaveData.Stress = _internalCombustion.Stress;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is CombustionDeepMinerSaveData combustionDeepMinerSaveData)
		{
			_internalCombustion.Throttle = combustionDeepMinerSaveData.Throttle;
			_internalCombustion.CombustionLimiter = combustionDeepMinerSaveData.CombustionLimiter;
			_internalCombustion.Rpm = combustionDeepMinerSaveData.Rpm;
			_internalCombustion.Stress = combustionDeepMinerSaveData.Stress;
		}
	}

	public override void BuildUpdate(RocketBinaryWriter writer, ushort networkUpdateType)
	{
		base.BuildUpdate(writer, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			writer.WriteSingle(_internalCombustion.Throttle);
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			writer.WriteSingle(_internalCombustion.CombustionLimiter);
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			writer.WriteSingle(_internalCombustion.Rpm);
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			writer.WriteSingle(_internalCombustion.Stress);
		}
	}

	public override void ProcessUpdate(RocketBinaryReader reader, ushort networkUpdateType)
	{
		base.ProcessUpdate(reader, networkUpdateType);
		if (Thing.IsNetworkUpdateRequired(4096u, networkUpdateType))
		{
			_internalCombustion.Throttle = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(8192u, networkUpdateType))
		{
			_internalCombustion.CombustionLimiter = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(16384u, networkUpdateType))
		{
			_internalCombustion.Rpm = reader.ReadSingle();
		}
		if (Thing.IsNetworkUpdateRequired(32768u, networkUpdateType))
		{
			_internalCombustion.Stress = reader.ReadSingle();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(_internalCombustion.Throttle);
		writer.WriteSingle(_internalCombustion.CombustionLimiter);
		writer.WriteSingle(_internalCombustion.Rpm);
		writer.WriteSingle(_internalCombustion.Stress);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_internalCombustion.Throttle = reader.ReadSingle();
		_internalCombustion.CombustionLimiter = reader.ReadSingle();
		_internalCombustion.Rpm = reader.ReadSingle();
		_internalCombustion.Stress = reader.ReadSingle();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		switch (logicType)
		{
		case LogicType.CombustionLimiter:
			return true;
		case LogicType.Throttle:
			return true;
		case LogicType.Rpm:
			return true;
		case LogicType.Stress:
			return true;
		case LogicType.Setting:
		case LogicType.Maximum:
		case LogicType.Ratio:
			return false;
		default:
			return base.CanLogicRead(logicType);
		}
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.CombustionLimiter => _internalCombustion.CombustionLimiter, 
			LogicType.Throttle => _internalCombustion.Throttle, 
			LogicType.Rpm => _internalCombustion.Rpm, 
			LogicType.Stress => _internalCombustion.Stress, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.CombustionLimiter => true, 
			LogicType.Throttle => true, 
			LogicType.Setting => false, 
			_ => base.CanLogicWrite(logicType), 
		};
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		switch (logicType)
		{
		case LogicType.CombustionLimiter:
			_internalCombustion.CombustionLimiter = (float)value;
			break;
		case LogicType.Throttle:
			_internalCombustion.Throttle = (float)value;
			break;
		}
		base.SetLogicValue(logicType, value);
	}
}
