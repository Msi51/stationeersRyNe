using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Sound;
using Assets.Scripts.UI;
using Assets.Scripts.Util;
using Reagents;
using Trading;
using UnityEngine;

namespace Assets.Scripts.Objects.Pipes;

public class CombustionCentrifuge : DeviceInputOutputImportExportCircuit, IThermal, IInternalCombustion, IReferencable, IEvaluable
{
	[SerializeField]
	private Dial dial;

	[SerializeField]
	private Transform drum;

	[SerializeField]
	private Transform throttleLever;

	[SerializeField]
	private Transform combustionLever;

	[SerializeField]
	private Transform openLever;

	[SerializeField]
	private Ore reagentMixPrefab;

	private static readonly VolumeLitres Volume = new VolumeLitres(1.0);

	private byte _processing;

	private InternalCombustion _internalCombustion;

	private Vector3 _drumRestPosition;

	public float wobbleFactor = 0.02f;

	public float wobbleSpeed = 10f;

	private float _wobbleTime;

	private const float STRESS_WOBBLE_THRESHOLD = 40f;

	private const float WOBBLE_RANGE = 60f;

	private const float Y_WOBBLE = 1.1f;

	private const float Z_WOBBLE = 1.2f;

	private const float AUDIBLE_SQAURE_DISTANCE = 900f;

	private GameAudioEvent _idleAudio;

	private GameAudioEvent _runningAudio;

	private GameAudioEvent _stressAudio;

	private GameAudioEvent _maxStressAudio;

	private GameAudioEvent _60RpmAudio;

	private GameAudioEvent _480RpmAudio;

	private GameAudioEvent _drumSweetenAudio;

	private static readonly float IdleVolumeThrottleCutOff = 30f;

	private static readonly float RunningVolumeThrottleCutOff = 40f;

	private static readonly float RunningPitchLower = 0.5f;

	private static readonly float RunningPitchUpper = 1.5f;

	private static readonly float IdlePitchUpper = 1.5f;

	private static readonly float CombustionPitchLower = 0.8f;

	private static readonly float CombustionPitchUpper = 1f;

	private static readonly float CombustionVolumeUpper = 1f;

	private object _rpmStressLock = new object();

	public const int MAX_REAGENTS = 3000;

	private float _currentProgress;

	public Transform ThrottleLever => throttleLever;

	public Transform CombustionLever => combustionLever;

	public override bool HasReadableReagentMixture => true;

	public byte Processing
	{
		get
		{
			return _processing;
		}
		set
		{
			if (value != Processing && NetworkManager.IsServer)
			{
				base.NetworkUpdateFlags |= 512;
			}
			_processing = value;
		}
	}

	public override bool CanCompleteImport
	{
		get
		{
			if (base.CanCompleteImport)
			{
				return ReagentMixture.TotalReagents <= 0.0;
			}
			return false;
		}
	}

	public override bool CanBeginImport
	{
		get
		{
			if (OnOff && Powered && base.CanBeginImport)
			{
				return ImportingThing is ICentrifugable;
			}
			return false;
		}
	}

	private bool IsProcessedReady
	{
		get
		{
			if (ImportingThing is ICentrifugable importingThing)
			{
				return _currentProgress >= Centrifuge.ProgressRequired(importingThing);
			}
			return false;
		}
	}

	private bool IsProcessedFinished => (object)ImportingThing == null;

	private bool CanProcess => ImportingThing is ICentrifugable;

