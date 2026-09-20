using Assets.Scripts.Inventory;
using Assets.Scripts.Localization2;
using Assets.Scripts.Networking;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Objects.Electrical;

public class LogicDial : LogicInputBase
{
	private static string[] _modeStrings;

	public static int MaxMode = 1000;

	[SerializeField]
	private Knob knob;

	public static readonly int DialTurnHash = Animator.StringToHash("DialTurn");

	public override string[] ModeStrings => _modeStrings;

	public override void Awake()
	{
		base.Awake();
		knob.Initialize(this);
	}

	public override void OnPrefabLoad()
	{
		base.OnPrefabLoad();
		if (_modeStrings == null)
		{
			GenerateStrings();
		}
	}

	protected override int ReadInteractableState(RocketBinaryReader reader, Interactable interactable)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			return reader.ReadInt16();
		}
		return base.ReadInteractableState(reader, interactable);
	}

	protected override void SetInteractableStateOnJoin(Interactable interactable, int state)
	{
		interactable.Interact(state, skipAnimation: false);
	}

	protected override void WriteInteractableState(RocketBinaryWriter writer, Interactable interactable)
	{
		if (interactable.Action == InteractableType.Mode)
		{
			writer.WriteInt16((short)interactable.State);
		}
		else
		{
			base.WriteInteractableState(writer, interactable);
		}
	}

	private void GenerateStrings()
	{
		_modeStrings = new string[MaxMode];
		for (int i = 0; i < MaxMode; i++)
		{
			_modeStrings[i] = i.ToString();
		}
	}

	public override void OnInteractableUpdated(Interactable interactable)
	{
		base.OnInteractableUpdated(interactable);
		UpdateKnob();
	}

	public override void OnSettingChanged()
	{
		base.OnSettingChanged();
		UpdateKnob();
	}

	public override void OnFinishedLoad()
	{
		base.OnFinishedLoad();
		UpdateKnob();
	}

	private void UpdateKnob()
	{
		if (!GameManager.IsMainThread)
		{
			UpdateKnobFromThread().Forget();
		}
		else
		{
			knob.SetKnob((int)Setting, Mode).Forget();
		}
	}

	private async UniTaskVoid UpdateKnobFromThread()
	{
		await UniTask.SwitchToMainThread();
		UpdateKnob();
	}

	public override bool CanLogicRead(LogicType logicType)
	{
		if (logicType == LogicType.Setting || logicType == LogicType.Ratio)
		{
			return true;
		}
		return base.CanLogicRead(logicType);
	}

	public override double GetLogicValue(LogicType logicType)
	{
		return logicType switch
		{
			LogicType.Setting => Setting, 
			LogicType.Ratio => Setting / (double)Mode, 
			_ => base.GetLogicValue(logicType), 
		};
	}

	public override bool CanLogicWrite(LogicType logicType)
	{
		if (logicType == LogicType.Setting)
		{
			return true;
		}
		return base.CanLogicWrite(logicType);
	}

	public override void SetLogicValue(LogicType logicType, double value)
	{
		base.SetLogicValue(logicType, value);
		if (base.IsStructureCompleted && logicType == LogicType.Setting)
		{
			Setting = (int)Mathf.Clamp((float)value, 0f, Mode);
		}
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
			Labeller labeller = interaction.SourceSlot.Occupant as Labeller;
			if ((bool)labeller)
			{
				delayedActionInstance.ActionMessage = ActionStrings.Set;
				delayedActionInstance.AppendStateMessage(GameStrings.DeviceManualInputWindow);
				if (!labeller.OnOff)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNotOn);
				}
				if (!labeller.IsOperable)
				{
					return delayedActionInstance.Fail(GameStrings.DeviceNoPower);
				}
				if (!doAction)
				{
					return delayedActionInstance.Succeed();
				}
				labeller.Set(this, LogicType.Mode);
				return delayedActionInstance.Succeed();
			}
			delayedActionInstance.AppendStateMessage(GameStrings.GlobalMaxMode, StringManager.Get(Mode));
			delayedActionInstance.AppendStateMessage(GameStrings.HoldForSmallIncrements, Localization.QuantityModifierKey);
			if (!(interaction.SourceSlot.Occupant is Screwdriver))
			{
				return delayedActionInstance.Fail(GameStrings.RequiresScrewdriver);
			}
			if (!doAction)
			{
				return delayedActionInstance.Succeed();
			}
			ScrewSound();
			if (GameManager.RunSimulation)
			{
				int num = Mode;
				switch (interactable.Action)
				{
				case InteractableType.Button2:
					num = Mathf.Clamp(Mode - (interaction.AltKey ? 1 : 10), 0, MaxMode);
					break;
				case InteractableType.Button1:
					num = Mathf.Clamp(Mode + (interaction.AltKey ? 1 : 10), 0, MaxMode);
					break;
				}
				if (RocketMath.Approximately(num, Mode))
				{
					return delayedActionInstance.Succeed();
				}
				OnServer.Interact(base.InteractMode, num);
			}
			return delayedActionInstance.Succeed();
		}
		if (interactable.Action == InteractableType.Button3 || interactable.Action == InteractableType.Button4)
		{
			DelayedActionInstance delayedActionInstance2 = new DelayedActionInstance
			{
				Duration = 0f,
				ActionMessage = interactable.ContextualName
			};
			if (!IsAuthorized(interaction.SourceThing))
			{
				return delayedActionInstance2.Fail(GameStrings.AccessCardUnableToInteract);
			}
			if (Mode == 0)
			{
				return delayedActionInstance2.Succeed();
			}
			Labeller labeller2 = interaction.SourceSlot.Occupant as Labeller;
			if ((bool)labeller2)
			{
				delayedActionInstance2.ActionMessage = ActionStrings.Set;
				delayedActionInstance2.AppendStateMessage(GameStrings.DeviceManualInputWindow);
				if (!labeller2.OnOff)
				{
					return delayedActionInstance2.Fail(GameStrings.DeviceNotOn);
				}
				if (!labeller2.IsOperable)
				{
					return delayedActionInstance2.Fail(GameStrings.DeviceNoPower);
				}
				if (!doAction)
				{
					return delayedActionInstance2.Succeed();
				}
				labeller2.Set(this);
				return delayedActionInstance2.Succeed();
			}
			switch (interactable.Action)
			{
			case InteractableType.Button3:
				delayedActionInstance2.ActionMessage = GameStrings.GlobalDecrease.AsString();
				delayedActionInstance2.AppendStateMessage(GameStrings.GlobalValue, StringManager.Get(Setting));
				if (!doAction)
				{
					return delayedActionInstance2.Succeed();
				}
				knob.PlayKnobSound((int)Setting, increase: false, Mode);
				if (!GameManager.RunSimulation)
				{
					return delayedActionInstance2.Succeed();
				}
				Setting = Mathf.Max((int)Setting - ((!interaction.AltKey) ? 1 : 10), 0);
				UpdateKnob();
				return DelayedActionInstance.Success(interactable.ContextualName);
			case InteractableType.Button4:
				delayedActionInstance2.ActionMessage = GameStrings.GlobalIncrease.AsString();
				delayedActionInstance2.AppendStateMessage(GameStrings.GlobalValue, StringManager.Get(Setting));
				if (!doAction)
				{
					return delayedActionInstance2.Succeed();
				}
				knob.PlayKnobSound((int)Setting, increase: true, Mode);
				if (!GameManager.RunSimulation)
				{
					return delayedActionInstance2.Succeed();
				}
				Setting = Mathf.Min((int)Setting + ((!interaction.AltKey) ? 1 : 10), Mode);
				knob.SetKnob((int)Setting, Mode).Forget();
				return DelayedActionInstance.Success(interactable.ContextualName);
			default:
				return delayedActionInstance2.Succeed();
			}
		}
		return base.InteractWith(interactable, interaction, doAction);
	}
}