	protected override bool IsOperable
	{
		get
		{
			bool flag = base.ProgrammableChip != null && (CodeErrorState != 0 || base.ProgrammableChip.CompilationError);
			bool flag2 = base.IsInputValid && base.IsOutputValid && !flag && ReagentMixture.TotalReagents < 3000.0;
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
		ReagentMixture = new ReagentMixture(this);
		dial.Init();
		_drumRestPosition = drum.localPosition;
		_runningAudio = GetAudioEvent(Animator.StringToHash("MotorRunning"));
		_idleAudio = GetAudioEvent(Animator.StringToHash("MotorIdle"));
		_stressAudio = GetAudioEvent(Animator.StringToHash("DrumStress"));
		_60RpmAudio = GetAudioEvent(Animator.StringToHash("Drum60Rpm"));
		_480RpmAudio = GetAudioEvent(Animator.StringToHash("Drum480Rpm"));
		_drumSweetenAudio = GetAudioEvent(Animator.StringToHash("DrumSweeten"));
		_maxStressAudio = GetAudioEvent(Animator.StringToHash("DrumMaxStress"));
		if (!IsCursor)
		{
			_internalCombustion = new InternalCombustion(this);
		}
	}

	public override void InitInternalAtmosphere()
	{
		if (base.InternalAtmosphere == null)
		{
			base.InternalAtmosphere = new Atmosphere(this, Volume, 0L);
		}
	}

	protected override void RefreshAnimState(bool skipAnimation = false)
	{
		base.RefreshAnimState(skipAnimation);
	}

	public override void UpdateEachFrame()
	{
		base.UpdateEachFrame();
		if (!IsOccluded && _internalCombustion != null)
		{
			dial.UpdatePosition(_internalCombustion.Stress, Time.deltaTime);
			if (_internalCombustion.Rpm > 0.1f)
			{
				drum.Rotate(Vector3.right, 360f * GameManager.DeltaTime * _internalCombustion.Rpm / 60f);
			}
			HandleStressAnim();
		}
	}

	public override void OnAnimationStart()
	{
		if (Button3 == 1)
		{
			SetContentsVisibility(isVisible: true);
		}
	}

	public override void OnAnimationStop()
	{
		if (Button3 == 0)
		{
			SetContentsVisibility(isVisible: false);
		}
		if (Button3 == 1)
		{
			SetContentsVisibility(isVisible: true);
		}
	}

	public override string GetContextualName(Interactable interactable)
	{
		if (interactable.Action == InteractableType.Button3)
		{
			if (Button3 != 1)
			{
				return ActionStrings.Open;
			}
			return ActionStrings.Close;
		}
		return base.GetContextualName(interactable);
	}

	private void HandleStressAnim()
	{
		if (_internalCombustion.Stress > 40f)
		{
			float num = wobbleFactor * ((_internalCombustion.Stress - 40f) / 60f) * Mathf.Clamp01((_internalCombustion.Rpm - 20f) / 100f);
			float y = num * Mathf.Sin(_wobbleTime * wobbleSpeed * 1.1f);
			float z = num * Mathf.Sin(_wobbleTime * wobbleSpeed * 1.2f);
			_wobbleTime += GameManager.DeltaTime;
			drum.localPosition = _drumRestPosition + new Vector3(0f, y, z);
		}
		else
		{
			drum.localPosition = _drumRestPosition;
			_wobbleTime = 0f;
		}
	}

	public override void UpdateAudio(float deltaTime)
	{
		base.UpdateAudio(deltaTime);
		bool outOfRange = IsOccluded || Vector3.SqrMagnitude(base.Position - InventoryManager.ParentPosition) > 900f;
		HandleMotorSound(deltaTime, outOfRange);
		HandleDrumSound(deltaTime, outOfRange);
		HandleStressAnimSound(deltaTime, outOfRange);
	}

	private void HandleStressAnimSound(float deltaTime, bool outOfRange)
	{
		if (outOfRange)
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

	private void HandleMotorSound(float deltaTime, bool outOfRange)
	{
		if (GameManager.GameState != GameState.Running)
		{
			return;
		}
		if (!OnOff || !Powered || Error == 1 || outOfRange)
		{
			_idleAudio.UpdatePlayState(shouldPlay: false);
			_runningAudio.UpdatePlayState(shouldPlay: false);
			return;
		}
		float num = Mathf.Lerp(1f, 0f, Mathf.Clamp01(_internalCombustion.Throttle / IdleVolumeThrottleCutOff));
		float targetPitchMultiplier = Mathf.Lerp(1f, IdlePitchUpper, Mathf.Clamp01(_internalCombustion.Throttle / IdleVolumeThrottleCutOff));
		float num2 = Mathf.Lerp(0f, 1f, Mathf.Clamp01((_internalCombustion.Throttle - 9f) / RunningVolumeThrottleCutOff));
		float targetPitchMultiplier2 = Mathf.Lerp(RunningPitchLower, RunningPitchUpper, Mathf.Clamp01(_internalCombustion.Throttle / 100f));
		float num3 = 1f;
		if (_internalCombustion.Stress > 99f)
		{
			targetPitchMultiplier2 = 0.25f * Mathf.Sin(_wobbleTime * 10f) + 2.25f;
			num3 = 10f;
		}
		if (!_internalCombustion.DidCombustionLastTick)
		{
			num = 0f;
			num2 = 0f;
		}
		bool flag = num > 0f;
		bool flag2 = num2 > 0f;
		_idleAudio.UpdatePlayState(flag);
		_runningAudio.UpdatePlayState(flag2);
		if (flag)
		{
			_idleAudio.LerpVolumeAndPitch(num, targetPitchMultiplier, deltaTime);
		}
		if (flag2)
		{
			_runningAudio.LerpVolumeAndPitch(num2, targetPitchMultiplier2, deltaTime * num3);
		}
	}

	private void HandleDrumSound(float deltaTime, bool outOfRange)
	{
		if (outOfRange)
		{
			_60RpmAudio.UpdatePlayState(shouldPlay: false);
			_480RpmAudio.UpdatePlayState(shouldPlay: false);
			_drumSweetenAudio.UpdatePlayState(shouldPlay: false);
			return;
		}
		float rpm = _internalCombustion.Rpm;
		float num = Mathf.Lerp(0.7f, 0f, Mathf.Clamp01((rpm - 60f) / 120f));
		if (rpm < 1f)
		{
			num = 0f;
		}
		float num2 = Mathf.Lerp(0f, 0.8f, Mathf.Clamp01((rpm - 60f) / 120f));
		float targetPitchMultiplier = Mathf.Clamp(rpm / 60f, 0f, 3f);
		float targetPitchMultiplier2 = Mathf.Clamp(rpm / 480f, 0f, 3f);
		float num3 = Mathf.Lerp(0.2f, 0.5f, rpm / 120f);
		float targetPitchMultiplier3 = Mathf.Lerp(0.5f, 1.2f, rpm / 180f);
		if (rpm < 2f)
		{
			num3 = 0f;
		}
		bool flag = num > 0f;
		bool flag2 = num2 > 0f;
		bool flag3 = num3 > 0f;
		_60RpmAudio.UpdatePlayState(flag);
		_480RpmAudio.UpdatePlayState(flag2);
		_drumSweetenAudio.UpdatePlayState(flag3);
		if (flag)
		{
			_60RpmAudio.LerpVolumeAndPitch(num, targetPitchMultiplier, deltaTime);
		}
		if (flag2)
		{
			_480RpmAudio.LerpVolumeAndPitch(num2, targetPitchMultiplier2, deltaTime);
		}
		if (flag3)
		{
			_drumSweetenAudio.LerpVolumeAndPitch(num3, targetPitchMultiplier3, deltaTime);
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		if (interactable == base.InteractOpen)
		{
			PlayPooledAudioSound(IsOpen ? Defines.Sounds.LeverDown : Defines.Sounds.LeverUp, openLever.localPosition);
		}
	}

	protected override void OnServerImportTick()
	{
		if (!base.IsStructureCompleted)
		{
			_currentProgress = 0f;
			return;
		}
		if (IsNextImportReady)
		{
			TryChuteImport();
		}
		if (CanBeginImport)
		{
			OnServer.Interact(base.InteractImport, 1);
		}
		if ((object)ImportingThing == null)
		{
			_currentProgress = 0f;
		}
		if (base.IsImportClosed && ImportingThing != null && IsProcessedReady)
		{
			float currentProgress = _currentProgress - Centrifuge.ProgressRequired(ImportingThing as ICentrifugable);
			CollectResource(ImportingThing as ICentrifugable);
			_currentProgress = currentProgress;
		}
		if (!OnOff)
		{
			OnServer.Interact(base.InteractImport, 0);
			_currentProgress = 0f;
		}
	}

	protected override void OnServerExportTick(float deltaTime)
	{
		if (!base.IsStructureCompleted)
		{
			return;
		}
		if (IsNextExportReady && IsOpen && ReagentMixture.TotalReagents > 0.0)
		{
			Ore prefabForNextReagent = GetPrefabForNextReagent();
			ReagentMixture reagentMixture = new ReagentMixture();
			int quantity = ReagentMixture.AddNextReagent(reagentMixture, prefabForNextReagent.MaxQuantity);
			Ore ore = Centrifuge.CreateOutput(prefabForNextReagent, quantity, ExportSlot);
			if ((object)ore != null)
			{
				ore.CreatedReagentMixture = reagentMixture;
				ExportingThing = ore;
			}
			float num = Mathf.Max(_internalCombustion.Rpm / 10f - 1f, 0f);
			lock (_rpmStressLock)
			{
				_internalCombustion.Stress += num;
				_internalCombustion.Rpm -= Mathf.Min(_internalCombustion.Rpm / 10f, _internalCombustion.Rpm);
				return;
			}
		}
		if (CanBeginExport)
		{
			OnServer.Interact(base.InteractExport, 1);
		}
		if (base.IsImportClosed && (object)ImportingThing == null)
		{
			OnServer.Interact(base.InteractImport, 0);
		}
	}

	public Ore GetPrefabForNextReagent()
	{
		ReagentMixture nextMix = ReagentMixture.GetNextMix();
		Centrifuge.RecipeComparable.AllRecipes.TryGetValue(new Recipe(nextMix, null), out var value);
		if ((object)value == null)
		{
			return reagentMixPrefab;
		}
		return value;
	}

	public void CollectResource(ICentrifugable input)
	{
		ReagentMixture.Add(input.CentrifugeProcessUnit());
		if (!(input is Stackable))
		{
			OnServer.Destroy(input as Thing);
		}
	}

	public override void OnImportClosingComplete()
	{
		base.OnImportClosingComplete();
		if (GameManager.RunSimulation && !(ImportingThing is ICentrifugable))
		{
			OnServer.Interact(base.InteractImport, 0);
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
			if (Error == 1)
			{
				ReagentMixture reagentMixture = ReagentMixture;
				if (reagentMixture != null && reagentMixture.TotalReagents >= 3000.0)
				{
					Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.CentrifugeFull.AsString(ToTooltip()));
				}
			}
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
		if (hitCollider == _infoScreen?.InfoTrigger)
		{
			PassiveTooltip passiveTooltip = new PassiveTooltip(true);
			passiveTooltip.Title = DisplayName;
			PassiveTooltip result2 = passiveTooltip;
			Tooltip.ToolTipStringBuilder.Clear();
			if (Error == 1 && ReagentMixture.TotalReagents >= 3000.0)
			{
				Tooltip.ToolTipStringBuilder.AppendLine(GameStrings.CentrifugeFull.AsString(ToTooltip()));
			}
			Tooltip.ToolTipStringBuilder.AppendLine(_internalCombustion.Rpm.ToStringPrefix("RPM", "yellow"));
			if ((bool)ImportingThing)
			{
				Tooltip.SetProcessingText(ImportingThing, (int)Processing);
			}
			string value = ReagentMixture.ToString();
			Tooltip.ToolTipStringBuilder.AppendLine(value);
			result2.Title = Localization.GetInterface("Contents");
			result2.Extended = Tooltip.ToolTipStringBuilder.ToString();
			return result2;
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

	public override void OnAtmosphericTick()
	{
		base.OnAtmosphericTick();
		lock (_rpmStressLock)
		{
			HandleBrokenMix();
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
			if (CanProcess)
			{
				ProgressProcessing();
			}
			_internalCombustion.HandleGasOutput(base.PressurePerTick, InputNetwork, OutputNetwork);
			_internalCombustion.HandleGasInput(InputNetwork);
			if (OnOff && Powered)
			{
				base.InternalAtmosphere.Sparked = true;
			}
		}
	}

	public void HandleBrokenMix()
	{
		if (IsBroken)
		{
			Atmosphere atmosphere = base.AtmosphericsController.SampleGlobalAtmosphere(base.WorldGrid);
			if (base.InternalAtmosphere.PressureGassesAndLiquids < Chemistry.ResetThreshold && atmosphere == null)
			{
				base.InternalAtmosphere.GasMixture.Reset();
				return;
			}
			Atmosphere outputAtmos = base.AtmosphericsController.CloneGlobalAtmosphere(base.WorldGrid, 0L);
			AtmosphereHelper.Mix(base.InternalAtmosphere, outputAtmos, AtmosphereHelper.MatterState.All);
		}
	}

	private void ProgressProcessing()
	{
		if (OnOff && Powered)
		{
			_currentProgress += GameManager.GameTickSpeedSeconds * Centrifuge.RpmToProgressMultiplier(_internalCombustion.Rpm);
			Processing = (byte)(Mathf.Clamp01(_currentProgress / Centrifuge.ProgressRequired(ImportingThing as ICentrifugable)) * 100f);
		}
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
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			writer.WriteByte(Processing);
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
		if (Thing.IsNetworkUpdateRequired(512u, networkUpdateType))
		{
			Processing = reader.ReadByte();
		}
	}

	public override void SerializeOnJoin(RocketBinaryWriter writer)
	{
		base.SerializeOnJoin(writer);
		writer.WriteSingle(_internalCombustion.Throttle);
		writer.WriteSingle(_internalCombustion.CombustionLimiter);
		writer.WriteSingle(_internalCombustion.Rpm);
		writer.WriteSingle(_internalCombustion.Stress);
		writer.WriteByte(Processing);
	}

	public override void DeserializeOnJoin(RocketBinaryReader reader)
	{
		base.DeserializeOnJoin(reader);
		_internalCombustion.Throttle = reader.ReadSingle();
		_internalCombustion.CombustionLimiter = reader.ReadSingle();
		_internalCombustion.Rpm = reader.ReadSingle();
		_internalCombustion.Stress = reader.ReadSingle();
		Processing = reader.ReadByte();
	}

	public override ThingSaveData SerializeSave()
	{
		ThingSaveData savedData;
		ThingSaveData result = (savedData = new CombustionCentrifugeSaveData());
		InitialiseSaveData(ref savedData);
		return result;
	}

	protected override void InitialiseSaveData(ref ThingSaveData savedData)
	{
		base.InitialiseSaveData(ref savedData);
		if (savedData is CombustionCentrifugeSaveData combustionCentrifugeSaveData && _internalCombustion != null)
		{
			combustionCentrifugeSaveData.Throttle = _internalCombustion.Throttle;
			combustionCentrifugeSaveData.CombustionLimiter = _internalCombustion.CombustionLimiter;
			combustionCentrifugeSaveData.Rpm = _internalCombustion.Rpm;
			combustionCentrifugeSaveData.Stress = _internalCombustion.Stress;
		}
	}

	public override void DeserializeSave(ThingSaveData savedData)
	{
		base.DeserializeSave(savedData);
		if (savedData is CombustionCentrifugeSaveData combustionCentrifugeSaveData)
		{
			_internalCombustion.Throttle = combustionCentrifugeSaveData.Throttle;
			_internalCombustion.CombustionLimiter = combustionCentrifugeSaveData.CombustionLimiter;
			_internalCombustion.Rpm = combustionCentrifugeSaveData.Rpm;
			_internalCombustion.Stress = combustionCentrifugeSaveData.Stress;
		}
	}
}
